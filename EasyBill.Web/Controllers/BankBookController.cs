using EasyBill.DataAccess.Repository.IRepository;
using AOne.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.Models.Entity;
using AOne.Models.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace EasyBill.UI.Controllers
{
    public class BankBookController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISalesRepository _salesRepository;
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IModeOfPaymentRepository _modeOfPaymentRepository;

        public BankBookController(
            IUnitOfWork unitOfWork,
            ISalesRepository salesRepository,
            IPurchaseRepository purchaseRepository,
            IModeOfPaymentRepository modeOfPaymentRepository)
        {
            _unitOfWork = unitOfWork;
            _salesRepository = salesRepository;
            _purchaseRepository = purchaseRepository;
            _modeOfPaymentRepository = modeOfPaymentRepository;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string[] selectedBanks)
        {
            if (selectedBanks != null && selectedBanks.Length > 0)
            {
                selectedBanks = selectedBanks
                    .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .Select(x => x.Trim())
                    .ToArray();
            }

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now.Date;
            }

            // Fetch list of all distinct bank names from database for filter dropdown
            var banks = await _unitOfWork.GetRepository<Bank>()
                .Query()
                .Where(b => !string.IsNullOrWhiteSpace(b.BankName))
                .Select(b => b.BankName.Trim())
                .Distinct()
                .ToListAsync();

            var returnUrl = $"/BankBook/Index?fromDate={fromDate.Value:yyyy-MM-dd}&toDate={toDate.Value:yyyy-MM-dd}";
            if (selectedBanks != null && selectedBanks.Length > 0)
            {
                foreach (var bank in selectedBanks)
                {
                    returnUrl += $"&selectedBanks={Uri.EscapeDataString(bank)}";
                }
            }
            var escapedReturnUrl = Uri.EscapeDataString(returnUrl);

            // Get Bank Payment Modes
            var paymentModes = await _modeOfPaymentRepository.GetAll();
            var bankPaymentModeIds = paymentModes
                .Where(x => x.PaymentType == AOne.Utility.Enums.modeofpayment.Bank)
                .Select(x => x.Id)
                .ToHashSet();
            var paymentModeBankMap = paymentModes
                .Where(x => x.Bank != null && !string.IsNullOrWhiteSpace(x.Bank.BankName))
                .ToDictionary(
                    x => x.Id,
                    x => x.Bank.BankName.Trim()
                );

            // 1. Sales Bank Transactions
            var sales = await _salesRepository.GetAll();
            var salesTransactions = sales
                .Where(s => s.SalsePaymentDetails != null)
                .SelectMany(s => s.SalsePaymentDetails
                    .Where(pd => bankPaymentModeIds.Contains(pd.PaymentModeId) && pd.Amount > 0)
                    .Select(pd => new BankBookDetailVM
                    {
                        Date = pd.Date ?? s.BillDate ?? DateTime.Now,
                        VoucherNo = s.BillNo ?? string.Empty,
                        Particular = s.Customers?.Name != null ? $"{s.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                        Type = "Sales",
                        Bank = paymentModeBankMap.ContainsKey(pd.PaymentModeId)? paymentModeBankMap[pd.PaymentModeId]: "-",
                        Debit = pd.Amount,
                        Credit = 0,
                        ActionUrl = $"/Sales/Edit?Id={s.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"
                    }))
                .ToList();

            // 2. Purchase Bank Transactions
            var purchases = await _purchaseRepository.GetAll();
            var purchaseTransactions = purchases
                .Where(p => p.PaymentDetails != null)
                .SelectMany(p => p.PaymentDetails
                    .Where(pd => bankPaymentModeIds.Contains(pd.PaymentModeId) && pd.Amount > 0)
                    .Select(pd => new BankBookDetailVM
                    {
                        Date = pd.Date ?? p.BillDate ?? DateTime.Now,
                        VoucherNo = p.BillNo ?? string.Empty,
                        Particular = p.Suppliers?.FirstName != null ? $"{p.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                        Type = "Purchase",
                        Bank = paymentModeBankMap.ContainsKey(pd.PaymentModeId)? paymentModeBankMap[pd.PaymentModeId]: "-",
                        Debit = 0,
                        Credit = pd.Amount,
                        ActionUrl = $"/Purchase/Edit?Id={p.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"
                    }))
                .ToList();

            // 3. PaymentVoucher Bank Transactions
            var paymentVouchers = await _unitOfWork.GetRepository<PaymentVoucher>()
                .Query()
                .Include(x => x.Suppliers)
                .Include(x => x.Customer)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .ToListAsync();

            var paymentVoucherTransactions = paymentVouchers
                .Where(pv => pv.PaymentModeId.HasValue && bankPaymentModeIds.Contains(pv.PaymentModeId.Value) && (pv.NetAmount > 0 || pv.Amount > 0))
                .Select(pv => {
                    string particular = "Payment";
                    if (pv.Suppliers != null) particular = $"{pv.Suppliers.FirstName} (Supplier)";
                    else if (pv.Customer != null) particular = $"{pv.Customer.Name} (Customer)";
                    else if (pv.Employee != null) particular = $"{pv.Employee.Name} (Employee)";
                    else if (pv.Party.HasValue) {
                        particular = pv.Party.Value switch {
                            AOne.Utility.Enums.Party.Customer => "Customer (Customer)",
                            AOne.Utility.Enums.Party.Supplier => "Supplier (Supplier)",
                            AOne.Utility.Enums.Party.Staff => "Staff (Employee)",
                            _ => "Other (Other)"
                        };
                    }
                    else if (!string.IsNullOrWhiteSpace(pv.Description)) particular = pv.Description;

                    return new BankBookDetailVM
                    {
                        Date = pv.Date ?? DateTime.Now,
                        VoucherNo = pv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Voucher",
                        Bank = pv.PaymentModeId.HasValue && paymentModeBankMap.ContainsKey(pv.PaymentModeId.Value) ? paymentModeBankMap[pv.PaymentModeId.Value] : "-",
                        Debit = 0,
                        Credit = pv.NetAmount > 0 ? pv.NetAmount : pv.Amount,
                        ActionUrl = $"/PaymentVoucher/Edit?Id={pv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pv.ModeOfPayment != null ? pv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pv.RefNo) ? "" : " - " + pv.RefNo)}{(string.IsNullOrWhiteSpace(pv.ChequeNo) ? "" : " - Cheque: " + pv.ChequeNo)}"
                    };
                })
                .ToList();

            // 4. ReceiveVoucher Bank Transactions
            var receiveVouchers = await _unitOfWork.GetRepository<ReceiveVoucher>()
                .Query()
                .Include(x => x.Customers)
                .Include(x => x.Supplier)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .ToListAsync();

            var receiveVoucherTransactions = receiveVouchers
                .Where(rv => rv.PaymentModeId.HasValue && bankPaymentModeIds.Contains(rv.PaymentModeId.Value) && rv.NetAmount > 0)
                .Select(rv => {
                    string particular = "Receipt";
                    if (rv.Customers != null) particular = $"{rv.Customers.Name} (Customer)";
                    else if (rv.Supplier != null) particular = $"{rv.Supplier.FirstName} (Supplier)";
                    else if (rv.Employee != null) particular = $"{rv.Employee.Name} (Employee)";
                    else if (rv.Party.HasValue) {
                        particular = rv.Party.Value switch {
                            AOne.Utility.Enums.Party.Customer => "Customer (Customer)",
                            AOne.Utility.Enums.Party.Supplier => "Supplier (Supplier)",
                            AOne.Utility.Enums.Party.Staff => "Staff (Employee)",
                            _ => "Other (Other)"
                        };
                    }
                    else if (!string.IsNullOrWhiteSpace(rv.Description)) particular = rv.Description;

                    return new BankBookDetailVM
                    {
                        Date = rv.Date,
                        VoucherNo = rv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Receive",
                        Bank = rv.PaymentModeId.HasValue && paymentModeBankMap.ContainsKey(rv.PaymentModeId.Value) ? paymentModeBankMap[rv.PaymentModeId.Value] : "-",
                        Debit = rv.NetAmount,
                        Credit = 0,
                        ActionUrl = $"/ReceiveVoucher/Edit?Id={rv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(rv.ModeOfPayment != null ? rv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(rv.RefNo) ? "" : " - " + rv.RefNo)}{(string.IsNullOrWhiteSpace(rv.ChequeNo) ? "" : " - Cheque: " + rv.ChequeNo)}"
                    };
                })
                .ToList();

            // 5. Contra Bank Transactions
            var contras = await _unitOfWork.GetRepository<Contra>()
                .Query()
                .Include(c => c.Bank)
                .ToListAsync();

            var contraBankTransactions = contras
                .Where(c => c.Amount > 0 && c.BankId.HasValue)
                .Select(c => {
                    decimal debit = 0;
                    decimal credit = 0;
                    string particular = "";

                    if (c.Category == AOne.Utility.Enums.ContraCategory.Deposit)
                    {
                        debit = c.Amount;
                        particular = "Cash Deposit";
                    }
                    else if (c.Category == AOne.Utility.Enums.ContraCategory.Withdraw)
                    {
                        credit = c.Amount;
                        particular = "Cash Withdrawal";
                    }

                    return new BankBookDetailVM
                    {
                        Date = c.Date ?? DateTime.Now,
                        VoucherNo = c.VouncherNo,
                        Particular = particular,
                        Type = "Contra Voucher",
                        Bank = c.Bank != null ? c.Bank.BankName.Trim() : "-",
                        Debit = debit,
                        Credit = credit,
                        ActionUrl = $"/Contra/Edit?Id={c.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = c.CashAndBank.HasValue ? c.CashAndBank.Value.ToString() : ""
                    };
                })
                .ToList();

            // Combine all transactions
            var allTransactions = salesTransactions
                .Concat(purchaseTransactions)
                .Concat(paymentVoucherTransactions)
                .Concat(receiveVoucherTransactions)
                .Concat(contraBankTransactions)
                .ToList();

            // Filter transactions by selected banks if specified (and does not contain "All")
            if (selectedBanks != null && selectedBanks.Length > 0 && !selectedBanks.Contains("All"))
            {
                allTransactions = allTransactions
                    .Where(t => selectedBanks.Contains(t.Bank))
                    .ToList();
            }

            // Calculate Opening Balance (all transactions strictly before fromDate)
            decimal openingBalance = allTransactions
                .Where(t => t.Date.Date < fromDate.Value.Date)
                .Sum(t => t.Debit - t.Credit);

            // Filter transactions within date range
            var filteredTransactions = allTransactions
                .Where(t => t.Date.Date >= fromDate.Value.Date && t.Date.Date <= toDate.Value.Date)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.VoucherNo)
                .ToList();

            // Calculate Running Balance
            decimal runningBalance = openingBalance;
            foreach (var trans in filteredTransactions)
            {
                runningBalance += trans.Debit - trans.Credit;
                trans.Balance = runningBalance;
            }

            decimal closingBalance = runningBalance;

            var viewModel = new BankBookVM
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                Details = filteredTransactions
            };

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            ViewBag.Banks = banks;
            ViewBag.SelectedBanks = selectedBanks ?? new string[0];

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ExportBankBookExcel(DateTime? fromDate, DateTime? toDate, string[] selectedBanks)
        {
            if (selectedBanks != null && selectedBanks.Length > 0)
            {
                selectedBanks = selectedBanks
                    .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .Select(x => x.Trim())
                    .ToArray();
            }

            var tenantId = User.FindFirst("TenantId")?.Value;
            var tenant = await _unitOfWork.GetRepository<Tenant>().GetAll().FirstOrDefaultAsync(t => t.Id == tenantId);
            string companyName = tenant?.Name ?? "Company";

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now.Date;
            }

            var returnUrl = $"/BankBook/Index?fromDate={fromDate.Value:yyyy-MM-dd}&toDate={toDate.Value:yyyy-MM-dd}";
            if (selectedBanks != null && selectedBanks.Length > 0)
            {
                foreach (var bank in selectedBanks)
                {
                    returnUrl += $"&selectedBanks={Uri.EscapeDataString(bank)}";
                }
            }
            var escapedReturnUrl = Uri.EscapeDataString(returnUrl);

            var paymentModes = await _modeOfPaymentRepository.GetAll();
            var bankPaymentModeIds = paymentModes
                .Where(x => x.PaymentType == AOne.Utility.Enums.modeofpayment.Bank)
                .Select(x => x.Id)
                .ToHashSet();
            var paymentModeBankMap = paymentModes
                .Where(x => x.Bank != null && !string.IsNullOrWhiteSpace(x.Bank.BankName))
                .ToDictionary(
                    x => x.Id,
                    x => x.Bank.BankName.Trim()
                );

            var sales = await _salesRepository.GetAll();
            var salesTransactions = sales
                .Where(s => s.SalsePaymentDetails != null)
                .SelectMany(s => s.SalsePaymentDetails
                    .Where(pd => bankPaymentModeIds.Contains(pd.PaymentModeId) && pd.Amount > 0)
                    .Select(pd => new BankBookDetailVM
                    {
                        Date = pd.Date ?? s.BillDate ?? DateTime.Now,
                        VoucherNo = s.BillNo ?? string.Empty,
                        Particular = s.Customers?.Name != null ? $"{s.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                        Type = "Sales",
                        Bank = paymentModeBankMap.ContainsKey(pd.PaymentModeId)? paymentModeBankMap[pd.PaymentModeId]: "-",
                        Debit = pd.Amount,
                        Credit = 0,
                        ActionUrl = $"/Sales/Edit?Id={s.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"
                    }))
                .ToList();

            var purchases = await _purchaseRepository.GetAll();
            var purchaseTransactions = purchases
                .Where(p => p.PaymentDetails != null)
                .SelectMany(p => p.PaymentDetails
                    .Where(pd => bankPaymentModeIds.Contains(pd.PaymentModeId) && pd.Amount > 0)
                    .Select(pd => new BankBookDetailVM
                    {
                        Date = pd.Date ?? p.BillDate ?? DateTime.Now,
                        VoucherNo = p.BillNo ?? string.Empty,
                        Particular = p.Suppliers?.FirstName != null ? $"{p.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                        Type = "Purchase",
                        Bank = paymentModeBankMap.ContainsKey(pd.PaymentModeId)? paymentModeBankMap[pd.PaymentModeId]: "-",
                        Debit = 0,
                        Credit = pd.Amount,
                        ActionUrl = $"/Purchase/Edit?Id={p.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"
                    }))
                .ToList();

            var paymentVouchers = await _unitOfWork.GetRepository<PaymentVoucher>()
                .Query()
                .Include(x => x.Suppliers)
                .Include(x => x.Customer)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .ToListAsync();

            var paymentVoucherTransactions = paymentVouchers
                .Where(pv => pv.PaymentModeId.HasValue && bankPaymentModeIds.Contains(pv.PaymentModeId.Value) && (pv.NetAmount > 0 || pv.Amount > 0))
                .Select(pv => {
                    string particular = "Payment";
                    if (pv.Suppliers != null) particular = $"{pv.Suppliers.FirstName} (Supplier)";
                    else if (pv.Customer != null) particular = $"{pv.Customer.Name} (Customer)";
                    else if (pv.Employee != null) particular = $"{pv.Employee.Name} (Employee)";
                    else if (pv.Party.HasValue) {
                        particular = pv.Party.Value switch {
                            AOne.Utility.Enums.Party.Customer => "Customer (Customer)",
                            AOne.Utility.Enums.Party.Supplier => "Supplier (Supplier)",
                            AOne.Utility.Enums.Party.Staff => "Staff (Employee)",
                            _ => "Other (Other)"
                        };
                    }
                    else if (!string.IsNullOrWhiteSpace(pv.Description)) particular = pv.Description;

                    return new BankBookDetailVM
                    {
                        Date = pv.Date ?? DateTime.Now,
                        VoucherNo = pv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Voucher",
                        Bank = pv.PaymentModeId.HasValue && paymentModeBankMap.ContainsKey(pv.PaymentModeId.Value) ? paymentModeBankMap[pv.PaymentModeId.Value] : "-",
                        Debit = 0,
                        Credit = pv.NetAmount > 0 ? pv.NetAmount : pv.Amount,
                        ActionUrl = $"/PaymentVoucher/Edit?Id={pv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pv.ModeOfPayment != null ? pv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pv.RefNo) ? "" : " - " + pv.RefNo)}{(string.IsNullOrWhiteSpace(pv.ChequeNo) ? "" : " - Cheque: " + pv.ChequeNo)}"
                    };
                })
                .ToList();

            var receiveVouchers = await _unitOfWork.GetRepository<ReceiveVoucher>()
                .Query()
                .Include(x => x.Customers)
                .Include(x => x.Supplier)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .ToListAsync();

            var receiveVoucherTransactions = receiveVouchers
                .Where(rv => rv.PaymentModeId.HasValue && bankPaymentModeIds.Contains(rv.PaymentModeId.Value) && rv.NetAmount > 0)
                .Select(rv => {
                    string particular = "Receipt";
                    if (rv.Customers != null) particular = $"{rv.Customers.Name} (Customer)";
                    else if (rv.Supplier != null) particular = $"{rv.Supplier.FirstName} (Supplier)";
                    else if (rv.Employee != null) particular = $"{rv.Employee.Name} (Employee)";
                    else if (rv.Party.HasValue) {
                        particular = rv.Party.Value switch {
                            AOne.Utility.Enums.Party.Customer => "Customer (Customer)",
                            AOne.Utility.Enums.Party.Supplier => "Supplier (Supplier)",
                            AOne.Utility.Enums.Party.Staff => "Staff (Employee)",
                            _ => "Other (Other)"
                        };
                    }
                    else if (!string.IsNullOrWhiteSpace(rv.Description)) particular = rv.Description;

                    return new BankBookDetailVM
                    {
                        Date = rv.Date,
                        VoucherNo = rv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Receive",
                        Bank = rv.PaymentModeId.HasValue && paymentModeBankMap.ContainsKey(rv.PaymentModeId.Value) ? paymentModeBankMap[rv.PaymentModeId.Value] : "-",
                        Debit = rv.NetAmount,
                        Credit = 0,
                        ActionUrl = $"/ReceiveVoucher/Edit?Id={rv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(rv.ModeOfPayment != null ? rv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(rv.RefNo) ? "" : " - " + rv.RefNo)}{(string.IsNullOrWhiteSpace(rv.ChequeNo) ? "" : " - Cheque: " + rv.ChequeNo)}"
                    };
                })
                .ToList();

            var contras = await _unitOfWork.GetRepository<Contra>()
                .Query()
                .Include(c => c.Bank)
                .ToListAsync();

            var contraBankTransactions = contras
                .Where(c => c.Amount > 0 && c.BankId.HasValue)
                .Select(c => {
                    decimal debit = 0;
                    decimal credit = 0;
                    string particular = "";

                    if (c.Category == AOne.Utility.Enums.ContraCategory.Deposit)
                    {
                        debit = c.Amount;
                        particular = "Cash Deposit";
                    }
                    else if (c.Category == AOne.Utility.Enums.ContraCategory.Withdraw)
                    {
                        credit = c.Amount;
                        particular = "Cash Withdrawal";
                    }

                    return new BankBookDetailVM
                    {
                        Date = c.Date ?? DateTime.Now,
                        VoucherNo = c.VouncherNo,
                        Particular = particular,
                        Type = "Contra Voucher",
                        Bank = c.Bank != null ? c.Bank.BankName.Trim() : "-",
                        Debit = debit,
                        Credit = credit,
                        ActionUrl = $"/Contra/Edit?Id={c.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = c.CashAndBank.HasValue ? c.CashAndBank.Value.ToString() : ""
                    };
                })
                .ToList();

            var allTransactions = salesTransactions
                .Concat(purchaseTransactions)
                .Concat(paymentVoucherTransactions)
                .Concat(receiveVoucherTransactions)
                .Concat(contraBankTransactions)
                .ToList();

            if (selectedBanks != null && selectedBanks.Length > 0 && !selectedBanks.Contains("All"))
            {
                allTransactions = allTransactions
                    .Where(t => selectedBanks.Contains(t.Bank))
                    .ToList();
            }

            decimal openingBalance = allTransactions
                .Where(t => t.Date.Date < fromDate.Value.Date)
                .Sum(t => t.Debit - t.Credit);

            var filteredTransactions = allTransactions
                .Where(t => t.Date.Date >= fromDate.Value.Date && t.Date.Date <= toDate.Value.Date)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.VoucherNo)
                .ToList();

            decimal runningBalance = openingBalance;
            foreach (var trans in filteredTransactions)
            {
                runningBalance += trans.Debit - trans.Credit;
                trans.Balance = runningBalance;
            }

            decimal closingBalance = runningBalance;

            var viewModel = new BankBookVM
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                Details = filteredTransactions
            };

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Bank Book");

            // Company Name
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 9).Merge()
                .Style.Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Report Name (Exactly "Bank Book", no "Report" suffix)
            ws.Cell(2, 1).Value = "Bank Book";
            ws.Range(2, 1, 2, 9).Merge()
                .Style.Font.SetBold().Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Date Range
            ws.Cell(3, 1).Value = $"From: {viewModel.FromDate:dd-MM-yyyy}   To: {viewModel.ToDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 9).Merge()
                .Style.Font.SetItalic()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Opening & Closing Balance Info
            ws.Cell(4, 1).Value = $"Opening Balance: {viewModel.OpeningBalance:0.00}   |   Closing Balance: {viewModel.ClosingBalance:0.00}";
            ws.Range(4, 1, 4, 9).Merge()
                .Style.Font.SetBold()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 6;
            string[] headers = { "S.No", "Date", "Voucher No", "Particulars", "Type", "Bank", "Debit (Dr)", "Credit (Cr)", "Balance" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 9).Style.Font.SetBold();
            row++;

            int sno = 1;
            foreach (var item in viewModel.Details)
            {
                ws.Cell(row, 1).Value = sno;
                ws.Cell(row, 2).Value = item.Date.ToString("dd-MM-yyyy");
                ws.Cell(row, 3).Value = item.VoucherNo;
                ws.Cell(row, 4).Value = item.Particular;
                ws.Cell(row, 5).Value = item.Type + (string.IsNullOrWhiteSpace(item.PaymentModeRef) ? "" : " - " + item.PaymentModeRef);
                ws.Cell(row, 6).Value = item.Bank;
                if (item.Debit > 0)
                    ws.Cell(row, 7).Value = item.Debit;
                else
                    ws.Cell(row, 7).Value = "-";

                if (item.Credit > 0)
                    ws.Cell(row, 8).Value = item.Credit;
                else
                    ws.Cell(row, 8).Value = "-";
                ws.Cell(row, 9).Value = item.Balance;
                row++;
                sno++;
            }

            // TOTALS ROW
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 7).Value = viewModel.Details.Sum(x => x.Debit);
            ws.Cell(row, 8).Value = viewModel.Details.Sum(x => x.Credit);
            ws.Cell(row, 9).Value = viewModel.ClosingBalance;

            ws.Range(row, 1, row, 9).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BankBook.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportBankBookPdf(DateTime? fromDate, DateTime? toDate, string[] selectedBanks)
        {
            if (selectedBanks != null && selectedBanks.Length > 0)
            {
                selectedBanks = selectedBanks
                    .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .Select(x => x.Trim())
                    .ToArray();
            }

            var tenantId = User.FindFirst("TenantId")?.Value;
            var tenant = await _unitOfWork.GetRepository<Tenant>().GetAll().FirstOrDefaultAsync(t => t.Id == tenantId);
            string companyName = tenant?.Name ?? "Company";

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now.Date;
            }

            var returnUrl = $"/BankBook/Index?fromDate={fromDate.Value:yyyy-MM-dd}&toDate={toDate.Value:yyyy-MM-dd}";
            if (selectedBanks != null && selectedBanks.Length > 0)
            {
                foreach (var bank in selectedBanks)
                {
                    returnUrl += $"&selectedBanks={Uri.EscapeDataString(bank)}";
                }
            }
            var escapedReturnUrl = Uri.EscapeDataString(returnUrl);

            var paymentModes = await _modeOfPaymentRepository.GetAll();
            var bankPaymentModeIds = paymentModes
                .Where(x => x.PaymentType == AOne.Utility.Enums.modeofpayment.Bank)
                .Select(x => x.Id)
                .ToHashSet();
            var paymentModeBankMap = paymentModes
                .Where(x => x.Bank != null && !string.IsNullOrWhiteSpace(x.Bank.BankName))
                .ToDictionary(
                    x => x.Id,
                    x => x.Bank.BankName.Trim()
                );

            var sales = await _salesRepository.GetAll();
            var salesTransactions = sales
                .Where(s => s.SalsePaymentDetails != null)
                .SelectMany(s => s.SalsePaymentDetails
                    .Where(pd => bankPaymentModeIds.Contains(pd.PaymentModeId) && pd.Amount > 0)
                    .Select(pd => new BankBookDetailVM
                    {
                        Date = pd.Date ?? s.BillDate ?? DateTime.Now,
                        VoucherNo = s.BillNo ?? string.Empty,
                        Particular = s.Customers?.Name != null ? $"{s.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                        Type = "Sales",
                        Bank = paymentModeBankMap.ContainsKey(pd.PaymentModeId)? paymentModeBankMap[pd.PaymentModeId]: "-",
                        Debit = pd.Amount,
                        Credit = 0,
                        ActionUrl = $"/Sales/Edit?Id={s.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"
                    }))
                .ToList();

            var purchases = await _purchaseRepository.GetAll();
            var purchaseTransactions = purchases
                .Where(p => p.PaymentDetails != null)
                .SelectMany(p => p.PaymentDetails
                    .Where(pd => bankPaymentModeIds.Contains(pd.PaymentModeId) && pd.Amount > 0)
                    .Select(pd => new BankBookDetailVM
                    {
                        Date = pd.Date ?? p.BillDate ?? DateTime.Now,
                        VoucherNo = p.BillNo ?? string.Empty,
                        Particular = p.Suppliers?.FirstName != null ? $"{p.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                        Type = "Purchase",
                        Bank = paymentModeBankMap.ContainsKey(pd.PaymentModeId)? paymentModeBankMap[pd.PaymentModeId]: "-",
                        Debit = 0,
                        Credit = pd.Amount,
                        ActionUrl = $"/Purchase/Edit?Id={p.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"
                    }))
                .ToList();

            var paymentVouchers = await _unitOfWork.GetRepository<PaymentVoucher>()
                .Query()
                .Include(x => x.Suppliers)
                .Include(x => x.Customer)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .ToListAsync();

            var paymentVoucherTransactions = paymentVouchers
                .Where(pv => pv.PaymentModeId.HasValue && bankPaymentModeIds.Contains(pv.PaymentModeId.Value) && (pv.NetAmount > 0 || pv.Amount > 0))
                .Select(pv => {
                    string particular = "Payment";
                    if (pv.Suppliers != null) particular = $"{pv.Suppliers.FirstName} (Supplier)";
                    else if (pv.Customer != null) particular = $"{pv.Customer.Name} (Customer)";
                    else if (pv.Employee != null) particular = $"{pv.Employee.Name} (Employee)";
                    else if (pv.Party.HasValue) {
                        particular = pv.Party.Value switch {
                            AOne.Utility.Enums.Party.Customer => "Customer (Customer)",
                            AOne.Utility.Enums.Party.Supplier => "Supplier (Supplier)",
                            AOne.Utility.Enums.Party.Staff => "Staff (Employee)",
                            _ => "Other (Other)"
                        };
                    }
                    else if (!string.IsNullOrWhiteSpace(pv.Description)) particular = pv.Description;

                    return new BankBookDetailVM
                    {
                        Date = pv.Date ?? DateTime.Now,
                        VoucherNo = pv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Voucher",
                        Bank = pv.PaymentModeId.HasValue && paymentModeBankMap.ContainsKey(pv.PaymentModeId.Value) ? paymentModeBankMap[pv.PaymentModeId.Value] : "-",
                        Debit = 0,
                        Credit = pv.NetAmount > 0 ? pv.NetAmount : pv.Amount,
                        ActionUrl = $"/PaymentVoucher/Edit?Id={pv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pv.ModeOfPayment != null ? pv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pv.RefNo) ? "" : " - " + pv.RefNo)}{(string.IsNullOrWhiteSpace(pv.ChequeNo) ? "" : " - Cheque: " + pv.ChequeNo)}"
                    };
                })
                .ToList();

            var receiveVouchers = await _unitOfWork.GetRepository<ReceiveVoucher>()
                .Query()
                .Include(x => x.Customers)
                .Include(x => x.Supplier)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .ToListAsync();

            var receiveVoucherTransactions = receiveVouchers
                .Where(rv => rv.PaymentModeId.HasValue && bankPaymentModeIds.Contains(rv.PaymentModeId.Value) && rv.NetAmount > 0)
                .Select(rv => {
                    string particular = "Receipt";
                    if (rv.Customers != null) particular = $"{rv.Customers.Name} (Customer)";
                    else if (rv.Supplier != null) particular = $"{rv.Supplier.FirstName} (Supplier)";
                    else if (rv.Employee != null) particular = $"{rv.Employee.Name} (Employee)";
                    else if (rv.Party.HasValue) {
                        particular = rv.Party.Value switch {
                            AOne.Utility.Enums.Party.Customer => "Customer (Customer)",
                            AOne.Utility.Enums.Party.Supplier => "Supplier (Supplier)",
                            AOne.Utility.Enums.Party.Staff => "Staff (Employee)",
                            _ => "Other (Other)"
                        };
                    }
                    else if (!string.IsNullOrWhiteSpace(rv.Description)) particular = rv.Description;

                    return new BankBookDetailVM
                    {
                        Date = rv.Date,
                        VoucherNo = rv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Receive",
                        Bank = rv.PaymentModeId.HasValue && paymentModeBankMap.ContainsKey(rv.PaymentModeId.Value) ? paymentModeBankMap[rv.PaymentModeId.Value] : "-",
                        Debit = rv.NetAmount,
                        Credit = 0,
                        ActionUrl = $"/ReceiveVoucher/Edit?Id={rv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(rv.ModeOfPayment != null ? rv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(rv.RefNo) ? "" : " - " + rv.RefNo)}{(string.IsNullOrWhiteSpace(rv.ChequeNo) ? "" : " - Cheque: " + rv.ChequeNo)}"
                    };
                })
                .ToList();

            var contras = await _unitOfWork.GetRepository<Contra>()
                .Query()
                .Include(c => c.Bank)
                .ToListAsync();

            var contraBankTransactions = contras
                .Where(c => c.Amount > 0 && c.BankId.HasValue)
                .Select(c => {
                    decimal debit = 0;
                    decimal credit = 0;
                    string particular = "";

                    if (c.Category == AOne.Utility.Enums.ContraCategory.Deposit)
                    {
                        debit = c.Amount;
                        particular = "Cash Deposit";
                    }
                    else if (c.Category == AOne.Utility.Enums.ContraCategory.Withdraw)
                    {
                        credit = c.Amount;
                        particular = "Cash Withdrawal";
                    }

                    return new BankBookDetailVM
                    {
                        Date = c.Date ?? DateTime.Now,
                        VoucherNo = c.VouncherNo,
                        Particular = particular,
                        Type = "Contra Voucher",
                        Bank = c.Bank != null ? c.Bank.BankName.Trim() : "-",
                        Debit = debit,
                        Credit = credit,
                        ActionUrl = $"/Contra/Edit?Id={c.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = c.CashAndBank.HasValue ? c.CashAndBank.Value.ToString() : ""
                    };
                })
                .ToList();

            var allTransactions = salesTransactions
                .Concat(purchaseTransactions)
                .Concat(paymentVoucherTransactions)
                .Concat(receiveVoucherTransactions)
                .Concat(contraBankTransactions)
                .ToList();

            if (selectedBanks != null && selectedBanks.Length > 0 && !selectedBanks.Contains("All"))
            {
                allTransactions = allTransactions
                    .Where(t => selectedBanks.Contains(t.Bank))
                    .ToList();
            }

            decimal openingBalance = allTransactions
                .Where(t => t.Date.Date < fromDate.Value.Date)
                .Sum(t => t.Debit - t.Credit);

            var filteredTransactions = allTransactions
                .Where(t => t.Date.Date >= fromDate.Value.Date && t.Date.Date <= toDate.Value.Date)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.VoucherNo)
                .ToList();

            decimal runningBalance = openingBalance;
            foreach (var trans in filteredTransactions)
            {
                runningBalance += trans.Debit - trans.Credit;
                trans.Balance = runningBalance;
            }

            decimal closingBalance = runningBalance;

            var viewModel = new BankBookVM
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                Details = filteredTransactions
            };

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Company Name
            document.Add(new Paragraph(companyName)
                .SetFont(bold)
                .SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER));

            // Report Name (Exactly "Bank Book", no "Report" suffix)
            document.Add(new Paragraph("Bank Book")
                .SetFont(bold)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));

            // Date Range
            document.Add(new Paragraph($"From: {viewModel.FromDate:dd-MM-yyyy}   To: {viewModel.ToDate:dd-MM-yyyy}")
                .SetTextAlignment(TextAlignment.CENTER));

            // Opening & Closing Balance Info
            document.Add(new Paragraph($"Opening Balance: {viewModel.OpeningBalance:0.00}   |   Closing Balance: {viewModel.ClosingBalance:0.00}")
                .SetFont(bold)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            Table table = new Table(new float[] { 2, 4, 5, 8, 7, 5, 4, 4, 4 }).UseAllAvailableWidth();

            string[] headers = { "S.No", "Date", "Voucher No", "Particulars", "Type", "Bank", "Debit (Dr)", "Credit (Cr)", "Balance" };
            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold)).SetTextAlignment(TextAlignment.CENTER));
            }

            int sno = 1;
            foreach (var item in viewModel.Details)
            {
                table.AddCell(new Paragraph(sno.ToString()).SetFont(normal).SetTextAlignment(TextAlignment.CENTER));
                table.AddCell(new Paragraph(item.Date.ToString("dd-MM-yyyy")).SetFont(normal));
                table.AddCell(new Paragraph(item.VoucherNo ?? "").SetFont(normal));
                table.AddCell(new Paragraph(item.Particular ?? "").SetFont(normal));
                table.AddCell(new Paragraph(item.Type + (string.IsNullOrWhiteSpace(item.PaymentModeRef) ? "" : " - " + item.PaymentModeRef)).SetFont(normal));
                table.AddCell(new Paragraph(item.Bank ?? "").SetFont(normal));
                table.AddCell(new Paragraph(item.Debit > 0 ? $"{item.Debit:0.00}" : "-").SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(item.Credit > 0 ? $"{item.Credit:0.00}" : "-").SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph($"{item.Balance:0.00}").SetTextAlignment(TextAlignment.RIGHT));
                sno++;
            }

            // TOTALS
            table.AddCell(new Cell(1, 6).Add(new Paragraph("TOTAL").SetFont(bold)));
            table.AddCell(new Paragraph($"{viewModel.Details.Sum(x => x.Debit):0.00}").SetFont(bold).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph($"{viewModel.Details.Sum(x => x.Credit):0.00}").SetFont(bold).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph($"{viewModel.ClosingBalance:0.00}").SetFont(bold).SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "BankBook.pdf");
        }
    }
}
