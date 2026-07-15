using AOne.Utility;
using AOne.Models;
using Microsoft.EntityFrameworkCore;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IStockReturnRepository _stockReturnRepo;
        private readonly IPurchaseReturnRepository _purchaseReturnRepo;
        private readonly IReceiveVoucherRepository _receiveVoucherRepo;
        private readonly IContraRepository _contraRepo;
        private readonly IBankRepository _bankRepo;
        private readonly ISalsePaymentDetailsRepository _salsePaymentDetailsRepo;

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
            ISupplierAdvanceRepository supplierAdvanceRepo,
            IStockReturnRepository stockReturnRepo,
            IPurchaseReturnRepository purchaseReturnRepo,
            IReceiveVoucherRepository receiveVoucherRepo,
            IContraRepository contraRepo,
            IBankRepository bankRepo,
            ISalsePaymentDetailsRepository salsePaymentDetailsRepo)
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
            _stockReturnRepo = stockReturnRepo;
            _purchaseReturnRepo = purchaseReturnRepo;
            _receiveVoucherRepo = receiveVoucherRepo;
            _contraRepo = contraRepo;
            _bankRepo = bankRepo;
            _salsePaymentDetailsRepo = salsePaymentDetailsRepo;
        }
        public async Task<IActionResult> Index()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            var data = await _paymentvoucherservice.GetAll();

            if (!string.IsNullOrEmpty(tenantId))
            {
                data = data.Where(pv => pv.TenantId == tenantId).ToList();
            }

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

            var paymentModes = (await _modeofpaymentservice.GetAll())
                              .Select(x => new
                              {
                                  Id = x.Id,
                                  BankModeOfPaymentName = $"{x.Name} - {x.Bank?.BankName}"
                              }).ToList();

            ViewBag.PaymentModeList = new SelectList(paymentModes,
                "Id",
                "BankModeOfPaymentName"
            );
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
                var paymentMode = await _modeofpaymentservice.GetById(VM.PaymentModeId ?? 1);
                bool isChequeMode = paymentMode != null && (paymentMode.Name.Contains("Cheque", StringComparison.OrdinalIgnoreCase) || paymentMode.Name.Contains("Check", StringComparison.OrdinalIgnoreCase));

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
                    ChequeDate = VM.ChequeDate,
                    RefNo = VM.RefNo,
                    PaymentModeId = VM.PaymentModeId,
                    ClearedDate = isChequeMode ? null : (VM.Date ?? DateTime.Now),
                    SelectedSalesIds = selectedAppointments != null && selectedAppointments.Any() ? string.Join(",", selectedAppointments) : null,
                    SelectedPurchaseIds = selectedPurchases != null && selectedPurchases.Any() ? string.Join(",", selectedPurchases) : null
                };
                await _paymentvoucherservice.Create(model);

                if (!isChequeMode)
                {
                    decimal remainingAmount = VM.Amount;

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
                                purchase.PaidAmount;

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
                            purchase.PaidAmount += paymentToApply;

                            purchase.Balance =
                                purchase.TotalPayable -
                                purchase.PaidAmount;

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
                VM.ChequeDate = model.ChequeDate;
                VM.RefNo = model.RefNo;
            }
            ViewBag.SupplierList = new SelectList(await _supplierservice.GetALL(), "Id", "FirstName");
            ViewBag.PaymentCategorylist = new SelectList(await _paymentvouchercategoryservice.GetAll(), "Id", "Name");
            ViewBag.CustomerList = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.EmployeeList = new SelectList(await _employeeservice.GetAll(), "Id", "Name"); 

            var paymentModes = (await _modeofpaymentservice.GetAll())
                             .Select(x => new
                             {
                                 Id = x.Id,
                                 BankModeOfPaymentName = $"{x.Name} - {x.Bank?.BankName}"
                             }).ToList();

            ViewBag.PaymentModeList = new SelectList(paymentModes,
                "Id",
                "BankModeOfPaymentName"
            );
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
                .Where(x => x.SupplierId == venderId && (x.TotalPayable - x.PaidAmount) > 0)
                .Select(x => new
                {
                    id = x.Id,
                    purchaseNo = x.BillNo,
                    purchaseDate = x.BillDate,
                    supplierName = x.Suppliers?.FirstName ?? string.Empty,
                    totalAmount = x.Total,
                    totalpayable = x.TotalPayable,
                    paidAmount = x.PaidAmount,

                    balance = (x.TotalPayable) - (x.PaidAmount)
                })
                .Where(p => p.balance > 0)
                .ToList();

            return Json(new { success = true, data = result });
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyBalance(string type, int id)
        {
            decimal balance = 0;

            if (string.IsNullOrEmpty(type))
                return Json(new { success = false, balance = 0 });

            type = type.Trim().ToLowerInvariant();

            try
            {
                if (type == "customer")
                {
                    var sales = (await _salesRepo.GetAll())
                        .Where(x => x.CustomerId == id)
                        .ToList();

                    var totalDebit = sales.Sum(x => x.TotalPayable);
                    var totalCredit = sales.Sum(x => x.SalsePaymentDetails?.Sum(p => p.Amount) ?? 0);

                    var stockReturns = (await _stockReturnRepo.GetAll())
                        .Where(s => s.CustomerId == id)
                        .ToList();
                    totalCredit += stockReturns.Sum(x => x.TotalPayable);

                    balance = totalDebit - totalCredit;
                }
                else if (type == "supplier")
                {
                    var purchases = (await _purchaseRepo.GetAll())
                        .Where(x => x.SupplierId == id)
                        .ToList();

                    var totalCredit = purchases.Sum(x => x.TotalPayable);
                    var totalDebit = purchases.Sum(x => x.PaymentDetails?.Sum(p => p.Amount) ?? 0);

                    var purchaseReturns = (await _purchaseReturnRepo.GetAll())
                        .Where(p => p.SupplierId == id)
                        .ToList();
                    totalDebit += purchaseReturns.Sum(x => x.TotalPayable);

                    balance = totalCredit - totalDebit;
                }
                else if (type == "staff" || type == "employee")
                {
                    var totalPayments = (await _paymentvoucherservice.GetAll())
                        .Where(pv => pv.EmployeeId == id)
                        .Sum(pv => pv.Amount);

                    var totalReceipts = (await _receiveVoucherRepo.GetAll())
                        .Where(rv => rv.EmployeeId == id)
                        .Sum(rv => rv.NetAmount);

                    balance = totalPayments - totalReceipts;
                }
                else if (type == "bank")
                {
                    var bank = await _bankRepo.GetById(id);
                    if (bank != null)
                    {
                        decimal openingBalance = bank.OpeningBalance;

                        var bankPaymentModeIds = (await _modeofpaymentservice.GetAll())
                            .Where(m => m.BankId == id)
                            .Select(m => m.Id)
                            .ToList();

                        var salesDebit = (await _salsePaymentDetailsRepo.GetAll())
                            .Where(pd => bankPaymentModeIds.Contains(pd.PaymentModeId) && pd.Amount > 0 && pd.SalseId != null 
                                && (pd.Description == null || !pd.Description.Contains("Voucher", StringComparison.OrdinalIgnoreCase)))
                            .Sum(pd => pd.Amount);

                        var rvDebit = (await _receiveVoucherRepo.GetAll())
                            .Where(rv => rv.PaymentModeId.HasValue && bankPaymentModeIds.Contains(rv.PaymentModeId.Value) && rv.NetAmount > 0)
                            .Sum(rv => rv.NetAmount);

                        var contraDebit = (await _contraRepo.GetAll())
                            .Where(c => c.BankId == id && c.Category == AOne.Utility.Enums.ContraCategory.Deposit && c.Amount > 0)
                            .Sum(c => c.Amount);

                        var purchasesCredit = (await _salsePaymentDetailsRepo.GetAll())
                            .Where(pd => bankPaymentModeIds.Contains(pd.PaymentModeId) && pd.Amount > 0 && pd.PurchaseId != null
                                && (pd.Description == null || !pd.Description.Contains("Voucher", StringComparison.OrdinalIgnoreCase)))
                            .Sum(pd => pd.Amount);

                        var pvCredit = (await _paymentvoucherservice.GetAll())
                            .Where(pv => pv.PaymentModeId.HasValue && bankPaymentModeIds.Contains(pv.PaymentModeId.Value) && pv.Amount > 0)
                            .Sum(pv => pv.Amount);

                        var contraCredit = (await _contraRepo.GetAll())
                            .Where(c => c.BankId == id && c.Category == AOne.Utility.Enums.ContraCategory.Withdraw && c.Amount > 0)
                            .Sum(c => c.Amount);

                        balance = openingBalance + salesDebit + rvDebit + contraDebit - purchasesCredit - pvCredit - contraCredit;
                    }
                }
                else if (type == "cash")
                {
                    var cashPaymentModeIds = (await _modeofpaymentservice.GetAll())
                        .Where(m => m.PaymentType == AOne.Utility.Enums.modeofpayment.Cash)
                        .Select(m => m.Id)
                        .ToList();

                    var salesDebit = (await _salsePaymentDetailsRepo.GetAll())
                        .Where(pd => cashPaymentModeIds.Contains(pd.PaymentModeId) && pd.Amount > 0 && pd.SalseId != null
                            && (pd.Description == null || !pd.Description.Contains("Voucher", StringComparison.OrdinalIgnoreCase)))
                        .Sum(pd => pd.Amount);

                    var rvDebit = (await _receiveVoucherRepo.GetAll())
                        .Where(rv => rv.PaymentModeId.HasValue && cashPaymentModeIds.Contains(rv.PaymentModeId.Value) && rv.NetAmount > 0)
                        .Sum(rv => rv.NetAmount);

                    var contraDebit = (await _contraRepo.GetAll())
                        .Where(c => c.Category == AOne.Utility.Enums.ContraCategory.Withdraw && c.Amount > 0)
                        .Sum(c => c.Amount);

                    var purchasesCredit = (await _salsePaymentDetailsRepo.GetAll())
                        .Where(pd => cashPaymentModeIds.Contains(pd.PaymentModeId) && pd.Amount > 0 && pd.PurchaseId != null
                            && (pd.Description == null || !pd.Description.Contains("Voucher", StringComparison.OrdinalIgnoreCase)))
                        .Sum(pd => pd.Amount);

                    var pvCredit = (await _paymentvoucherservice.GetAll())
                        .Where(pv => pv.PaymentModeId.HasValue && cashPaymentModeIds.Contains(pv.PaymentModeId.Value) && pv.Amount > 0)
                        .Sum(pv => pv.Amount);

                    var contraCredit = (await _contraRepo.GetAll())
                        .Where(c => c.Category == AOne.Utility.Enums.ContraCategory.Deposit && c.Amount > 0)
                        .Sum(c => c.Amount);

                    balance = salesDebit + rvDebit + contraDebit - purchasesCredit - pvCredit - contraCredit;
                }
            }
            catch
            {
                // Safety fallback
            }

            return Json(new { success = true, balance = balance });
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentModeDetails(int id)
        {
            var mode = await _modeofpaymentservice.GetById(id);
            if (mode == null)
            {
                return Json(new { success = false });
            }

            var typeStr = mode.PaymentType == AOne.Utility.Enums.modeofpayment.Cash ? "cash" : "bank";
            return Json(new { success = true, type = typeStr, bankId = mode.BankId });
        }
    }
}
