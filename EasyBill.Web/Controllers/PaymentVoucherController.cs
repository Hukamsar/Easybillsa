using AOne.Utility;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class PaymentVoucherController : Controller
    {
        private readonly IpaymentVoucherRepository _paymentvoucherservice;
        private readonly ISupplierRepository _supplierservice;
        private readonly IPaymentVoucherCategoryRepository _paymentvouchercategoryservice;
        private readonly ICustomerRepository _customerservice;
        private readonly IEmployeeRepository _employeeservice;
        private readonly IModeOfPaymentRepository _modeofpaymentservice;
        private readonly IPurchaseRepository _purchaseRepo;
        private readonly ISalesRepository _salesRepo;
        private readonly ICustomerAdvanceRepository _customerAdvanceRepo;
        private readonly ISupplierAdvanceRepository _supplierAdvanceRepo;
        public PaymentVoucherController(
            IpaymentVoucherRepository paymentvoucherservice, 
            ISupplierRepository supplierservice,
            IPaymentVoucherCategoryRepository paymentvouchercategoryservice,
            ICustomerRepository customerservice,
            IEmployeeRepository employeeservice,
            IModeOfPaymentRepository modeofpaymentservice,
            IPurchaseRepository purchaseRepo,
            ISalesRepository salesRepo,
            ICustomerAdvanceRepository customerAdvanceRepo,
            ISupplierAdvanceRepository supplierAdvanceRepo)
        {
            _paymentvoucherservice = paymentvoucherservice;
            _supplierservice = supplierservice;
            _paymentvouchercategoryservice = paymentvouchercategoryservice;
            _customerservice = customerservice;
            _employeeservice = employeeservice;
            _modeofpaymentservice = modeofpaymentservice;
            _purchaseRepo = purchaseRepo;
            _salesRepo = salesRepo;
            _customerAdvanceRepo = customerAdvanceRepo;
            _supplierAdvanceRepo = supplierAdvanceRepo;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _paymentvoucherservice.GetAll();
          
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new PaymentVoucherVM()
            {
                VouncherNo = await GenerateVoucherNumber(),
                Date = DateTime.Now
            };
            ViewBag.SupplierList = new SelectList(await _supplierservice.GetALL(), "Id", "FirstName");
            ViewBag.PaymentCategorylist = new SelectList(await _paymentvouchercategoryservice.GetAll(), "Id", "Name");
            ViewBag.CustomerList = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.EmployeeList = new SelectList(await _employeeservice.GetAll(), "Id", "Name");
            ViewBag.PaymentModeList = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            return View(viewModel);
        }
         
        public async Task<string> GenerateVoucherNumber()
        {
            var data = await _paymentvoucherservice.GetAll();
            var lastCode = data
                .Where(p => !string.IsNullOrEmpty(p.VouncherNo))
                .Select(p => p.VouncherNo)
                .LastOrDefault();

            if (string.IsNullOrEmpty(lastCode) || lastCode.Length < 2)
                return "VN0001";

            string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray());

            string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

            int number = 0;
            int.TryParse(numberPart, out number);

            string nextCode = prefix + (number + 1).ToString("D" + numberPart.Length);

            return nextCode;
        }

        [HttpPost]
        public async Task<IActionResult> Create(PaymentVoucherVM VM)
        { 
            
            if (VM != null)
            {
                string fileName = "";
                if (VM.UploadAttachments != null && VM.UploadAttachments.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Payment Voucher");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    fileName = VM.UploadAttachments.FileName;
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        VM.UploadAttachments.CopyTo(fileStream);
                    }
                    VM.Attachments = fileName;
                }
                var model = new PaymentVoucher
                {
                    VouncherNo = VM.VouncherNo,
                    Date = VM.Date,
                    SupplierId = VM.SupplierId,
                    VoucherCategoryId = VM.VoucherCategoryId,
                    CustomerId = VM.CustomerId,
                    EmployeeId = VM.EmployeeId,
                    Party = VM.Party,
                    Amount = VM.Amount,
                    GST = VM.GST,
                    GSTAmount = VM.GSTAmount,
                    NetAmount = VM.NetAmount,
                    Description = VM.Description,
                    Attachments = VM.Attachments,
                    ChequeNo = VM.ChequeNo,
                    ChequeDate = VM.ChequeDate,
                    RefNo = VM.RefNo,
                    PaymentModeId = VM.PaymentModeId,
                };
                await _paymentvoucherservice.Create(model);
                //var selectedAppointments = JsonConvert.DeserializeObject<List<int>>(Request.Form["selectedsales"]);
                //var selectedPurchases = JsonConvert.DeserializeObject<List<int>>(Request.Form["SelectedPurchases"]);
                List<int> selectedAppointments = new List<int>();
                List<int> selectedPurchases = new List<int>();

                if (!string.IsNullOrEmpty(Request.Form["selectedsales"]))
                {
                    selectedAppointments = JsonConvert.DeserializeObject<List<int>>(Request.Form["selectedsales"]);
                }

                if (!string.IsNullOrEmpty(Request.Form["SelectedPurchases"]))
                {
                    selectedPurchases = JsonConvert.DeserializeObject<List<int>>(Request.Form["SelectedPurchases"]);
                }


                decimal remainingAmount = VM.Amount;

                //if (VM.Party == Party.Customer && selectedAppointments != null && selectedAppointments.Count > 0)
                //{
                //    foreach (var bookingId in selectedAppointments)
                //    {
                //        if (remainingAmount <= 0) break;

                //        var billing = await _salesRepo.GetById(bookingId);
                //        //if (billing != null && billing.PaymentStatus != "Refund")
                //        //{
                //        //    decimal refundAmt = Math.Min(remainingAmount, billing.PaidAmount);
                //        //    billing.PaymentStatus = "Refund";
                //        //    billing.RefundAmount = refundAmt;

                //        //    await _testBookingRepo.Update(billing);
                //        //    remainingAmount -= refundAmt;
                //        //}
                //    }
                //}
                if (VM.Party == Party.Customer)
                {
                    // ============================================
                    // GET OLD CUSTOMER ADVANCE
                    // ============================================

                    var customerAdvance =
                        (await _customerAdvanceRepo
                        .GetByCustomerId(VM.CustomerId))
                        .FirstOrDefault();

                    if (customerAdvance != null &&
                        customerAdvance.AdvanceAmount > 0)
                    {
                        remainingAmount += customerAdvance.AdvanceAmount;
                    }

                    // ============================================
                    // AUTO LOAD PENDING BILLS
                    // ============================================

                    if (selectedAppointments == null ||
                        selectedAppointments.Count == 0)
                    {
                        var pendingBills =
                            await _salesRepo
                            .GetPendingBillsByCustomerId(
                                VM.CustomerId);

                        selectedAppointments = pendingBills
                            .Where(x => x.Balance > 0)
                            .OrderBy(x => x.BillDate)
                            .Select(x => x.Id)
                            .ToList();
                    }

                    // ============================================
                    // BILL ADJUSTMENT
                    // ============================================

                    foreach (var bookingId in selectedAppointments)
                    {
                        if (remainingAmount <= 0)
                            break;

                        var sale = await _salesRepo.GetById(bookingId);

                        if (sale == null)
                            continue;

                        decimal balance =
                            sale.TotalPayable - sale.PaidAmount;

                        if (balance <= 0)
                            continue;

                        decimal paymentToApply =
                            Math.Min(balance, remainingAmount);

                        if (sale.SalsePaymentDetails == null)
                        {
                            sale.SalsePaymentDetails = new List<SalsePaymentDetails>();
                        }

                        sale.SalsePaymentDetails.Add(
                            new SalsePaymentDetails
                            {
                                PaymentModeId = VM.PaymentModeId ?? 1,
                                Amount = paymentToApply,
                                Description = "Payment paid via Payment Voucher " + VM.VouncherNo,
                                CustomerId = VM.CustomerId,
                                Date = VM.Date
                            });

                        sale.PaidAmount += paymentToApply;

                        sale.Balance =
                            sale.TotalPayable - sale.PaidAmount;

                        if (sale.Balance <= 0)
                        {
                            sale.PaymentStatus = "Paid";
                        }
                        else
                        {
                            sale.PaymentStatus = "Partial";
                        }

                        remainingAmount -= paymentToApply;

                        await _salesRepo.Update(sale);
                    }

                    // ============================================
                    // SAVE REMAINING AS ADVANCE
                    // ============================================

                    if (customerAdvance != null)
                    {
                        customerAdvance.AdvanceAmount = remainingAmount;

                        await _customerAdvanceRepo
                            .Update(customerAdvance);
                    }
                    else if (remainingAmount > 0)
                    {
                        await _customerAdvanceRepo.Create(
                            new CustomerAdvance
                            {
                                CustomerId = VM.CustomerId ?? 0,
                                AdvanceAmount = remainingAmount,
                                Date = DateTime.Now,
                                Remarks =
                                    "Advance from Payment Voucher"
                            });
                    }
                }
                else if (VM.Party == Party.Supplier)
                {
                    // ============================================
                    // GET OLD SUPPLIER ADVANCE
                    // ============================================

                    var supplierAdvance =
                        (await _supplierAdvanceRepo
                        .GetBySupplierId(VM.SupplierId))
                        .FirstOrDefault();

                    if (supplierAdvance != null &&
                        supplierAdvance.AdvanceAmount > 0)
                    {
                        remainingAmount += supplierAdvance.AdvanceAmount;
                    }

                    // ============================================
                    // AUTO LOAD PENDING PURCHASES
                    // ============================================

                    if (selectedPurchases == null ||
                        selectedPurchases.Count == 0)
                    {
                        var pendingPurchases =
                            await _purchaseRepo
                            .GetPendingBillsBySupplierId(
                                VM.SupplierId);

                        selectedPurchases = pendingPurchases
                            .Where(x =>
                                (x.TotalPayable - x.PaymentAmt) > 0)
                            .OrderBy(x => x.BillDate)
                            .Select(x => x.Id)
                            .ToList();
                    }

                    // ============================================
                    // PURCHASE ADJUSTMENT
                    // ============================================

                    foreach (var purchaseId in selectedPurchases)
                    {
                        if (remainingAmount <= 0)
                            break;

                        var purchase =
                            await _purchaseRepo.GetById(purchaseId);

                        if (purchase == null)
                            continue;

                        decimal balance =
                            purchase.TotalPayable -
                            purchase.PaymentAmt;

                        if (balance <= 0)
                            continue;

                        decimal paymentToApply =
                            Math.Min(balance, remainingAmount);

                        if (purchase.PaymentDetails == null)
                        {
                            purchase.PaymentDetails = new List<SalsePaymentDetails>();
                        }

                        purchase.PaymentDetails.Add(
                            new SalsePaymentDetails
                            {
                                PaymentModeId = VM.PaymentModeId ?? 1,
                                Amount = paymentToApply,
                                Description = "Payment paid via Payment Voucher " + VM.VouncherNo,
                                Date = VM.Date
                            });

                        purchase.PaymentAmt += paymentToApply;

                        purchase.Balance =
                            purchase.TotalPayable -
                            purchase.PaymentAmt;

                        if (purchase.Balance <= 0)
                        {
                            purchase.PaymentStatus = "Paid";
                        }
                        else
                        {
                            purchase.PaymentStatus = "Partial";
                        }

                        remainingAmount -= paymentToApply;

                        await _purchaseRepo.Update(purchase);
                    }

                    // ============================================
                    // SAVE REMAINING AS ADVANCE
                    // ============================================

                    if (supplierAdvance != null)
                    {
                        supplierAdvance.AdvanceAmount =
                            remainingAmount;

                        await _supplierAdvanceRepo
                            .Update(supplierAdvance);
                    }
                    else if (remainingAmount > 0)
                    {
                        await _supplierAdvanceRepo.Create(
                            new SupplierAdvance
                            {
                                SupplierId = VM.SupplierId ?? 0,
                                AdvanceAmount = remainingAmount,
                                Date = DateTime.Now,
                                Remarks =
                                    "Advance from Payment Voucher"
                            });
                    }
                }
            }
            //return RedirectToAction("Index
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            PaymentVoucher model = await _paymentvoucherservice.GetById(Id);
            PaymentVoucherVM VM = new PaymentVoucherVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.VouncherNo = model.VouncherNo;
                VM.Date = model.Date;
                VM.SupplierId = model.SupplierId;
                VM.VoucherCategoryId = model.VoucherCategoryId;
                VM.PaymentModeId = model.PaymentModeId;
                VM.CustomerId = model.CustomerId;
                VM.EmployeeId = model.EmployeeId;
                VM.Party = model.Party;
                VM.Amount = model.Amount;
                VM.GSTAmount = model.GSTAmount;
                VM.NetAmount = model.NetAmount;
                VM.Description = model.Description;
                VM.Attachments = model.Attachments;
                VM.ChequeNo = model.ChequeNo;
                VM.ChequeDate = model.ChequeDate;
                VM.RefNo = model.RefNo;
            }
            ViewBag.SupplierList = new SelectList(await _supplierservice.GetALL(), "Id", "FirstName");
            ViewBag.PaymentCategorylist = new SelectList(await _paymentvouchercategoryservice.GetAll(), "Id", "Name");
            ViewBag.CustomerList = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.EmployeeList = new SelectList(await _employeeservice.GetAll(), "Id", "Name");
            ViewBag.PaymentModeList = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(PaymentVoucherVM VM)
        {
            PaymentVoucher model = await _paymentvoucherservice.GetById(VM.Id);
            string fileName = "";
            if (VM.UploadAttachments != null && VM.UploadAttachments.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Payment Voucher");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                fileName = VM.UploadAttachments.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    VM.UploadAttachments.CopyTo(fileStream);
                }
                VM.Attachments = fileName;
            }
            if (model != null)
            {
                model.Id = VM.Id;
                model.VouncherNo = VM.VouncherNo;
                model.Date = VM.Date;
                model.SupplierId = VM.SupplierId;
                model.VoucherCategoryId = VM.VoucherCategoryId;
                model.CustomerId = VM.CustomerId;
                model.EmployeeId = VM.EmployeeId;
                model.Party = VM.Party;
                model.Amount = VM.Amount;
                model.GSTAmount = VM.GSTAmount;
                model.NetAmount = VM.NetAmount;
                model.Description = VM.Description;
                model.Attachments = VM.Attachments;
                model.ChequeNo = VM.ChequeNo;
                model.ChequeDate = VM.ChequeDate;
                model.RefNo = VM.RefNo;
                model.PaymentModeId = VM.PaymentModeId;
                await _paymentvoucherservice.Update(model);
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid Id for deletion." });
                }
                var model = await _paymentvoucherservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _paymentvoucherservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPurchaseBySupplierId(int venderId)
        {
            var purchases = await _purchaseRepo.GetAll();

            var result = purchases
                .Where(x => x.SupplierId == venderId && (x.TotalPayable - x.PaymentAmt) > 0)
                .Select(x => new
                {
                    id = x.Id,
                    purchaseNo = x.BillNo,
                    purchaseDate = x.BillDate,
                    supplierName = x.Suppliers?.FirstName ?? string.Empty,
                    totalAmount = x.Total,
                    totalpayable = x.TotalPayable,
                    paidAmount = x.PaymentAmt,

                    balance = (x.TotalPayable) - (x.PaymentAmt)
                })
                .Where(p => p.balance > 0)
                .ToList();

            return Json(new { success = true, data = result });
        }
    }
}
