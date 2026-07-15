using AOne.DataAccess.ProfileService;
using ClosedXML.Excel;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.Formula.Functions;
using System.Globalization;
using System.Text.RegularExpressions;
using static iText.Kernel.Pdf.Colorspace.PdfPattern.Tiling;

namespace EasyBill.UI.Controllers
{
    public class StockReturnController : Controller
    {
        private readonly IStockReturnRepository _stockReturnservice;
        private readonly IProfileService _profileService;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IPurchaseItemRepository _purchaseitemservice;
        private readonly ISalesRepository _salesservice;
        private readonly ISalseSettingRepository _salsesettingservice;
        private readonly UserManager<ApplicationUsers> _usersManager;
        private readonly ITenantRegistrationRepository _tenantRepository;
        private readonly IStockService _currentstockservice;
        public StockReturnController(
            IStockReturnRepository stockReturnservice,
            IProfileService profileService,
            ICustomerRepository customerservice,
            IItemMasterRepository itemmasterservice,
            IPurchaseItemRepository purchaseitemservice,
            ISalesRepository salesservice,
            ISalseSettingRepository salsesettingservice,
            ITenantRegistrationRepository tenantRegistration,
            UserManager<ApplicationUsers> usersManager,
            IStockService currentstockservice)
        {
            _stockReturnservice = stockReturnservice;
            _profileService = profileService;
            _customerservice = customerservice;
            _itemmasterservice = itemmasterservice;
            _purchaseitemservice = purchaseitemservice;
            _salesservice = salesservice;
            _salsesettingservice = salsesettingservice;
            _tenantRepository = tenantRegistration;
            _usersManager = usersManager;
            _currentstockservice = currentstockservice;
        }
        public async Task<IActionResult> Index()
        {
            await _profileService.Set(User);

            // Get all stock returns
            var data = await _stockReturnservice.GetAll();

            // Get only customers who have stock returns
            ViewBag.CustomerList = await _stockReturnservice.GetCustomersWithReturns();

            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> GetDataByFilter(
        string? BillNo,
        DateTime? StartBillDate,
        DateTime? EndBillDate,
        int? CustomerId,
        string? MobileNo)
        {
            // Get all data first
            var allData = await _stockReturnservice.GetAll();

            // Apply filters in memory
            var filteredData = allData.AsQueryable();

            if (!string.IsNullOrWhiteSpace(BillNo))
                filteredData = filteredData.Where(s => s.ChallanNo.Contains(BillNo));

            if (StartBillDate.HasValue)
                filteredData = filteredData.Where(s => s.ChallanDate >= StartBillDate.Value);

            if (EndBillDate.HasValue)
                filteredData = filteredData.Where(s => s.ChallanDate <= EndBillDate.Value);

            if (CustomerId.HasValue)
                filteredData = filteredData.Where(s => s.CustomerId == CustomerId.Value);

            if (!string.IsNullOrWhiteSpace(MobileNo))
                filteredData = filteredData.Where(s => s.MobileNo != null && s.MobileNo.Contains(MobileNo));

            var result = filteredData
                .OrderByDescending(s => s.ChallanDate)
                .ToList();

            return PartialView("_SalesReturnTable", result);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new StockReturnVM();
            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            viewModel.DoctorRequired = setting?.DoctorRequired ?? false;

            // Check bussiness type
            var tenantId = User?.FindFirst("TenantId")?.Value;
            var user = (await _tenantRepository.GetAll())
           .FirstOrDefault(x => x.Id == tenantId);
            var businessType = user?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;

            viewModel.ChallanDate = DateTime.Now;
            viewModel.ChallanNo = await GenerateNxtNumber();
            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(StockReturnVM Vm)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new {
                        Field = x.Key,
                        Message = x.Value.Errors.First().ErrorMessage
                    })
                    .ToList();

                var firstError = errors.FirstOrDefault();
                return BadRequest(new
                {
                    success = false,
                    message = firstError?.Message ?? "Please fill all required fields correctly.",
                    errors = errors
                });
            }

            var data = await _stockReturnservice.GetAll();
            var challanNo = Vm.ChallanNo?.Trim();

            bool isDuplicate = data.Any(x =>
                x.ChallanNo != null &&
                x.ChallanNo.Trim().Equals(challanNo, StringComparison.OrdinalIgnoreCase)
            );

