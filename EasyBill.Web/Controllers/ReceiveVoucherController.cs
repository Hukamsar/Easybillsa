using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class ReceiveVoucherController : Controller
    {
        private readonly IReceiveVoucherRepository _receiveVoucherRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IPaymentVoucherCategoryRepository _paymentVoucherCategoryRepo; 
        private readonly IModeOfPaymentRepository _paymentmodeRepo; 
        private readonly ISupplierRepository _supplierRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly ISalesRepository _salesRepo;
        private readonly IPurchaseRepository _purchaseRepo;
        private readonly ICustomerAdvanceRepository _customerAdvanceRepo;
        private readonly ISupplierAdvanceRepository _supplierAdvanceRepo;
        public ReceiveVoucherController(
            IReceiveVoucherRepository receiveVoucherRepo,
            ICustomerRepository customerRepo,
            IPaymentVoucherCategoryRepository paymentVoucherCategory,
            IModeOfPaymentRepository paymentModeRepo,
            ISupplierRepository supplierRepo,
            IEmployeeRepository employeeRepo,
            ISalesRepository salesRepo,
            IPurchaseRepository purchaseRepo,
            ICustomerAdvanceRepository customerAdvanceRepo,
            ISupplierAdvanceRepository supplierAdvanceRepo)
        {
            _receiveVoucherRepo = receiveVoucherRepo;
            _customerRepo = customerRepo;
            _paymentVoucherCategoryRepo = paymentVoucherCategory; 
            _paymentmodeRepo = paymentModeRepo; 
            _supplierRepo = supplierRepo;
            _employeeRepo = employeeRepo;
            _salesRepo = salesRepo;
            _purchaseRepo = purchaseRepo;
            _customerAdvanceRepo = customerAdvanceRepo;
            _supplierAdvanceRepo = supplierAdvanceRepo;
        }
        public async Task<IActionResult> Index()
        {
            var data = (await _receiveVoucherRepo.GetAll()).OrderByDescending(x => x.Id);
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.CustomerList = new SelectList(await _customerRepo.GetAll(), "Id", "Name");
            ViewBag.SupplierList = new SelectList(await _supplierRepo.GetALL(), "Id", "FirstName");
            ViewBag.EmployeeList = new SelectList(await _employeeRepo.GetAll(), "Id", "Name");
            ViewBag.ExpenseCategories = new SelectList(await _paymentVoucherCategoryRepo.GetAll(), "Id", "Name");
            ViewBag.PaymentModeList = new SelectList(await _paymentmodeRepo.GetAll(), "Id", "Name");
            var ViewModel = new ReceiveVoucherVM();
            ViewModel.Date = DateTime.Today.Date;
            ViewModel.VouncherNo = await GenerateVoucherNumber();

            return View(ViewModel);
        }
        [HttpGet]
        public async Task<IActionResult> ClearanceCreate(int? patientId)
        {
            ViewBag.SupplierList = new SelectList(await _customerRepo.GetAll(), "Id", "Name");
 
            ViewBag.ExpenseCategories = new SelectList(await _paymentVoucherCategoryRepo.GetAll(), "Id", "Name");
            var ViewModel = new ReceiveVoucherVM();
            ViewModel.Date = DateTime.Today.Date;
            ViewModel.PatientId = patientId;
            ViewModel.VouncherNo = await GenerateVoucherNumber();

            return View(ViewModel);
        }
        public async Task<string> GenerateVoucherNumber()
        {
            var data = await _receiveVoucherRepo.GetAll();
            var lastCode = data
                .Where(p => !string.IsNullOrEmpty(p.VouncherNo))
                .Select(p => p.VouncherNo)
                .LastOrDefault();

            if (string.IsNullOrEmpty(lastCode) || lastCode.Length < 2)
                return "RV0001";

            string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray());

            string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

            int number = 0;
            int.TryParse(numberPart, out number);

            string nextCode = prefix + (number + 1).ToString("D" + numberPart.Length);

            return nextCode;
        }
        //[HttpPost]
        //public async Task<IActionResult> Create(ReceiveVoucherVM viewModel)
        //{
        //    if (viewModel == null) return View(viewModel);
        //    string fileName = "";
        //    if (viewModel.UploadAttachments != null && viewModel.UploadAttachments.Length > 0)
        //    {
        //        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Receive Voucher");
        //        if (!Directory.Exists(uploadsFolder))
        //        {
        //            Directory.CreateDirectory(uploadsFolder);
        //        }

        //        fileName = viewModel.UploadAttachments.FileName;
        //        string filePath = Path.Combine(uploadsFolder, fileName);

        //        using (var fileStream = new FileStream(filePath, FileMode.Create))
        //        {
        //            viewModel.UploadAttachments.CopyTo(fileStream);
        //        }
        //        viewModel.Attachments = fileName;
        //    }
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
        //        Attachments = viewModel.Attachments,
        //        Description = viewModel.Description,
        //        PaymentModeId = viewModel.PaymentModeId,
        //    };
        //    await _receiveVoucherRepo.Create(model);

        //    var selectedsales = JsonConvert.DeserializeObject<List<int>>(Request.Form["selectedsales"]);
        //    var selectedPurchases = JsonConvert.DeserializeObject<List<int>>(Request.Form["SelectedPurchases"]);

        //    decimal remainingAmount = viewModel.Amount;

        //    if (viewModel.Party == Party.Customer && selectedsales != null && selectedsales.Count > 0)
        //    {
        //        foreach (var salesId in selectedsales)
        //        {
        //            if (remainingAmount <= 0) break;

        //            var sales = await _salesRepo.GetById(salesId);

        //            if (sales != null && sales.Balance > 0)
        //            {
        //                decimal deduction = Math.Min(sales.Balance, remainingAmount);
        //                remainingAmount -= deduction;

        //                sales.SalsePaymentDetails.Add(new SalsePaymentDetails
        //                {
        //                    PaymentModeId = (int)viewModel.PaymentModeId,
        //                    Amount = deduction,
        //                    Description = "Payment received via Receive Voucher",
        //                    CustomerId = viewModel.CustomerId,
        //                    Date = DateTime.Today,
        //                });

        //                sales.PaidAmount += deduction;
        //                //  sales.Balance = sales.TotalPayable - sales.PaidAmount;
        //                sales.Balance -= deduction;
        //                await _salesRepo.Update(sales);
        //            }
        //        }

        //        // Extra Amount Save as Advance
        //        if (remainingAmount > 0)
        //        {
        //            var existingAdvance = (await _customerAdvanceRepo.GetByCustomerId(viewModel.CustomerId)).FirstOrDefault();

        //            if (existingAdvance != null)
        //            {
        //                existingAdvance.AdvanceAmount += remainingAmount;
        //                await _customerAdvanceRepo.Update(existingAdvance);
        //            }
        //            else
        //            {
        //                var advance = new CustomerAdvance
        //                {
        //                    CustomerId = viewModel.CustomerId ?? 0,
        //                    AdvanceAmount = remainingAmount,
        //                    Date = DateTime.Now,
        //                    Remarks = "Advance received from Receive Voucher"
        //                };
        //                await _customerAdvanceRepo.Create(advance); 
        //            }

        //        }
        //    }
        //    else if (viewModel.Party == Party.Supplier && selectedPurchases != null && selectedPurchases.Count > 0)
        //    {
        //        foreach (var purchaseId in selectedPurchases)
        //        {
        //            if (remainingAmount <= 0) break;

        //            var purchase = await _purchaseRepo.GetById(purchaseId);
        //            if (purchase == null) continue;

        //            decimal balance = (purchase.TotalPayable) - (purchase.PaymentAmt);
        //            decimal paymentToApply = Math.Min(balance, remainingAmount);

        //            purchase.PaymentAmt += paymentToApply;

        //            if (purchase.PaymentAmt >= purchase.TotalPayable)
        //            {
        //                purchase.PaymentStatus = "Paid";
        //            }
        //            else
        //            {
        //                purchase.PaymentStatus = "Partial";
        //            }

        //            await _purchaseRepo.Update(purchase);
        //            remainingAmount -= paymentToApply;
        //        }
        //    }
        //    return Json(new { success = true });
        //}

        [HttpPost]
        public async Task<IActionResult> Create(ReceiveVoucherVM viewModel)
        {
            if (viewModel == null)
                return View(viewModel);

            string fileName = "";

            #region Upload Attachment

            if (viewModel.UploadAttachments != null &&
                viewModel.UploadAttachments.Length > 0)
            {
                string uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "docs",
                    "Receive Voucher");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                fileName = viewModel.UploadAttachments.FileName;

                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.UploadAttachments.CopyToAsync(fileStream);
                }

                viewModel.Attachments = fileName;
            }

            #endregion

            #region Save Receive Voucher

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
                Attachments = viewModel.Attachments,
                Description = viewModel.Description,
                PaymentModeId = viewModel.PaymentModeId,
            };

            await _receiveVoucherRepo.Create(model);

            #endregion

            var selectedsales =
                JsonConvert.DeserializeObject<List<int>>(
                    Request.Form["selectedsales"]);

            var selectedPurchases =
                JsonConvert.DeserializeObject<List<int>>(
                    Request.Form["SelectedPurchases"]);

            decimal remainingAmount = viewModel.Amount;

            #region CUSTOMER PAYMENT

            if (viewModel.Party == Party.Customer)
            {
                // ============================================
                // AUTO ADJUST IF NO BILL SELECTED
                // ============================================

                var customerAdvance =
                                    (await _customerAdvanceRepo
                                    .GetByCustomerId(viewModel.CustomerId))
                                    .FirstOrDefault();

                decimal oldCustomerAdvance = 0;

                if (customerAdvance != null &&
                    customerAdvance.AdvanceAmount > 0)
                {
                    oldCustomerAdvance = customerAdvance.AdvanceAmount;

                    remainingAmount += oldCustomerAdvance;
                }

                if (selectedsales == null || selectedsales.Count == 0)
                {
                    var pendingBills =
                        await _salesRepo.GetPendingBillsByCustomerId(
                            viewModel.CustomerId);

                    selectedsales = pendingBills
                        .Where(x => x.Balance > 0)
                        .OrderBy(x => x.BillDate)
                        .Select(x => x.Id)
                        .ToList();
                }

                // ============================================
                // BILL ADJUSTMENT
                // ============================================

                if (selectedsales != null && selectedsales.Count > 0)
                {
                    foreach (var salesId in selectedsales)
                    {
                        if (remainingAmount <= 0)
                            break;

                        var sales = await _salesRepo.GetById(salesId);

                        if (sales == null)
                            continue;

                        if (sales.Balance <= 0)
                            continue;

                        decimal deduction =
                            Math.Min(sales.Balance, remainingAmount);

                        remainingAmount -= deduction;

                        if (sales.SalsePaymentDetails == null)
                        {
                            sales.SalsePaymentDetails = new List<SalsePaymentDetails>();
                        }

                        // PAYMENT HISTORY

                        sales.SalsePaymentDetails.Add(
                            new SalsePaymentDetails
                            {
                                PaymentModeId = viewModel.PaymentModeId ?? 1,
                                Amount = deduction,
                                Description =
                                    "Payment received via Receive Voucher " + viewModel.VouncherNo,
                                CustomerId = viewModel.CustomerId,
                                Date = viewModel.Date,
                            });

                        // UPDATE SALES

                        sales.PaidAmount += deduction;

                        sales.Balance -= deduction;

                        // PAYMENT STATUS

                        if (sales.Balance <= 0)
                        {
                            sales.PaymentStatus = "Paid";
                        }
                        else
                        {
                            sales.PaymentStatus = "Partial";
                        }

                        await _salesRepo.Update(sales);
                    }
                }

                // ============================================
                // SAVE EXTRA AMOUNT AS ADVANCE
                // ============================================

                if (customerAdvance != null)
                {
                    customerAdvance.AdvanceAmount = remainingAmount;

                    await _customerAdvanceRepo.Update(customerAdvance);
                }
                else if (remainingAmount > 0)
                {
                    var advance = new CustomerAdvance
                    {
                        CustomerId = viewModel.CustomerId ?? 0,
                        AdvanceAmount = remainingAmount,
                        Date = DateTime.Now,
                        Remarks = "Advance received from Receive Voucher"
                    };

                    await _customerAdvanceRepo.Create(advance);
                }
            }

            #endregion

            #region SUPPLIER PAYMENT

            else if (viewModel.Party == Party.Supplier)
            {
                // ============================================
                // AUTO ADJUST PURCHASE IF NOT SELECTED
                // ============================================
                var supplierAdvance =
                                      (await _supplierAdvanceRepo
                                      .GetBySupplierId(viewModel.SupplierId))
                                      .FirstOrDefault();

                decimal oldSupplierAdvance = 0;

                if (supplierAdvance != null &&
                    supplierAdvance.AdvanceAmount > 0)
                {
                    oldSupplierAdvance = supplierAdvance.AdvanceAmount;

                    remainingAmount += oldSupplierAdvance;
                }

                if (selectedPurchases == null ||
                    selectedPurchases.Count == 0)
                {
                    var pendingPurchases =
                        await _purchaseRepo
                        .GetPendingBillsBySupplierId(
                            viewModel.SupplierId);

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

                if (selectedPurchases != null &&
                    selectedPurchases.Count > 0)
                {
                    foreach (var purchaseId in selectedPurchases)
                    {
                        if (remainingAmount <= 0)
                            break;

                        var purchase =
                            await _purchaseRepo.GetById(purchaseId);

                        if (purchase == null)
                            continue;

                        decimal balance =
                            purchase.TotalPayable - purchase.PaymentAmt;

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
                                PaymentModeId = viewModel.PaymentModeId ?? 1,
                                Amount = paymentToApply,
                                Description =
                                    "Payment paid via Receive Voucher " + viewModel.VouncherNo,
                                Date = viewModel.Date
                            });

                        purchase.PaymentAmt += paymentToApply;

                        remainingAmount -= paymentToApply;

                        if (purchase.PaymentAmt >= purchase.TotalPayable)
                        {
                            purchase.PaymentStatus = "Paid";
                        }
                        else
                        {
                            purchase.PaymentStatus = "Partial";
                        }

                        await _purchaseRepo.Update(purchase);
                    }
                }

                // ============================================
                // SAVE EXTRA AS SUPPLIER ADVANCE
                // ============================================

                if (supplierAdvance != null)
                {
                    supplierAdvance.AdvanceAmount = remainingAmount;

                    await _supplierAdvanceRepo.Update(supplierAdvance);
                }
                else if (remainingAmount > 0)
                {
                    var advance = new SupplierAdvance
                    {
                        SupplierId = viewModel.SupplierId ?? 0,
                        AdvanceAmount = remainingAmount,
                        Date = DateTime.Now,
                        Remarks = "Advance paid from Receive Voucher"
                    };

                    await _supplierAdvanceRepo.Create(advance);
                }
            }
            //else if (viewModel.Party == Party.Supplier && selectedPurchases != null && selectedPurchases.Count > 0)
            //{
            //    foreach (var purchaseId in selectedPurchases)
            //    {
            //        if (remainingAmount <= 0) break;

            //        var purchase = await _purchaseRepo.GetById(purchaseId);
            //        if (purchase == null) continue;

            //        decimal balance = (purchase.TotalPayable) - (purchase.PaymentAmt);
            //        decimal paymentToApply = Math.Min(balance, remainingAmount);

            //        purchase.PaymentAmt += paymentToApply;

            //        if (purchase.PaymentAmt >= purchase.TotalPayable)
            //        {
            //            purchase.PaymentStatus = "Paid";
            //        }
            //        else
            //        {
            //            purchase.PaymentStatus = "Partial";
            //        }

            //        await _purchaseRepo.Update(purchase);
            //        remainingAmount -= paymentToApply;
            //    }
            //}
            #endregion

            return Json(new
            {
                success = true,
                message = "Voucher saved successfully"
            });
        }
        [HttpPost]
        public async Task<IActionResult> ClearanceCreate(ReceiveVoucherVM viewModel)
        {
            if (viewModel == null) return View(viewModel);
            string fileName = "";
            if (viewModel.UploadAttachments != null && viewModel.UploadAttachments.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Receive Voucher");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                fileName = viewModel.UploadAttachments.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    viewModel.UploadAttachments.CopyTo(fileStream);
                }
                viewModel.Attachments = fileName;
            }
            var model = new ReceiveVoucher()
            {
                VouncherNo = viewModel.VouncherNo,
                Date = viewModel.Date,
                Party = viewModel.Party,
               // PatientId = viewModel.PatientId,
                CustomerId = viewModel.CustomerId,
                SupplierId = viewModel.SupplierId,
                EmployeeId = viewModel.EmployeeId,
                PaymentCategoryId = viewModel.CategoryId,
                PaymentModeId = viewModel.PaymentModeId,
                Amount = viewModel.Amount,
                Attachments = viewModel.Attachments,
                Description = viewModel.Description,
            };
            await _receiveVoucherRepo.Create(model);
            TempData["success"] = "Saved successfully!.";
            return RedirectToAction("Index", "Clearance");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            ReceiveVoucher model = await _receiveVoucherRepo.GetById(Id);
         
            ReceiveVoucherVM VM = new ReceiveVoucherVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.VouncherNo = model.VouncherNo;
                VM.Date = model.Date;
                VM.Party = model.Party;
               // VM.PatientId = model.PatientId;
                VM.CustomerId = model.CustomerId;
                VM.SupplierId = model.SupplierId;
                VM.EmployeeId = model.EmployeeId;
                VM.CategoryId = model.PaymentCategoryId;
                VM.Amount = model.Amount;
                VM.Attachments = model.Attachments;
                VM.Description = model.Description;
                VM.PaymentModeId = model.PaymentModeId;
            }
            ViewBag.CustomerList = new SelectList(await _customerRepo.GetAll(), "Id", "Name");
            ViewBag.SupplierList = new SelectList(await _supplierRepo.GetALL(), "Id", "FirstName");
            ViewBag.EmployeeList = new SelectList(await _employeeRepo.GetAll(), "Id", "Name");
            ViewBag.ExpenseCategories = new SelectList(await _paymentVoucherCategoryRepo.GetAll(), "Id", "Name");
            ViewBag.PaymentModeList = new SelectList(await _paymentmodeRepo.GetAll(), "Id", "Name");
            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(ReceiveVoucherVM VM)
        {
            ReceiveVoucher model = await _receiveVoucherRepo.GetById(VM.Id);
            string fileName = "";
            if (VM.UploadAttachments != null && VM.UploadAttachments.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Receive Voucher");
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
                model.Party = VM.Party;
              //  model.PatientId = VM.PatientId;
                model.CustomerId = VM.CustomerId;
                model.SupplierId = VM.SupplierId;
                model.EmployeeId = VM.EmployeeId;
                model.PaymentCategoryId = VM.CategoryId;
                model.Amount = VM.Amount;
                model.Attachments = VM.Attachments;
                model.Description = VM.Description;
                model.PaymentModeId = VM.PaymentModeId;
                await _receiveVoucherRepo.Update(model);
                //var selectedAppointments = JsonConvert.DeserializeObject<List<int>>(Request.Form["SelectedAppointments"]);
                //decimal remainingAmount = VM.Amount;

                //foreach (var bookingId in selectedAppointments)
                //{
                //    if (remainingAmount <= 0) break;

                //    var booking = await _testbookingRepo.GetById(bookingId);

                //    if (booking != null && booking.Balance > 0)
                //    {
                //        decimal deduction = Math.Min(booking.Balance, remainingAmount);
                //        remainingAmount -= deduction;



                //        booking.PaymentDetails.Add(new PaymentDetails
                //        {
                //            PaymentModeId = (int)VM.PaymentModeId,
                //            Amount = deduction,
                //            Description = "Payment received via Receive Voucher",
                //            PatientId = VM.PatientId,
                //            Date = DateTime.Today,
                //        });

                //        booking.PaidAmount += deduction;
                //        booking.Balance = booking.TotalPayable - booking.PaidAmount;

                //        await _testbookingRepo.Update(booking);
                //    }
                //}
            }
            return Json(new { success = true });
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
                var model = await _receiveVoucherRepo.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Expense not found." });
                }
                await _receiveVoucherRepo.Delete(model);

                return Json(new { success = true, message = "Expense deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> RefundReport(DateTime? startDate, DateTime? endDate, int? patientId, int? vendorId)
        {
            IEnumerable<ReceiveVoucher> allExpense = await _receiveVoucherRepo.GetAll();

            if (startDate.HasValue)
                allExpense = allExpense.Where(x => x.Date.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                allExpense = allExpense.Where(x => x.Date.Date <= endDate.Value.Date);
            //if (patientId.HasValue)
            //    allExpense = allExpense.Where(x => x.PatientId == patientId);
            if (vendorId.HasValue)
                allExpense = allExpense.Where(x => x.CustomerId == vendorId);

           // ViewBag.Patients = new SelectList(await _patientService.GetAll(), "Id", "Name");
          //  ViewBag.VenderList = new SelectList(await _supplierService.GetAll(), "Id", "CompanyName");

            var expenseVM = allExpense.Select(inv => new ReceiveVoucherVM
            {
                VouncherNo = inv.VouncherNo,
                Date = inv.Date,
                Party = inv.Party,
                //PatientId = inv.PatientId,
                //Patient = inv.Patient?.Name ?? "",
                CustomerId = inv.CustomerId,
               // Vender = inv.Vender?.CompanyName ?? "",
               // Category = inv.Category?.Name,
                Amount = inv.Amount,
                Attachments = inv.Attachments

            }).ToList();
            return View(expenseVM);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetPendingBalanceByPatient(int patientId)
        //{
        //    var bookings = (await _testbookingRepo.GetAll())
        //        .Where(x => x.PatientId == patientId)
        //        .ToList();

        //    if (bookings == null || bookings.Count == 0)
        //        return Json(new { success = false, totalBalance = 0, data = new List<object>() });

        //    var pendingbookings = bookings
        //        .Where(a => a.Balance > 0)
        //        .ToList();

        //    if (pendingbookings.Count == 0)
        //        return Json(new { success = false, totalBalance = 0, data = new List<object>() });


        //    var appointmentData = pendingbookings.Select(a => new
        //    {
        //        a.Id,
        //        a.Code,
        //        a.Date,
        //        PatientName = a.Patient?.Name ?? "",
        //        Age = a.Patient?.Age ?? 0,
        //        Gender = a.Patient?.Gender ?? null,
        //        PayableAmount = a.TotalPayable,
        //        PaidAmount = a.PaidAmount,
        //        Balance = a.Balance
        //    });

        //    return Json(new
        //    {
        //        success = true,
        //        data = appointmentData
        //    });
        //}

        [HttpGet]
        public async Task<IActionResult> GetSuppliersByGroup(string type)
        {
            var suppliers = await _supplierRepo.GetALL();

            if (type == "Supplier")
            {
                suppliers = suppliers.Where(x => x.AccountGroup != null && x.AccountGroup.Name.Contains("Sundry Creditor")).ToList();
            }
            else if (type == "Other")
            {
                suppliers = suppliers.Where(x => x.AccountGroup == null || !x.AccountGroup.Name.Contains("Sundry Creditor")).ToList();
            }

            var result = suppliers.Select(x => new
            {
                id = x.Id,
                name = x.FirstName
            });

            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetSaleData(int customerId)
        {
            var sales = (await _salesRepo.GetAll())
                .Where(x => x.CustomerId == customerId)
                .ToList();

            if (!sales.Any())
                return Json(new { success = false, data = new List<object>() });

            // ✅ Sirf jinka balance > 0 hai
            var pendingSales = sales
                .Where(x => (x.TotalPayable - (x.PaidAmount)) > 0)
                .Select(x => new
                {
                    id = x.Id,
                    billno = x.BillNo,
                    billdate = x.BillDate,
                    customername = x.Customers?.Name ?? "",
                    phone = x.Customers?.PhoneNo ?? "",
                    payableAmount = x.TotalPayable,
                    paidAmount = x.PaidAmount,
                    balance = x.TotalPayable - (x.PaidAmount)
                })
                .ToList();

            if (!pendingSales.Any())
                return Json(new { success = false, data = new List<object>() });

            return Json(new
            {
                success = true,
                data = pendingSales
            });
        }


    }
}
