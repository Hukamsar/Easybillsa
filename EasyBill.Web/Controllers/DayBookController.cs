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
using AOne.Models;

namespace EasyBill.UI.Controllers
{
    public class DayBookController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISalesRepository _salesRepository;
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IModeOfPaymentRepository _modeOfPaymentRepository;

        public DayBookController(
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

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = now.Date;
                toDate = now.Date;
            }

            var returnUrl = $"/DayBook/Index?fromDate={fromDate.Value:yyyy-MM-dd}&toDate={toDate.Value:yyyy-MM-dd}";
            var escapedReturnUrl = Uri.EscapeDataString(returnUrl);

            var tenantId = User.FindFirst("TenantId")?.Value;



            // 1. Sales Transactions (all sales, using TotalPayable)
            var sales = await _unitOfWork.GetRepository<Sales>()
                .GetAll()
                .Include(s => s.Customers)
                .Include(s => s.SalsePaymentDetails)
                    .ThenInclude(pd => pd.ModeOfPayment)
                .Where(s => string.IsNullOrEmpty(tenantId) || s.TenantId == tenantId)
                .ToListAsync();

            var salesTransactions = sales
                .Select(s => {
                    var billPayments = s.SalsePaymentDetails != null
                        ? s.SalsePaymentDetails
                            .Where(pd => pd.Description == null || 
                                (pd.Description.IndexOf("via Payment Voucher", StringComparison.OrdinalIgnoreCase) < 0 && 
                                 pd.Description.IndexOf("via Receive Voucher", StringComparison.OrdinalIgnoreCase) < 0))
                            .ToList()
                        : new List<SalsePaymentDetails>();

                    return new DayBookDetailVM
                    {
                        Date = s.BillDate ?? DateTime.Now,
                        VoucherNo = s.BillNo ?? string.Empty,
                        Particular = s.Customers?.Name != null ? $"{s.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                        Type = "Sales",
                        Debit = billPayments.Sum(pd => pd.Amount),
                        Credit = s.TotalPayable,
                        ActionUrl = $"/Sales/Edit?Id={s.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = string.Join(", ", billPayments
                            .Select(pd => $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"))
                    };
                })
                .ToList();

            // 2. Purchase Transactions (all purchases: cash & credit)
            var purchases = await _unitOfWork.GetRepository<Purchase>()
                .GetAll()
                .Include(p => p.Suppliers)
                .Include(p => p.PaymentDetails)
                    .ThenInclude(pd => pd.ModeOfPayment)
                .Where(p => string.IsNullOrEmpty(tenantId) || p.TenantId == tenantId)
                .ToListAsync();

            var purchaseTransactions = purchases
                .Select(p => {
                    var billPayments = p.PaymentDetails != null
                        ? p.PaymentDetails
                            .Where(pd => pd.Description == null || 
                                (pd.Description.IndexOf("via Payment Voucher", StringComparison.OrdinalIgnoreCase) < 0 && 
                                 pd.Description.IndexOf("via Receive Voucher", StringComparison.OrdinalIgnoreCase) < 0))
                            .ToList()
                        : new List<SalsePaymentDetails>();

                    return new DayBookDetailVM
                    {
                        Date = p.BillDate ?? DateTime.Now,
                        VoucherNo = p.BillNo ?? string.Empty,
                        Particular = p.Suppliers?.FirstName != null ? $"{p.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                        Type = "Purchase",
                        Debit = billPayments.Sum(pd => pd.Amount),
                        Credit = p.TotalPayable,
                        ActionUrl = $"/Purchase/Edit?Id={p.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = string.Join(", ", billPayments
                            .Select(pd => $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"))
                    };
                })
                .ToList();

            // 2a. Sales Return Transactions (Stock Returns)
            var stockReturns = await _unitOfWork.GetRepository<StockReturn>()
                .GetAll()
                .Include(x => x.Customers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var salesReturnTransactions = stockReturns
                .Select(sr => new DayBookDetailVM
                {
                    Date = sr.ChallanDate ?? DateTime.Now,
                    VoucherNo = sr.ChallanNo ?? string.Empty,
                    Particular = sr.Customers?.Name != null ? $"{sr.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                    Type = "Sales Return",
                    Debit = 0,
                    Credit = sr.TotalPayable,
                    ActionUrl = $"/StockReturn/Edit?Id={sr.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = sr.PaymentType ?? ""
                })
                .ToList();

            // 2b. Purchase Return Transactions
            var purchaseReturns = await _unitOfWork.GetRepository<PurchaseReturn>()
                .GetAll()
                .Include(x => x.Suppliers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var purchaseReturnTransactions = purchaseReturns
                .Select(pr => new DayBookDetailVM
                {
                    Date = pr.BillDate ?? DateTime.Now,
                    VoucherNo = pr.BillNo ?? string.Empty,
                    Particular = pr.Suppliers?.FirstName != null ? $"{pr.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                    Type = "Purchase Return",
                    Debit = pr.TotalPayable,
                    Credit = 0,
                    ActionUrl = $"/PurchaseReturn/Edit?Id={pr.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = pr.Reason.HasValue ? pr.Reason.Value.ToString() : ""
                })
                .ToList();

            // 2c. Stock Issue Transactions (Stock Issues)
            var stockIssues = await _unitOfWork.GetRepository<StockIssue>()
                .GetAll()
                .Include(x => x.Customers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var stockIssueTransactions = stockIssues
                .Select(si => new DayBookDetailVM
                {
                    Date = si.ChallanDate ?? DateTime.Now,
                    VoucherNo = si.ChallanNo ?? string.Empty,
                    Particular = si.Customers?.Name != null ? $"{si.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                    Type = "Stock Issue",
                    Debit = si.TotalPayable,
                    Credit = 0,
                    ActionUrl = $"/StockIssue/Edit?Id={si.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = si.PaymentType ?? ""
                })
                .ToList();

            // 2d. Stock Receive Transactions (Stock Receives)
            var stockReceives = await _unitOfWork.GetRepository<StockReceive>()
                .GetAll()
                .Include(x => x.Customers)
                .Include(x => x.Supplier)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var stockReceiveTransactions = stockReceives
                .Select(sr => new DayBookDetailVM
                {
                    Date = sr.ChallanDate ?? DateTime.Now,
                    VoucherNo = sr.ChallanNo ?? string.Empty,
                    Particular = sr.Supplier?.FirstName != null ? $"{sr.Supplier.FirstName} (Supplier)" : (sr.Customers?.Name != null ? $"{sr.Customers.Name} (Customer)" : "Supplier/Customer"),
                    Type = "Stock Receive",
                    Debit = 0,
                    Credit = sr.TotalPayable,
                    ActionUrl = $"/StockReceive/Edit?Id={sr.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = sr.PaymentType ?? ""
                })
                .ToList();

            // 2e. Purchase Challan Transactions
            var purchaseChallans = await _unitOfWork.GetRepository<PurchaseChallan>()
                .GetAll()
                .Include(x => x.Suppliers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var purchaseChallanTransactions = purchaseChallans
                .Select(pc => new DayBookDetailVM
                {
                    Date = pc.BillDate ?? DateTime.Now,
                    VoucherNo = pc.BillNo ?? string.Empty,
                    Particular = pc.Suppliers?.FirstName != null ? $"{pc.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                    Type = "Purchase Challan",
                    Debit = 0,
                    Credit = pc.TotalPayable,
                    ActionUrl = $"/PurchaseChallan/Edit?Id={pc.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = pc.PaymentType ?? ""
                })
                .ToList();

            // 3. PaymentVoucher Transactions (all payment vouchers)
            var paymentVouchers = await _unitOfWork.GetRepository<PaymentVoucher>()
                .GetAll()
                .Include(x => x.Suppliers)
                .Include(x => x.Customer)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .Include(x => x.paymentVouchercategory)
                .Where(pv => string.IsNullOrEmpty(tenantId) || pv.TenantId == tenantId)
                .ToListAsync();

            var paymentVoucherTransactions = paymentVouchers
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

                    bool isCollection = pv.paymentVouchercategory != null && string.Equals(pv.paymentVouchercategory.Name, "Collection", StringComparison.OrdinalIgnoreCase);
                    decimal amount = pv.NetAmount > 0 ? pv.NetAmount : pv.Amount;

                    return new DayBookDetailVM
                    {
                        Date = pv.Date ?? DateTime.Now,
                        VoucherNo = pv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Voucher",
                        Debit = isCollection ? 0 : amount,
                        Credit = isCollection ? amount : 0,
                        ActionUrl = $"/PaymentVoucher/Edit?Id={pv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pv.ModeOfPayment != null ? pv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pv.RefNo) ? "" : " - " + pv.RefNo)}{(string.IsNullOrWhiteSpace(pv.ChequeNo) ? "" : " - Cheque: " + pv.ChequeNo)}"
                    };
                })
                .ToList();

            // 4. ReceiveVoucher Transactions (all receive vouchers)
            var receiveVouchers = await _unitOfWork.GetRepository<ReceiveVoucher>()
                .GetAll()
                .Include(x => x.Customers)
                .Include(x => x.Supplier)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .Where(rv => string.IsNullOrEmpty(tenantId) || rv.TenantId == tenantId)
                .ToListAsync();

            var receiveVoucherTransactions = receiveVouchers
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

                    return new DayBookDetailVM
                    {
                        Date = rv.Date,
                        VoucherNo = rv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Receive",
                        Debit = rv.NetAmount > 0 ? rv.NetAmount : rv.Amount,
                        Credit = 0,
                        ActionUrl = $"/ReceiveVoucher/Edit?Id={rv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(rv.ModeOfPayment != null ? rv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(rv.RefNo) ? "" : " - " + rv.RefNo)}{(string.IsNullOrWhiteSpace(rv.ChequeNo) ? "" : " - Cheque: " + rv.ChequeNo)}"
                    };
                })
                .ToList();

            // 5. Contra Transactions (all contras)
            var contras = await _unitOfWork.GetRepository<Contra>()
                .GetAll()
                .Include(c => c.Bank)
                .Where(c => string.IsNullOrEmpty(tenantId) || c.TenantId == tenantId)
                .ToListAsync();

            var contraTransactions = contras
                .Where(c => c.Amount > 0)
                .Select(c => {
                    decimal debit = 0;
                    decimal credit = 0;
                    string particular = "";
                    string bankName = c.Bank != null ? c.Bank.BankName : "Bank";

                    if (c.CashAndBank == AOne.Utility.Enums.CashAndBank.Cash)
                    {
                        if (c.Category == AOne.Utility.Enums.ContraCategory.Deposit)
                        {
                            credit = c.Amount;
                            particular = $"Deposit to {bankName}";
                        }
                        else if (c.Category == AOne.Utility.Enums.ContraCategory.Withdraw)
                        {
                            debit = c.Amount;
                            particular = $"Withdraw from {bankName}";
                        }
                    }
                    else if (c.CashAndBank == AOne.Utility.Enums.CashAndBank.Bank)
                    {
                        if (c.Category == AOne.Utility.Enums.ContraCategory.Deposit)
                        {
                            debit = c.Amount;
                            particular = $"Cash Deposit ({bankName})";
                        }
                        else if (c.Category == AOne.Utility.Enums.ContraCategory.Withdraw)
                        {
                            credit = c.Amount;
                            particular = $"Cash Withdrawal ({bankName})";
                        }
                    }

                    return new DayBookDetailVM
                    {
                        Date = c.Date ?? DateTime.Now,
                        VoucherNo = c.VouncherNo,
                        Particular = particular,
                        Type = "Contra Voucher",
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
                .Concat(salesReturnTransactions)
                .Concat(purchaseReturnTransactions)
                .Concat(stockIssueTransactions)
                .Concat(stockReceiveTransactions)
                .Concat(purchaseChallanTransactions)
                .Concat(paymentVoucherTransactions)
                .Concat(receiveVoucherTransactions)
                .Concat(contraTransactions)
                .ToList();

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

            var viewModel = new DayBookVM
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                Details = filteredTransactions
            };

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ExportDayBookExcel(DateTime? fromDate, DateTime? toDate)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            var tenant = await _unitOfWork.GetRepository<Tenant>().GetAll().FirstOrDefaultAsync(t => t.Id == tenantId);
            string companyName = tenant?.Name ?? "Company";

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = now.Date;
                toDate = now.Date;
            }

            var returnUrl = $"/DayBook/Index?fromDate={fromDate.Value:yyyy-MM-dd}&toDate={toDate.Value:yyyy-MM-dd}";
            var escapedReturnUrl = Uri.EscapeDataString(returnUrl);



            var sales = await _unitOfWork.GetRepository<Sales>()
                .GetAll()
                .Include(s => s.Customers)
                .Include(s => s.SalsePaymentDetails)
                    .ThenInclude(pd => pd.ModeOfPayment)
                .Where(s => string.IsNullOrEmpty(tenantId) || s.TenantId == tenantId)
                .ToListAsync();

            var salesTransactions = sales
                .Select(s => {
                    var billPayments = s.SalsePaymentDetails != null
                        ? s.SalsePaymentDetails
                            .Where(pd => pd.Description == null || 
                                (pd.Description.IndexOf("via Payment Voucher", StringComparison.OrdinalIgnoreCase) < 0 && 
                                 pd.Description.IndexOf("via Receive Voucher", StringComparison.OrdinalIgnoreCase) < 0))
                            .ToList()
                        : new List<SalsePaymentDetails>();

                    return new DayBookDetailVM
                    {
                        Date = s.BillDate ?? DateTime.Now,
                        VoucherNo = s.BillNo ?? string.Empty,
                        Particular = s.Customers?.Name != null ? $"{s.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                        Type = "Sales",
                        Debit = billPayments.Sum(pd => pd.Amount),
                        Credit = s.TotalPayable,
                        ActionUrl = $"/Sales/Edit?Id={s.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = string.Join(", ", billPayments
                            .Select(pd => $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"))
                    };
                })
                .ToList();

            var purchases = await _unitOfWork.GetRepository<Purchase>()
                .GetAll()
                .Include(p => p.Suppliers)
                .Include(p => p.PaymentDetails)
                    .ThenInclude(pd => pd.ModeOfPayment)
                .Where(p => string.IsNullOrEmpty(tenantId) || p.TenantId == tenantId)
                .ToListAsync();

            var purchaseTransactions = purchases
                .Select(p => {
                    var billPayments = p.PaymentDetails != null
                        ? p.PaymentDetails
                            .Where(pd => pd.Description == null || 
                                (pd.Description.IndexOf("via Payment Voucher", StringComparison.OrdinalIgnoreCase) < 0 && 
                                 pd.Description.IndexOf("via Receive Voucher", StringComparison.OrdinalIgnoreCase) < 0))
                            .ToList()
                        : new List<SalsePaymentDetails>();

                    return new DayBookDetailVM
                    {
                        Date = p.BillDate ?? DateTime.Now,
                        VoucherNo = p.BillNo ?? string.Empty,
                        Particular = p.Suppliers?.FirstName != null ? $"{p.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                        Type = "Purchase",
                        Debit = billPayments.Sum(pd => pd.Amount),
                        Credit = p.TotalPayable,
                        ActionUrl = $"/Purchase/Edit?Id={p.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = string.Join(", ", billPayments
                            .Select(pd => $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"))
                    };
                })
                .ToList();

            var stockReturns = await _unitOfWork.GetRepository<StockReturn>()
                .GetAll()
                .Include(x => x.Customers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var salesReturnTransactions = stockReturns
                .Select(sr => new DayBookDetailVM
                {
                    Date = sr.ChallanDate ?? DateTime.Now,
                    VoucherNo = sr.ChallanNo ?? string.Empty,
                    Particular = sr.Customers?.Name != null ? $"{sr.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                    Type = "Sales Return",
                    Debit = 0,
                    Credit = sr.TotalPayable,
                    ActionUrl = $"/StockReturn/Edit?Id={sr.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = sr.PaymentType ?? ""
                })
                .ToList();

            var purchaseReturns = await _unitOfWork.GetRepository<PurchaseReturn>()
                .GetAll()
                .Include(x => x.Suppliers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var purchaseReturnTransactions = purchaseReturns
                .Select(pr => new DayBookDetailVM
                {
                    Date = pr.BillDate ?? DateTime.Now,
                    VoucherNo = pr.BillNo ?? string.Empty,
                    Particular = pr.Suppliers?.FirstName != null ? $"{pr.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                    Type = "Purchase Return",
                    Debit = pr.TotalPayable,
                    Credit = 0,
                    ActionUrl = $"/PurchaseReturn/Edit?Id={pr.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = pr.Reason.HasValue ? pr.Reason.Value.ToString() : ""
                })
                .ToList();

            var stockIssues = await _unitOfWork.GetRepository<StockIssue>()
                .GetAll()
                .Include(x => x.Customers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var stockIssueTransactions = stockIssues
                .Select(si => new DayBookDetailVM
                {
                    Date = si.ChallanDate ?? DateTime.Now,
                    VoucherNo = si.ChallanNo ?? string.Empty,
                    Particular = si.Customers?.Name != null ? $"{si.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                    Type = "Stock Issue",
                    Debit = si.TotalPayable,
                    Credit = 0,
                    ActionUrl = $"/StockIssue/Edit?Id={si.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = si.PaymentType ?? ""
                })
                .ToList();

            var stockReceives = await _unitOfWork.GetRepository<StockReceive>()
                .GetAll()
                .Include(x => x.Customers)
                .Include(x => x.Supplier)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var stockReceiveTransactions = stockReceives
                .Select(sr => new DayBookDetailVM
                {
                    Date = sr.ChallanDate ?? DateTime.Now,
                    VoucherNo = sr.ChallanNo ?? string.Empty,
                    Particular = sr.Supplier?.FirstName != null ? $"{sr.Supplier.FirstName} (Supplier)" : (sr.Customers?.Name != null ? $"{sr.Customers.Name} (Customer)" : "Supplier/Customer"),
                    Type = "Stock Receive",
                    Debit = 0,
                    Credit = sr.TotalPayable,
                    ActionUrl = $"/StockReceive/Edit?Id={sr.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = sr.PaymentType ?? ""
                })
                .ToList();

            var purchaseChallans = await _unitOfWork.GetRepository<PurchaseChallan>()
                .GetAll()
                .Include(x => x.Suppliers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var purchaseChallanTransactions = purchaseChallans
                .Select(pc => new DayBookDetailVM
                {
                    Date = pc.BillDate ?? DateTime.Now,
                    VoucherNo = pc.BillNo ?? string.Empty,
                    Particular = pc.Suppliers?.FirstName != null ? $"{pc.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                    Type = "Purchase Challan",
                    Debit = 0,
                    Credit = pc.TotalPayable,
                    ActionUrl = $"/PurchaseChallan/Edit?Id={pc.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = pc.PaymentType ?? ""
                })
                .ToList();

            var paymentVouchers = await _unitOfWork.GetRepository<PaymentVoucher>()
                .GetAll()
                .Include(x => x.Suppliers)
                .Include(x => x.Customer)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .Include(x => x.paymentVouchercategory)
                .Where(pv => string.IsNullOrEmpty(tenantId) || pv.TenantId == tenantId)
                .ToListAsync();

            var paymentVoucherTransactions = paymentVouchers
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

                    bool isCollection = pv.paymentVouchercategory != null && string.Equals(pv.paymentVouchercategory.Name, "Collection", StringComparison.OrdinalIgnoreCase);
                    decimal amount = pv.NetAmount > 0 ? pv.NetAmount : pv.Amount;

                    return new DayBookDetailVM
                    {
                        Date = pv.Date ?? DateTime.Now,
                        VoucherNo = pv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Voucher",
                        Debit = isCollection ? 0 : amount,
                        Credit = isCollection ? amount : 0,
                        ActionUrl = $"/PaymentVoucher/Edit?Id={pv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pv.ModeOfPayment != null ? pv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pv.RefNo) ? "" : " - " + pv.RefNo)}{(string.IsNullOrWhiteSpace(pv.ChequeNo) ? "" : " - Cheque: " + pv.ChequeNo)}"
                    };
                })
                .ToList();

            var receiveVouchers = await _unitOfWork.GetRepository<ReceiveVoucher>()
                .GetAll()
                .Include(x => x.Customers)
                .Include(x => x.Supplier)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .Where(rv => string.IsNullOrEmpty(tenantId) || rv.TenantId == tenantId)
                .ToListAsync();

            var receiveVoucherTransactions = receiveVouchers
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

                    return new DayBookDetailVM
                    {
                        Date = rv.Date,
                        VoucherNo = rv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Receive",
                        Debit = rv.NetAmount > 0 ? rv.NetAmount : rv.Amount,
                        Credit = 0,
                        ActionUrl = $"/ReceiveVoucher/Edit?Id={rv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(rv.ModeOfPayment != null ? rv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(rv.RefNo) ? "" : " - " + rv.RefNo)}{(string.IsNullOrWhiteSpace(rv.ChequeNo) ? "" : " - Cheque: " + rv.ChequeNo)}"
                    };
                })
                .ToList();

            var contras = await _unitOfWork.GetRepository<Contra>()
                .GetAll()
                .Include(c => c.Bank)
                .Where(c => string.IsNullOrEmpty(tenantId) || c.TenantId == tenantId)
                .ToListAsync();

            var contraTransactions = contras
                .Where(c => c.Amount > 0)
                .Select(c => {
                    decimal debit = 0;
                    decimal credit = 0;
                    string particular = "";
                    string bankName = c.Bank != null ? c.Bank.BankName : "Bank";

                    if (c.CashAndBank == AOne.Utility.Enums.CashAndBank.Cash)
                    {
                        if (c.Category == AOne.Utility.Enums.ContraCategory.Deposit)
                        {
                            credit = c.Amount;
                            particular = $"Deposit to {bankName}";
                        }
                        else if (c.Category == AOne.Utility.Enums.ContraCategory.Withdraw)
                        {
                            debit = c.Amount;
                            particular = $"Withdraw from {bankName}";
                        }
                    }
                    else if (c.CashAndBank == AOne.Utility.Enums.CashAndBank.Bank)
                    {
                        if (c.Category == AOne.Utility.Enums.ContraCategory.Deposit)
                        {
                            debit = c.Amount;
                            particular = $"Cash Deposit ({bankName})";
                        }
                        else if (c.Category == AOne.Utility.Enums.ContraCategory.Withdraw)
                        {
                            credit = c.Amount;
                            particular = $"Cash Withdrawal ({bankName})";
                        }
                    }

                    return new DayBookDetailVM
                    {
                        Date = c.Date ?? DateTime.Now,
                        VoucherNo = c.VouncherNo,
                        Particular = particular,
                        Type = "Contra Voucher",
                        Debit = debit,
                        Credit = credit,
                        ActionUrl = $"/Contra/Edit?Id={c.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = c.CashAndBank.HasValue ? c.CashAndBank.Value.ToString() : ""
                    };
                })
                .ToList();

            var allTransactions = salesTransactions
                .Concat(purchaseTransactions)
                .Concat(salesReturnTransactions)
                .Concat(purchaseReturnTransactions)
                .Concat(stockIssueTransactions)
                .Concat(stockReceiveTransactions)
                .Concat(purchaseChallanTransactions)
                .Concat(paymentVoucherTransactions)
                .Concat(receiveVoucherTransactions)
                .Concat(contraTransactions)
                .ToList();

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

            var viewModel = new DayBookVM
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                Details = filteredTransactions
            };

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Day Book");

            // Company Name
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 8).Merge()
                .Style.Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Report Name (Exactly "Day Book", no "Report" suffix)
            ws.Cell(2, 1).Value = "Day Book";
            ws.Range(2, 1, 2, 8).Merge()
                .Style.Font.SetBold().Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Date Range
            ws.Cell(3, 1).Value = $"From: {viewModel.FromDate:dd-MM-yyyy}   To: {viewModel.ToDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 8).Merge()
                .Style.Font.SetItalic()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Opening & Closing Balance Info
            ws.Cell(4, 1).Value = $"Opening Balance: {viewModel.OpeningBalance:0.00}   |   Closing Balance: {viewModel.ClosingBalance:0.00}";
            ws.Range(4, 1, 4, 8).Merge()
                .Style.Font.SetBold()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 6;
            string[] headers = { "S.No", "Date", "Voucher No", "Particulars", "Type", "Debit (Dr)", "Credit (Cr)", "Balance" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 8).Style.Font.SetBold();
            row++;

            int sno = 1;
            foreach (var item in viewModel.Details)
            {
                ws.Cell(row, 1).Value = sno;
                ws.Cell(row, 2).Value = item.Date.ToString("dd-MM-yyyy");
                ws.Cell(row, 3).Value = item.VoucherNo;
                ws.Cell(row, 4).Value = item.Particular;
                ws.Cell(row, 5).Value = item.Type + (string.IsNullOrWhiteSpace(item.PaymentModeRef) ? "" : " - " + item.PaymentModeRef);
                if (item.Debit > 0)
                    ws.Cell(row, 6).Value = item.Debit;
                else
                    ws.Cell(row, 6).Value = "-";

                if (item.Credit > 0)
                    ws.Cell(row, 7).Value = item.Credit;
                else
                    ws.Cell(row, 7).Value = "-";
                ws.Cell(row, 8).Value = item.Balance;
                row++;
                sno++;
            }

            // TOTALS ROW
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 6).Value = viewModel.Details.Sum(x => x.Debit);
            ws.Cell(row, 7).Value = viewModel.Details.Sum(x => x.Credit);
            ws.Cell(row, 8).Value = viewModel.ClosingBalance;

            ws.Range(row, 1, row, 8).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DayBook.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportDayBookPdf(DateTime? fromDate, DateTime? toDate)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            var tenant = await _unitOfWork.GetRepository<Tenant>().GetAll().FirstOrDefaultAsync(t => t.Id == tenantId);
            string companyName = tenant?.Name ?? "Company";

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = now.Date;
                toDate = now.Date;
            }

            var returnUrl = $"/DayBook/Index?fromDate={fromDate.Value:yyyy-MM-dd}&toDate={toDate.Value:yyyy-MM-dd}";
            var escapedReturnUrl = Uri.EscapeDataString(returnUrl);



            var sales = await _unitOfWork.GetRepository<Sales>()
                .GetAll()
                .Include(s => s.Customers)
                .Include(s => s.SalsePaymentDetails)
                    .ThenInclude(pd => pd.ModeOfPayment)
                .Where(s => string.IsNullOrEmpty(tenantId) || s.TenantId == tenantId)
                .ToListAsync();

            var salesTransactions = sales
                .Select(s => {
                    var billPayments = s.SalsePaymentDetails != null
                        ? s.SalsePaymentDetails
                            .Where(pd => pd.Description == null || 
                                (pd.Description.IndexOf("via Payment Voucher", StringComparison.OrdinalIgnoreCase) < 0 && 
                                 pd.Description.IndexOf("via Receive Voucher", StringComparison.OrdinalIgnoreCase) < 0))
                            .ToList()
                        : new List<SalsePaymentDetails>();

                    return new DayBookDetailVM
                    {
                        Date = s.BillDate ?? DateTime.Now,
                        VoucherNo = s.BillNo ?? string.Empty,
                        Particular = s.Customers?.Name != null ? $"{s.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                        Type = "Sales",
                        Debit = billPayments.Sum(pd => pd.Amount),
                        Credit = s.TotalPayable,
                        ActionUrl = $"/Sales/Edit?Id={s.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = string.Join(", ", billPayments
                            .Select(pd => $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"))
                    };
                })
                .ToList();

            var purchases = await _unitOfWork.GetRepository<Purchase>()
                .GetAll()
                .Include(p => p.Suppliers)
                .Include(p => p.PaymentDetails)
                    .ThenInclude(pd => pd.ModeOfPayment)
                .Where(p => string.IsNullOrEmpty(tenantId) || p.TenantId == tenantId)
                .ToListAsync();

            var purchaseTransactions = purchases
                .Select(p => {
                    var billPayments = p.PaymentDetails != null
                        ? p.PaymentDetails
                            .Where(pd => pd.Description == null || 
                                (pd.Description.IndexOf("via Payment Voucher", StringComparison.OrdinalIgnoreCase) < 0 && 
                                 pd.Description.IndexOf("via Receive Voucher", StringComparison.OrdinalIgnoreCase) < 0))
                            .ToList()
                        : new List<SalsePaymentDetails>();

                    return new DayBookDetailVM
                    {
                        Date = p.BillDate ?? DateTime.Now,
                        VoucherNo = p.BillNo ?? string.Empty,
                        Particular = p.Suppliers?.FirstName != null ? $"{p.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                        Type = "Purchase",
                        Debit = billPayments.Sum(pd => pd.Amount),
                        Credit = p.TotalPayable,
                        ActionUrl = $"/Purchase/Edit?Id={p.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = string.Join(", ", billPayments
                            .Select(pd => $"{(pd.ModeOfPayment != null ? pd.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pd.ReferenceNo) ? "" : " - " + pd.ReferenceNo)}"))
                    };
                })
                .ToList();

            var stockReturns = await _unitOfWork.GetRepository<StockReturn>()
                .GetAll()
                .Include(x => x.Customers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var salesReturnTransactions = stockReturns
                .Select(sr => new DayBookDetailVM
                {
                    Date = sr.ChallanDate ?? DateTime.Now,
                    VoucherNo = sr.ChallanNo ?? string.Empty,
                    Particular = sr.Customers?.Name != null ? $"{sr.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                    Type = "Sales Return",
                    Debit = 0,
                    Credit = sr.TotalPayable,
                    ActionUrl = $"/StockReturn/Edit?Id={sr.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = sr.PaymentType ?? ""
                })
                .ToList();

            var purchaseReturns = await _unitOfWork.GetRepository<PurchaseReturn>()
                .GetAll()
                .Include(x => x.Suppliers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var purchaseReturnTransactions = purchaseReturns
                .Select(pr => new DayBookDetailVM
                {
                    Date = pr.BillDate ?? DateTime.Now,
                    VoucherNo = pr.BillNo ?? string.Empty,
                    Particular = pr.Suppliers?.FirstName != null ? $"{pr.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                    Type = "Purchase Return",
                    Debit = pr.TotalPayable,
                    Credit = 0,
                    ActionUrl = $"/PurchaseReturn/Edit?Id={pr.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = pr.Reason.HasValue ? pr.Reason.Value.ToString() : ""
                })
                .ToList();

            var stockIssues = await _unitOfWork.GetRepository<StockIssue>()
                .GetAll()
                .Include(x => x.Customers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var stockIssueTransactions = stockIssues
                .Select(si => new DayBookDetailVM
                {
                    Date = si.ChallanDate ?? DateTime.Now,
                    VoucherNo = si.ChallanNo ?? string.Empty,
                    Particular = si.Customers?.Name != null ? $"{si.Customers.Name} (Customer)" : "Walk-in Customer (Customer)",
                    Type = "Stock Issue",
                    Debit = si.TotalPayable,
                    Credit = 0,
                    ActionUrl = $"/StockIssue/Edit?Id={si.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = si.PaymentType ?? ""
                })
                .ToList();

            var stockReceives = await _unitOfWork.GetRepository<StockReceive>()
                .GetAll()
                .Include(x => x.Customers)
                .Include(x => x.Supplier)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var stockReceiveTransactions = stockReceives
                .Select(sr => new DayBookDetailVM
                {
                    Date = sr.ChallanDate ?? DateTime.Now,
                    VoucherNo = sr.ChallanNo ?? string.Empty,
                    Particular = sr.Supplier?.FirstName != null ? $"{sr.Supplier.FirstName} (Supplier)" : (sr.Customers?.Name != null ? $"{sr.Customers.Name} (Customer)" : "Supplier/Customer"),
                    Type = "Stock Receive",
                    Debit = 0,
                    Credit = sr.TotalPayable,
                    ActionUrl = $"/StockReceive/Edit?Id={sr.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = sr.PaymentType ?? ""
                })
                .ToList();

            var purchaseChallans = await _unitOfWork.GetRepository<PurchaseChallan>()
                .GetAll()
                .Include(x => x.Suppliers)
                .Where(x => string.IsNullOrEmpty(tenantId) || x.TenantId == tenantId)
                .ToListAsync();

            var purchaseChallanTransactions = purchaseChallans
                .Select(pc => new DayBookDetailVM
                {
                    Date = pc.BillDate ?? DateTime.Now,
                    VoucherNo = pc.BillNo ?? string.Empty,
                    Particular = pc.Suppliers?.FirstName != null ? $"{pc.Suppliers.FirstName} (Supplier)" : "Supplier (Supplier)",
                    Type = "Purchase Challan",
                    Debit = 0,
                    Credit = pc.TotalPayable,
                    ActionUrl = $"/PurchaseChallan/Edit?Id={pc.Id}&returnUrl={escapedReturnUrl}",
                    PaymentModeRef = pc.PaymentType ?? ""
                })
                .ToList();

            var paymentVouchers = await _unitOfWork.GetRepository<PaymentVoucher>()
                .GetAll()
                .Include(x => x.Suppliers)
                .Include(x => x.Customer)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .Include(x => x.paymentVouchercategory)
                .Where(pv => string.IsNullOrEmpty(tenantId) || pv.TenantId == tenantId)
                .ToListAsync();

            var paymentVoucherTransactions = paymentVouchers
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

                    bool isCollection = pv.paymentVouchercategory != null && string.Equals(pv.paymentVouchercategory.Name, "Collection", StringComparison.OrdinalIgnoreCase);
                    decimal amount = pv.NetAmount > 0 ? pv.NetAmount : pv.Amount;

                    return new DayBookDetailVM
                    {
                        Date = pv.Date ?? DateTime.Now,
                        VoucherNo = pv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Voucher",
                        Debit = isCollection ? 0 : amount,
                        Credit = isCollection ? amount : 0,
                        ActionUrl = $"/PaymentVoucher/Edit?Id={pv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(pv.ModeOfPayment != null ? pv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(pv.RefNo) ? "" : " - " + pv.RefNo)}{(string.IsNullOrWhiteSpace(pv.ChequeNo) ? "" : " - Cheque: " + pv.ChequeNo)}"
                    };
                })
                .ToList();

            var receiveVouchers = await _unitOfWork.GetRepository<ReceiveVoucher>()
                .GetAll()
                .Include(x => x.Customers)
                .Include(x => x.Supplier)
                .Include(x => x.Employee)
                .Include(x => x.ModeOfPayment)
                .Where(rv => string.IsNullOrEmpty(tenantId) || rv.TenantId == tenantId)
                .ToListAsync();

            var receiveVoucherTransactions = receiveVouchers
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

                    return new DayBookDetailVM
                    {
                        Date = rv.Date,
                        VoucherNo = rv.VouncherNo,
                        Particular = particular,
                        Type = "Payment Receive",
                        Debit = rv.NetAmount > 0 ? rv.NetAmount : rv.Amount,
                        Credit = 0,
                        ActionUrl = $"/ReceiveVoucher/Edit?Id={rv.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = $"{(rv.ModeOfPayment != null ? rv.ModeOfPayment.Name : "")}{(string.IsNullOrWhiteSpace(rv.RefNo) ? "" : " - " + rv.RefNo)}{(string.IsNullOrWhiteSpace(rv.ChequeNo) ? "" : " - Cheque: " + rv.ChequeNo)}"
                    };
                })
                .ToList();