            if (isDuplicate)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"{challanNo} - This Challan Number already exists."
                });
            }

            #region Customer Validation
            if (Vm.billingType == "registered")
            {
                var customerList = await _customerservice.GetAll();
                var isExist = customerList.Any(x =>
                    !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                    x.PhoneNo.Trim() == Vm.MobileNo.Trim()
                );

                if (!isExist)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid customer. Please try again."
                    });
                }
            }
            #endregion
             
            // GET SETTINGS & ITEM MASTERS
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name);

            var model = new StockReturn
            {
                CustomerId = Vm.billingType == "registered" ? Vm.CustomerId : null,

                MobileNo = Vm.billingType == "registered" ? Vm.MobileNo : null,  

                Address = Vm.billingType == "registered" ? Vm.Address : null,  

                billingType = Vm.billingType == "registered" ? Vm.billingType : "Cash",   
                ChallanDate = Vm.ChallanDate,
                ChallanNo = Vm.ChallanNo,  
                PharmacyDoctorId = Vm.PharmacyDoctorId,
                DoctorMobileNumber = Vm.DoctorMobileNumber,
                DoctorRegNumber = Vm.DoctorRegNumber,
                Total = Vm.Total,
                TotalGstAmt = Vm.TotalGstAmt,
                discountPercent = Vm.discountPercent,
                discountAmount = Vm.discountAmount,
                Totaldiscount = Vm.Totaldiscount,
                TotalPayable = Vm.TotalPayable, 
                PaymentType = Vm.PaymentType,
                TotalCessAmt = Vm.TotalCessAmt,
                // QTY CONVERSION LOGIC (SAME AS SALES)
                StockReturnItems = Vm.StockReturnItemVMs?.Select(x =>
                {
                    var item = itemMasters.FirstOrDefault(i => i.Id == x.ItemMasterId);

                    decimal finalQty = x.Qty;
                    if (isTabletWise && item != null && item.Conversion > 0)
                    {
                        // Convert: Strips + (Tablets / Conversion) = Total in Strips
                        finalQty = x.Qty + ((decimal)x.TabletQty / item.Conversion);
                    }

                    return new StockReturnItem()
                    {
                        ItemMasterId = x.ItemMasterId,
                        PurchaseItemId = x.PurchaseItemId,
                        Batch = x.Batch,
                        Expirydate = x.Expirydate,
                        Mrp = x.Mrp,
                        Qty = finalQty,  // CONVERTED QTY
                        Rate = x.Rate,
                        StripRate = x.StripRate,
                        Gst = x.Gst,
                        Cess = x.Cess,
                        Discount = x.Discount,
                        Amount = x.Amount,
                        Reason = x.Reason
                    };
                }).ToList() ?? new List<StockReturnItem>()
            };

            try
            {
                foreach (var item in model.StockReturnItems)
                {
                    await _currentstockservice.UpdateStock(
                        item.ItemMasterId,
                        item.Batch?.Trim() ?? "",
                        item.Qty,   // 🔥 ADD stock
                        item.Expirydate,
                        item.Mrp,
                        item.Rate
                    );
                }
                await _stockReturnservice.Create(model);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            // GET USER SETTINGS FIRST
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            StockReturn model = await _stockReturnservice.GetById(Id);
            StockReturnVM VM = new StockReturnVM();

            // Get all items for name lookup
            var allItems = await _itemmasterservice.GetAll();

            if (model != null)
            {
                VM.Id = model.Id;
                VM.CustomerId = model.CustomerId;
                VM.CustomerName = model.Customers?.Name;
                VM.ChallanNo = model.ChallanNo;
                VM.ChallanDate = model.ChallanDate;
                VM.MobileNo = model.MobileNo;
                VM.Address = model.Address;
                VM.PharmacyDoctorId = model.PharmacyDoctorId;
                VM.DoctorName = model.PharmacyDoctor?.Name;
                VM.DoctorMobileNumber = model.DoctorMobileNumber;
                VM.DoctorRegNumber = model.DoctorRegNumber;
                VM.Total = model.Total;
                VM.TotalGstAmt = model.TotalGstAmt;
                VM.discountPercent = model.discountPercent;
                VM.discountAmount = model.discountAmount;
                VM.Totaldiscount = model.Totaldiscount;
                VM.TotalPayable = model.TotalPayable;
                VM.billingType = model.billingType;
                VM.PaymentType = model.PaymentType;
                VM.TotalCessAmt = model.TotalCessAmt;
                // GET ALL ITEMS FOR CONVERSION LOOKUP (Dictionary)
                var itemMastersDictionary = allItems.ToDictionary(x => x.Id, x => x);

                // CHECK IF TABLET-WISE MODE
                bool isTabletWiseMode = setting != null && setting.ItemConversion == "TabletWise";

                // SPLIT QTY FOR TABLET-WISE MODE
                VM.StockReturnItemVMs = model.StockReturnItems != null
                    ? model.StockReturnItems.Select(x =>
                    {
                        decimal displayQty = x.Qty;          // strips
                        decimal displayTabletQty = 0;        // tablets

                        // SPLIT QTY IF TABLET-WISE MODE
                        if (isTabletWiseMode && itemMastersDictionary.ContainsKey(x.ItemMasterId))
                        {
                            var item = itemMastersDictionary[x.ItemMasterId];
                            int conversion = item.Conversion > 0 ? item.Conversion : 1;

                            // Step 1: convert strip qty to total tablets
                            int totalTablets = (int)Math.Round(x.Qty * conversion, MidpointRounding.AwayFromZero);

                            // Step 2: split tablets into strip + tablets
                            displayQty = totalTablets / conversion;          // strips
                            displayTabletQty = totalTablets % conversion;    // tablets
                        }

                        return new StockReturnItemVM
                        {
                            Id = x.Id,
                            StockReturnId = x.StockReturnId,
                            ItemMasterId = x.ItemMasterId,
                            ItemName = itemMastersDictionary.ContainsKey(x.ItemMasterId)
                                ? itemMastersDictionary[x.ItemMasterId].Name
                                : "",
                            PurchaseItemId = x.PurchaseItemId,
                            Batch = x.Batch,
                            Expirydate = x.Expirydate,
                            Mrp = x.Mrp,
                            Qty = displayQty,              // STRIPS (or original qty if StripWise)
                            TabletQty = displayTabletQty,  // REMAINING TABLETS (0 if StripWise)
                            Rate = x.Rate,
                            StripRate = x.StripRate,
                            Gst = x.Gst,
                            Cess = x.Cess,
                            Discount = x.Discount,
                            Amount = x.Amount,
                            Reason = x.Reason,
                        };
                    }).ToList()
                    : new List<StockReturnItemVM>();
            }

            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");

            // DOCTOR REQUIRED SETTING
            VM.DoctorRequired = setting?.DoctorRequired ?? false;

            // ADD BUSINESSTYPE
            var tenantId = User?.FindFirst("TenantId")?.Value;
            var user = (await _tenantRepository.GetAll())
                .FirstOrDefault(x => x.Id == tenantId);
            var businessType = user?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;

            // GET CUSTOMER'S SALES ITEMS
            var sales = await _salesservice.GetByCustomerId(VM.CustomerId);

            // Get unique ItemMasterIds from sales items
            var itemMasterIds = sales.SelectMany(x => x.SalesItems)
                .Where(x => x.ItemMaster != null)
                .Select(x => x.ItemMasterId)
                .Distinct()
                .ToList();

            // RENAMED: Fetch full ItemMaster entities based on IDs
            var filteredItemMasters = allItems.Where(x => itemMasterIds.Contains(x.Id));

            // Prepare the result
            ViewBag.Items = new SelectList(filteredItemMasters.Select(i => new
            {
                Value = i.Id,
                Text = i.Name
            }).ToList(), "Value", "Text");

            var allPurchaseItems = await _purchaseitemservice.GetAll();
            var stockissueitemmasterId = VM.StockReturnItemVMs.Select(x => x.ItemMasterId).ToList();

            var filteredPurchaseItems = allPurchaseItems
                .Where(x => stockissueitemmasterId.Contains(x.ItemId))
                .ToList();

            ViewBag.purchaseItem = new SelectList(filteredPurchaseItems, "Id", "Batch");

            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(StockReturnVM VM)
        {

            if (!ModelState.IsValid)
            {
                ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
                ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
                return View(VM);
            }

            // GET SETTINGS & ITEM MASTERS
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name);


            StockReturn model = await _stockReturnservice.GetById(VM.Id);
            if (model != null)
            {
                model.CustomerId = VM.CustomerId;
                model.ChallanDate = VM.ChallanDate;
                model.ChallanNo = VM.ChallanNo;
                model.MobileNo = VM.MobileNo;
                model.Address = VM.Address;
                model.PharmacyDoctorId = VM.PharmacyDoctorId;
                model.DoctorRegNumber = VM.DoctorRegNumber;
                model.DoctorMobileNumber = VM.DoctorMobileNumber;
                model.Total = VM.Total;
                model.TotalGstAmt = VM.TotalGstAmt;
                model.discountPercent = VM.discountPercent;
                model.discountAmount = VM.discountAmount;
                model.Totaldiscount = VM.Totaldiscount;
                model.TotalPayable = VM.TotalPayable;
                model.billingType = VM.billingType;
                model.PaymentType = VM.PaymentType;
                model.TotalCessAmt = VM.TotalCessAmt;
                var removedItems = model.StockReturnItems.Where(dbItem => !VM.StockReturnItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

                // ❌ Remove Deleted Items
                foreach (var item in removedItems)
                {
                    await _currentstockservice.UpdateStock(
                        item.ItemMasterId,
                        item.Batch?.Trim() ?? "",
                        -(item.Qty),  // 🔥 reverse (remove added stock)
                        item.Expirydate,
                        item.Mrp,
                        item.Rate
                    );
                    model.StockReturnItems.Remove(item);
                }
                foreach (var item in VM.StockReturnItemVMs)
                {
                    // QTY CONVERSION LOGIC
                    var itemMaster = itemMasters.FirstOrDefault(i => i.Id == item.ItemMasterId);
                    decimal finalQty = item.Qty;

                    if (isTabletWise && itemMaster != null && itemMaster.Conversion > 0)
                    {
                        // Convert: Strips + (Tablets / Conversion) = Total in Strips
                        finalQty = item.Qty + ((decimal)item.TabletQty / itemMaster.Conversion);
                    }

                    if (item.Id > 0) // Update existing items
                    {
                        var existingItem = model.StockReturnItems.FirstOrDefault(x => x.Id == item.Id);
                        if (existingItem != null)
                        {
                            decimal oldQty = existingItem.Qty;
                            decimal newQty = finalQty;

                            // 🔴 remove old stock
                            await _currentstockservice.UpdateStock(
                                existingItem.ItemMasterId,
                                existingItem.Batch?.Trim() ?? "",
                                -oldQty,
                                existingItem.Expirydate,
                                existingItem.Mrp,
                                existingItem.Rate
                            );

                            // 🟢 add new stock
                            await _currentstockservice.UpdateStock(
                                item.ItemMasterId,
                                item.Batch?.Trim() ?? "",
                                newQty,
                                item.Expirydate,
                                item.Mrp,
                                item.Rate
                            );

                            existingItem.ItemMasterId = item.ItemMasterId;
                            existingItem.PurchaseItemId = item.PurchaseItemId;
                            existingItem.Batch = item.Batch;
                            existingItem.Expirydate = item.Expirydate;
                            existingItem.Mrp = item.Mrp;
                            //existingItem.Qty = item.Qty;
                            existingItem.Qty = finalQty;                    // CONVERTED QTY
                            existingItem.Rate = item.Rate;
                            existingItem.StripRate = item.StripRate;
                            existingItem.Gst = item.Gst;
                            existingItem.Cess = item.Cess;
                            existingItem.Discount = item.Discount;
                            existingItem.Reason = item.Reason;
                        }
                    }
                    else // Add new items
                    {
                        await _currentstockservice.UpdateStock(
                            item.ItemMasterId,
                            item.Batch?.Trim() ?? "",
                            finalQty,   // 🔥 ADD
                            item.Expirydate,
                            item.Mrp,
                            item.Rate
                        );
                        model.StockReturnItems.Add(new StockReturnItem
                        {
                            ItemMasterId = item.ItemMasterId,
                            PurchaseItemId = item.PurchaseItemId,
                            Batch = item.Batch,
                            Expirydate = item.Expirydate,
                            Mrp = item.Mrp,
                            //Qty = item.Qty,
                            Qty = finalQty,  // CONVERTED QTY
                            Rate = item.Rate,
                            StripRate = item.StripRate,
                            Gst = item.Gst,
                            Cess = item.Cess,
                            Discount = item.Discount,
                            Amount = item.Amount,
                            Reason = item.Reason
                        });
                    }
                }
                await _stockReturnservice.Update(model);
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

                var model = await _stockReturnservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                foreach (var item in model.StockReturnItems)
                {
                    await _currentstockservice.UpdateStock(
                        item.ItemMasterId,
                        item.Batch ?? "",
                        -(item.Qty),
                        item.Expirydate,
                        item.Mrp,
                        item.Rate
                    );
                }
                await _stockReturnservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        //[HttpPost]
        //public async Task<IActionResult> Create(StockReturnVM Vm)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState
        //            .Where(x => x.Value.Errors.Count > 0)
        //            .Select(x => new {
        //                Field = x.Key,
        //                Message = x.Value.Errors.First().ErrorMessage
        //            })
        //            .ToList();

        //        var firstError = errors.FirstOrDefault();
        //        return BadRequest(new
        //        {
        //            success = false,
        //            message = firstError?.Message ?? "Please fill all required fields correctly.",
        //            errors = errors
        //        });
        //    }

        //    var data = await _stockReturnservice.GetAll();
        //    var challanNo = Vm.ChallanNo?.Trim();

        //    bool isDuplicate = data.Any(x =>
        //        x.ChallanNo != null &&
        //        x.ChallanNo.Trim().Equals(challanNo, StringComparison.OrdinalIgnoreCase)
        //    );

        //    if (isDuplicate)
        //    {
        //        return BadRequest(new
        //        {
        //            success = false,
        //            message = $"{challanNo} - This Challan Number already exists."
        //        });
        //    }

        //    if (Vm != null)
        //    {

        //        #region Cutomer validate
        //        var customerList = await _customerservice.GetAll();
        //        var isExist = customerList.Any(x =>
        //            !string.IsNullOrWhiteSpace(x.PhoneNo) &&
        //            x.PhoneNo.Trim() == Vm.MobileNo.Trim()
        //        );

        //        // 🔴 Customer NOT found → ERROR
        //        if (!isExist)
        //        {
        //            TempData["error"] = "Invalid customer. Please try again.";
        //            return View(Vm);
        //        }
        //        #endregion

        //        var model = new StockReturn
        //        {
        //            CustomerId = Vm.CustomerId,
        //            ChallanDate = Vm.ChallanDate,
        //            ChallanNo = Vm.ChallanNo,
        //            MobileNo = Vm.MobileNo,
        //            Address = Vm.Address,
        //            PharmacyDoctorId = Vm.PharmacyDoctorId,
        //            DoctorMobileNumber = Vm.DoctorMobileNumber,
        //            DoctorRegNumber = Vm.DoctorRegNumber,
        //            Total = Vm.Total,
        //            TotalGstAmt = Vm.TotalGstAmt,
        //            discountPercent = Vm.discountPercent,
        //            discountAmount = Vm.discountAmount,
        //            Totaldiscount = Vm.Totaldiscount,
        //            TotalPayable = Vm.TotalPayable,
        //            StockReturnItems = Vm.StockReturnItemVMs.Select(x => new StockReturnItem()
        //            {
        //                ItemMasterId = x.ItemMasterId,
        //                PurchaseItemId = x.PurchaseItemId,
        //                Batch = x.Batch,
        //                Expirydate = x.Expirydate,
        //                Mrp = x.Mrp,
        //                Qty = x.Qty,
        //                Rate = x.Rate,
        //                Gst = x.Gst,
        //                Discount = x.Discount,
        //                Amount = x.Amount,
        //                Reason = x.Reason
        //            }).ToList() ?? new List<StockReturnItem>()
        //        };
        //        await _stockReturnservice.Create(model);
        //    }

        //    return RedirectToAction("Index");
        //}
        public async Task<string> GenerateNxtNumber()
        {
            var lastCode = (await _stockReturnservice.GetAll())
                .Where(x => !string.IsNullOrWhiteSpace(x.ChallanNo))
                .OrderByDescending(x => x.Created)
                .Select(x => x.ChallanNo)
                .FirstOrDefault();

            return GenerateNextProductCode(lastCode);
        }

        private string GenerateNextProductCode(string lastCode)
        {
            if (string.IsNullOrWhiteSpace(lastCode))
                return "SR0001";

            // Last numeric sequence only
            var match = Regex.Match(lastCode, @"(\d+)(?!.*\d)");

            if (!match.Success)
                return "SR0001";

            if (!long.TryParse(match.Value, out long number))
            {
                return "SR0001";
            }

            string prefix = lastCode[..match.Index];
            string suffix = lastCode[(match.Index + match.Length)..];

            string nextNumber = (number + 1).ToString($"D{match.Length}");

            return $"{prefix}{nextNumber}{suffix}";
        }

        //public async Task<string> GenerateNxtNumber()
        //{
        //    var data = await _stockReturnservice.GetAll();
        //    var lastCode = data
        //        .Where(p => !string.IsNullOrEmpty(p.ChallanNo))
        //        .Select(p => p.ChallanNo)
        //        .LastOrDefault();

        //    return GenerateNextProductCode(lastCode);
        //}

        //private string GenerateNextProductCode(string lastCode)
        //{
        //    if (string.IsNullOrEmpty(lastCode) || lastCode.Length < 2)
        //        return "SR0001";

        //    string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray());

        //    string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

        //    int number = 0;
        //    int.TryParse(numberPart, out number);

        //    string nextCode = prefix + (number + 1).ToString("D" + numberPart.Length);

        //    return nextCode;
        //}

        //[HttpGet]
        //public async Task<IActionResult> GetItemsByCustomerIdForEditOnly(int customerId)
        //{
        //    // 1. Get all sales for customer
        //    var sales = await _salesservice.GetByCustomerId(customerId);

        //    if (sales == null || !sales.Any())
        //    {
        //        return Json(new { success = false, items = new List<object>() });
        //    }

        //    // 2. FLATTEN SalesItems (Deleted ignore)
        //    var salesItems = sales
        //        .SelectMany(s => s.SalesItems)
        //        .Where(si => si.Deleted == null && si.ItemMaster != null)
        //        .ToList();

        //    // 3. SOLD QTY (ItemMaster wise)
        //    var soldQtyByItem = salesItems
        //        .GroupBy(si => si.ItemMasterId)
        //        .Select(g => new
        //        {
        //            ItemMasterId = g.Key,
        //            SoldQty = g.Sum(x => x.Qty)
        //        })
        //        .ToList();

        //    // 4. RETURNED QTY (ItemMaster wise)
        //    var returnedQtyByItem = (await _stockReturnservice.GetByCustomerId(customerId))
        //        .SelectMany(x => x.StockReturnItems)
        //        .GroupBy(r => r.ItemMasterId)
        //        .Select(g => new
        //        {
        //            ItemMasterId = g.Key,
        //            ReturnedQty = g.Sum(x => x.Qty)
        //        })
        //        .ToList();

        //    // 5. ItemMaster data
        //    var itemMasters = await _itemmasterservice.GetAll();

        //    // 6. FINAL RESULT (Sold – Returned)
        //    var result = soldQtyByItem
        //        .Join(itemMasters,
        //            s => s.ItemMasterId,
        //            i => i.Id,
        //            (s, i) =>
        //            {
        //                var returnedQty = returnedQtyByItem
        //                    .FirstOrDefault(r => r.ItemMasterId == s.ItemMasterId)
        //                    ?.ReturnedQty ?? 0;

        //                //var finalQty = s.SoldQty - returnedQty;
        //                var finalQty = s.SoldQty;

        //                return new
        //                {
        //                    value = i.Id,
        //                    text = i.Name,
        //                    //qty = finalQty > 0 ? finalQty : 0,   // minus applied
        //                    qty = finalQty > 0 ? finalQty : 0, 
        //                    unit = i.Unit1,
        //                    mrp = i.Mrp
        //                };
        //            })
        //        .Where(x => x.qty > 0)   // 🔥 OPTIONAL: zero qty items hide
        //        .ToList();

        //    return Json(new { success = true, items = result });
        //}

        [HttpGet]
        public async Task<IActionResult> GetItemsByCustomerIdForEditOnly(int customerId, string billingType)
        {
            // ✅ GET USER SETTINGS
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            // 1. Get all sales for customer
            // var sales = await _salesservice.GetByCustomerId(customerId);

            List<Sales> sales = new List<Sales>();
             
            if (billingType == "walkin")
            {
                var allSales = await _salesservice.GetAll();

                sales = allSales
                    .Where(s => s.billingType == "Cash")
                    .ToList();
            }
             
            else if (billingType == "registered")
            {
                sales = (await _salesservice.GetByCustomerId(customerId))
                    .Where(s => s.billingType == "registered")
                    .ToList();
            }

            if (sales == null || !sales.Any())
            {
                return Json(new { success = false, items = new List<object>() });
            }

            // 2. FLATTEN SalesItems (Deleted ignore)
            var salesItems = sales
                .SelectMany(s => s.SalesItems)
                .Where(si => si.Deleted == null && si.ItemMaster != null)
                .ToList();

            // 3. SOLD QTY (ItemMaster wise)
            var soldQtyByItem = salesItems
                .GroupBy(si => si.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    SoldQty = g.Sum(x => x.Qty)
                })
                .ToList();

            // 4. RETURNED QTY (ItemMaster wise)
            //var returnedQtyByItem = (await _stockReturnservice.GetByCustomerId(customerId))
            //    .SelectMany(x => x.StockReturnItems)
            //    .GroupBy(r => r.ItemMasterId)
            //    .Select(g => new
            //    {
            //        ItemMasterId = g.Key,
            //        ReturnedQty = g.Sum(x => x.Qty)
            //    })
            //    .ToList();

            var returnedQtyByItem = new List<dynamic>();

            if (billingType == "registered")
            {
                returnedQtyByItem = (await _stockReturnservice.GetByCustomerId(customerId))
                    .SelectMany(x => x.StockReturnItems)
                    .GroupBy(r => r.ItemMasterId)
                    .Select(g => new
                    {
                        ItemMasterId = g.Key,
                        ReturnedQty = g.Sum(x => x.Qty)
                    })
                    .Cast<dynamic>()
                    .ToList();
            }

            // 5. ItemMaster data
            var itemMasters = await _itemmasterservice.GetAll();

            // 6. FINAL RESULT (Sold – Returned)
            var result = soldQtyByItem
                .Join(itemMasters,
                    s => s.ItemMasterId,
                    i => i.Id,
                    (s, i) =>
                    {
                        var returnedQty = returnedQtyByItem
                            .FirstOrDefault(r => r.ItemMasterId == s.ItemMasterId)
                            ?.ReturnedQty ?? 0;

                        // ✅ EDIT MODE: Show total sold qty (not minus returned)
                        var finalQty = s.SoldQty;

                        // ✅ STRIP:TABS FORMAT (TabletWise mode)
                        string displayQty;
                        if (setting != null && setting.ItemConversion == "TabletWise" && i.Conversion > 0)
                        {
                            int conversion = i.Conversion;
                            int totalTablets = (int)Math.Round(finalQty * conversion, MidpointRounding.AwayFromZero);
                            int strips = totalTablets / conversion;
                            int tablets = totalTablets % conversion;
                            displayQty = $"{strips}:{tablets}";
                        }
                        else
                        {
                            displayQty = finalQty.ToString("0.##");
                        }

                        return new
                        {
                            value = i.Id,
                            text = i.Name,
                            qty = finalQty > 0 ? finalQty : 0,
                            displayQty = displayQty,                          // ✅ ADDED
                            unit = i.Unit1,
                            mrp = i.Mrp,
                            conversion = i.Conversion > 0 ? (decimal)i.Conversion : 1m  // ✅ ADDED
                        };
                    })
                .Where(x => x.qty > 0)
                .ToList();

            return Json(new { success = true, items = result });
        }


        //[HttpGet]
        //public async Task<IActionResult> GetBatchesByItemIdForEditOnly(int id, int customerId, string billingType)
        //{
        //    // ✅ GET USER SETTINGS
        //    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    var setting = await _salsesettingservice.GetByUserId(userId);

        //    var item = await _itemmasterservice.GetByItemMasterId(id);

        //    if (item == null)
        //        return Json(new { error = "Item details not found." });

        //    // 🔹 1. Sales (customer-wise)
        //    var sales = await _salesservice.GetByCustomerId(customerId);
        //    if (sales == null)
        //        return Json(new { error = "No sales found for this customer." });

        //    var salesItems = sales
        //        .SelectMany(x => x.SalesItems)
        //        .Where(s => s.ItemMasterId == id && s.Deleted == null)
        //        .ToList();

        //    if (!salesItems.Any())
        //        return Json(new { error = "No sales items found for this item and customer." });

        //    // 🔹 2. Stock Returns (customer-wise)
        //    var returnedItems = (await _stockReturnservice.GetByCustomerId(customerId))
        //        .SelectMany(x => x.StockReturnItems)
        //        .Where(r => r.ItemMasterId == id)
        //        .ToList();

        //    // 🔹 3. Group SALES batch-wise
        //    var groupedResult = salesItems
        //        .GroupBy(b => new
        //        {
        //            b.Batch,
        //            b.Expirydate,
        //            b.Mrp,
        //            b.Rate
        //        })
        //        .Select(g =>
        //        {
        //            // 🔻 Returned qty for SAME batch
        //            var returnedQty = returnedItems
        //                .Where(r =>
        //                    r.Batch == g.Key.Batch &&
        //                    r.Expirydate == g.Key.Expirydate &&
        //                    r.Mrp == g.Key.Mrp &&
        //                    r.Rate == g.Key.Rate)
        //                .Sum(r => r.Qty);

        //            var soldQty = g.Sum(x => x.Qty);

        //            // ✅ EDIT MODE: Show total sold qty (not minus returned)
        //            var finalQty = soldQty;

        //            // ✅ STRIP:TABS FORMAT (TabletWise mode)
        //            string stripTabsQty;
        //            if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
        //            {
        //                int conversion = item.Conversion;
        //                int totalTablets = (int)Math.Round(finalQty * conversion, MidpointRounding.AwayFromZero);
        //                int strips = totalTablets / conversion;
        //                int tablets = totalTablets % conversion;
        //                stripTabsQty = $"{strips}:{tablets}";
        //            }
        //            else
        //            {
        //                stripTabsQty = finalQty.ToString("0.##");
        //            }

        //            return new
        //            {
        //                ItemId = id,
        //                batch = g.Key.Batch,
        //                expiry = g.Key.Expirydate,
        //                rate = g.Key.Rate,
        //                mrp = g.Key.Mrp,
        //                qty = finalQty > 0 ? finalQty : 0,
        //                stripTabsQty = stripTabsQty,                          // ✅ ADDED
        //                conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m  // ✅ ADDED
        //            };
        //        })
        //        .Where(x => x.qty > 0)
        //        .ToList();

        //    return Json(new
        //    {
        //        gst = item.Hsn?.IGST ?? 0,
        //        groupedResult
        //    });
        //}

        [HttpGet]
        public async Task<IActionResult> GetBatchesByItemIdForEditOnly(int id, int customerId, string billingType)
        { 
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            var item = await _itemmasterservice.GetByItemMasterId(id);

            if (item == null)
                return Json(new { error = "Item details not found." });
             
            var sales = await _salesservice.GetAll();

            if (billingType == "walkin")
            {
                sales = sales.Where(x => x.billingType == "Cash").ToList();
            }
            else
            {
                sales = sales.Where(x => x.billingType == "registered" && x.CustomerId == customerId).ToList();
            }

            var salesItems = sales.SelectMany(x => x.SalesItems).Where(s => s.ItemMasterId == id && s.Deleted == null).ToList();

            if (!salesItems.Any())
                return Json(new { error = "No sales items found for this item." });
             
            var returns = await _stockReturnservice.GetAll();

            if (billingType == "walkin")
            {
                returns = returns
                    .Where(x => x.billingType == "Cash")
                    .ToList();
            }
            else
            {
                returns = returns.Where(x => x.billingType == "registered" && x.CustomerId == customerId).ToList();
            }

            var returnedItems = returns.SelectMany(x => x.StockReturnItems).Where(r => r.ItemMasterId == id).ToList();
             
            var groupedResult = salesItems
                .GroupBy(b => new
                {
                    b.Batch,
                    b.Expirydate,
                    b.Mrp,
                    b.Rate
                })
                .Select(g =>
                {
                    var returnedQty = returnedItems
                        .Where(r =>
                            r.Batch == g.Key.Batch &&
                            r.Expirydate == g.Key.Expirydate &&
                            r.Mrp == g.Key.Mrp &&
                            r.Rate == g.Key.Rate)
                        .Sum(r => r.Qty);
                    var striperate = g.FirstOrDefault()?.StripRate ?? 0;
                    var soldQty = g.Sum(x => x.Qty);

                    var finalQty = soldQty;

                    string stripTabsQty;

                    if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
                    {
                        int conversion = item.Conversion;
                        int totalTablets = (int)Math.Round(finalQty * conversion, MidpointRounding.AwayFromZero);
                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;
                        stripTabsQty = $"{strips}:{tablets}";
                    }
                    else
                    {
                        stripTabsQty = finalQty.ToString("0.##");
                    }

                    return new
                    {
                        itemId = id,
                        batch = g.Key.Batch,
                        expiry = g.Key.Expirydate,
                        rate = g.Key.Rate,
                        striprate = striperate,
                        mrp = g.Key.Mrp,
                        qty = finalQty > 0 ? finalQty : 0,
                        stripTabsQty = stripTabsQty,
                        conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m
                    };
                }).Where(x => x.qty > 0).ToList();

            return Json(new
            {
                gst = item.Hsn?.IGST ?? 0,
                cess = item.Hsn?.Cess ?? 0,
                groupedResult
            });
        }
        //[HttpGet]
        //public async Task<IActionResult> GetBatchesByItemIdForEditOnly(int id, int customerId)
        //{
        //    var item = await _itemmasterservice.GetByItemMasterId(id);

        //    if (item == null)
        //        return Json(new { error = "Item details not found." });

        //    // 🔹 1. Sales (customer-wise)
        //    var sales = await _salesservice.GetByCustomerId(customerId);
        //    if (sales == null)
        //        return Json(new { error = "No sales found for this customer." });

        //    var salesItems = sales
        //        .SelectMany(x => x.SalesItems)
        //        .Where(s => s.ItemMasterId == id && s.Deleted == null)
        //        .ToList();

        //    if (!salesItems.Any())
        //        return Json(new { error = "No sales items found for this item and customer." });

        //    // 🔹 2. Stock Returns (customer-wise)
        //    var returnedItems = (await _stockReturnservice.GetByCustomerId(customerId))
        //        .SelectMany(x => x.StockReturnItems)
        //        .Where(r => r.ItemMasterId == id)
        //        .ToList();

        //    // 🔹 3. Group SALES batch-wise
        //    var groupedResult = salesItems
        //        .GroupBy(b => new
        //        {
        //            b.Batch,
        //            b.Expirydate,
        //            b.Mrp,
        //            b.Rate
        //        })
        //        .Select(g =>
        //        {
        //            // 🔻 Returned qty for SAME batch
        //            var returnedQty = returnedItems
        //                .Where(r =>
        //                    r.Batch == g.Key.Batch &&
        //                    r.Expirydate == g.Key.Expirydate &&
        //                    r.Mrp == g.Key.Mrp &&
        //                    r.Rate == g.Key.Rate)
        //                .Sum(r => r.Qty);

        //            var soldQty = g.Sum(x => x.Qty);
        //            //var finalQty = soldQty - returnedQty;
        //            var finalQty = soldQty;

        //            return new
        //            {
        //                ItemId = id,
        //                batch = g.Key.Batch,
        //                expiry = g.Key.Expirydate,
        //                rate = g.Key.Rate,
        //                mrp = g.Key.Mrp,
        //                qty = finalQty > 0 ? finalQty : 0   //  minus applied safely
        //            };
        //        })
        //        .Where(x => x.qty > 0)   // 🔥 zero qty batches hide
        //        .ToList();

        //    return Json(new
        //    {
        //        gst = item.Hsn?.IGST ?? 0,
        //        groupedResult
        //    });
        //}

        [HttpGet]
        public async Task<IActionResult> GetReturnedQtyDataForEditOnly(int customerId, int? excludeReturnId = null)
        {
            // All sales
            var sales = await _salesservice.GetByCustomerId(customerId);
            if (sales == null || !sales.Any())
                return Json(new List<object>());

            var salesItems = sales
                .SelectMany(x => x.SalesItems)
                .Where(x => x.Deleted == null)
                .ToList();

            // All returns (EXCLUDE current editing entry)
            var allReturns = await _stockReturnservice.GetByCustomerId(customerId);

            var returnItems = allReturns
                .Where(r => !excludeReturnId.HasValue || r.Id != excludeReturnId.Value)
                .SelectMany(x => x.StockReturnItems)
                .ToList();

            // Group by SAME KEYS
            var result = salesItems
                .GroupBy(s => new
                {
                    s.ItemMasterId,
                    s.Batch,
                    s.Expirydate,
                    s.Mrp,
                    s.Rate
                })
                .Select(g =>
                {
                    // Total Sold Qty
                    var soldQty = g.Sum(x => x.Qty);

                    // Total Returned Qty (same keys)
                    var returnedQty = returnItems
                        .Where(r =>
                            r.ItemMasterId == g.Key.ItemMasterId &&
                            r.Batch == g.Key.Batch &&
                            r.Expirydate == g.Key.Expirydate &&
                            r.Mrp == g.Key.Mrp &&
                            r.Rate == g.Key.Rate
                        )
                        .Sum(x => x.Qty);

                    return new
                    {
                        ItemMasterId = g.Key.ItemMasterId,
                        Batch = g.Key.Batch,
                        Expiry = g.Key.Expirydate,
                        Mrp = g.Key.Mrp,
                        Rate = g.Key.Rate,

                        ReturnedQty = soldQty
                        //ReturnedQty = Math.Min(returnedQty, soldQty),
                    };
                })
                .ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetItemsByCustomerId(int customerId, string billingType)
        {
            // Get user settings
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            // 1. Get all sales for customer
            // var sales = await _salesservice.GetByCustomerId(customerId);
            ICollection<Sales> sales;

            if (billingType == "registered")
            {
                sales = await _salesservice.GetByCustomerId(customerId);
            }
            else
            {
                sales = (await _salesservice.GetAll()).Where(x => x.billingType == "Cash").ToList();
            }

            if (sales == null || !sales.Any())
            {
                return Json(new { success = false, items = new List<object>() });
            }

            // 2. FLATTEN SalesItems (Deleted ignore)
            var salesItems = sales
                .SelectMany(s => s.SalesItems)
                .Where(si => si.Deleted == null && si.ItemMaster != null)
                .ToList();

            // 3. SOLD QTY (ItemMaster wise)
            var soldQtyByItem = salesItems
                .GroupBy(si => si.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    SoldQty = g.Sum(x => x.Qty)
                })
                .ToList();

            // 4. RETURNED QTY (ItemMaster wise)
            var returnedQtyByItem = (await _stockReturnservice.GetByCustomerId(customerId))
                .SelectMany(x => x.StockReturnItems)
                .GroupBy(r => r.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    ReturnedQty = g.Sum(x => x.Qty)
                })
                .ToList();

            // 5. ItemMaster data
            var itemMasters = await _itemmasterservice.GetAll();

            // 6. FINAL RESULT (Sold – Returned)
            var result = soldQtyByItem
                .Join(itemMasters,
                    s => s.ItemMasterId,
                    i => i.Id,
                    (s, i) =>
                    {
                        var returnedQty = returnedQtyByItem
                            .FirstOrDefault(r => r.ItemMasterId == s.ItemMasterId)
                            ?.ReturnedQty ?? 0;

                        var finalQty = s.SoldQty - returnedQty;

                        // ✅ STRIP:TABS FORMAT (TabletWise mode)
                        string displayQty;
                        if (setting != null && setting.ItemConversion == "TabletWise" && i.Conversion > 0)
                        {
                            int conversion = i.Conversion;
                            int totalTablets = (int)Math.Round(finalQty * conversion, MidpointRounding.AwayFromZero);
                            int strips = totalTablets / conversion;
                            int tablets = totalTablets % conversion;
                            displayQty = $"{strips}:{tablets}";
                        }
                        else
                        {
                            displayQty = finalQty.ToString("0.##");
                        }

                        return new
                        {
                            value = i.Id,
                            text = i.Name,
                            qty = finalQty > 0 ? finalQty : 0,   // ✅ minus applied
                            displayQty = displayQty,
                            unit = i.Unit1,
                            mrp = i.Mrp,
                            conversion = i.Conversion > 0 ? (decimal)i.Conversion : 1m
                        };
                    })
                .Where(x => x.qty > 0)   // 🔥 OPTIONAL: zero qty items hide
                .ToList();

            return Json(new { success = true, items = result });
        }


        //[HttpGet]
        //public async Task<IActionResult> GetItemsByCustomerId(int customerId)
        //{
        //    var sales = await _salesservice.GetByCustomerId(customerId);

        //    if (sales == null || !sales.Any())
        //    {
        //        return Json(new { success = false, items = new List<object>() });
        //    }

        //    // 🔹 Flatten all SalesItems (Deleted != null ignore)
        //    var salesItems = sales
        //        .SelectMany(s => s.SalesItems)
        //        .Where(si => si.Deleted == null && si.ItemMaster != null)
        //        .ToList();

        //    // 🔹 Group by ItemMasterId and SUM Qty
        //    var groupedItems = salesItems
        //        .GroupBy(si => si.ItemMasterId)
        //        .Select(g => new
        //        {
        //            ItemMasterId = g.Key,
        //            TotalQty = g.Sum(x => x.Qty)
        //        })
        //        .ToList();

        //    // 🔹 Get ItemMaster details
        //    var itemMasters = await _itemmasterservice.GetAll();

        //    var result = groupedItems
        //        .Join(itemMasters,
        //              g => g.ItemMasterId,
        //              i => i.Id,
        //              (g, i) => new
        //              {
        //                  value = i.Id,
        //                  text = i.Name,
        //                  qty = g.TotalQty,
        //                  unit = i.Unit1,
        //                  mrp = i.Mrp
        //              })
        //        .ToList();

        //    return Json(new { success = true, items = result });
        //}


        //[HttpGet]
        //public async Task<IActionResult> GetItemsByCustomerId(int customerId)
        //{
        //    // Get all sales for the given customer
        //    var sales = await _salesservice.GetByCustomerId(customerId);

        //    if (sales == null)
        //    {
        //        return Json(new { success = false, items = new List<object>() });
        //    }

        //    // Get unique ItemMasterIds from sales items
        //    var itemMasterIds = sales.SelectMany(x => x.SalesItems).Where(x => x.ItemMaster != null).Select(x => x.ItemMasterId).Distinct().ToList();

        //    // Fetch full ItemMaster entities based on IDs (optional - only if needed)
        //    var itemMasters = (await _itemmasterservice.GetAll()).Where(x => itemMasterIds.Contains(x.Id));

        //    // Prepare the result
        //    var result = itemMasters.Select(i => new
        //    {
        //        value = i.Id,
        //        text = i.Name
        //    });

        //    return Json(new { success = true, items = result });
        //}

        //[HttpGet]
        //public async Task<IActionResult> GetReturnedQtyData(int customerId)
        //{
        //    var returnedQtyData = (await _stockReturnservice.GetByCustomerId(customerId)).SelectMany(x => x.StockReturnItems)
        //        .GroupBy(r => new
        //        {
        //            r.ItemMasterId,
        //            r.Batch,
        //            Expiry = r.Expirydate,
        //            r.Mrp,
        //            r.Rate,
        //        })
        //        .Select(g => new
        //        {
        //            g.Key.ItemMasterId,
        //            g.Key.Batch,
        //            Expiry = g.Key.Expiry,
        //            Mrp = g.Key.Mrp,
        //            Rate = g.Key.Rate,
        //            ReturnedQty = g.Sum(x => x.Qty)
        //        })
        //        .ToList();

        //    return Json(returnedQtyData);
        //}

        [HttpGet]
        public async Task<IActionResult> GetReturnedQtyData(int customerId)
        {
            // 1️⃣ All sales
            var sales = await _salesservice.GetByCustomerId(customerId);
            if (sales == null || !sales.Any())
                return Json(new List<object>());

            var salesItems = sales
                .SelectMany(x => x.SalesItems)
                .Where(x => x.Deleted == null)
                .ToList();

            // 2️⃣ All returns
            var returnItems = (await _stockReturnservice.GetByCustomerId(customerId))
                .SelectMany(x => x.StockReturnItems)
                .ToList();

            // 3️⃣ Group by SAME KEYS
            var result = salesItems
                .GroupBy(s => new
                {
                    s.ItemMasterId,
                    s.Batch,
                    s.Expirydate,
                    s.Mrp,
                    s.Rate
                })
                .Select(g =>
                {
                    var soldQty = g.Sum(x => x.Qty);

                    var returnedQty = returnItems
                        .Where(r =>
                            r.ItemMasterId == g.Key.ItemMasterId &&
                            r.Batch == g.Key.Batch &&
                            r.Expirydate == g.Key.Expirydate &&
                            r.Mrp == g.Key.Mrp &&
                            r.Rate == g.Key.Rate
                        )
                        .Sum(x => x.Qty);

                    return new
                    {
                        ItemMasterId = g.Key.ItemMasterId,
                        Batch = g.Key.Batch,
                        Expiry = g.Key.Expirydate,
                        Mrp = g.Key.Mrp,
                        Rate = g.Key.Rate,

                        // 🔐 HARD SAFETY
                        ReturnedQty = Math.Min(returnedQty, soldQty)
                    };
                })
                .ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetBatchesByItemId(int id, int customerId, string billingType)
        {
            // Get user settings
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            var item = await _itemmasterservice.GetByItemMasterId(id);

            if (item == null)
                return Json(new { error = "Item details not found." });

            // 🔹 1. Sales (customer-wise)
            // var sales = await _salesservice.GetByCustomerId(customerId);
            ICollection<Sales> sales;

            if (billingType == "registered")
            {
                sales = await _salesservice.GetByCustomerId(customerId);
            }
            else
            {
                sales = (await _salesservice.GetAll())
                    .Where(x => x.billingType == "Cash")
                    .ToList();
            }
            if (sales == null)
                return Json(new { error = "No sales found for this customer." });

            var salesItems = sales
                .SelectMany(x => x.SalesItems)
                .Where(s => s.ItemMasterId == id && s.Deleted == null)
                .ToList();

            if (!salesItems.Any())
                return Json(new { error = "No sales items found for this item and customer." });

            // 🔹 2. Stock Returns (customer-wise)
            //var returnedItems = (await _stockReturnservice.GetByCustomerId(customerId))
            //    .SelectMany(x => x.StockReturnItems)
            //    .Where(r => r.ItemMasterId == id)
            //    .ToList();

            List<StockReturnItem> returnedItems;

            if (billingType == "registered")
            {
                returnedItems = (await _stockReturnservice.GetByCustomerId(customerId))
                    .SelectMany(x => x.StockReturnItems)
                    .Where(r => r.ItemMasterId == id)
                    .ToList();
            }
            else
            {
                returnedItems = (await _stockReturnservice.GetAll())
                    .Where(x => x.billingType == "Cash")
                    .SelectMany(x => x.StockReturnItems)
                    .Where(r => r.ItemMasterId == id)
                    .ToList();
            }

            // 🔹 3. Group SALES batch-wise
            var groupedResult = salesItems
                .GroupBy(b => new
                {
                    b.Batch,
                    b.Expirydate,
                    b.Mrp,
                    b.Rate
                })
                .Select(g =>
                {
                    // 🔻 Returned qty for SAME batch
                    var returnedQty = returnedItems
                        .Where(r =>
                            r.Batch == g.Key.Batch &&
                            r.Expirydate == g.Key.Expirydate &&
                            r.Mrp == g.Key.Mrp &&
                            r.Rate == g.Key.Rate)
                        .Sum(r => r.Qty);

                    var soldQty = g.Sum(x => x.Qty);
                    var finalQty = soldQty - returnedQty;

                    string stripTabsQty;
                    if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
                    {
                        int conversion = item.Conversion;
                        int totalTablets = (int)Math.Round(finalQty * conversion, MidpointRounding.AwayFromZero);
                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;
                        stripTabsQty = $"{strips}:{tablets}";
                    }
                    else
                    {
                        stripTabsQty = finalQty.ToString("0.##");
                    }

                    return new
                    {
                        ItemId = id,
                        batch = g.Key.Batch,
                        expiry = g.Key.Expirydate,
                        rate = g.Key.Rate,
                        striprate = g.FirstOrDefault()?.StripRate ?? 0,
                        mrp = g.Key.Mrp,
                        qty = finalQty > 0 ? finalQty : 0,   // ✅ minus applied safely
                        stripTabsQty = stripTabsQty,
                        conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m
                    };
                })
                .Where(x => x.qty > 0)   // 🔥 zero qty batches hide
                .ToList();

            return Json(new
            {
                gst = item.Hsn?.IGST ?? 0,
                cess = item.Hsn?.Cess ?? 0,
                groupedResult
            });
        }

        //[HttpGet]
        //public async Task<IActionResult> GetBatchesByItemId(int id, int customerId)
        //{
        //    var item = await _itemmasterservice.GetByItemMasterId(id);

        //    if (item != null)
        //    {
        //        // 👇 Get all sales for this customer (include SalesItems and related ItemMaster if not already)
        //        var sales = await _salesservice.GetByCustomerId(customerId);

        //        if (sales == null)
        //        {
        //            return Json(new { error = "No sales found for this customer." });
        //        }

        //        // 👇 Filter sales items by ItemMasterId
        //        var salesItems = sales.SelectMany(x => x.SalesItems).Where(s => s.ItemMasterId == id).ToList();

        //        if (!salesItems.Any())
        //        {
        //            return Json(new { error = "No sales items found for this item and customer." });
        //        }

        //        // 👇 Group and format data like you did before
        //        var groupedResult = salesItems
        //            .GroupBy(b => new { b.Batch, b.Expirydate, b.Mrp, b.Rate })
        //            .Select(g => new
        //            {
        //                ItemId = g.Select(x => x.ItemMasterId).FirstOrDefault(),
        //                batch = g.Key.Batch,
        //                expiry = g.Key.Expirydate,
        //                rate = g.Key.Rate,
        //                mrp = g.Key.Mrp,
        //                qty = g.Sum(x => x.Qty), // total qty from sales
        //                salesItemId = g.Select(x => x.Id).FirstOrDefault()
        //            }).ToList();

        //        var model = new
        //        {
        //            gst = item.Hsn?.IGST ?? 0,
        //            groupedResult = groupedResult
        //        };

        //        return Json(model);
        //    }
        //    else
        //    {
        //        return Json(new { error = "Item details not found." });
        //    }
        //}

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
                        purchaseItems = (await _purchaseitemservice.GetByItemMasterId(itemId)).Select(x => new
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
        public async Task<IActionResult> GetSalesReturnSummary(int customerId, int itemId)
        {
            // 🔹 1. All sales for customer + item
            var sales = await _salesservice.GetByCustomerId(customerId);

            if (sales == null || !sales.Any())
                return Json(new { success = false, data = new List<object>() });

            var salesItems = sales
                .SelectMany(s => s.SalesItems)
                .Where(si =>
                    si.Deleted == null &&
                    si.ItemMasterId == itemId)
                .Select(si => new
                {
                    si.Sales.BillNo,
                    si.ItemMasterId,
                    si.Batch,
                    si.Expirydate,
                    si.Mrp,
                    si.Rate,
                    SalesQty = si.Qty,
                    SalesId = si.SalesId
                })
                .OrderBy(x => x.SalesId)   // 🔑 FIFO (oldest bill first)
                .ToList();

            if (!salesItems.Any())
                return Json(new { success = false, data = new List<object>() });

            // 🔹 2. Total returned qty (batch-wise, NOT bill-wise)
            var returnedQty = (await _stockReturnservice.GetByCustomerId(customerId))
                .SelectMany(r => r.StockReturnItems)
                .Where(r => r.ItemMasterId == itemId)
                .GroupBy(r => new
                {
                    r.ItemMasterId,
                    r.Batch,
                    r.Expirydate,
                    r.Mrp,
                    r.Rate
                })
                .Select(g => new
                {
                    g.Key.ItemMasterId,
                    g.Key.Batch,
                    g.Key.Expirydate,
                    g.Key.Mrp,
                    g.Key.Rate,
                    ReturnedQty = g.Sum(x => x.Qty)
                })
                .ToList();

            // 🔹 3. FIFO adjustment
            var result = new List<object>();

            foreach (var grp in salesItems.GroupBy(x => new
            {
                x.ItemMasterId,
                x.Batch,
                x.Expirydate,
                x.Mrp,
                x.Rate
            }))
            {
                var totalReturned = returnedQty
                    .FirstOrDefault(r =>
                        r.ItemMasterId == grp.Key.ItemMasterId &&
                        r.Batch == grp.Key.Batch &&
                        r.Expirydate == grp.Key.Expirydate &&
                        r.Mrp == grp.Key.Mrp &&
                        r.Rate == grp.Key.Rate)
                    ?.ReturnedQty ?? 0;

                decimal remainingReturnToAdjust = totalReturned;

                foreach (var sale in grp)
                {
                    decimal appliedReturn = 0;

                    if (remainingReturnToAdjust > 0)
                    {
                        appliedReturn = Math.Min(remainingReturnToAdjust, sale.SalesQty);
                        remainingReturnToAdjust -= appliedReturn;
                    }

                    result.Add(new
                    {
                        BillNo = sale.BillNo,
                        Batch = sale.Batch,
                        SalesQty = sale.SalesQty,
                        ReturnQty = appliedReturn,
                        RemainingQty = sale.SalesQty - appliedReturn
                    });
                }
            }

            return Json(new
            {
                success = true,
                data = result
                    .OrderBy(x => x.GetType().GetProperty("BillNo")?.GetValue(x))
                    .ToList()
            });
        }

        

        //[HttpGet]
        //public async Task<IActionResult> Edit(int Id)
        //{
        //    StockReturn model = await _stockReturnservice.GetById(Id);
        //    StockReturnVM VM = new StockReturnVM();

        //    // Get all items for name lookup
        //    var allItems = await _itemmasterservice.GetAll();

        //    if (model != null)
        //    {
        //        VM.Id = model.Id;
        //        VM.CustomerId = model.CustomerId;
        //        VM.CustomerName = model.Customers?.Name;
        //        VM.ChallanNo = model.ChallanNo;
        //        VM.ChallanDate = model.ChallanDate;
        //        VM.MobileNo = model.MobileNo;
        //        VM.Address = model.Address;
        //        VM.PharmacyDoctorId = model.PharmacyDoctorId;
        //        VM.DoctorName = model.PharmacyDoctor?.Name;
        //        VM.DoctorMobileNumber = model.DoctorMobileNumber;
        //        VM.DoctorRegNumber = model.DoctorRegNumber;
        //        VM.Total = model.Total;
        //        VM.TotalGstAmt = model.TotalGstAmt;
        //        VM.discountPercent = model.discountPercent;
        //        VM.discountAmount = model.discountAmount;
        //        VM.Totaldiscount = model.Totaldiscount;
        //        VM.TotalPayable = model.TotalPayable;

        //        // Proper ItemName assignment
        //        VM.StockReturnItemVMs = model.StockReturnItems != null
        //            ? model.StockReturnItems.Select(x =>
        //            {

        //                var item = allItems.FirstOrDefault(i => i.Id == x.ItemMasterId);
        //                return new StockReturnItemVM
        //                {
        //                    Id = x.Id,
        //                    StockReturnId = x.StockReturnId,
        //                    ItemMasterId = x.ItemMasterId,
        //                    ItemName = item?.Name ?? "",  // Item name from lookup
        //                    PurchaseItemId = x.PurchaseItemId,
        //                    Batch = x.Batch,
        //                    Expirydate = x.Expirydate,
        //                    Mrp = x.Mrp,
        //                    Qty = x.Qty,
        //                    Rate = x.Rate,
        //                    Gst = x.Gst,
        //                    Discount = x.Discount,
        //                    Amount = x.Amount,
        //                    Reason = x.Reason,
        //                };
        //            }).ToList()
        //            : new List<StockReturnItemVM>();
        //    }

        //    ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");

        //    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    var setting = await _salsesettingservice.GetByUserId(userId);
        //    VM.DoctorRequired = setting?.DoctorRequired ?? false;

        //    // Add BusinessType
        //    var tenantId = User?.FindFirst("TenantId")?.Value;
        //    var user = (await _tenantRepository.GetAll())
        //        .FirstOrDefault(x => x.Id == tenantId);
        //    var businessType = user?.BusinessType ?? 0;
        //    ViewBag.BusinessType = (int)businessType;

        //    var sales = await _salesservice.GetByCustomerId(VM.CustomerId);

        //    // Get unique ItemMasterIds from sales items
        //    var itemMasterIds = sales.SelectMany(x => x.SalesItems)
        //        .Where(x => x.ItemMaster != null)
        //        .Select(x => x.ItemMasterId)
        //        .Distinct()
        //        .ToList();

        //    // Fetch full ItemMaster entities based on IDs
        //    var itemMasters = allItems.Where(x => itemMasterIds.Contains(x.Id));

        //    // Prepare the result
        //    ViewBag.Items = new SelectList(itemMasters.Select(i => new
        //    {
        //        Value = i.Id,
        //        Text = i.Name
        //    }).ToList(), "Value", "Text");

        //    var allPurchaseItems = await _purchaseitemservice.GetAll();
        //    var stockissueitemmasterId = VM.StockReturnItemVMs.Select(x => x.ItemMasterId).ToList();
        //    var filteredPurchaseItems = allPurchaseItems
        //        .Where(x => stockissueitemmasterId.Contains(x.ItemId))
        //        .ToList();

        //    ViewBag.purchaseItem = new SelectList(filteredPurchaseItems, "Id", "Batch");

        //    return View(VM);
        //}

        //[HttpGet]
        //public async Task<IActionResult> Edit(int Id)
        //{
        //    StockReturn model = await _stockReturnservice.GetById(Id);
        //    StockReturnVM VM = new StockReturnVM();

        //    if (model != null)
        //    {
        //        VM.Id = model.Id;
        //        VM.CustomerId = model.CustomerId;
        //        VM.CustomerName = model.Customers?.Name;
        //        VM.ChallanNo = model.ChallanNo;
        //        VM.ChallanDate = model.ChallanDate;
        //        VM.MobileNo = model.MobileNo;
        //        VM.Address = model.Address;
        //        VM.PharmacyDoctorId = model.PharmacyDoctorId;
        //        VM.DoctorName = model.PharmacyDoctor?.Name;
        //        VM.DoctorMobileNumber = model.DoctorMobileNumber;
        //        VM.DoctorRegNumber = model.DoctorRegNumber;
        //        VM.Total = model.Total;
        //        VM.TotalGstAmt = model.TotalGstAmt;
        //        VM.discountPercent = model.discountPercent;
        //        VM.discountAmount = model.discountAmount;
        //        VM.Totaldiscount = model.Totaldiscount;
        //        VM.TotalPayable = model.TotalPayable;
        //        VM.StockReturnItemVMs = model.StockReturnItems != null
        //            ? model.StockReturnItems.Select(x => new StockReturnItemVM
        //            {
        //                Id = x.Id,
        //                StockReturnId = x.StockReturnId,
        //                ItemMasterId = x.ItemMasterId,
        //                PurchaseItemId = x.PurchaseItemId,
        //                Batch = x.Batch,
        //                Expirydate = x.Expirydate,
        //                Mrp = x.Mrp,
        //                Qty = x.Qty,
        //                Rate = x.Rate,
        //                Gst = x.Gst,
        //                Discount = x.Discount,
        //                Amount = x.Amount,
        //                Reason = x.Reason,
        //            }).ToList()
        //            : new List<StockReturnItemVM>();
        //    }
        //    ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");

        //    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    var setting = await _salsesettingservice.GetByUserId(userId);
        //    VM.DoctorRequired = setting?.DoctorRequired ?? false;

        //    var sales = await _salesservice.GetByCustomerId(VM.CustomerId);

        //    // Get unique ItemMasterIds from sales items
        //    var itemMasterIds = sales.SelectMany(x => x.SalesItems).Where(x => x.ItemMaster != null).Select(x => x.ItemMasterId).Distinct().ToList();

        //    // Fetch full ItemMaster entities based on IDs (optional - only if needed)
        //    var itemMasters = (await _itemmasterservice.GetAll()).Where(x => itemMasterIds.Contains(x.Id));

        //    // Prepare the result
        //    ViewBag.Items = new SelectList(itemMasters.Select(i => new
        //    {
        //        Value = i.Id,
        //        Text = i.Name
        //    }).ToList(), "Value", "Text");

        //    // ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
        //    var allPurchaseItems = await _purchaseitemservice.GetAll();
        //    var stockissueitemmasterId = VM.StockReturnItemVMs.Select(x => x.ItemMasterId).ToList();

        //    var filteredPurchaseItems = allPurchaseItems
        //        .Where(x => stockissueitemmasterId.Contains(x.ItemId))
        //        .ToList();

        //    ViewBag.purchaseItem = new SelectList(filteredPurchaseItems, "Id", "Batch");
        //    return View(VM);
        //}



        public async Task<IActionResult> StockReturnReportCustomerWise(DateTime? fromDate,DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            var sales = (await _stockReturnservice.GetAll())
       .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
       .ToList();
            var customerwiseList = sales
                .GroupBy(x => x.CustomerId)
                .Select(sale => new StockReturnVM
                {
                    Id = sale.Select(x => x.Id).FirstOrDefault(),
                    ChallanNo = sale.Select(x => x.ChallanNo).FirstOrDefault(),
                    ChallanDate = sale.Select(x => x.ChallanDate).FirstOrDefault(),
                    CustomerName = sale.Select(x => x.Customers?.Name).FirstOrDefault() ?? "Unknown",
                    MobileNo = sale.Select(x => x.MobileNo).FirstOrDefault(),
                    TotalGstAmt = sale.Sum(x => x.TotalGstAmt),
                    TotalPayable = sale
                        .SelectMany(x => x.StockReturnItems ?? new List<StockReturnItem>())
                        .Sum(item => item.Amount)
                })
                .OrderByDescending(x => x.ChallanDate)
                .ToList();
            ViewBag.TotalGst=customerwiseList.Sum(x => x.TotalGstAmt);
            ViewBag.TotalPayable=customerwiseList.Sum(x => x.TotalPayable);
            return View(customerwiseList);
        }

        //EXPORT TO EXCEL
        [HttpGet]
        public async Task<IActionResult> ExportStockReturnCustomerWiseExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = (await _stockReturnservice.GetAll())
                .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
                .ToList();

            var data = sales
                .GroupBy(x => x.CustomerId)
                .Select(g => new
                {
                    CreditNoteNo = g.First().ChallanNo,
                    Date = g.First().ChallanDate,
                    CustomerName = g.First().Customers?.Name ?? "Unknown",
                    TotalGstAmt = g.Sum(x => x.TotalGstAmt),
                    TotalPayable = g
                        .SelectMany(x => x.StockReturnItems ?? new List<StockReturnItem>())
                        .Sum(i => i.Amount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stock Return");

            // Company Name
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            // HEADER
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 5).Merge().Style
                .Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Stock Return Customer Wise Report";
            ws.Range(2, 1, 2, 5).Merge().Style
                .Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 5).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TABLE HEADER
            int row = 5;

            string[] headers =
            { "Credit Note No", "Date", "CustomerName", "TotalGstAmt", "TotalPayable" };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
                ws.Cell(row, i + 1).Style.Font.Bold = true;
            }

            row++;

            decimal totalGst = 0;
            decimal totalPayable = 0;

            foreach (var x in data)
            {
                ws.Cell(row, 1).Value = x.CreditNoteNo;
                ws.Cell(row, 2).Value = x.Date?.ToString("dd-MM-yyyy");
                ws.Cell(row, 3).Value = x.CustomerName;
                ws.Cell(row, 4).Value = x.TotalGstAmt;
                ws.Cell(row, 5).Value = x.TotalPayable;

                ws.Range(row, 4, row, 5).Style.NumberFormat.Format = "0.00";

                totalGst += x.TotalGstAmt;
                totalPayable += x.TotalPayable;

                row++;
            }

            // TOTAL ROW (EXACT FORMAT)
            ws.Cell(row, 1).Value = "Total";
            ws.Cell(row, 1).Style.Font.Bold = true;

            ws.Cell(row, 4).Value = totalGst;
            ws.Cell(row, 5).Value = totalPayable;

            ws.Range(row, 4, row, 5).Style.NumberFormat.Format = "0.00";
            ws.Range(row, 1, row, 5).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "StockReturnCustomerWise.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportStockReturnCustomerWisePdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = (await _stockReturnservice.GetAll())
                .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
                .ToList();

            var data = sales
                .GroupBy(x => x.CustomerId)
                .Select(g => new
                {
                    CreditNoteNo = g.First().ChallanNo,
                    Date = g.First().ChallanDate,
                    CustomerName = g.First().Customers?.Name ?? "Unknown",
                    TotalGstAmt = g.Sum(x => x.TotalGstAmt),
                    TotalPayable = g
                        .SelectMany(x => x.StockReturnItems ?? new List<StockReturnItem>())
                        .Sum(i => i.Amount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fs = 9;

            // Company Name
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            // HEADER
            document.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Stock Return Customer Wise Report")
                .SetFont(bold).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // TABLE
            Table table = new Table(new float[] { 4, 3, 6, 3, 3 })
                .UseAllAvailableWidth();

            string[] headers =
            { "Credit Note No", "Date", "CustomerName", "TotalGstAmt", "TotalPayable" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(fs))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            decimal totalGst = 0;
            decimal totalPayable = 0;

            foreach (var x in data)
            {
                table.AddCell(new Paragraph(x.CreditNoteNo ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Date?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.CustomerName ?? "").SetFont(normal).SetFontSize(fs));

                table.AddCell(new Paragraph(x.TotalGstAmt.ToString("0.00"))
                    .SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Paragraph(x.TotalPayable.ToString("0.00"))
                    .SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

                totalGst += x.TotalGstAmt;
                totalPayable += x.TotalPayable;
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 3)
                .Add(new Paragraph("Total").SetFont(bold).SetFontSize(fs))
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(totalGst.ToString("0.00"))
                .SetFont(bold).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(totalPayable.ToString("0.00"))
                .SetFont(bold).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(),
                "application/pdf",
                "StockReturnCustomerWise.pdf");
        }


        [HttpGet]
        public async Task<IActionResult> StockReturnReportItemwise(DateTime? fromDate, DateTime? toDate)
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

            var stockReturns = (await _stockReturnservice.GetAll())
                .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
                .ToList();

            var stockreturnItems = stockReturns
                .Where(s => s.StockReturnItems != null)
                .SelectMany(s => s.StockReturnItems)
                .ToList();

            var groupedstockreturn = stockreturnItems
                .GroupBy(x => x.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Amount),
                    TotalGst = g.Sum(x => x.Gst)
                })
                .ToList();

            var stockreturnList = (from item in itemMasters
                                   join g in groupedstockreturn on item.Id equals g.ItemMasterId
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
                                        
                                       StockDisplay = GetConvertedQty(g.TotalQty, item.Conversion),
                                       Conversion = item.Conversion,
                                       Amount = g.TotalAmount,
                                       GstAmount = g.TotalGst
                                   }).ToList();
            var totalQty = stockreturnList.Sum(x => x.Stocks);
            ViewBag.TotalQty = totalQty;

            int totalTablets = 0;

            foreach (var item in stockreturnList)
            {
                if (item.Conversion > 0)
                {
                    totalTablets += (int)Math.Round(item.Stocks * item.Conversion);
                }
            }

            int conversion = stockreturnList.FirstOrDefault()?.Conversion ?? 1;

            int strip = totalTablets / conversion;
            int tablet = totalTablets % conversion;

            ViewBag.TotalQtyDisplay = $"{strip}:{tablet}";
           // ViewBag.TotalQtyDisplay = GetConvertedQty(totalQty, stockreturnList.FirstOrDefault()?.Conversion ?? 0);
            ViewBag.TotalAmt = stockreturnList.Sum(x => x.Amount);
            ViewBag.TotalGst = stockreturnList.Sum(x => x.GstAmount);

            return View(stockreturnList);
        }
        private string GetConvertedQty(decimal qty, int conversion)
        {
            if (conversion <= 0)
                return qty.ToString("0.##");

            int totalTablets = (int)Math.Round(qty * conversion, MidpointRounding.AwayFromZero);

            int strip = totalTablets / conversion;
            int tab = totalTablets % conversion;

            return $"{strip}:{tab}";
        }

        //EXPORT TO EXCEL
        [HttpGet]
        public async Task<IActionResult> ExportStockReturnItemwiseExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();

            var stockReturns = (await _stockReturnservice.GetAll())
                .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
                .ToList();

            var stockreturnItems = stockReturns
                .Where(s => s.StockReturnItems != null)
                .SelectMany(s => s.StockReturnItems)
                .ToList();

            var grouped = stockreturnItems
                .GroupBy(x => x.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    Qty = g.Sum(x => x.Qty),
                    Amount = g.Sum(x => x.Amount)
                }).ToList();

            var data = (from item in itemMasters
                        join g in grouped on item.Id equals g.ItemMasterId
                        where g.Qty > 0
                        select new StockVM
                        {
                            ItemCode = item.Code,
                            ItemName = item.Name,
                            CategoryName = item.Category?.CategoryName ?? "",
                            Stocks = g.Qty,
                            StockDisplay = GetConvertedQty(g.Qty, item.Conversion),
                            Conversion = item.Conversion,
                            Amount = g.Amount
                        }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stock Return Itemwise");

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

            ws.Cell(2, 1).Value = "Stock Return Item Wise Report";
            ws.Range(2, 1, 2, 5).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 5).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "Item Code", "Item Name", "Category", "Total Qty", "Total Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 5).Style.Font.SetBold();
            row++;

            foreach (var x in data)
            {
                ws.Cell(row, 1).Value = x.ItemCode;
                ws.Cell(row, 2).Value = x.ItemName;
                ws.Cell(row, 3).Value = x.CategoryName;
                ws.Cell(row, 4).Value = x.StockDisplay;
                ws.Cell(row, 5).Value = x.Amount;
                row++;
            }

            // TOTAL
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 4).Value = data.FirstOrDefault()?.StockDisplay ?? "0:0";
            ws.Cell(row, 5).Value = data.Sum(x => x.Amount);

            ws.Range(row, 1, row, 5).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "StockReturnItemwise.xlsx");
        }
        //EXPORT TO PDF 
        [HttpGet]
        public async Task<IActionResult> ExportStockReturnItemwisePdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();

            var stockReturns = (await _stockReturnservice.GetAll())
                .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
                .ToList();

            var stockreturnItems = stockReturns
                .Where(s => s.StockReturnItems != null)
                .SelectMany(s => s.StockReturnItems)
                .ToList();

            var grouped = stockreturnItems
                .GroupBy(x => x.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    Qty = g.Sum(x => x.Qty),
                    Amount = g.Sum(x => x.Amount)
                }).ToList();

            var data = (from item in itemMasters
                        join g in grouped on item.Id equals g.ItemMasterId
                        where g.Qty > 0
                        select new StockVM
                        {
                            ItemCode = item.Code,
                            ItemName = item.Name,
                            CategoryName = item.Category?.CategoryName ?? "",
                            Stocks = g.Qty,
                            StockDisplay = GetConvertedQty(g.Qty, item.Conversion),
                            Amount = g.Amount
                        }).ToList();

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

            doc.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph("Stock Return Item Wise Report")
                .SetFont(bold).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            var table = new Table(new float[] { 3, 6, 4, 3, 3 }).UseAllAvailableWidth();

            string[] headers = { "Item Code", "Item Name", "Category", "Total Qty", "Total Amount" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(fs))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            foreach (var x in data)
            {
                table.AddCell(new Paragraph(x.ItemCode ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.ItemName ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.CategoryName ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.StockDisplay ?? "0:0").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Amount.ToString())
                    .SetFont(normal).SetFontSize(fs)
                    .SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 3)
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(fs)));

            table.AddCell(new Paragraph(data.FirstOrDefault()?.StockDisplay ?? "0:0")
                .SetFont(bold).SetFontSize(fs));

            table.AddCell(new Paragraph(data.Sum(x => x.Amount).ToString())
                .SetFont(bold).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.RIGHT));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "StockReturnItemwise.pdf");
        }
    }
}
