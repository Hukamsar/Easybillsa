using AOne.DataAccess.ProfileService;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using EasyBill.Models.Entity;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Globalization;
using System.Text.RegularExpressions;
using Table = iText.Layout.Element.Table;
namespace EasyBill.UI.Controllers
{
    public class StockIssueController : Controller
    {
        private readonly IStockIssueRepository _stockissueservice;
        private readonly IProfileService _profileService;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IPurchaseItemRepository _purchaseitemservice;
        private readonly UserManager<ApplicationUsers> _usersManager;
        private readonly ITenantRegistrationRepository _tenantRepository;
        private readonly IStockService _currentstockService;
        private readonly ISalseSettingRepository _salsesettingservice;
        private readonly IModeOfPaymentRepository _modeofpaymentservice;
        public StockIssueController(
            IStockIssueRepository stockissueservice, 
            IProfileService profileService, 
            ICustomerRepository customerservice,
            IItemMasterRepository itemmasterservice,
            IPurchaseItemRepository purchaseitemservice,
            UserManager<ApplicationUsers> userManager,
            ITenantRegistrationRepository tenantRepository,
            IStockService currentstockService,
            ISalseSettingRepository salsesettingservice,
            IModeOfPaymentRepository modeofpaymentservice
            )
        {
            _usersManager = userManager;
            _stockissueservice = stockissueservice;
            _profileService = profileService;
            _customerservice = customerservice;
            _itemmasterservice = itemmasterservice;
            _purchaseitemservice = purchaseitemservice;
            _tenantRepository=tenantRepository;
            _currentstockService = currentstockService;
            _salsesettingservice = salsesettingservice;
            _modeofpaymentservice = modeofpaymentservice;
        }
        public async Task<IActionResult> Index()
        {
            await _profileService.Set(User);
            var data = await _stockissueservice.GetAll();
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create(string? returnUrl)
        {
            ViewBag.ReturnUrl=returnUrl;
            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            ViewBag.Items = new SelectList(
                (await _itemmasterservice.GetAll())
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name),
                "Id",
                "Name");

            var tenantId = User?.FindFirst("TenantId")?.Value;
            var tenantdata = (await _tenantRepository.GetAll()).FirstOrDefault(x => x.Id == tenantId);
            ViewBag.BusinessType = (int)(tenantdata?.BusinessType ?? 0);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            ViewBag.HasSetting = setting != null;

            var vm = new StockIssueVM
            {
                ChallanDate = DateTime.Today,
                ChallanNo = await GenerateNxtNumber(),
                billingType = "walkin",
                PaymentType = "cash",
                TaxCalculation = "Yes",
                DoctorRequired = setting?.DoctorRequired ?? false
            };
            return View(vm);
        }
        public async Task<string> GenerateNxtNumber()
        {
            var nextNumber = (await _stockissueservice.GetAll())
                .Where(x => !string.IsNullOrWhiteSpace(x.ChallanNo))
                .Select(x =>
                {
                    var digits = new string((x.ChallanNo ?? string.Empty)
                        .Trim()
                        .Reverse()
                        .TakeWhile(char.IsDigit)
                        .Reverse()
                        .ToArray());

                    return int.TryParse(digits, out var number) ? number : 0;
                })
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"CH{nextNumber:D4}";
        }

