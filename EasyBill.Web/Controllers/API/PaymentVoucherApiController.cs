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
    public class PaymentVoucherApiController : ControllerBase
    {
        private readonly IpaymentVoucherRepository _paymentVoucherRepo;
        private readonly IPurchaseRepository _purchaseRepo;
        private readonly ISalesRepository _salesRepo;

        public PaymentVoucherApiController(
            IpaymentVoucherRepository paymentVoucherRepo,
            IPurchaseRepository purchaseRepo,
            ISalesRepository salesRepo)
        {
            _paymentVoucherRepo = paymentVoucherRepo;
            _purchaseRepo = purchaseRepo;
            _salesRepo = salesRepo;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] PaymentVoucherVM vm)
        {
            if (vm == null) return BadRequest(new { success = false, message = "Invalid Data" });
            if (!Enum.IsDefined(typeof(Party), vm.Party))
                return BadRequest(new { success = false, message = "Invalid Party type" });

            var voucher = new PaymentVoucher
            {
                VouncherNo = vm.VouncherNo,
                Date = vm.Date,
                Party = vm.Party,
                SupplierId = vm.SupplierId,
                CustomerId = vm.CustomerId,
                Amount = vm.Amount,
                PaymentModeId = vm.PaymentModeId ?? 1,
                Description = vm.Description
            };

            await _paymentVoucherRepo.Create(voucher);
            decimal remainingAmount = vm.Amount;

            // -------------------
            // Supplier Payment
            // -------------------
            if (vm.Party == Party.Supplier && vm.SupplierId.HasValue)
            {
                var purchases = (await _purchaseRepo.GetAll())
     .Where(x => x.SupplierId == vm.SupplierId
         && (x.TotalPayable - x.PaidAmount) > 0)
     .OrderBy(x => x.BillDate)
     .ToList(); ;

                if (vm.SelectedBillNos?.Any() == true)
                {
                    var billNos = vm.SelectedBillNos
                        .Select(b => b.Trim().ToLower())
                        .ToList();

                    purchases = purchases
                        .Where(x => billNos.Contains(x.BillNo.Trim().ToLower()))
                        .ToList();
                }
                else if (vm.SelectedPurchaseIds?.Any() == true)
                {
                    purchases = purchases.Where(x => vm.SelectedPurchaseIds.Contains(x.Id)).ToList();
                }
                foreach (var p in purchases)
                {
                    if (remainingAmount <= 0) break;
                    var dbPurchase = await _purchaseRepo.GetById(p.Id);
                    var balance = dbPurchase.TotalPayable - dbPurchase.PaidAmount;
                    if (balance <= 0) continue;

                    var pay = Math.Min(balance, remainingAmount);

                    if (dbPurchase.PaymentDetails == null)
                        dbPurchase.PaymentDetails = new List<SalsePaymentDetails>();

                    dbPurchase.PaymentDetails.Add(new SalsePaymentDetails
                    {
                        PaymentModeId = vm.PaymentModeId ?? 1,
                        Amount = pay,
                        Description = "Payment paid via Payment Voucher " + vm.VouncherNo,
                        Date = vm.Date
                    });

                    dbPurchase.PaidAmount += pay;
                    dbPurchase.Balance = dbPurchase.TotalPayable - dbPurchase.PaidAmount;
                    dbPurchase.PaymentStatus = dbPurchase.PaidAmount == 0 ? "Unpaid" :
                                               dbPurchase.PaidAmount < dbPurchase.TotalPayable ? "Partial" : "Paid";

                    await _purchaseRepo.Update(dbPurchase);
                    remainingAmount -= pay;
                }
            }

            // -------------------
            // Customer Payment
            // -------------------
            else if (vm.Party == Party.Customer && vm.CustomerId.HasValue)
            {
                var sales = (await _salesRepo.GetAll())
                    .Where(x => x.CustomerId == vm.CustomerId && x.Balance > 0)
                    .OrderBy(x => x.BillDate)
                    .ToList();

                // ✅ ADD HERE
                if (vm.SelectedBillNos?.Any() == true)
                {
                    sales = sales.Where(x => vm.SelectedBillNos.Contains(x.BillNo)).ToList();
                }
                else if (vm.SelectedSalesIds?.Any() == true)
                {
                    sales = sales.Where(x => vm.SelectedSalesIds.Contains(x.Id)).ToList();
                }
                foreach (var s in sales)
                {
                    if (remainingAmount <= 0) break;
                    var dbSale = await _salesRepo.GetById(s.Id);
                    var balance = dbSale.TotalPayable - dbSale.PaidAmount;
                    if (balance <= 0) continue;

                    var pay = Math.Min(balance, remainingAmount);

                    if (dbSale.SalsePaymentDetails == null)
                        dbSale.SalsePaymentDetails = new List<SalsePaymentDetails>();

                    dbSale.SalsePaymentDetails.Add(new SalsePaymentDetails
                    {
                        PaymentModeId = vm.PaymentModeId ?? 1,
                        Amount = pay,
                        Description = "Payment paid via Payment Voucher " + vm.VouncherNo,
                        CustomerId = vm.CustomerId,
                        SalseId = dbSale.Id,
                        Date = vm.Date
                    });

                    dbSale.PaidAmount += pay;

                    dbSale.Balance = dbSale.TotalPayable - dbSale.PaidAmount;

                    dbSale.PaymentStatus = dbSale.PaidAmount == 0 ? "Unpaid" :
                                           dbSale.PaidAmount < dbSale.TotalPayable ? "Partial" : "Paid";
                    await _salesRepo.Update(dbSale);
                    remainingAmount -= pay;
                }
            }

            return Ok(new
            {
                success = true,
                message = "Payment Done Successfully",
                remainingAmount
            });
        }
    }
}
