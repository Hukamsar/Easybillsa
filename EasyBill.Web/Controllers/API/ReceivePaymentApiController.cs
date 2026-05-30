using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class ReceivePaymentApiController : ControllerBase
    {
        private readonly IReceiveVoucherRepository _receiveVoucherRepo;
        private readonly ISalesRepository _salesRepo;
        private readonly IPurchaseRepository _purchaseRepo;

        public ReceivePaymentApiController(
            IReceiveVoucherRepository receiveVoucherRepo,
            ISalesRepository salesRepo,
            IPurchaseRepository purchaseRepo)
        {
            _receiveVoucherRepo = receiveVoucherRepo;
            _salesRepo = salesRepo;
            _purchaseRepo = purchaseRepo;
        }


        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] ReceiveVoucherVM viewModel)
        {
            if (viewModel == null)
                return BadRequest(new { success = false, message = "Invalid Data" });

            // ================= SAVE VOUCHER =================
            var model = new ReceiveVoucher()
            {
                VouncherNo = viewModel.VouncherNo,
                Date = viewModel.Date,
                Party = viewModel.Party,
                CustomerId = viewModel.CustomerId,
                SupplierId = viewModel.SupplierId,
                EmployeeId = viewModel.EmployeeId,
                PaymentCategoryId = viewModel.CategoryId,
                Amount = viewModel.Amount,
                Description = viewModel.Description,
                PaymentModeId = viewModel.PaymentModeId
            };

            await _receiveVoucherRepo.Create(model);

            decimal remainingAmount = viewModel.Amount;

            // =====================================================
            // ✅ CUSTOMER PAYMENT (SALES)
            // =====================================================
            if (viewModel.Party == Party.Customer)
            {
                List<Sales> salesList = new List<Sales>();

                var allSales = await _salesRepo.GetAll();

                // 👉 Bill wise
                // 👉 Bill wise (MULTIPLE SUPPORT)
                if (viewModel.SelectedBillNos?.Any() == true)
                {
                    salesList = allSales
                        .Where(x => viewModel.SelectedBillNos.Contains(x.BillNo)
                                 && x.CustomerId == viewModel.CustomerId
                                 && (x.TotalPayable - x.PaidAmount) > 0)
                        .OrderBy(x => x.BillDate)
                        .ToList();
                }
                // 👉 Selected IDs
                else if (viewModel.SelectedSalesIds?.Any() == true)
                {
                    salesList = allSales
                        .Where(x => viewModel.SelectedSalesIds.Contains(x.Id)
                                 && (x.TotalPayable - x.PaidAmount) > 0)
                        .OrderBy(x => x.BillDate)
                        .ToList();
                }
                // 👉 FIFO Auto
                else
                {
                    salesList = allSales
                        .Where(x => x.CustomerId == viewModel.CustomerId
                                 && (x.TotalPayable - x.PaidAmount) > 0)
                        .OrderBy(x => x.BillDate)
                        .ToList();
                }
            
                foreach (var sale in salesList)
                {
                    if (remainingAmount <= 0) break;

                    decimal balance = sale.TotalPayable - sale.PaidAmount;
                    if (balance <= 0) continue;

                    decimal payment = Math.Min(balance, remainingAmount);

                    // Ensure list
                    if (sale.SalsePaymentDetails == null)
                        sale.SalsePaymentDetails = new List<SalsePaymentDetails>();

                    sale.SalsePaymentDetails.Add(new SalsePaymentDetails
                    {
                        PaymentModeId = viewModel.PaymentModeId ?? 1,
                        Amount = payment,
                        Description = "Payment received via Receive Voucher " + viewModel.VouncherNo,
                        CustomerId = viewModel.CustomerId,
                        SalseId = sale.Id,
                        Date = viewModel.Date
                    });

                    // ✅ ONLY ONCE
                    sale.PaidAmount += payment;
                    sale.Balance = sale.TotalPayable - sale.PaidAmount;

                    // ✅ OPTIONAL (recommended)
                    if (sale.PaidAmount == 0)
                        sale.PaymentStatus = "Unpaid";
                    else if (sale.PaidAmount < sale.TotalPayable)
                        sale.PaymentStatus = "Partial";
                    else
                        sale.PaymentStatus = "Paid";

                    await _salesRepo.Update(sale);

                    remainingAmount -= payment;
                }
            }

            // =====================================================
            // ✅ SUPPLIER PAYMENT (PURCHASE)
            // =====================================================
            else if (viewModel.Party == Party.Supplier)
            {
                List<Purchase> purchaseList = new List<Purchase>();

                var allPurchases = await _purchaseRepo.GetAll();

                // 👉 Bill wise (MULTIPLE SUPPORT)
                if (viewModel.SelectedBillNos?.Any() == true)
                {
                    purchaseList = allPurchases
                        .Where(x => viewModel.SelectedBillNos.Contains(x.BillNo)
                                 && x.SupplierId == viewModel.SupplierId
                                 && (x.TotalPayable - x.PaidAmount) > 0)
                        .OrderBy(x => x.BillDate)
                        .ToList();
                }
                // 👉 Selected IDs
                else if (viewModel.SelectedPurchaseIds?.Any() == true)
                {
                    purchaseList = allPurchases
                        .Where(x => viewModel.SelectedPurchaseIds.Contains(x.Id)
                                 && (x.TotalPayable - x.PaidAmount) > 0)
                        .OrderBy(x => x.BillDate)
                        .ToList();
                }
                // 👉 FIFO Auto
                else
                {
                    purchaseList = allPurchases
                        .Where(x => x.SupplierId == viewModel.SupplierId
                                 && (x.TotalPayable - x.PaidAmount) > 0)
                        .OrderBy(x => x.BillDate)
                        .ToList();
                }

                foreach (var purchase in purchaseList)
                {
                    if (remainingAmount <= 0) break;

                    decimal balance = purchase.TotalPayable - purchase.PaidAmount;
                    if (balance <= 0) continue;

                    decimal payment = Math.Min(balance, remainingAmount);

                    if (purchase.PaymentDetails == null)
                        purchase.PaymentDetails = new List<SalsePaymentDetails>();

                    purchase.PaymentDetails.Add(new SalsePaymentDetails
                    {
                        PaymentModeId = viewModel.PaymentModeId ?? 1,
                        Amount = payment,
                        Description = "Payment paid via Receive Voucher " + viewModel.VouncherNo,
                        Date = viewModel.Date
                    });

                    purchase.PaidAmount += payment;
                    purchase.Balance = purchase.TotalPayable - purchase.PaidAmount;

                    // ✅ STATUS FIX
                    if (purchase.PaidAmount == 0)
                        purchase.PaymentStatus = "Unpaid";
                    else if (purchase.PaidAmount < purchase.TotalPayable)
                        purchase.PaymentStatus = "Partial";
                    else
                        purchase.PaymentStatus = "Paid";

                    await _purchaseRepo.Update(purchase);

                    remainingAmount -= payment;
                }
            }

            return Ok(new
            {
                success = true,
                message = "Payment received successfully",
                remainingAmount = remainingAmount
            });
        }

        // ✅ MAIN API
        //[HttpPost("Create")]
        //public async Task<IActionResult> Create([FromBody] ReceiveVoucherVM viewModel)
        //{
        //    if (viewModel == null)
        //        return BadRequest(new { success = false, message = "Invalid Data" });

        //    // ==============================
        //    // ✅ Step 1: Save Voucher
        //    // ==============================
        //    var model = new ReceiveVoucher()
        //    {
        //        VouncherNo = viewModel.VouncherNo,
        //        Date = viewModel.Date,
        //        Party = viewModel.Party,
        //        CustomerId = viewModel.CustomerId,
        //        SupplierId = viewModel.SupplierId,
        //        EmployeeId = viewModel.EmployeeId,
        //        PaymentCategoryId = viewModel.CategoryId,
        //        Amount = viewModel.Amount,
        //        Description = viewModel.Description,
        //        PaymentModeId = viewModel.PaymentModeId
        //    };

        //    await _receiveVoucherRepo.Create(model);

        //    decimal remainingAmount = viewModel.Amount;

        //    // =====================================================
        //    // ✅ CUSTOMER PAYMENT (Sales Adjust)
        //    // =====================================================
        //    if (viewModel.Party == Party.Customer)
        //    {
        //        List<Sales> salesList = new List<Sales>();

        //        // ✅ Case 1: Bill Wise Payment
        //        if (!string.IsNullOrEmpty(viewModel.BillNo))
        //        {
        //            salesList = (await _salesRepo.GetAll())
        //                .Where(x => x.BillNo == viewModel.BillNo
        //                         && x.CustomerId == viewModel.CustomerId
        //                         && (x.TotalPayable - x.PaidAmount) > 0)
        //                .ToList();
        //        }

        //        // ✅ Case 2: Selected Sales Payment
        //        else if (viewModel.SelectedSalesIds != null && viewModel.SelectedSalesIds.Count > 0)
        //        {
        //            foreach (var salesId in viewModel.SelectedSalesIds)
        //            {
        //                var sale = await _salesRepo.GetById(salesId);
        //                if (sale != null)
        //                    salesList.Add(sale);
        //            }
        //        }

        //        // ✅ Case 3: Auto FIFO Payment
        //        else
        //        {
        //            salesList = (await _salesRepo.GetAll())
        //                .Where(x => x.CustomerId == viewModel.CustomerId
        //                         && (x.TotalPayable - x.PaidAmount) > 0)
        //                .OrderBy(x => x.BillDate)
        //                .ToList();
        //        }

        //        // 🔥 COMMON LOOP
        //        foreach (var sale in salesList)
        //        {
        //            if (remainingAmount <= 0) break;

        //            decimal balance = sale.TotalPayable - sale.PaidAmount;
        //            decimal deduction = Math.Min(balance, remainingAmount);

        //            remainingAmount -= deduction;

        //            // ✅ Null Safety Fix
        //            if (sale.SalsePaymentDetails == null)
        //                sale.SalsePaymentDetails = new List<SalsePaymentDetails>();

        //            sale.SalsePaymentDetails.Add(new SalsePaymentDetails
        //            {
        //                PaymentModeId = viewModel.PaymentModeId ?? 1,
        //                Amount = deduction,
        //                Description = "Payment received via API",
        //                CustomerId = viewModel.CustomerId,
        //                Date = DateTime.Now
        //            });

        //            sale.PaidAmount += deduction;
        //            sale.Balance = sale.TotalPayable - sale.PaidAmount;

        //            await _salesRepo.Update(sale);
        //        }
        //    }

        //    else if (viewModel.Party == Party.Supplier)
        //    {
        //        List<Purchase> purchaseList = new List<Purchase>();

        //        // ✅ Case 1: Bill Wise Payment
        //        if (!string.IsNullOrEmpty(viewModel.BillNo))
        //        {
        //            purchaseList = (await _purchaseRepo.GetAll())
        //            .Where(x => x.BillNo.Trim().ToLower() == viewModel.BillNo.Trim().ToLower()
        //                     && x.SupplierId == viewModel.SupplierId
        //                     && (x.TotalPayable - x.PaidAmount) > 0)
        //            .OrderBy(x => x.BillDate)
        //            .ToList();
        //        }

        //        // ✅ Case 2: Selected Purchase Payment
        //        else if (viewModel.SelectedPurchaseIds != null && viewModel.SelectedPurchaseIds.Count > 0)
        //        {
        //            foreach (var purchaseId in viewModel.SelectedPurchaseIds)
        //            {
        //                var purchase = await _purchaseRepo.GetById(purchaseId);
        //                if (purchase != null)
        //                    purchaseList.Add(purchase);
        //            }
        //        }

        //        // ✅ Case 3: FIFO Auto Payment
        //        else
        //        {
        //            purchaseList = (await _purchaseRepo.GetAll())
        //                .Where(x => x.SupplierId == viewModel.SupplierId
        //                         && (x.TotalPayable - x.PaidAmount) > 0 )
        //                .OrderBy(x => x.BillDate)
        //                .ToList();
        //        }

        //        // 🔥 COMMON LOOP
        //        foreach (var purchase in purchaseList)
        //        {
        //            if (remainingAmount <= 0) break;

        //            decimal balance = purchase.TotalPayable - purchase.PaidAmount;

        //            if (balance <= 0)
        //                continue;

        //            decimal paymentToApply = Math.Min(balance, remainingAmount);

        //            // ✅ Payment add
        //            purchase.PaidAmount += paymentToApply;

        //            // ✅ Balance update
        //            purchase.Balance = purchase.TotalPayable - purchase.PaidAmount;

        //            // ✅ Status update
        //            if (purchase.PaidAmount == 0)
        //                purchase.PaymentStatus = "Unpaid";
        //            else if (purchase.PaidAmount < purchase.TotalPayable)
        //                purchase.PaymentStatus = "Partial";
        //            else
        //                purchase.PaymentStatus = "Paid";

        //            // ✅ SAVE ONLY ONCE
        //            await _purchaseRepo.Update(purchase);

        //            // ✅ Remaining update ONLY ONCE
        //            remainingAmount -= paymentToApply;
        //        }
        //    }
        //    // ==============================
        //    // ✅ Final Response
        //    // ==============================
        //    return Ok(new
        //    {
        //        success = true,
        //        message = "Payment received successfully",
        //        remainingAmount = remainingAmount
        //    });
        //}
    }
}