            var contras = await _unitOfWork.GetRepository<Contra>()
                .GetAll()
                .Include(c => c.Bank)
                .Where(c => string.IsNullOrEmpty(tenantId) || c.TenantId == tenantId)
                .ToListAsync();

            var contraTransactions = contras
                .Where(c => c.Amount > 0)
                .Select(c => {
                    decimal debit = 0;
                    decimal credit = 0;
                    string particular = "";
                    string bankName = c.Bank != null ? c.Bank.BankName : "Bank";

                    if (c.CashAndBank == AOne.Utility.Enums.CashAndBank.Cash)
                    {
                        if (c.Category == AOne.Utility.Enums.ContraCategory.Deposit)
                        {
                            credit = c.Amount;
                            particular = $"Deposit to {bankName}";
                        }
                        else if (c.Category == AOne.Utility.Enums.ContraCategory.Withdraw)
                        {
                            debit = c.Amount;
                            particular = $"Withdraw from {bankName}";
                        }
                    }
                    else if (c.CashAndBank == AOne.Utility.Enums.CashAndBank.Bank)
                    {
                        if (c.Category == AOne.Utility.Enums.ContraCategory.Deposit)
                        {
                            debit = c.Amount;
                            particular = $"Cash Deposit ({bankName})";
                        }
                        else if (c.Category == AOne.Utility.Enums.ContraCategory.Withdraw)
                        {
                            credit = c.Amount;
                            particular = $"Cash Withdrawal ({bankName})";
                        }
                    }

                    return new DayBookDetailVM
                    {
                        Date = c.Date ?? DateTime.Now,
                        VoucherNo = c.VouncherNo,
                        Particular = particular,
                        Type = "Contra Voucher",
                        Debit = debit,
                        Credit = credit,
                        ActionUrl = $"/Contra/Edit?Id={c.Id}&returnUrl={escapedReturnUrl}",
                        PaymentModeRef = c.CashAndBank.HasValue ? c.CashAndBank.Value.ToString() : ""
                    };
                })
                .ToList();