        [HttpPost]
        public async Task<IActionResult> Create(StockIssueVM Vm, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);
            // 1. Validation check (Same as SalesController)
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Message = x.Value.Errors.First().ErrorMessage })
                    .ToList();

                return Json(new { success = false, message = errors.FirstOrDefault()?.Message ?? "Validation failed.", errors });
            }

            // 2. Duplicate Challan Check
            var data = await _stockissueservice.GetAll();
            var challanNo = Vm.ChallanNo?.Trim();
            if (data.Any(x => x.ChallanNo != null && x.ChallanNo.Trim().Equals(challanNo, StringComparison.OrdinalIgnoreCase)))
            {
                return Json(new { success = false, message = $"{challanNo} - This Challan Number already exists." });
            }

            // 3. Customer Validation
            if (Vm.billingType == "registered")
            {
                if (string.IsNullOrWhiteSpace(Vm.MobileNo))
                    return Json(new { success = false, message = "Mobile number is required for registered customer." });

                var customerList = await _customerservice.GetAll();
                if (!customerList.Any(x => !string.IsNullOrWhiteSpace(x.PhoneNo) && x.PhoneNo.Trim() == Vm.MobileNo.Trim()))
                    return Json(new { success = false, message = "Customer does not exist. Please create customer first." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";
            var itemMasters = (await _itemmasterservice.GetAll()).Where(x => x.IsActive).ToList();

            // 4. Mapping View-Model to Entity
            var model = new StockIssue
            {
                CustomerId = Vm.billingType == "registered" ? Vm.CustomerId : null,
                MobileNo = Vm.billingType == "registered" ? Vm.MobileNo : null,
                Address = Vm.billingType == "registered" ? Vm.Address : null,
                billingType = Vm.billingType == "registered" ? Vm.billingType : "Cash",
                PaymentType = Vm.PaymentType,
                ChallanDate = Vm.ChallanDate,
                ChallanNo = Vm.ChallanNo,
                PharmacyDoctorId = Vm.PharmacyDoctorId,
                DoctorMobileNumber = Vm.DoctorMobileNumber,
                DoctorRegNumber = Vm.DoctorRegNumber,
                Total = Vm.Total,
                TotalGstAmt = Vm.TaxCalculation == "No" ? 0 : Vm.TotalGstAmt,
                TotalCessAmount = Vm.TaxCalculation == "No" ? 0M : (Vm.TotalCessAmount ?? 0M),
                discountPercent = Vm.discountPercent,
                discountAmount = Vm.discountAmount,
                Totaldiscount = Vm.Totaldiscount,
                TotalPayable = Vm.TotalPayable,
                RoundOffAmount = Vm.RoundOffAmount ?? 0M,
                PaidAmount = Vm.PaidAmount ?? 0M,
                ReturnAmount = Math.Max(0, (Vm.PaidAmount ?? 0M) - Vm.TotalPayable),
                NetCollection = (Vm.PaidAmount ?? 0M) - Math.Max(0, (Vm.PaidAmount ?? 0M) - Vm.TotalPayable),
                Balance = Vm.Balance ?? 0M,
                TaxCalculation = Vm.TaxCalculation,

                StockIssuesItems = Vm.StockIssuesItemVMs?.Where(x => x.ItemMasterId > 0).Select(x =>
                {
                    var item = itemMasters.FirstOrDefault(i => i.Id == x.ItemMasterId);
                    decimal finalQty = x.Qty;
                    if (isTabletWise && item != null && item.Conversion > 0)
                    {
                        finalQty = x.Qty + (x.TabletQty / item.Conversion);
                    }

                    return new StockIssueItem
                    {
                        ItemMasterId = x.ItemMasterId,
                        HsnId = x.HsnId,
                        HsnCode = x.HsnCode,
                        PurchaseItemId = x.PurchaseItemId > 0 ? x.PurchaseItemId : null,
                        Batch = x.Batch,
                        Expirydate = x.Expirydate,
                        Mrp = x.Mrp,
                        Qty = finalQty,
                        Rate = x.Rate,
                        StripRate = x.StripRate ?? 0M,
                        Gst = x.Gst,
                        IGst = x.IGst,
                        CGst = x.CGst,
                        SGst = x.SGst,
                        Cess = Vm.TaxCalculation == "No" ? 0M : (x.Cess ?? 0M),
                        Discount = x.Discount,
                        Amount = x.Amount,
                        IsSoldInTablets = isTabletWise
                    };
                }).ToList() ?? new List<StockIssueItem>(),

                SalsePaymentDetails = Vm.SalsePaymentDetails?.Where(x => x.PaymentModeId > 0).Select(pd => new SalsePaymentDetails
                {
                    PaymentModeId = pd.PaymentModeId,
                    Amount = pd.Amount,
                    ReferenceNo = pd.ReferenceNo,
                    Description = pd.Description,
                    CustomerId = Vm.billingType == "registered" ? Vm.CustomerId : null
                }).ToList() ?? new List<SalsePaymentDetails>()
            };

            try
            {
                // 5. Update Stock
                foreach (var item in model.StockIssuesItems)
                {
                    await _currentstockService.UpdateStock(item.ItemMasterId, item.Batch?.Trim() ?? "", -item.Qty, item.Expirydate, item.Mrp, item.Rate);
                }

                var createdIssue = await _stockissueservice.Create(model);
                return Json(new { success = true, id = createdIssue.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Something went wrong." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id, string? returnUrl)
        {
            ViewBag.ReturnUrl=returnUrl;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            StockIssue model = await _stockissueservice.GetById(Id);
            var vm = new StockIssueVM
            {
                ChallanDate = DateTime.Today,
                ChallanNo = await GenerateNxtNumber(),
                billingType = "walkin",
                PaymentType = "cash",
                TaxCalculation = "Yes",
                DoctorRequired = setting?.DoctorRequired ?? false
            };

            if (model != null)
            {
                var allItems = (await _itemmasterservice.GetAll()).ToDictionary(x => x.Id, x => x);

                vm.Id = model.Id;
                vm.CustomerId = model.CustomerId;
                vm.CustomerName = model.Customers?.Name;
                vm.ChallanNo = model.ChallanNo;
                vm.ChallanDate = model.ChallanDate;
                vm.MobileNo = model.MobileNo;
                vm.Address = model.Address;
                vm.PharmacyDoctorId = model.PharmacyDoctorId;
                vm.DoctorName = model.PharmacyDoctor?.Name;
                vm.DoctorRegNumber = model.DoctorRegNumber;
                vm.DoctorMobileNumber = model.DoctorMobileNumber;
                vm.Total = model.Total;
                vm.TotalGstAmt = model.TotalGstAmt;
                vm.TotalCessAmount = model.TotalCessAmount;
                vm.discountPercent = model.discountPercent;
                vm.discountAmount = model.discountAmount;
                vm.Totaldiscount = model.Totaldiscount;
                vm.TotalPayable = model.TotalPayable;
                vm.billingType = model.billingType == "Cash" ? "walkin" : "registered";
                vm.PaymentType = model.PaymentType;
                vm.PaidAmount = model.PaidAmount;
                vm.ReturnAmount = model.ReturnAmount;
                vm.Balance = model.Balance;
                vm.RoundOffAmount = model.RoundOffAmount;
                vm.NetCollection = model.NetCollection;
                vm.TaxCalculation = string.Equals(model.TaxCalculation, "No", StringComparison.OrdinalIgnoreCase) ? "No" : "Yes";
                vm.StockIssuesItemVMs = model.StockIssuesItems != null
                    ? model.StockIssuesItems.Select(x =>
                    {
                        decimal displayQty = x.Qty;
                        decimal displayTabletQty = 0;

                        if (isTabletWise && allItems.TryGetValue(x.ItemMasterId, out var itemMaster))
                        {
                            int conversion = itemMaster.Conversion > 0 ? itemMaster.Conversion : 1;
                            int totalTablets = (int)Math.Round(x.Qty * conversion, MidpointRounding.AwayFromZero);
                            displayQty = totalTablets / conversion;
                            displayTabletQty = totalTablets % conversion;
                        }

                        return new StockIssueItemVM
                        {
                            Id = x.Id,
                            StockIssueId = x.StockIssueId,
                            ItemMasterId = x.ItemMasterId,
                            HsnId = x.HsnId,
                            HsnCode = x.HsnCode,
                            ItemName = allItems.TryGetValue(x.ItemMasterId, out var item) ? item.Name : x.ItemMaster?.Name,
                            ItemBarcodeNumber = allItems.TryGetValue(x.ItemMasterId, out var itemByBarcode) ? itemByBarcode.Name : x.ItemMaster?.Name,
                            PurchaseItemId = x.PurchaseItemId,
                            Batch = x.Batch,
                            Expirydate = x.Expirydate,
                            Mrp = x.Mrp,
                            Qty = displayQty,
                            TabletQty = displayTabletQty,
                            Rate = x.Rate,
                            StripRate = x.StripRate,
                            Gst = x.Gst,
                            IGst = x.IGst,
                            CGst = x.CGst,
                            SGst = x.SGst,
                            Cess = x.Cess,
                            Discount = x.Discount,
                            Amount = x.Amount,
                            IsSoldInTablets = x.IsSoldInTablets
                        };
                    }).ToList()
                    : new List<StockIssueItemVM>();

                vm.SalsePaymentDetails = model.SalsePaymentDetails?.Select(pd => new SalsePaymentDetailsVM
                {
                    Id = pd.Id,
                    PaymentModeId = pd.PaymentModeId,
                    Amount = pd.Amount,
                    ReferenceNo = pd.ReferenceNo,
                    Description = pd.Description,
                    CustomerId = pd.CustomerId,
                    StockIssueId = pd.StockIssueId
                }).ToList() ?? new List<SalsePaymentDetailsVM>();
            }

            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.Items = new SelectList(
                (await _itemmasterservice.GetAll())
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name),
                "Id",
                "Name");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");

            var tenantId = User?.FindFirst("TenantId")?.Value;
            var tenantdata = (await _tenantRepository.GetAll()).FirstOrDefault(x => x.Id == tenantId);
            ViewBag.BusinessType = (int)(tenantdata?.BusinessType ?? 0);
            ViewBag.HasSetting = setting != null;

            return View("Create", vm);
        }

        //[HttpPost]
        //public async Task<IActionResult> Edit(StockIssueVM VM)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
        //        ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
        //        return View(VM);
        //    }

        //    StockIssue model = await _stockissueservice.GetById(VM.Id);
        //    if (model != null)
        //    {
        //        model.CustomerId = VM.CustomerId;
        //        model.ChallanDate = VM.ChallanDate;
        //        model.ChallanNo = VM.ChallanNo;
        //        model.MobileNo = VM.MobileNo;
        //        model.Address = VM.Address;
        //        model.PharmacyDoctorId = VM.PharmacyDoctorId;
        //        model.DoctorMobileNumber = VM.DoctorMobileNumber;
        //        model.DoctorRegNumber = VM.DoctorRegNumber;
        //        model.Total = VM.Total;
        //        model.TotalGstAmt = VM.TotalGstAmt;
        //        model.discountPercent = VM.discountPercent;
        //        model.discountAmount = VM.discountAmount;
        //        model.Totaldiscount = VM.Totaldiscount;
        //        model.TotalPayable = VM.TotalPayable;
        //        var removedItems = model.StockIssuesItems.Where(dbItem => !VM.StockIssuesItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

        //        // ❌ Remove Deleted Items
        //        foreach (var item in removedItems)
        //        {
        //            await _currentstockService.UpdateStock(
        //                item.ItemMasterId,
        //                item.Batch?.Trim() ?? "",
        //                +(item.Qty), // 🔥 ADD BACK
        //                item.Expirydate,
        //                item.Mrp,
        //                item.Rate
        //            );

        //            model.StockIssuesItems.Remove(item);
        //        }
        //        foreach (var item in VM.StockIssuesItemVMs)
        //        {
        //            if (item.Id > 0) // Update existing items
        //            {
        //                var existingItem = model.StockIssuesItems.FirstOrDefault(x => x.Id == item.Id);
        //                if (existingItem != null)
        //                {
        //                    decimal oldQty = existingItem.Qty;
        //                    decimal newQty = item.Qty;
        //                    decimal diff = newQty - oldQty;

        //                    // ISSUE → stock minus hota hai
        //                    await _currentstockService.UpdateStock(
        //                        item.ItemMasterId,
        //                        item.Batch?.Trim() ?? "",
        //                        -(diff), // 🔥 VERY IMPORTANT
        //                        item.Expirydate,
        //                        item.Mrp,
        //                        item.Rate
        //                    );
        //                    existingItem.ItemMasterId = item.ItemMasterId;
        //                    existingItem.PurchaseItemId = item.PurchaseItemId;
        //                    existingItem.Batch = item.Batch;
        //                    existingItem.Expirydate = item.Expirydate;
        //                    existingItem.Mrp = item.Mrp;
        //                    existingItem.Qty = item.Qty;
        //                    existingItem.Rate = item.Rate;
        //                    existingItem.Gst = item.Gst;
        //                    existingItem.Discount = item.Discount;
        //                }
        //            }
        //            else // Add new items
        //            {
        //                await _currentstockService.UpdateStock(
        //                    item.ItemMasterId,
        //                    item.Batch?.Trim() ?? "",
        //                    -(item.Qty),
        //                    item.Expirydate,
        //                    item.Mrp,
        //                    item.Rate
        //                );
        //                model.StockIssuesItems.Add(new StockIssueItem
        //                {
        //                    ItemMasterId = item.ItemMasterId,
        //                    PurchaseItemId = item.PurchaseItemId,
        //                    Batch = item.Batch,
        //                    Expirydate = item.Expirydate,
        //                    Mrp = item.Mrp,
        //                    Qty = item.Qty,
        //                    Rate = item.Rate,
        //                    Gst = item.Gst,
        //                    Discount = item.Discount,
        //                    Amount = item.Amount,
        //                });
        //            }
        //        }
        //        await _stockissueservice.Update(model);
        //    }

        //    return RedirectToAction("Index");
        //}
        [HttpPost]
        public async Task<IActionResult> Edit(StockIssueVM VM)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Validation failed. Please check all fields." });
            }

            VM.ChallanNo = VM.ChallanNo?.Trim();
            VM.TaxCalculation = string.Equals(VM.TaxCalculation, "No", StringComparison.OrdinalIgnoreCase) ? "No" : "Yes";

            var duplicateChallan = (await _stockissueservice.GetAll()).Any(x =>
                x.Id != VM.Id &&
                x.ChallanNo != null &&
                x.ChallanNo.Trim().Equals(VM.ChallanNo, StringComparison.OrdinalIgnoreCase));

            if (duplicateChallan)
            {
                return Json(new { success = false, message = $"{VM.ChallanNo} - This Challan Number already exists." });
            }

            if (VM.billingType == "registered")
            {
                if (string.IsNullOrWhiteSpace(VM.MobileNo))
                    return Json(new { success = false, message = "Mobile number is required for registered customer." });

                var customerList = await _customerservice.GetAll();
                if (!customerList.Any(x => !string.IsNullOrWhiteSpace(x.PhoneNo) && x.PhoneNo.Trim() == VM.MobileNo.Trim()))
                    return Json(new { success = false, message = "Customer does not exist. Please create customer first." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";
            var itemMasters = (await _itemmasterservice.GetAll()).Where(x => x.IsActive).ToList();

            StockIssue model = await _stockissueservice.GetById(VM.Id);
            if (model == null) return Json(new { success = false, message = "Record not found." });

            VM.StockIssuesItemVMs = VM.StockIssuesItemVMs?
                .Where(x => x.ItemMasterId > 0)
                .ToList() ?? new List<StockIssueItemVM>();

            if (!VM.StockIssuesItemVMs.Any())
            {
                return Json(new { success = false, message = "Please add at least one item." });
            }

            VM.SalsePaymentDetails = VM.SalsePaymentDetails?
                .Where(x => x.PaymentModeId > 0)
                .ToList() ?? new List<SalsePaymentDetailsVM>();

            // Update Header
            model.CustomerId = VM.billingType == "registered" ? VM.CustomerId : null;
            model.ChallanDate = VM.ChallanDate;
            model.ChallanNo = VM.ChallanNo;
            model.MobileNo = VM.billingType == "registered" ? VM.MobileNo : null;
            model.Address = VM.billingType == "registered" ? VM.Address : null;
            model.billingType = VM.billingType == "registered" ? VM.billingType : "Cash";
            model.PaymentType = VM.PaymentType;
            model.PharmacyDoctorId = VM.PharmacyDoctorId;
            model.DoctorMobileNumber = VM.DoctorMobileNumber;
            model.DoctorRegNumber = VM.DoctorRegNumber;
            model.Total = VM.Total;
            model.TotalGstAmt = VM.TaxCalculation == "No" ? 0 : VM.TotalGstAmt;
            model.TotalCessAmount = VM.TaxCalculation == "No" ? 0M : (VM.TotalCessAmount ?? 0M);
            model.discountPercent = VM.discountPercent;
            model.discountAmount = VM.discountAmount;
            model.Totaldiscount = VM.Totaldiscount;
            model.TotalPayable = VM.TotalPayable;
            model.PaidAmount = VM.PaidAmount ?? 0M;
            model.ReturnAmount = Math.Max(0, (VM.PaidAmount ?? 0M) - VM.TotalPayable);
            model.Balance = VM.Balance ?? 0M;
            model.NetCollection = (VM.PaidAmount ?? 0M) - model.ReturnAmount;
            model.RoundOffAmount = VM.RoundOffAmount ?? 0M;
            model.TaxCalculation = VM.TaxCalculation;

            model.StockIssuesItems ??= new List<StockIssueItem>();
            VM.StockIssuesItemVMs ??= new List<StockIssueItemVM>();

            // 1. Remove Deleted Items & Reverse Stock
            var removedItems = model.StockIssuesItems.Where(dbItem => !VM.StockIssuesItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();
            foreach (var item in removedItems)
            {
                await _currentstockService.UpdateStock(item.ItemMasterId, item.Batch?.Trim() ?? "", item.Qty, item.Expirydate, item.Mrp, item.Rate);
                model.StockIssuesItems.Remove(item);
            }

            // 2. Update/Add Items
            foreach (var item in VM.StockIssuesItemVMs)
            {
                var itemMaster = itemMasters.FirstOrDefault(i => i.Id == item.ItemMasterId);
                decimal finalQty = item.Qty;
                if (isTabletWise && itemMaster != null && itemMaster.Conversion > 0)
                {
                    finalQty = item.Qty + (item.TabletQty / itemMaster.Conversion);
                }

                if (item.Id > 0)
                {
                    var existingItem = model.StockIssuesItems.FirstOrDefault(x => x.Id == item.Id);
                    if (existingItem != null)
                    {
                        // Add old stock back, deduct new stock
                        await _currentstockService.UpdateStock(existingItem.ItemMasterId, existingItem.Batch?.Trim() ?? "", existingItem.Qty, existingItem.Expirydate, existingItem.Mrp, existingItem.Rate);
                        await _currentstockService.UpdateStock(item.ItemMasterId, item.Batch?.Trim() ?? "", -finalQty, item.Expirydate, item.Mrp, item.Rate);

                        existingItem.ItemMasterId = item.ItemMasterId;
                        existingItem.PurchaseItemId = item.PurchaseItemId > 0 ? item.PurchaseItemId : null;
                        existingItem.HsnId = item.HsnId;
                        existingItem.HsnCode = item.HsnCode;
                        existingItem.Batch = item.Batch;
                        existingItem.Expirydate = item.Expirydate;
                        existingItem.Mrp = item.Mrp;
                        existingItem.Qty = finalQty;
                        existingItem.Rate = item.Rate;
                        existingItem.StripRate = item.StripRate ?? 0M;
                        existingItem.Gst = item.Gst;
                        existingItem.IGst = item.IGst;
                        existingItem.CGst = item.CGst;
                        existingItem.SGst = item.SGst;
                        existingItem.Cess = VM.TaxCalculation == "No" ? 0M : (item.Cess ?? 0M);
                        existingItem.Discount = item.Discount;
                        existingItem.Amount = item.Amount;
                        existingItem.IsSoldInTablets = isTabletWise;
                    }
                }
                else
                {
                    await _currentstockService.UpdateStock(item.ItemMasterId, item.Batch?.Trim() ?? "", -finalQty, item.Expirydate, item.Mrp, item.Rate);
                    model.StockIssuesItems.Add(new StockIssueItem
                    {
                        ItemMasterId = item.ItemMasterId,
                        HsnId = item.HsnId,
                        HsnCode = item.HsnCode,
                        PurchaseItemId = item.PurchaseItemId > 0 ? item.PurchaseItemId : null,
                        Batch = item.Batch,
                        Expirydate = item.Expirydate,
                        Mrp = item.Mrp,
                        Qty = finalQty,
                        Rate = item.Rate,
                        StripRate = item.StripRate ?? 0M,
                        Gst = item.Gst,
                        IGst = item.IGst,
                        CGst = item.CGst,
                        SGst = item.SGst,
                        Cess = VM.TaxCalculation == "No" ? 0M : (item.Cess ?? 0M),
                        Discount = item.Discount,
                        Amount = item.Amount,
                        IsSoldInTablets = isTabletWise
                    });
                }
            }

            // 3. Payment Details Updates (Same logic)
            model.SalsePaymentDetails ??= new List<SalsePaymentDetails>();
            VM.SalsePaymentDetails ??= new List<SalsePaymentDetailsVM>();

            var removedPaymentDetails = model.SalsePaymentDetails.Where(dbPayment => !VM.SalsePaymentDetails.Any(vmItem => vmItem.Id == dbPayment.Id)).ToList();
            foreach (var payment in removedPaymentDetails) model.SalsePaymentDetails.Remove(payment);

            foreach (var data in VM.SalsePaymentDetails)
            {
                if (data.Id > 0)
                {
                    var existingPayment = model.SalsePaymentDetails.FirstOrDefault(x => x.Id == data.Id);
                    if (existingPayment != null)
                    {
                        existingPayment.PaymentModeId = data.PaymentModeId;
                        existingPayment.Amount = data.Amount;
                        existingPayment.ReferenceNo = data.ReferenceNo;
                        existingPayment.Description = data.Description;
                        existingPayment.CustomerId = VM.billingType == "registered" ? VM.CustomerId : null;
                    }
                }
                else
                {
                    model.SalsePaymentDetails.Add(new SalsePaymentDetails
                    {
                        PaymentModeId = data.PaymentModeId,
                        Amount = data.Amount,
                        ReferenceNo = data.ReferenceNo,
                        Description = data.Description,
                        CustomerId = VM.billingType == "registered" ? VM.CustomerId : null,
                    });
                }
            }

            await _stockissueservice.Update(model);
            return Json(new { success = true, id = model.Id });
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

                var model = await _stockissueservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                if (model.StockIssuesItems != null)
                {
                    foreach (var item in model.StockIssuesItems)
                    {
                        await _currentstockService.UpdateStock(
                            item.ItemMasterId,
                            item.Batch?.Trim() ?? "",
                            item.Qty,
                            item.Expirydate,
                            item.Mrp,
                            item.Rate);
                    }
                }

                await _stockissueservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetByMobile(string mobile)
        {
            var customer = (await _customerservice.GetAll()).FirstOrDefault(x => x.PhoneNo == mobile);
            if (customer == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                customerId = customer.Id,
                name = customer.Name,
                address = customer.Address
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerBalance(int customerId)
        {
            var totalIssues = (await _stockissueservice.GetAll())
                .Where(x => x.CustomerId == customerId)
                .Sum(x => (decimal?)x.TotalPayable) ?? 0;

            var totalPayments = (await _stockissueservice.GetAll())
                .Where(x => x.CustomerId == customerId)
                .Sum(x => (decimal?)x.PaidAmount) ?? 0;

            return Json(new
            {
                success = true,
                balance = totalIssues - totalPayments
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerVM Vm)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray());

                return BadRequest(new
                {
                    success = false,
                    errors
                });
            }

            var customerList = await _customerservice.GetAll();
            if (!string.IsNullOrEmpty(Vm.PhoneNo))
            {
                var isExist = customerList.Any(x =>
                    !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                    x.PhoneNo.Trim() == Vm.PhoneNo.Trim());

                if (isExist)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Customer's phone number already exists."
                    });
                }
            }

            var customer = new Customer
            {
                Name = Vm.Name,
                PhoneNo = Vm.PhoneNo,
                Address = Vm.Address,
                Email = Vm.Email
            };

            var result = await _customerservice.Create(customer);

            return Ok(new
            {
                success = true,
                data = new
                {
                    result.Id,
                    result.Name,
                    result.PhoneNo,
                    result.Address
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllItems()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var setting = await _salsesettingservice.GetByUserId(userId);

            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToList();

            var currentStocks = await _currentstockService.GetAll();

            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var stockList = itemMasters.Select(item =>
            {
                decimal currentStock = stockDict.ContainsKey(item.Id)
                    ? stockDict[item.Id]
                    : 0;

                string availableQty;

                if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
                {
                    int conversion = item.Conversion;
                    int totalTablets = (int)Math.Round(currentStock * conversion, MidpointRounding.AwayFromZero);
                    int strips = totalTablets / conversion;
                    int tablets = totalTablets % conversion;
                    availableQty = $"{strips}:{tablets}";
                }
                else
                {
                    availableQty = currentStock.ToString("0.##");
                }

                return new
                {
                    id = item.Id,
                    barcode = item.Barcode,
                    code = item.Code,
                    name = item.Name,
                    gst = item.Hsn?.IGST ?? 0,
                    cess = item.Hsn?.Cess ?? 0,
                    hsnId = item.HsnId,
                    hsnCode = item.Hsn?.HsnCode,
                    qty = currentStock,
                    availableQty = availableQty,
                    maximumdiscount = item.MaximumDiscount,
                    allowNegative = setting?.AllowNegative ?? false,
                    minimumQty = item.MinimumQty,
                    conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m,
                    ItemConversion = setting?.ItemConversion ?? "StripWise"
                };
            });

            if (setting != null && !setting.AllowNegative)
            {
                stockList = stockList.Where(x => x.qty > 0);
            }

            ViewBag.ItemConversion = setting?.ItemConversion ?? "StripWise";

            return Json(stockList.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> GetAllItemsForEdit(int stockIssueId = 0)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var setting = await _salsesettingservice.GetByUserId(userId);

            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToList();

            var currentStocks = await _currentstockService.GetAll();

            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var currentIssueItems = new List<StockIssueItem>();
            if (stockIssueId > 0)
            {
                var stockIssue = await _stockissueservice.GetById(stockIssueId);
                if (stockIssue?.StockIssuesItems != null)
                {
                    currentIssueItems = stockIssue.StockIssuesItems.ToList();
                }
            }

            var stockList = itemMasters.Select(item =>
            {
                decimal stock = stockDict.ContainsKey(item.Id)
                    ? stockDict[item.Id]
                    : 0;

                decimal issueQty = currentIssueItems
                    .Where(x => x.ItemMasterId == item.Id)
                    .Sum(x => x.Qty);

                decimal currentStock = stock + issueQty;

                string availableQty;

                if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
                {
                    int conversion = item.Conversion;
                    int totalTablets = (int)Math.Round(currentStock * conversion, MidpointRounding.AwayFromZero);
                    int strips = totalTablets / conversion;
                    int tablets = totalTablets % conversion;
                    availableQty = $"{strips}:{tablets}";
                }
                else
                {
                    availableQty = currentStock.ToString("0.##");
                }

                return new
                {
                    id = item.Id,
                    barcode = item.Barcode,
                    code = item.Code,
                    name = item.Name,
                    gst = item.Hsn?.IGST ?? 0,
                    cess = item.Hsn?.Cess ?? 0,
                    hsnId = item.HsnId,
                    hsnCode = item.Hsn?.HsnCode,
                    qty = currentStock,
                    availableQty = availableQty,
                    maximumdiscount = item.MaximumDiscount,
                    allowNegative = setting?.AllowNegative ?? false,
                    minimumQty = item.MinimumQty,
                    conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m,
                    ItemConversion = setting?.ItemConversion ?? "StripWise"
                };
            });

            if (setting != null && !setting.AllowNegative)
            {
                stockList = stockList.Where(x => x.qty > 0);
            }

            ViewBag.ItemConversion = setting?.ItemConversion ?? "StripWise";

            return Json(stockList.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> GetBatchesByItemId(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
            {
                return Json(new { error = "Item not found." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            var currentStocks = await _currentstockService.GetAll();
            var purchaseItems = await _purchaseitemservice.GetByItemMasterId(id);

            var groupedResult = currentStocks
                .Where(x => x.ItemId == id)
                .GroupBy(x => new { x.Batch, x.ExpiryDate, x.Mrp, x.PurchaseRate })
                .Select(g =>
                {
                    decimal qty = g.Sum(x => x.Qty);
                    decimal conversion = item.Conversion > 0 ? item.Conversion : 1;
                    decimal stripRate = g.FirstOrDefault()?.SalesRateA ?? 0;
                    decimal ratePerUnit = stripRate;

                    if (setting != null && setting.ItemConversion == "TabletWise")
                    {
                        ratePerUnit = conversion != 0 ? stripRate / conversion : stripRate;
                    }

                    string stripTabsQty;
                    if (setting != null && setting.ItemConversion == "TabletWise" && conversion > 0)
                    {
                        int conv = (int)conversion;
                        int totalTablets = (int)Math.Round(qty * conv, MidpointRounding.AwayFromZero);
                        int strips = totalTablets / conv;
                        int tablets = totalTablets % conv;
                        stripTabsQty = $"{strips}:{tablets}";
                    }
                    else
                    {
                        stripTabsQty = qty.ToString("0.##");
                    }

                    var purchaseItemId = purchaseItems
                        .Where(x =>
                            x.Batch == g.Key.Batch &&
                            x.ExpiryDate == g.Key.ExpiryDate &&
                            x.Mrp == g.Key.Mrp)
                        .OrderByDescending(x => x.Created)
                        .Select(x => x.Id)
                        .FirstOrDefault();

                    return new
                    {
                        batch = g.Key.Batch,
                        expiry = g.Key.ExpiryDate,
                        rate = ratePerUnit,
                        striprate = stripRate,
                        mrp = g.Key.Mrp,
                        qty = qty,
                        StripTabsQty = stripTabsQty,
                        purchaseItemId = purchaseItemId,
                        conversion = conversion
                    };
                });

            if (setting != null && !setting.AllowNegative)
            {
                groupedResult = groupedResult.Where(x => x.qty > 0);
            }

            if (setting != null && setting.ExpiryAllowedDays > 0)
            {
                var allowedDate = DateTime.Now.AddDays(setting.ExpiryAllowedDays);
                groupedResult = groupedResult.Where(x => x.expiry == null || x.expiry >= allowedDate);
            }

            return Json(new
            {
                gst = item.Hsn?.IGST ?? 0,
                cess = item.Hsn?.Cess ?? 0,
                hsnId = item.HsnId,
                hsnCode = item.Hsn?.HsnCode,
                groupedResult = groupedResult.OrderBy(x => x.expiry).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetBatchesByItemIdForEdit(int id, int stockIssueId = 0)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
            {
                return Json(new { error = "Item not found." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            var currentStocks = await _currentstockService.GetAll();
            var purchaseItems = await _purchaseitemservice.GetByItemMasterId(id);

            var currentIssueItems = new List<StockIssueItem>();
            if (stockIssueId > 0)
            {
                var stockIssue = await _stockissueservice.GetById(stockIssueId);
                if (stockIssue?.StockIssuesItems != null)
                {
                    currentIssueItems = stockIssue.StockIssuesItems.ToList();
                }
            }

            var groupedResult = currentStocks
                .Where(x => x.ItemId == id)
                .GroupBy(x => new { x.Batch, x.ExpiryDate, x.Mrp, x.PurchaseRate })
                .Select(g =>
                {
                    decimal qty = g.Sum(x => x.Qty);

                    decimal issueQty = currentIssueItems
                        .Where(x =>
                            x.ItemMasterId == id &&
                            x.Batch == g.Key.Batch &&
                            x.Mrp == g.Key.Mrp &&
                            x.Expirydate == g.Key.ExpiryDate)
                        .Sum(x => x.Qty);

                    qty += issueQty;

                    decimal conversion = item.Conversion > 0 ? item.Conversion : 1;
                    decimal stripRate = g.FirstOrDefault()?.SalesRateA ?? 0;
                    decimal ratePerUnit = stripRate;

                    if (setting != null && setting.ItemConversion == "TabletWise")
                    {
                        ratePerUnit = conversion != 0 ? stripRate / conversion : stripRate;
                    }

                    string stripTabsQty;
                    if (setting != null && setting.ItemConversion == "TabletWise" && conversion > 0)
                    {
                        int conv = (int)conversion;
                        int totalTablets = (int)Math.Round(qty * conv, MidpointRounding.AwayFromZero);
                        int strips = totalTablets / conv;
                        int tablets = totalTablets % conv;
                        stripTabsQty = $"{strips}:{tablets}";
                    }
                    else
                    {
                        stripTabsQty = qty.ToString("0.##");
                    }

                    var purchaseItemId = purchaseItems
                        .Where(x =>
                            x.Batch == g.Key.Batch &&
                            x.ExpiryDate == g.Key.ExpiryDate &&
                            x.Mrp == g.Key.Mrp)
                        .OrderByDescending(x => x.Created)
                        .Select(x => x.Id)
                        .FirstOrDefault();

                    return new
                    {
                        batch = g.Key.Batch,
                        expiry = g.Key.ExpiryDate,
                        rate = ratePerUnit,
                        striprate = stripRate,
                        mrp = g.Key.Mrp,
                        qty = qty,
                        StripTabsQty = stripTabsQty,
                        purchaseItemId = purchaseItemId,
                        conversion = conversion
                    };
                });

            if (setting != null && !setting.AllowNegative)
            {
                groupedResult = groupedResult.Where(x => x.qty > 0);
            }

            if (setting != null && setting.ExpiryAllowedDays > 0)
            {
                var allowedDate = DateTime.Now.AddDays(setting.ExpiryAllowedDays);
                groupedResult = groupedResult.Where(x => x.expiry == null || x.expiry >= allowedDate);
            }

            return Json(new
            {
                gst = item.Hsn?.IGST ?? 0,
                cess = item.Hsn?.Cess ?? 0,
                hsnId = item.HsnId,
                hsnCode = item.Hsn?.HsnCode,
                groupedResult = groupedResult.OrderBy(x => x.expiry).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetItemDetails(int itemId)
        {
            if (itemId > 0)
            {
                var hsndata = await _itemmasterservice.GetByItemMasterId(itemId);
                if (hsndata != null)
                {
                    var model = new
                    {
                        gst = hsndata.Hsn?.IGST,
                        purchaseItems = (await _purchaseitemservice.GetByItemMasterId(itemId)).Select( x => new
                        {
                         x.Id,
                         Text = x.Batch
                        }).ToList(),
                    }; 
                    return Json(model);
                }
                else
                {
                    return Json(new { error = "Hsn Details not found." });
                }

            }
            else
            {
                return Json(new { error = "Hsn Details not found." });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPurchaseItemDetails(int Id)
        {
            if (Id > 0)
            {
                var purchasedata = await _purchaseitemservice.GetById(Id);
                if (purchasedata != null)
                {
                    var model = new
                    {
                        qty = purchasedata.Qty,
                        rate = purchasedata.Rate,
                        mrp = purchasedata.Mrp,
                        expirydate = purchasedata.ExpiryDate
                    };
                    return Json(model);
                }
                else
                {
                    return Json(new { error = "Hsn Details not found." });
                }

            }
            else
            {
                return Json(new { error = "Hsn Details not found." });
            }
        }
         
        

        [HttpGet]
        public async Task<IActionResult> StockIssueReportItemWise(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            var itemMasters = await _itemmasterservice.GetAll();
            var stockIssue = (await _stockissueservice.GetAll())
                        .Where(s => s.ChallanDate >= fromDate && s.ChallanDate <= toDate)
                        .ToList();


            // Flatten all SalesItems
            var stockissueItems = stockIssue.Where(s => s.StockIssuesItems != null).SelectMany(s => s.StockIssuesItems);

            // Group by ItemMasterId to calculate total sales
            var groupedStockIssue = stockissueItems
                              .GroupBy(x => x.ItemMasterId)
                              .Select(g => new
                              {
                                  ItemMasterId = g.Key,
                                  TotalQty = g.Sum(x => x.Qty),
                                  TotalAmount = g.Sum(x => x.Amount), // Assuming already discounted
                                  TotalGst = g.Sum(x =>
                                  {
                                      decimal rate = x.Rate;
                                      decimal qty = x.Qty;
                                      decimal discountPercent = x.Discount;
                                      decimal gstPercent = x.Gst;

                                      decimal gross = rate * qty;
                                      decimal discountAmt = gross * (discountPercent / 100);
                                      decimal taxable = gross - discountAmt;
                                      decimal gstAmt = taxable * (gstPercent / 100);

                                      return gstAmt;
                                  })
                              })
                              .ToList();


            // Join grouped results with item masters
            var stockissueList = (from item in itemMasters
                             join g in groupedStockIssue on item.Id equals g.ItemMasterId
                             where g.TotalQty > 0
                             select new StockVM
                             {
                                 ItemMasterId = item.Id,
                                 ItemCode = item.Code,
                                 CategoryName = item.Category?.CategoryName ?? "Unknown",
                                 ItemName = item.Name,
                                 Unit1 = item.Unit1 ?? "",
                                 Unit2 = item.Unit2 ?? "",
                                 Stocks = g.TotalQty,
                                 Amount = g.TotalAmount,
                                 GstAmount = g.TotalGst
                             }).ToList();
            ViewBag.TotalQty = stockissueList.Sum(x => x.Stocks);
            ViewBag.TotalGst = stockissueList.Sum(x => x.GstAmount);
            ViewBag.TotalAmount = stockissueList.Sum(x => x.Amount);
            return View(stockissueList);
        }

        //EXPORT TO EXCEL
        [HttpGet]
        public async Task<IActionResult> ExportStockIssueItemWiseExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();

            var stockIssue = (await _stockissueservice.GetAll())
                .Where(s => s.ChallanDate >= fromDate && s.ChallanDate <= toDate)
                .ToList();

            var stockissueItems = stockIssue
                .Where(s => s.StockIssuesItems != null)
                .SelectMany(s => s.StockIssuesItems);

            var grouped = stockissueItems
                .GroupBy(x => x.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Amount),
                    TotalGst = g.Sum(x =>
                    {
                        var gross = x.Rate * x.Qty;
                        var discount = gross * (x.Discount / 100);
                        var taxable = gross - discount;
                        return Math.Round(taxable * (x.Gst / 100), 2); // ✅ FIX
                    })
                }).ToList();

            var list = (from item in itemMasters
                        join g in grouped on item.Id equals g.ItemMasterId
                        where g.TotalQty > 0
                        select new StockVM
                        {
                            ItemCode = item.Code,
                            ItemName = item.Name,
                            CategoryName = item.Category?.CategoryName ?? "Unknown",
                            Stocks = g.TotalQty,
                            GstAmount = g.TotalGst,
                            Amount = g.TotalAmount
                        }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stock Issue Item Wise");

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";
            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Stock Issue Item Wise Report";
            ws.Range(2, 1, 2, 6).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 6).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "Item Code", "Item Name", "Category", "Total Qty", "Gst Amount", "Total Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 6).Style.Font.SetBold();
            row++;

            foreach (var x in list)
            {
                ws.Cell(row, 1).Value = x.ItemCode;
                ws.Cell(row, 2).Value = x.ItemName;
                ws.Cell(row, 3).Value = x.CategoryName;
                ws.Cell(row, 4).Value = Math.Round(x.Stocks, 2);
                ws.Cell(row, 5).Value = Math.Round(x.GstAmount ?? 0, 2);
                ws.Cell(row, 6).Value = Math.Round(x.Amount ?? 0, 2);
                row++;
            }

            // TOTAL ROW
            ws.Cell(row, 1).Value = "Total";
            ws.Cell(row, 4).Value = Math.Round(list.Sum(x => x.Stocks), 2);
            ws.Cell(row, 5).Value = Math.Round(list.Sum(x => x.GstAmount ?? 0), 2);
            ws.Cell(row, 6).Value = Math.Round(list.Sum(x => x.Amount ?? 0), 2);

            ws.Range(row, 1, row, 6).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "StockIssueItemWise.xlsx");
        }

        //EXPORT TO PDF
        [HttpGet]
        public async Task<IActionResult> ExportStockIssueItemWisePdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();

            var stockIssue = (await _stockissueservice.GetAll())
                .Where(s => s.ChallanDate >= fromDate && s.ChallanDate <= toDate)
                .ToList();

            var stockissueItems = stockIssue
                .Where(s => s.StockIssuesItems != null)
                .SelectMany(s => s.StockIssuesItems);

            var grouped = stockissueItems
                .GroupBy(x => x.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Amount),
                    TotalGst = g.Sum(x =>
                    {
                        var gross = x.Rate * x.Qty;
                        var discount = gross * (x.Discount / 100);
                        var taxable = gross - discount;
                        return Math.Round(taxable * (x.Gst / 100), 2); // ✅ FIX
                    })
                }).ToList();

            var list = (from item in itemMasters
                        join g in grouped on item.Id equals g.ItemMasterId
                        where g.TotalQty > 0
                        select new StockVM
                        {
                            ItemCode = item.Code,
                            ItemName = item.Name,
                            CategoryName = item.Category?.CategoryName ?? "Unknown",
                            Stocks = g.TotalQty,
                            GstAmount = g.TotalGst,
                            Amount = g.TotalAmount
                        }).ToList();

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";
            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // HEADER
            doc.Add(new Paragraph(companyName).SetFont(bold).SetFontSize(16).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph("Stock Issue Item Wise Report").SetFont(bold).SetFontSize(13).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(10));

            float fs = 9;

            Table table = new Table(6).UseAllAvailableWidth();

            string[] headers = { "Item Code", "Item Name", "Category", "Total Qty", "Gst Amount", "Total Amount" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(fs))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            foreach (var x in list)
            {
                table.AddCell(new Paragraph(x.ItemCode ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.ItemName ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.CategoryName ?? "").SetFont(normal).SetFontSize(fs));

                table.AddCell(new Paragraph(x.Stocks.ToString("0.00"))
                    .SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Paragraph(x.GstAmount.ToString()) // ✅ FIX
                    .SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Paragraph(x.Amount.ToString())
                    .SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 3)
                .Add(new Paragraph("Total").SetFont(bold).SetFontSize(fs)));

            table.AddCell(new Paragraph(list.Sum(x => x.Stocks).ToString("0.00"))
                .SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(list.Sum(x => x.GstAmount).ToString())
                .SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(list.Sum(x => x.Amount).ToString())
                .SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "StockIssueItemWise.pdf");
        }

        public async Task<IActionResult> StockIssueReportBillWise(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            var stockissue = (await _stockissueservice.GetAll())
                .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
                .ToList();
            var billwiseList = stockissue.Select(stock  => new StockIssueVM
            {
                Id = stock.Id,
                ChallanNo = stock.ChallanNo,
                ChallanDate = stock.ChallanDate,
                CustomerName = stock.Customers?.Name ?? "Unknown",
                MobileNo = stock.MobileNo,
                TotalGstAmt = stock.TotalGstAmt,
                TotalPayable = stock.StockIssuesItems?.Sum(item => item.Amount) ?? 0
            })
            .Where(x => x.TotalPayable > 0) // Optional filter
            .OrderByDescending(x => x.ChallanDate)
            .ToList();
            ViewBag.TotalGst = billwiseList.Sum(x => x.TotalGstAmt);
            ViewBag.TotalAmount = billwiseList.Sum(x => x.TotalPayable);
            return View(billwiseList);
        }

        //Export to Excel
        [HttpGet]
        public async Task<IActionResult> ExportStockIssueBillWiseExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var stockissue = (await _stockissueservice.GetAll())
                .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
                .ToList();

            var data = stockissue.Select(stock => new StockIssueVM
            {
                ChallanNo = stock.ChallanNo,
                ChallanDate = stock.ChallanDate,
                CustomerName = stock.Customers?.Name ?? "Unknown",
                TotalGstAmt = stock.TotalGstAmt,
                TotalPayable = stock.StockIssuesItems?.Sum(i => i.Amount) ?? 0
            })
            .OrderByDescending(x => x.ChallanDate)
            .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stock Issue Bill Wise");

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 5).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Stock Issue Bill Wise Report";
            ws.Range(2, 1, 2, 5).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 5).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "Bill No", "Bill Date", "Customer", "Gst Amount", "Total Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 5).Style.Font.SetBold();
            row++;

            foreach (var x in data)
            {
                ws.Cell(row, 1).Value = x.ChallanNo;
                ws.Cell(row, 2).Value = x.ChallanDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 3).Value = x.CustomerName;
                ws.Cell(row, 4).Value = x.TotalGstAmt;
                ws.Cell(row, 5).Value = x.TotalPayable;
                row++;
            }

            // TOTAL ROW
            ws.Cell(row, 1).Value = "Total";
            ws.Cell(row, 4).Value = data.Sum(x => x.TotalGstAmt);
            ws.Cell(row, 5).Value = data.Sum(x => x.TotalPayable);

            ws.Range(row, 1, row, 5).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "StockIssueBillWise.xlsx");
        }

        //Export to PDF
        [HttpGet]
        public async Task<IActionResult> ExportStockIssueBillWisePdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var stockissue = (await _stockissueservice.GetAll())
                .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
                .ToList();

            var data = stockissue.Select(stock => new StockIssueVM
            {
                ChallanNo = stock.ChallanNo,
                ChallanDate = stock.ChallanDate,
                CustomerName = stock.Customers?.Name ?? "Unknown",
                TotalGstAmt = stock.TotalGstAmt,
                TotalPayable = stock.StockIssuesItems?.Sum(i => i.Amount) ?? 0
            })
            .OrderByDescending(x => x.ChallanDate)
            .ToList();

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fs = 9;

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            doc.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph("Stock Issue Bill Wise Report")
                .SetFont(bold).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            var table = new Table(new float[] { 3, 3, 5, 3, 3 }).UseAllAvailableWidth();

            string[] headers = { "Bill No", "Bill Date", "Customer", "Gst Amount", "Total Amount" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(fs))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            foreach (var x in data)
            {
                table.AddCell(new Paragraph(x.ChallanNo ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.ChallanDate?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.CustomerName ?? "").SetFont(normal).SetFontSize(fs));

                table.AddCell(new Paragraph(x.TotalGstAmt.ToString("0.00"))
                    .SetFont(normal).SetFontSize(fs)
                    .SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Paragraph(x.TotalPayable.ToString("0.00"))
                    .SetFont(normal).SetFontSize(fs)
                    .SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 3)
                .Add(new Paragraph("Total").SetFont(bold).SetFontSize(fs)));

            table.AddCell(new Paragraph(data.Sum(x => x.TotalGstAmt).ToString("0.00"))
                .SetFont(bold).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(data.Sum(x => x.TotalPayable).ToString("0.00"))
                .SetFont(bold).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.RIGHT));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(),
                "application/pdf",
                "StockIssueBillWise.pdf");
        }


    }
}
