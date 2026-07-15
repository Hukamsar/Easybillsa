using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AOne.Utility.Enums;

namespace EasyBill.UI.Controllers
{
    public class BankReconciliationController : Controller
    {
        private readonly IpaymentVoucherRepository _paymentVoucherRepo;
        private readonly IReceiveVoucherRepository _receiveVoucherRepo;
        private readonly IModeOfPaymentRepository _paymentModeRepo;
        private readonly ISalesRepository _salesRepo;
        private readonly IPurchaseRepository _purchaseRepo;
        private readonly ICustomerAdvanceRepository _customerAdvanceRepo;
        private readonly ISupplierAdvanceRepository _supplierAdvanceRepo;
        private readonly ISalsePaymentDetailsRepository _salsePaymentDetailsRepo;
        private readonly IBankRepository _bankRepo;

        public BankReconciliationController(
            IpaymentVoucherRepository paymentVoucherRepo,
            IReceiveVoucherRepository receiveVoucherRepo,
            IModeOfPaymentRepository paymentModeRepo,
            ISalesRepository salesRepo,
            IPurchaseRepository purchaseRepo,
            ICustomerAdvanceRepository customerAdvanceRepo,
            ISupplierAdvanceRepository supplierAdvanceRepo,
            ISalsePaymentDetailsRepository salsePaymentDetailsRepo,
            IBankRepository bankRepo)
        {
            _paymentVoucherRepo = paymentVoucherRepo;
            _receiveVoucherRepo = receiveVoucherRepo;
            _paymentModeRepo = paymentModeRepo;
            _salesRepo = salesRepo;
            _purchaseRepo = purchaseRepo;
            _customerAdvanceRepo = customerAdvanceRepo;
            _supplierAdvanceRepo = supplierAdvanceRepo;
            _salsePaymentDetailsRepo = salsePaymentDetailsRepo;
            _bankRepo = bankRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? bankId, DateTime? fromDate, DateTime? toDate, string? searchQuery)
        {
            var paymentVouchers = await _paymentVoucherRepo.GetAll();
            var receiveVouchers = await _receiveVoucherRepo.GetAll();

            var usedModeIds = new HashSet<int>();
            if (paymentVouchers != null)
            {
                foreach (var pv in paymentVouchers)
                {
                    if (pv.PaymentModeId.HasValue)
                    {
                        usedModeIds.Add(pv.PaymentModeId.Value);
                    }
                }
            }
            if (receiveVouchers != null)
            {
                foreach (var rv in receiveVouchers)
                {
                    if (rv.PaymentModeId.HasValue)
                    {
                        usedModeIds.Add(rv.PaymentModeId.Value);
                    }
                }
            }

            var modes = await _paymentModeRepo.GetAll();
            var usedModes = (modes ?? Enumerable.Empty<ModeOfPayment>())
                .Where(m => usedModeIds.Contains(m.Id))
                .ToList();

            var usedBankIds = usedModes
                .Where(m => m.BankId.HasValue)
                .Select(m => m.BankId.Value)
                .Distinct()
                .ToList();

            var allBanks = await _bankRepo.GetAll();
            var filteredBanks = (allBanks ?? Enumerable.Empty<Bank>())
                .Where(b => usedBankIds.Contains(b.Id))
                .ToList();
            
            var vm = new BankReconciliationVM
            {
                BankId = bankId,
                FromDate = fromDate ?? DateTime.Today.AddMonths(-1),
                ToDate = toDate ?? DateTime.Today,
                SearchQuery = searchQuery,
                Banks = filteredBanks.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.BankName,
                    Selected = b.Id == bankId
                }).ToList()
            };