            var allTransactions = salesTransactions
                .Concat(purchaseTransactions)
                .Concat(salesReturnTransactions)
                .Concat(purchaseReturnTransactions)
                .Concat(stockIssueTransactions)
                .Concat(stockReceiveTransactions)
                .Concat(purchaseChallanTransactions)
                .Concat(paymentVoucherTransactions)
                .Concat(receiveVoucherTransactions)
                .Concat(contraTransactions)
                .ToList();

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

            var viewModel = new DayBookVM
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

            // Report Name (Exactly "Day Book", no "Report" suffix)
            document.Add(new Paragraph("Day Book")
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

            Table table = new Table(new float[] { 2, 4, 5, 8, 7, 4, 4, 4 }).UseAllAvailableWidth();

            string[] headers = { "S.No", "Date", "Voucher No", "Particulars", "Type", "Debit (Dr)", "Credit (Cr)", "Balance" };
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
                table.AddCell(new Paragraph(item.Debit > 0 ? $"{item.Debit:0.00}" : "-").SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(item.Credit > 0 ? $"{item.Credit:0.00}" : "-").SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph($"{item.Balance:0.00}").SetTextAlignment(TextAlignment.RIGHT));
                sno++;
            }

            // TOTALS
            table.AddCell(new Cell(1, 5).Add(new Paragraph("TOTAL").SetFont(bold)));
            table.AddCell(new Paragraph($"{viewModel.Details.Sum(x => x.Debit):0.00}").SetFont(bold).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph($"{viewModel.Details.Sum(x => x.Credit):0.00}").SetFont(bold).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph($"{viewModel.ClosingBalance:0.00}").SetFont(bold).SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "DayBook.pdf");
        }
    }
}