            if (bankId.HasValue)
            {
                var targetModeIds = (modes ?? Enumerable.Empty<ModeOfPayment>())
                    .Where(m => m.BankId == bankId)
                    .Select(m => m.Id)
                    .ToList();

                var items = new List<BankReconciliationItemVM>();

                // Process Payment Vouchers (Debits/Outflow)
                if (paymentVouchers != null)
                {
                    foreach (var pv in paymentVouchers.Where(x => x.PaymentModeId.HasValue && targetModeIds.Contains(x.PaymentModeId.Value)))
                    {
                        var pvDate = pv.Date ?? DateTime.MinValue;
                        if (pvDate.Date >= vm.FromDate.Value.Date && pvDate.Date <= vm.ToDate.Value.Date)
                        {
                            var partyName = pv.Suppliers != null ? pv.Suppliers.FirstName : (pv.Customer != null ? pv.Customer.Name : (pv.Employee != null ? pv.Employee.Name : "N/A"));
                            
                            if (string.IsNullOrEmpty(searchQuery) || 
                                pv.VouncherNo.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                partyName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                            {
                                items.Add(new BankReconciliationItemVM
                                {
                                    Id = pv.Id,
                                    VoucherType = "Payment",
                                    VoucherNo = pv.VouncherNo,
                                    Date = pvDate,
                                    PartyName = partyName,
                                    PaymentModeName = pv.ModeOfPayment?.Name ?? "N/A",
                                    ChequeNo = null,
                                    ChequeDate = pv.ChequeDate,
                                    RefNo = pv.RefNo,
                                    Amount = pv.Amount,
                                    ClearedDate = pv.ClearedDate
                                });
                            }
                        }
                    }
                }

                // Process Receive Vouchers (Credits/Inflow)
                if (receiveVouchers != null)
                {
                    foreach (var rv in receiveVouchers.Where(x => x.PaymentModeId.HasValue && targetModeIds.Contains(x.PaymentModeId.Value)))
                    {
                        if (rv.Date.Date >= vm.FromDate.Value.Date && rv.Date.Date <= vm.ToDate.Value.Date)
                        {
                            var partyName = rv.Customers != null ? rv.Customers.Name : (rv.Supplier != null ? rv.Supplier.FirstName : (rv.Employee != null ? rv.Employee.Name : "N/A"));
                            
                            if (string.IsNullOrEmpty(searchQuery) || 
                                rv.VouncherNo.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                partyName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                            {
                                items.Add(new BankReconciliationItemVM
                                {
                                    Id = rv.Id,
                                    VoucherType = "Receive",
                                    VoucherNo = rv.VouncherNo,
                                    Date = rv.Date,
                                    PartyName = partyName,
                                    PaymentModeName = rv.ModeOfPayment?.Name ?? "N/A",
                                    ChequeNo = rv.ChequeNo,
                                    ChequeDate = rv.ChequeDate,
                                    RefNo = rv.RefNo,
                                    Amount = rv.Amount,
                                    ClearedDate = rv.ClearedDate
                                });
                            }
                        }
                    }
                }

                vm.Transactions = items.OrderBy(x => x.Date).ToList();
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ReconcileTransaction(int id, string type, DateTime? clearedDate)
        {
            try
            {
                if (type == "Payment")
                {
                    var pv = await _paymentVoucherRepo.GetById(id);
                    if (pv != null)
                    {
                        var oldClearedDate = pv.ClearedDate;
                        if (oldClearedDate == null && clearedDate != null)
                        {
                            await AdjustPaymentVoucher(pv);
                        }
                        else if (oldClearedDate != null && clearedDate == null)
                        {
                            await RevertPaymentVoucher(pv);
                        }

                        pv.ClearedDate = clearedDate;
                        await _paymentVoucherRepo.Update(pv);
                        return Json(new { success = true, message = "Transaction reconciled successfully!" });
                    }
                }
                else if (type == "Receive")
                {
                    var rv = await _receiveVoucherRepo.GetById(id);
                    if (rv != null)
                    {
                        var oldClearedDate = rv.ClearedDate;
                        if (oldClearedDate == null && clearedDate != null)
                        {
                            await AdjustReceiveVoucher(rv);
                        }
                        else if (oldClearedDate != null && clearedDate == null)
                        {
                            await RevertReceiveVoucher(rv);
                        }

                        rv.ClearedDate = clearedDate;
                        await _receiveVoucherRepo.Update(rv);
                        return Json(new { success = true, message = "Transaction reconciled successfully!" });
                    }
                }

                return Json(new { success = false, message = "Transaction not found." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task AdjustPaymentVoucher(PaymentVoucher voucher)
        {
            List<int> selectedSales = new List<int>();
            List<int> selectedPurchases = new List<int>();

            if (!string.IsNullOrEmpty(voucher.SelectedSalesIds))
            {
                selectedSales = voucher.SelectedSalesIds.Split(',').Select(int.Parse).ToList();
            }

            if (!string.IsNullOrEmpty(voucher.SelectedPurchaseIds))
            {
                selectedPurchases = voucher.SelectedPurchaseIds.Split(',').Select(int.Parse).ToList();
            }

            decimal remainingAmount = voucher.Amount;

            if (voucher.Party == Party.Customer)
            {
                var customerAdvance = (await _customerAdvanceRepo.GetByCustomerId(voucher.CustomerId))
                                      .FirstOrDefault();

                if (customerAdvance != null && customerAdvance.AdvanceAmount > 0)
                {
                    remainingAmount += customerAdvance.AdvanceAmount;
                }

                if (selectedSales.Count == 0)
                {
                    var pendingBills = await _salesRepo.GetPendingBillsByCustomerId(voucher.CustomerId);
                    selectedSales = pendingBills
                        .Where(x => x.Balance > 0)
                        .OrderBy(x => x.BillDate)
                        .Select(x => x.Id)
                        .ToList();
                }

                foreach (var saleId in selectedSales)
                {
                    if (remainingAmount <= 0) break;

                    var sale = await _salesRepo.GetById(saleId);
                    if (sale == null) continue;

                    decimal balance = sale.TotalPayable - sale.PaidAmount;
                    if (balance <= 0) continue;

                    decimal paymentToApply = Math.Min(balance, remainingAmount);

                    if (sale.SalsePaymentDetails == null)
                    {
                        sale.SalsePaymentDetails = new List<SalsePaymentDetails>();
                    }

                    sale.SalsePaymentDetails.Add(new SalsePaymentDetails
                    {
                        PaymentModeId = voucher.PaymentModeId ?? 1,
                        Amount = paymentToApply,
                        Description = "Payment paid via Payment Voucher " + voucher.VouncherNo,
                        CustomerId = voucher.CustomerId,
                        Date = voucher.Date
                    });

                    sale.PaidAmount += paymentToApply;
                    sale.Balance = sale.TotalPayable - sale.PaidAmount;
                    sale.PaymentStatus = sale.Balance <= 0 ? "Paid" : "Partial";

                    remainingAmount -= paymentToApply;
                    await _salesRepo.Update(sale);
                }

                if (customerAdvance != null)
                {
                    customerAdvance.AdvanceAmount = remainingAmount;
                    await _customerAdvanceRepo.Update(customerAdvance);
                }
                else if (remainingAmount > 0)
                {
                    await _customerAdvanceRepo.Create(new CustomerAdvance
                    {
                        CustomerId = voucher.CustomerId ?? 0,
                        AdvanceAmount = remainingAmount,
                        Date = DateTime.Now,
                        Remarks = "Advance from Payment Voucher"
                    });
                }
            }
            else if (voucher.Party == Party.Supplier)
            {
                var supplierAdvance = (await _supplierAdvanceRepo.GetBySupplierId(voucher.SupplierId))
                                      .FirstOrDefault();

                if (supplierAdvance != null && supplierAdvance.AdvanceAmount > 0)
                {
                    remainingAmount += supplierAdvance.AdvanceAmount;
                }

                if (selectedPurchases.Count == 0)
                {
                    var pendingPurchases = await _purchaseRepo.GetPendingBillsBySupplierId(voucher.SupplierId);
                    selectedPurchases = pendingPurchases
                        .Where(x => (x.TotalPayable - x.PaymentAmt) > 0)
                        .OrderBy(x => x.BillDate)
                        .Select(x => x.Id)
                        .ToList();
                }

                foreach (var purchaseId in selectedPurchases)
                {
                    if (remainingAmount <= 0) break;

                    var purchase = await _purchaseRepo.GetById(purchaseId);
                    if (purchase == null) continue;

                    decimal balance = purchase.TotalPayable - purchase.PaidAmount;
                    if (balance <= 0) continue;

                    decimal paymentToApply = Math.Min(balance, remainingAmount);

                    if (purchase.PaymentDetails == null)
                    {
                        purchase.PaymentDetails = new List<SalsePaymentDetails>();
                    }

                    purchase.PaymentDetails.Add(new SalsePaymentDetails
                    {
                        PaymentModeId = voucher.PaymentModeId ?? 1,
                        Amount = paymentToApply,
                        Description = "Payment paid via Payment Voucher " + voucher.VouncherNo,
                        Date = voucher.Date
                    });

                    purchase.PaymentAmt += paymentToApply;
                    purchase.PaidAmount += paymentToApply;
                    purchase.Balance = purchase.TotalPayable - purchase.PaidAmount;
                    purchase.PaymentStatus = purchase.Balance <= 0 ? "Paid" : "Partial";

                    remainingAmount -= paymentToApply;
                    await _purchaseRepo.Update(purchase);
                }

                if (supplierAdvance != null)
                {
                    supplierAdvance.AdvanceAmount = remainingAmount;
                    await _supplierAdvanceRepo.Update(supplierAdvance);
                }
                else if (remainingAmount > 0)
                {
                    await _supplierAdvanceRepo.Create(new SupplierAdvance
                    {
                        SupplierId = voucher.SupplierId ?? 0,
                        AdvanceAmount = remainingAmount,
                        Date = DateTime.Now,
                        Remarks = "Advance from Payment Voucher"
                    });
                }
            }
        }

        private async Task RevertPaymentVoucher(PaymentVoucher voucher)
        {
            var allDetails = await _salsePaymentDetailsRepo.GetAll();
            var details = allDetails.Where(d => d.Description != null && d.Description.Contains(voucher.VouncherNo)).ToList();

            decimal totalRevertedAmount = 0;

            foreach (var detail in details)
            {
                totalRevertedAmount += detail.Amount;

                if (detail.SalseId.HasValue)
                {
                    var sale = await _salesRepo.GetById(detail.SalseId.Value);
                    if (sale != null)
                    {
                        sale.PaidAmount -= detail.Amount;
                        sale.Balance = sale.TotalPayable - sale.PaidAmount;
                        sale.PaymentStatus = sale.PaidAmount <= 0 ? "Unpaid" : (sale.Balance <= 0 ? "Paid" : "Partial");
                        await _salesRepo.Update(sale);
                    }
                }
                else if (detail.PurchaseId.HasValue)
                {
                    var purchase = await _purchaseRepo.GetById(detail.PurchaseId.Value);
                    if (purchase != null)
                    {
                        purchase.PaymentAmt -= detail.Amount;
                        purchase.PaidAmount -= detail.Amount;
                        purchase.Balance = purchase.TotalPayable - purchase.PaidAmount;
                        purchase.PaymentStatus = purchase.PaidAmount <= 0 ? "Unpaid" : (purchase.PaidAmount >= purchase.TotalPayable ? "Paid" : "Partial");
                        await _purchaseRepo.Update(purchase);
                    }
                }

                await _salsePaymentDetailsRepo.Delete(detail);
            }

            // Revert advance
            decimal advanceAmountToRevert = voucher.Amount - totalRevertedAmount;
            if (advanceAmountToRevert > 0)
            {
                if (voucher.Party == Party.Customer && voucher.CustomerId.HasValue)
                {
                    var customerAdvance = (await _customerAdvanceRepo.GetByCustomerId(voucher.CustomerId.Value)).FirstOrDefault();
                    if (customerAdvance != null)
                    {
                        customerAdvance.AdvanceAmount -= advanceAmountToRevert;
                        if (customerAdvance.AdvanceAmount < 0) customerAdvance.AdvanceAmount = 0;
                        await _customerAdvanceRepo.Update(customerAdvance);
                    }
                }
                else if (voucher.Party == Party.Supplier && voucher.SupplierId.HasValue)
                {
                    var supplierAdvance = (await _supplierAdvanceRepo.GetBySupplierId(voucher.SupplierId.Value)).FirstOrDefault();
                    if (supplierAdvance != null)
                    {
                        supplierAdvance.AdvanceAmount -= advanceAmountToRevert;
                        if (supplierAdvance.AdvanceAmount < 0) supplierAdvance.AdvanceAmount = 0;
                        await _supplierAdvanceRepo.Update(supplierAdvance);
                    }
                }
            }
        }

        private async Task AdjustReceiveVoucher(ReceiveVoucher voucher)
        {
            List<int> selectedSales = new List<int>();
            List<int> selectedPurchases = new List<int>();

            if (!string.IsNullOrEmpty(voucher.SelectedSalesIds))
            {
                selectedSales = voucher.SelectedSalesIds.Split(',').Select(int.Parse).ToList();
            }

            if (!string.IsNullOrEmpty(voucher.SelectedPurchaseIds))
            {
                selectedPurchases = voucher.SelectedPurchaseIds.Split(',').Select(int.Parse).ToList();
            }

            decimal remainingAmount = voucher.Amount;

            if (voucher.Party == Party.Customer)
            {
                var customerAdvance = (await _customerAdvanceRepo.GetByCustomerId(voucher.CustomerId))
                                      .FirstOrDefault();

                if (customerAdvance != null && customerAdvance.AdvanceAmount > 0)
                {
                    remainingAmount += customerAdvance.AdvanceAmount;
                }

                if (selectedSales.Count == 0)
                {
                    var pendingBills = await _salesRepo.GetPendingBillsByCustomerId(voucher.CustomerId);
                    selectedSales = pendingBills
                        .Where(x => x.Balance > 0)
                        .OrderBy(x => x.BillDate)
                        .Select(x => x.Id)
                        .ToList();
                }

                foreach (var saleId in selectedSales)
                {
                    if (remainingAmount <= 0) break;

                    var sale = await _salesRepo.GetById(saleId);
                    if (sale == null) continue;

                    decimal balance = sale.TotalPayable - sale.PaidAmount;
                    if (balance <= 0) continue;

                    decimal paymentToApply = Math.Min(balance, remainingAmount);

                    if (sale.SalsePaymentDetails == null)
                    {
                        sale.SalsePaymentDetails = new List<SalsePaymentDetails>();
                    }

                    sale.SalsePaymentDetails.Add(new SalsePaymentDetails
                    {
                        PaymentModeId = voucher.PaymentModeId ?? 1,
                        Amount = paymentToApply,
                        Description = "Payment received via Receive Voucher " + voucher.VouncherNo,
                        CustomerId = voucher.CustomerId,
                        Date = voucher.Date
                    });

                    sale.PaidAmount += paymentToApply;
                    sale.Balance = sale.TotalPayable - sale.PaidAmount;
                    sale.PaymentStatus = sale.Balance <= 0 ? "Paid" : "Partial";

                    remainingAmount -= paymentToApply;
                    await _salesRepo.Update(sale);
                }

                if (customerAdvance != null)
                {
                    customerAdvance.AdvanceAmount = remainingAmount;
                    await _customerAdvanceRepo.Update(customerAdvance);
                }
                else if (remainingAmount > 0)
                {
                    await _customerAdvanceRepo.Create(new CustomerAdvance
                    {
                        CustomerId = voucher.CustomerId ?? 0,
                        AdvanceAmount = remainingAmount,
                        Date = DateTime.Now,
                        Remarks = "Advance received from Receive Voucher"
                    });
                }
            }
            else if (voucher.Party == Party.Supplier)
            {
                var supplierAdvance = (await _supplierAdvanceRepo.GetBySupplierId(voucher.SupplierId))
                                      .FirstOrDefault();

                if (supplierAdvance != null && supplierAdvance.AdvanceAmount > 0)
                {
                    remainingAmount += supplierAdvance.AdvanceAmount;
                }

                if (selectedPurchases.Count == 0)
                {
                    var pendingPurchases = await _purchaseRepo.GetPendingBillsBySupplierId(voucher.SupplierId);
                    selectedPurchases = pendingPurchases
                        .Where(x => (x.TotalPayable - x.PaymentAmt) > 0)
                        .OrderBy(x => x.BillDate)
                        .Select(x => x.Id)
                        .ToList();
                }

                foreach (var purchaseId in selectedPurchases)
                {
                    if (remainingAmount <= 0) break;

                    var purchase = await _purchaseRepo.GetById(purchaseId);
                    if (purchase == null) continue;

                    decimal balance = purchase.TotalPayable - purchase.PaidAmount;
                    if (balance <= 0) continue;

                    decimal paymentToApply = Math.Min(balance, remainingAmount);

                    if (purchase.PaymentDetails == null)
                    {
                        purchase.PaymentDetails = new List<SalsePaymentDetails>();
                    }

                    purchase.PaymentDetails.Add(new SalsePaymentDetails
                    {
                        PaymentModeId = voucher.PaymentModeId ?? 1,
                        Amount = paymentToApply,
                        Description = "Payment paid via Receive Voucher " + voucher.VouncherNo,
                        Date = voucher.Date
                    });

                    purchase.PaymentAmt += paymentToApply;
                    purchase.PaidAmount += paymentToApply;
                    purchase.Balance = purchase.TotalPayable - purchase.PaidAmount;
                    purchase.PaymentStatus = purchase.Balance <= 0 ? "Paid" : "Partial";

                    remainingAmount -= paymentToApply;
                    await _purchaseRepo.Update(purchase);
                }

                if (supplierAdvance != null)
                {
                    supplierAdvance.AdvanceAmount = remainingAmount;
                    await _supplierAdvanceRepo.Update(supplierAdvance);
                }
                else if (remainingAmount > 0)
                {
                    await _supplierAdvanceRepo.Create(new SupplierAdvance
                    {
                        SupplierId = voucher.SupplierId ?? 0,
                        AdvanceAmount = remainingAmount,
                        Date = DateTime.Now,
                        Remarks = "Advance paid from Receive Voucher"
                    });
                }
            }
        }

        private async Task RevertReceiveVoucher(ReceiveVoucher voucher)
        {
            var allDetails = await _salsePaymentDetailsRepo.GetAll();
            var details = allDetails.Where(d => d.Description != null && d.Description.Contains(voucher.VouncherNo)).ToList();

            decimal totalRevertedAmount = 0;

            foreach (var detail in details)
            {
                totalRevertedAmount += detail.Amount;

                if (detail.SalseId.HasValue)
                {
                    var sale = await _salesRepo.GetById(detail.SalseId.Value);
                    if (sale != null)
                    {
                        sale.PaidAmount -= detail.Amount;
                        sale.Balance = sale.TotalPayable - sale.PaidAmount;
                        sale.PaymentStatus = sale.PaidAmount <= 0 ? "Unpaid" : (sale.Balance <= 0 ? "Paid" : "Partial");
                        await _salesRepo.Update(sale);
                    }
                }
                else if (detail.PurchaseId.HasValue)
                {
                    var purchase = await _purchaseRepo.GetById(detail.PurchaseId.Value);
                    if (purchase != null)
                    {
                        purchase.PaymentAmt -= detail.Amount;
                        purchase.PaidAmount -= detail.Amount;
                        purchase.Balance = purchase.TotalPayable - purchase.PaidAmount;
                        purchase.PaymentStatus = purchase.PaidAmount <= 0 ? "Unpaid" : (purchase.PaidAmount >= purchase.TotalPayable ? "Paid" : "Partial");
                        await _purchaseRepo.Update(purchase);
                    }
                }

                await _salsePaymentDetailsRepo.Delete(detail);
            }

            // Revert advance
            decimal advanceAmountToRevert = voucher.Amount - totalRevertedAmount;
            if (advanceAmountToRevert > 0)
            {
                if (voucher.Party == Party.Customer && voucher.CustomerId.HasValue)
                {
                    var customerAdvance = (await _customerAdvanceRepo.GetByCustomerId(voucher.CustomerId.Value)).FirstOrDefault();
                    if (customerAdvance != null)
                    {
                        customerAdvance.AdvanceAmount -= advanceAmountToRevert;
                        if (customerAdvance.AdvanceAmount < 0) customerAdvance.AdvanceAmount = 0;
                        await _customerAdvanceRepo.Update(customerAdvance);
                    }
                }
                else if (voucher.Party == Party.Supplier && voucher.SupplierId.HasValue)
                {
                    var supplierAdvance = (await _supplierAdvanceRepo.GetBySupplierId(voucher.SupplierId.Value)).FirstOrDefault();
                    if (supplierAdvance != null)
                    {
                        supplierAdvance.AdvanceAmount -= advanceAmountToRevert;
                        if (supplierAdvance.AdvanceAmount < 0) supplierAdvance.AdvanceAmount = 0;
                        await _supplierAdvanceRepo.Update(supplierAdvance);
                    }
                }
            }
        }
    }
}
