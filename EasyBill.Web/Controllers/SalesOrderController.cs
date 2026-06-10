using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace EasyBill.UI.Controllers
{
    public class SalesOrderController : Controller
    {
        private readonly ISalesOrderRepository _salesOrderRepo;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IModeOfPaymentRepository _modeofpaymentservice;
        private readonly ISalseSettingRepository _salsesettingservice;
        private readonly IPurchaseItemRepository _purchaseitemservice;
        private readonly IOpticalRepository _opticalRepo;
        private readonly IPurchaseRepository _purchaseservice;
        private readonly IPurchaseReturnRepository _purchasereturnservice;
        private readonly ISalesRepository _salesService;
        private readonly IStockIssueRepository _stockissueservice;
        private readonly IStockReturnRepository _stockrturnservice;
        private readonly IStockReceiveRepository _stockreceiveservice;
        private readonly ITenantRegistrationRepository _tenantRepository;
        private readonly IStockService _currentstockRepo;
        private readonly IAccountGroupRepository _accountgroupRepo;
        public SalesOrderController(
            ISalesOrderRepository salesOrderRepo,
            ICustomerRepository customerservice,
            IItemMasterRepository itemmasterservice,
            IModeOfPaymentRepository modeofpaymentservice,
            ISalseSettingRepository salsesettingservice,
            IPurchaseItemRepository purchaseitemservice,
            IOpticalRepository opticalRepo,
            IPurchaseRepository purchaseservice,
            IPurchaseReturnRepository purchasereturnservice,
            ISalesRepository salesservice,
            IStockIssueRepository stockissueservice,
            IStockReturnRepository stockrturnservice,
            IStockReceiveRepository stockreceiveservice,
            ITenantRegistrationRepository tenantRegistration,
            IStockService currentstockRepo,
            IAccountGroupRepository accountgroupRepo
            )
        {
            _salesOrderRepo = salesOrderRepo;
            _customerservice = customerservice;
            _itemmasterservice = itemmasterservice;
            _modeofpaymentservice = modeofpaymentservice;
            _salsesettingservice = salsesettingservice;
            _purchaseitemservice = purchaseitemservice;
            _opticalRepo = opticalRepo;
            _purchaseservice = purchaseservice;
            _purchasereturnservice = purchasereturnservice;
            _salesService = salesservice;
            _stockissueservice = stockissueservice;
            _stockrturnservice = stockrturnservice;
            _stockreceiveservice = stockreceiveservice;
            _tenantRepository = tenantRegistration;
            _currentstockRepo = currentstockRepo;
            _accountgroupRepo = accountgroupRepo;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _salesOrderRepo.GetAll();
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new SalesOrderVM();
            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            viewModel.BillDate = DateTime.Now;
            viewModel.BillNo = await GenerateNxtNumber();
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            viewModel.DoctorRequired = setting?.DoctorRequired ?? false;

            // Check bussiness type
            var tenantId = User?.FindFirst("TenantId")?.Value;
            var user = (await _tenantRepository.GetAll())
           .FirstOrDefault(x => x.Id == tenantId);
            var businessType = user?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;

            return View(viewModel);

        }
        [HttpPost]
        public async Task<IActionResult> Create(SalesOrderVM Vm)
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

            var data = await _salesOrderRepo.GetAll();
            var billNo = Vm.BillNo?.Trim();

            bool isDuplicate = data.Any(x =>
                x.BillNo != null &&
                x.BillNo.Trim().Equals(billNo, StringComparison.OrdinalIgnoreCase)
            );

            if (isDuplicate)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"{billNo} - This Bill Number already exists."
                });
            }

            // GET USER SETTINGS
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            // GET ALL ITEM MASTERS (NEEDED FOR CONVERSION)
            var itemMasters = (await _itemmasterservice.GetAll()).Where(x => x.IsActive).OrderBy(x => x.Name);

            if (Vm != null)
            {
                var customerList = await _customerservice.GetAll();

                // Mobile required for registered customer
                if (string.IsNullOrWhiteSpace(Vm.MobileNo))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Mobile number is required for registered customer."
                    });
                }

                var isExist = customerList.Any(x =>
                    !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                    x.PhoneNo.Trim() == Vm.MobileNo.Trim()
                );

                // Customer NOT found → ERROR
                if (!isExist)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Customer does not exist. Please create customer first."
                    });
                }

                var model = new SalesOrder
                {
                    CustomerId = Vm.CustomerId,
                    BillDate = Vm.BillDate,
                    BillNo = Vm.BillNo,
                    MobileNo = Vm.MobileNo,
                    Address = Vm.Address,
                    PharmacyDoctorId = Vm.PharmacyDoctorId,
                    DoctorMobileNumber = Vm.DoctorMobileNumber,
                    DoctorRegNumber = Vm.DoctorRegNumber,
                    Total = Vm.Total,
                    TotalGstAmt = Vm.TotalGstAmt,
                    discountPercent = Vm.discountPercent,
                    discountAmount = Vm.discountAmount,
                    Totaldiscount = Vm.Totaldiscount,
                    TotalPayable = Vm.TotalPayable,
                    RoundOffAmount = Vm.RoundOffAmount,
                    PaidAmount = Vm.PaidAmount,
                    ReturnAmount = Vm.ReturnAmount,
                    OfferId = Vm.OfferId,
                    Balance = Vm.Balance,
                    TotalCessAmt = Vm.TotalCessAmt,
                    // SALES ORDER ITEMS WITH TABLET-WISE CONVERSION
                    salesOrderItems = Vm.SalesOrderItemVMs?.Select(x =>
                    {
                        var item = itemMasters.FirstOrDefault(i => i.Id == x.ItemMasterId);

                        decimal finalQty = x.Qty;

                        // TABLET-WISE CONVERSION
                        if (isTabletWise && item != null && item.Conversion > 0)
                        {
                            // Formula: finalQty = Qty + (TabletQty / Conversion)
                            // Example: 2 strips + 5 tablets, Conversion = 10
                            // finalQty = 2 + (5/10) = 2.5 strips
                            finalQty = x.Qty + ((decimal)x.TabletQty / item.Conversion);
                        }

                        // VALIDATION: Prevent negative quantities
                        if (finalQty < 0)
                        {
                            finalQty = 0;
                        }

                        return new SalesOrderItem()
                        {
                            ItemMasterId = x.ItemMasterId,
                            PurchaseItemId = x.PurchaseItemId,
                            Batch = x.Batch,
                            Expirydate = x.Expirydate,
                            Mrp = x.Mrp,
                            Qty = finalQty,   // Converted Qty
                            Rate = x.Rate,
                            StripRate = x.StripRate,
                            Gst = x.Gst,
                            Cess = x.Cess,
                            Discount = x.Discount,
                            Amount = x.Amount,
                        };
                    }).ToList() ?? new List<SalesOrderItem>(),

                    // PAYMENT DETAILS
                    SalsePaymentDetails = Vm.SalsePaymentDetails?.Select(pd => new SalsePaymentDetails
                    {
                        PaymentModeId = pd.PaymentModeId,
                        Amount = pd.Amount,
                        ReferenceNo = pd.ReferenceNo,
                        Description = pd.Description,
                        CustomerId = Vm.CustomerId,
                    }).ToList() ?? new List<SalsePaymentDetails>(),

                    // OPTICAL DETAILS
                    Opticals = Vm.OpticalVMs?.Select(pd => new Optical
                    {
                        ItemMasterId = pd.ItemMasterId,
                        Rx = pd.Rx,
                        Sphere = pd.Sphere,
                        Cylinder = pd.Cylinder,
                        Axis = pd.Axis,
                        Prism = pd.Prism,
                        Add = pd.Add,
                    }).ToList() ?? new List<Optical>()
                };

                // CREATE SALES ORDER
                var createdSale = await _salesOrderRepo.Create(model);

                return Json(new { success = true, saleId = createdSale.Id });
            }

            return Json(new { success = false });
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            SalesOrder model = await _salesOrderRepo.GetById(Id);
            SalesOrderVM VM = new SalesOrderVM();

            if (model != null)
            {
                VM.Id = model.Id;
                VM.CustomerId = model.CustomerId;
                VM.CustomerName = model.Customers?.Name;
                VM.BillNo = model.BillNo;
                VM.BillDate = model.BillDate;
                VM.MobileNo = model.MobileNo;

                #region Doctor Required
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var setting = await _salsesettingservice.GetByUserId(userId);
                VM.DoctorRequired = setting?.DoctorRequired ?? false;
                #endregion

                VM.PharmacyDoctorId = model.PharmacyDoctorId;
                VM.DoctorName = model.PharmacyDoctor?.Name;
                VM.DoctorMobileNumber = model.DoctorMobileNumber;
                VM.DoctorRegNumber = model.DoctorRegNumber;
                VM.Address = model.Address;
                VM.Total = model.Total;
                VM.TotalGstAmt = model.TotalGstAmt;
                VM.discountPercent = model.discountPercent;
                VM.discountAmount = model.discountAmount;
                VM.Totaldiscount = model.Totaldiscount;
                VM.TotalPayable = model.TotalPayable;
                VM.RoundOffAmount = model.RoundOffAmount;
                VM.PaidAmount = model.PaidAmount;
                VM.ReturnAmount = model.ReturnAmount;
                VM.OfferId = model.OfferId;
                VM.Balance = model.Balance;
                VM.TotalCessAmt = model.TotalCessAmt;
                // GET ALL ITEMS FOR CONVERSION LOOKUP
                var itemMasters = (await _itemmasterservice.GetAll()).ToDictionary(x => x.Id, x => x);

                // CHECK IF TABLET-WISE MODE
                bool isTabletWiseMode = setting != null && setting.ItemConversion == "TabletWise";

                // MAP SALES ORDER ITEMS WITH TABLET-WISE CONVERSION
                VM.SalesOrderItemVMs = model.salesOrderItems != null
                    ? model.salesOrderItems.Select(x =>
                    {
                        // CALCULATE DISPLAY QTY (STRIPS + TABLETS)
                        decimal displayQty = x.Qty;          // Default: strips
                        decimal displayTabletQty = 0;        // Default: 0 tablets

                        if (isTabletWiseMode && itemMasters.ContainsKey(x.ItemMasterId))
                        {
                            var item = itemMasters[x.ItemMasterId];
                            int conversion = item.Conversion > 0 ? item.Conversion : 1;

                            // Step 1: Convert strip qty → total tablets
                            int totalTablets = (int)Math.Round(
                                x.Qty * conversion,
                                MidpointRounding.AwayFromZero
                            );

                            // Step 2: Split tablets → strips + remaining tablets
                            displayQty = totalTablets / conversion;          // Strips
                            displayTabletQty = totalTablets % conversion;    // Remaining tablets
                        }

                        // RETURN ITEM VIEW MODEL
                        return new SalesOrderItemVM
                        {
                            Id = x.Id,
                            ItemMasterId = x.ItemMasterId,
                            ItemBarcodeNumber = x.ItemMaster?.Name,
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
                        };
                    }).ToList()
                    : new List<SalesOrderItemVM>();

                // MAP PAYMENT DETAILS
                VM.SalsePaymentDetails = model.SalsePaymentDetails?.Select(pd => new SalsePaymentDetailsVM
                {
                    Id = pd.Id,
                    PaymentModeId = pd.PaymentModeId,
                    Amount = pd.Amount,
                    ReferenceNo = pd.ReferenceNo,
                    Description = pd.Description,
                    CustomerId = pd.CustomerId
                }).ToList() ?? new List<SalsePaymentDetailsVM>();
            }

            // POPULATE DROPDOWNS
            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");

            // CHECK BUSINESS TYPE
            var tenantId = User?.FindFirst("TenantId")?.Value;
            var user = (await _tenantRepository.GetAll())
                .FirstOrDefault(x => x.Id == tenantId);
            var businessType = user?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;

            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(SalesOrderVM VM)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
                ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
                ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
                return View(VM);
            }

            var customerList = await _customerservice.GetAll();

            // Mobile required for registered customer
            if (string.IsNullOrWhiteSpace(VM.MobileNo))
            {
                ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
                ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
                ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
                TempData["error"] = "Mobile number is required for registered customer.";
                return View(VM);
            }

            var isExist = customerList.Any(x =>
                !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                x.PhoneNo.Trim() == VM.MobileNo.Trim()
            );

            // Customer NOT found → ERROR
            if (!isExist)
            {
                ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
                ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
                ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
                TempData["error"] = "Customer does not exist.Please create customer first.";
                return View(VM);
            }

            // GET SETTING & ITEM MASTERS
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name);

            SalesOrder model = await _salesOrderRepo.GetById(VM.Id);
            if (model != null)
            {
                model.CustomerId = VM.CustomerId;
                model.BillDate = VM.BillDate;
                model.BillNo = VM.BillNo;
                model.MobileNo = VM.MobileNo;
                model.Address = VM.Address;
                model.PharmacyDoctorId = VM.PharmacyDoctorId;
                model.DoctorMobileNumber = VM.DoctorMobileNumber;
                model.DoctorRegNumber = VM.DoctorRegNumber;
                model.Total = VM.Total;
                model.TotalGstAmt = VM.TotalGstAmt;
                model.TotalPayable = VM.TotalPayable;
                model.RoundOffAmount = VM.RoundOffAmount;
                model.discountPercent = VM.discountPercent;
                model.discountAmount = VM.discountAmount;
                model.Totaldiscount = VM.Totaldiscount;
                model.PaidAmount = VM.PaidAmount;
                model.ReturnAmount = VM.ReturnAmount;
                model.OfferId = VM.OfferId;
                model.Balance = VM.Balance;
                model.TotalCessAmt = VM.TotalCessAmt;
                if (model.salesOrderItems == null)
                    model.salesOrderItems = new List<SalesOrderItem>();

                if (VM.SalesOrderItemVMs == null)
                    VM.SalesOrderItemVMs = new List<SalesOrderItemVM>();
                var removedItems = model.salesOrderItems.Where(dbItem => !VM.SalesOrderItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

                // ❌ Remove Deleted Items
                foreach (var item in removedItems)
                {
                    model.salesOrderItems.Remove(item);
                }
                foreach (var item in VM.SalesOrderItemVMs)
                {
                    var itemMaster = itemMasters.FirstOrDefault(i => i.Id == item.ItemMasterId);

                    // CALCULATE FINAL QTY (STRIP-BASED, SAME AS CREATE)
                    decimal finalQty = item.Qty;

                    if (isTabletWise && itemMaster != null && itemMaster.Conversion > 0)
                    {
                        finalQty = item.Qty + ((decimal)item.TabletQty / itemMaster.Conversion);
                    }

                    if (item.Id > 0) // Update existing items
                    {
                        var existingItem = model.salesOrderItems.FirstOrDefault(x => x.Id == item.Id);
                        if (existingItem != null)
                        {
                            existingItem.ItemMasterId = item.ItemMasterId;
                            existingItem.PurchaseItemId = item.PurchaseItemId;
                            existingItem.Batch = item.Batch;
                            existingItem.Expirydate = item.Expirydate;
                            existingItem.Mrp = item.Mrp;
                            existingItem.Qty = finalQty;  // CONVERTED QTY
                            existingItem.Rate = item.Rate;
                            existingItem.StripRate = item.StripRate;
                            existingItem.Gst = item.Gst;
                            existingItem.Cess = item.Cess;
                            existingItem.Discount = item.Discount;
                            existingItem.Amount = item.Amount;
                        }
                    }
                    else // Add new items
                    {
                        model.salesOrderItems.Add(new SalesOrderItem
                        {
                            ItemMasterId = item.ItemMasterId,
                            PurchaseItemId = item.PurchaseItemId,
                            Batch = item.Batch,
                            Expirydate = item.Expirydate,
                            Mrp = item.Mrp,
                            Qty = finalQty,  // CONVERTED QTY
                            Rate = item.Rate,
                            StripRate = item.StripRate,
                            Gst = item.Gst,
                            Cess = item.Cess,
                            Discount = item.Discount,
                            Amount = item.Amount,
                        });
                    }
                }
                if (model.SalsePaymentDetails == null)
                    model.SalsePaymentDetails = new List<SalsePaymentDetails>();

                if (VM.SalsePaymentDetails == null)
                    VM.SalsePaymentDetails = new List<SalsePaymentDetailsVM>();
                var removedPaymentDetails = model.SalsePaymentDetails
                   .Where(dbPayment => !VM.SalsePaymentDetails.Any(vmItem => vmItem.Id == dbPayment.Id)).ToList();
                foreach (var payment in removedPaymentDetails)
                {
                    model.SalsePaymentDetails.Remove(payment);
                }
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
                            existingPayment.CustomerId = (int)VM.CustomerId;
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
                            CustomerId = (int)VM.CustomerId,
                        });
                    }
                }
                // 🟢 Optical Details Update
                if (model.Opticals == null)
                    model.Opticals = new List<Optical>();

                if (VM.OpticalVMs == null)
                    VM.OpticalVMs = new List<OpticalVM>();

                // Update or add new optical entries
                foreach (var opt in VM.OpticalVMs)
                {
                    if (opt.Id > 0) // update existing
                    {
                        var existingOpt = model.Opticals.FirstOrDefault(x => x.Id == opt.Id);
                        if (existingOpt != null)
                        {
                            existingOpt.ItemMasterId = opt.ItemMasterId;
                            existingOpt.Rx = opt.Rx;
                            existingOpt.Sphere = opt.Sphere;
                            existingOpt.Cylinder = opt.Cylinder;
                            existingOpt.Axis = opt.Axis;
                            existingOpt.Prism = opt.Prism;
                            existingOpt.Add = opt.Add;
                        }
                    }
                    else // add new
                    {
                        if (!model.Opticals.Any(x => x.ItemMasterId == opt.ItemMasterId && x.Rx == opt.Rx))
                        {
                            model.Opticals.Add(new Optical
                            {
                                ItemMasterId = opt.ItemMasterId,
                                Rx = opt.Rx,
                                Sphere = opt.Sphere,
                                Cylinder = opt.Cylinder,
                                Axis = opt.Axis,
                                Prism = opt.Prism,
                                Add = opt.Add
                            });
                        }
                    }
                }


                await _salesOrderRepo.Update(model);
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

                var model = await _salesOrderRepo.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                await _salesOrderRepo.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, AOne.Utility.Enums.OrderStatus status)
        {
            try
            {
                var model = await _salesOrderRepo.GetById(id);
                if (model == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                model.OrderStatus = status;
                await _salesOrderRepo.Update(model);
                return Json(new { success = true, message = "Status updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNewBillNo()
        {
            var lastBillNo = await GenerateNxtNumber();
            return Json(new { success = true, billNo = lastBillNo });
        }
        //[HttpGet]
        //public async Task<IActionResult> GetAllItems()
        //{
        //    //var itemMasters = (await _itemmasterservice.GetAll()).OrderBy(x => x.Name);
        //    var itemMasters = (await _itemmasterservice.GetAll())
        //        .Where(x => x.IsActive)
        //        .OrderBy(x => x.Name);

        //    var purchases = await _purchaseservice.GetAll();
        //    var purchasereturns = await _purchasereturnservice.GetAll();
        //    var sales = await _salesService.GetAll();
        //    var stockIssue = await _stockissueservice.GetAll();
        //    var stockReturn = await _stockrturnservice.GetAll();
        //    var stockreceive = await _stockreceiveservice.GetAll();

        //    var purchaseItems = purchases.Where(p => p.PurchaseItems != null).SelectMany(p => p.PurchaseItems);
        //    var purchaseReturnItems = purchasereturns.Where(r => r.PurchaseReturnItems != null).SelectMany(r => r.PurchaseReturnItems);
        //    var salesItems = sales.Where(s => s.SalesItems != null).SelectMany(s => s.SalesItems);
        //    var stockissueItems = stockIssue.Where(s => s.StockIssuesItems != null).SelectMany(s => s.StockIssuesItems);
        //    var stockreturnItems = stockReturn.Where(s => s.StockReturnItems != null).SelectMany(s => s.StockReturnItems);
        //    var stockreceiveItems = stockreceive.Where(s => s.StockReceiveItems != null).SelectMany(s => s.StockReceiveItems);

        //    var stockList = itemMasters.Select(item =>
        //    {
        //        var itemId = item.Id;

        //        var totalPurchasedQty = purchaseItems
        //            .Where(x => x.ItemId == itemId)
        //            .Sum(x => x.Qty + x.FreeQty);

        //        var totalpurchaseReturnedQty = purchaseReturnItems
        //            .Where(x => x.ItemId == itemId)
        //            .Sum(x => x.Qty + x.FreeQty);

        //        var totalsalseQty = salesItems
        //            .Where(x => x.ItemMasterId == itemId)
        //            .Sum(x => x.Qty);

        //        var totalstockissueQty = stockissueItems
        //            .Where(x => x.ItemMasterId == itemId)
        //            .Sum(x => x.Qty);

        //        var totalstockreturnQty = stockreturnItems
        //            .Where(x => x.ItemMasterId == itemId)
        //            .Sum(x => x.Qty);

        //        var totalstockreceiveQty = stockreceiveItems
        //            .Where(x => x.ItemMasterId == itemId)
        //            .Sum(x => x.Qty);

        //        var currentStock = totalPurchasedQty + totalstockreturnQty + totalstockreceiveQty
        //                         - totalpurchaseReturnedQty - totalsalseQty - totalstockissueQty;

        //        return new
        //        {
        //            id = itemId,
        //            barcode = item.Barcode,
        //            code = item.Code,
        //            name = item.Name,
        //            gst = item.Hsn?.IGST ?? 0,
        //            qty = currentStock,  // Can be 0 if no transactions
        //            maximumdiscount = item.MaximumDiscount
        //        };
        //    }).ToList();

        //    return Json(stockList);
        //}

        [HttpGet]
        public async Task<IActionResult> GetAllItems()
        {
            // ✅ GET USER SETTINGS
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            var itemMasters = (await _itemmasterservice.GetAll()).Where(x => x.IsActive).OrderBy(x => x.Name);

            var currentStocks = await _currentstockRepo.GetAll();

            var stockDict = currentStocks.GroupBy(x => x.ItemId).ToDictionary(g => g.Key,  g => g.Sum(x => x.Qty));

            //var purchases = await _purchaseservice.GetAll();
            //var purchasereturns = await _purchasereturnservice.GetAll();
            //var sales = await _salesService.GetAll();
            //var stockIssue = await _stockissueservice.GetAll();
            //var stockReturn = await _stockrturnservice.GetAll();
            //var stockreceive = await _stockreceiveservice.GetAll();

            //var purchaseItems = purchases.Where(p => p.PurchaseItems != null).SelectMany(p => p.PurchaseItems);
            //var purchaseReturnItems = purchasereturns.Where(r => r.PurchaseReturnItems != null).SelectMany(r => r.PurchaseReturnItems);
            //var salesItems = sales.Where(s => s.SalesItems != null).SelectMany(s => s.SalesItems);
            //var stockissueItems = stockIssue.Where(s => s.StockIssuesItems != null).SelectMany(s => s.StockIssuesItems);
            //var stockreturnItems = stockReturn.Where(s => s.StockReturnItems != null).SelectMany(s => s.StockReturnItems);
            //var stockreceiveItems = stockreceive.Where(s => s.StockReceiveItems != null).SelectMany(s => s.StockReceiveItems);

            var stockList = itemMasters.Select(item =>
            {
                var itemId = item.Id;
                decimal currentStock = stockDict.ContainsKey(itemId) ? stockDict[itemId]  : 0;
                // ✅ UNCHANGED - ORIGINAL STOCK CALCULATION LOGIC
                //var totalPurchasedQty = purchaseItems
                //    .Where(x => x.ItemId == itemId)
                //    .Sum(x => x.Qty + x.FreeQty);

                //var totalpurchaseReturnedQty = purchaseReturnItems
                //    .Where(x => x.ItemId == itemId)
                //    .Sum(x => x.Qty + x.FreeQty);

                //var totalsalseQty = salesItems
                //    .Where(x => x.ItemMasterId == itemId)
                //    .Sum(x => x.Qty);

                //var totalstockissueQty = stockissueItems
                //    .Where(x => x.ItemMasterId == itemId)
                //    .Sum(x => x.Qty);

                //var totalstockreturnQty = stockreturnItems
                //    .Where(x => x.ItemMasterId == itemId)
                //    .Sum(x => x.Qty);

                //var totalstockreceiveQty = stockreceiveItems
                //    .Where(x => x.ItemMasterId == itemId)
                //    .Sum(x => x.Qty);

                //var currentStock = totalPurchasedQty + totalstockreturnQty + totalstockreceiveQty
                //                 - totalpurchaseReturnedQty - totalsalseQty - totalstockissueQty;

                // ✅ NEW: TABLET-WISE QTY DISPLAY
                string displayQty;

                if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
                {
                    int conversion = item.Conversion;

                    // Convert strips → tablets
                    int totalTablets = (int)Math.Round(
                        currentStock * conversion,
                        MidpointRounding.AwayFromZero
                    );

                    // Split → strips + remaining tablets
                    int strips = totalTablets / conversion;
                    int tablets = totalTablets % conversion;

                    displayQty = $"{strips}:{tablets}";
                }
                else
                {
                    displayQty = currentStock.ToString("0.##");
                }

                return new
                {
                    id = itemId,
                    barcode = item.Barcode,
                    code = item.Code,
                    name = item.Name,
                    gst = item.Hsn?.IGST ?? 0,
                    cess = item.Hsn?.Cess ?? 0,
                    qty = currentStock,              // ORIGINAL qty (unchanged)
                    displayQty = displayQty,         
                    maximumdiscount = item.MaximumDiscount,
                    conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m
                };
            }).ToList();

            return Json(stockList);
        }

        

       

        //[HttpPost]
        //public async Task<IActionResult> Create(SalesOrderVM Vm)
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
        //            errors = errors // Optional: sab errors bhejne ke liye
        //        });
        //    }

        //    var data = await _salesOrderRepo.GetAll();
        //    var billNo = Vm.BillNo?.Trim();

        //    bool isDuplicate = data.Any(x =>
        //        x.BillNo != null &&
        //        x.BillNo.Trim().Equals(billNo, StringComparison.OrdinalIgnoreCase)
        //    );

        //    if (isDuplicate)
        //    {
        //        return BadRequest(new
        //        {
        //            success = false,
        //            message = $"{billNo} - This Bill Number already exists."
        //        });
        //    }


        //    if (Vm != null)
        //    {
        //        var customerList = await _customerservice.GetAll();

        //        // Mobile required for registered customer
        //        if (string.IsNullOrWhiteSpace(Vm.MobileNo))
        //        {
        //            return BadRequest(new
        //            {
        //                success = false,
        //                message = "Mobile number is required for registered customer."
        //            });
        //        }

        //        var isExist = customerList.Any(x =>
        //            !string.IsNullOrWhiteSpace(x.PhoneNo) &&
        //            x.PhoneNo.Trim() == Vm.MobileNo.Trim()
        //        );

        //        // Customer NOT found → ERROR
        //        if (!isExist)
        //        {
        //            return BadRequest(new
        //            {
        //                success = false,
        //                message = "Customer does not exist. Please create customer first."
        //            });
        //        }

        //        var model = new SalesOrder
        //        {
        //            CustomerId = Vm.CustomerId,
        //            BillDate = Vm.BillDate,
        //            BillNo = Vm.BillNo,
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
        //            PaidAmount = Vm.PaidAmount,
        //            ReturnAmount = Vm.ReturnAmount,
        //            OfferId = Vm.OfferId,
        //            Balance = Vm.Balance,
        //            salesOrderItems = Vm.SalesOrderItemVMs?.Select(x => new SalesOrderItem()
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
        //            }).ToList() ?? new List<SalesOrderItem>(),
        //            SalsePaymentDetails = Vm.SalsePaymentDetails?.Select(pd => new SalsePaymentDetails
        //            {
        //                PaymentModeId = pd.PaymentModeId,
        //                Amount = pd.Amount,
        //                ReferenceNo = pd.ReferenceNo,
        //                Description = pd.Description,
        //                CustomerId = Vm.CustomerId,
        //            }).ToList() ?? new List<SalsePaymentDetails>(),
        //            Opticals = Vm.OpticalVMs?.Select(pd => new Optical
        //            {
        //                ItemMasterId = pd.ItemMasterId,
        //                Rx = pd.Rx,
        //                Sphere = pd.Sphere,
        //                Cylinder = pd.Cylinder,
        //                Axis = pd.Axis,
        //                Prism = pd.Prism,
        //                Add = pd.Add,
        //            }).ToList() ?? new List<Optical>()

        //        };
        //        var createdSale = await _salesOrderRepo.Create(model);
        //        return Json(new { success = true, saleId = createdSale.Id });
        //    }

        //    return Json(new { success = false });
        //}

        public async Task<string> GenerateNxtNumber()
        {
            var lastCode = (await _salesOrderRepo.GetAll())
                .Where(x => !string.IsNullOrWhiteSpace(x.BillNo))
                .OrderByDescending(x => x.Created)
                .Select(x => x.BillNo)
                .FirstOrDefault();

            return GenerateNextProductCode(lastCode);
        }

        private string GenerateNextProductCode(string lastCode)
        {
            if (string.IsNullOrWhiteSpace(lastCode))
                return "SO0001";

            // Last numeric sequence only
            var match = Regex.Match(lastCode, @"(\d+)(?!.*\d)");

            if (!match.Success)
                return "SO0001";

            int number = int.Parse(match.Value);
            string prefix = lastCode[..match.Index];
            string suffix = lastCode[(match.Index + match.Length)..];

            string nextNumber = (number + 1).ToString($"D{match.Length}");

            return $"{prefix}{nextNumber}{suffix}";
        }

        //public async Task<string> GenerateNxtNumber()
        //{
        //    var data = await _salesOrderRepo.GetAll();
        //    var lastCode = data
        //        .Where(p => !string.IsNullOrEmpty(p.BillNo))
        //        .Select(p => p.BillNo)
        //        .LastOrDefault();

        //    return GenerateNextProductCode(lastCode);
        //}

        //private string GenerateNextProductCode(string lastCode)
        //{
        //    if (string.IsNullOrEmpty(lastCode) || lastCode.Length < 2)
        //        return "SO0001";

        //    string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray());

        //    string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

        //    int number = 0;
        //    int.TryParse(numberPart, out number);

        //    string nextCode = prefix + (number + 1).ToString("D" + numberPart.Length);

        //    return nextCode;
        //}

        [HttpGet]
        public async Task<IActionResult> GetBatchesByItemId(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
                return Json(new { error = "Item not found." });
            var currentStocks = (await _currentstockRepo.GetAll()).Where(x => x.ItemId == id).ToList();
            // ✅ GET USER SETTINGS
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            // --- Fetch all related transactions ---
            //var purchaseItems = await _purchaseitemservice.GetByItemMasterId(id);
            //var purchaseReturnItems = (await _purchasereturnservice.GetAll())
            //    .Where(r => r.PurchaseReturnItems != null)
            //    .SelectMany(r => r.PurchaseReturnItems)
            //    .Where(x => x.ItemId == id)
            //    .ToList();

            //var salesItems = (await _salesService.GetAll())
            //    .Where(s => s.SalesItems != null)
            //    .SelectMany(s => s.SalesItems)
            //    .Where(x => x.ItemMasterId == id)
            //    .ToList();

            //var stockIssueItems = (await _stockissueservice.GetAll())
            //    .Where(s => s.StockIssuesItems != null)
            //    .SelectMany(s => s.StockIssuesItems)
            //    .Where(x => x.ItemMasterId == id)
            //    .ToList();

            //var stockReturnItems = (await _stockrturnservice.GetAll())
            //    .Where(s => s.StockReturnItems != null)
            //    .SelectMany(s => s.StockReturnItems)
            //    .Where(x => x.ItemMasterId == id)
            //    .ToList();

            //var stockReceiveItems = (await _stockreceiveservice.GetAll())
            //    .Where(s => s.StockReceiveItems != null)
            //    .SelectMany(s => s.StockReceiveItems)
            //    .Where(x => x.ItemMasterId == id)
            //    .ToList();

            // --- Calculate remaining qty per batch ---
            //var groupedResult = purchaseItems
            //    .GroupBy(b => new { b.Batch, b.ExpiryDate, b.Mrp, b.Rate })
            //    .Select(g =>
            //    {
            //        var batchNo = g.Key.Batch;
            //        var expiry = g.Key.ExpiryDate;
            //        var mrp = g.Key.Mrp;
            //        decimal conversion = g.First().ItemMasters?.Conversion ?? 1;

            //        // Priority-based rate selection
            //        var firstItem = g.First();
            //        var rate = firstItem.salserateA > 0 ? firstItem.salserateA : 0;

            //        // UNCHANGED - ORIGINAL STOCK CALCULATION LOGIC
            //        var totalPurchased = g.Sum(x => x.Qty + x.FreeQty);

            //        var purchaseReturned = purchaseReturnItems
            //            .Where(x => x.Batch == batchNo)
            //            .Sum(x => x.Qty + x.FreeQty);

            //        var soldQty = salesItems
            //            .Where(x => x.Batch == batchNo)
            //            .Sum(x => x.Qty);

            //        var issuedQty = stockIssueItems
            //            .Where(x => x.Batch == batchNo)
            //            .Sum(x => x.Qty);

            //        var returnedQty = stockReturnItems
            //            .Where(x => x.Batch == batchNo)
            //            .Sum(x => x.Qty);

            //        var receivedQty = stockReceiveItems
            //            .Where(x => x.Batch == batchNo)
            //            .Sum(x => x.Qty);

            //        // Final available quantity (UNCHANGED)
            //        var currentQty = totalPurchased + returnedQty + receivedQty
            //                       - purchaseReturned - soldQty - issuedQty;

            //        // RATE CALCULATION (TabletWise vs StripWise)
            //        decimal ratePerUnit = rate; // default strip rate

            //        if (setting != null && setting.ItemConversion == "TabletWise")
            //        {
            //            ratePerUnit = conversion != 0 ? rate / conversion : rate;
            //        }

            //        // TABLET-WISE QTY DISPLAY
            //        string stripTabsQty;

            //        if (setting != null && setting.ItemConversion == "TabletWise" && conversion > 0)
            //        {
            //            int conversionInt = (int)conversion;

            //            int totalTablets = (int)Math.Round(
            //                currentQty * conversionInt,
            //                MidpointRounding.AwayFromZero
            //            );

            //            int strips = totalTablets / conversionInt;
            //            int tablets = totalTablets % conversionInt;

            //            stripTabsQty = $"{strips}:{tablets}";
            //        }
            //        else
            //        {
            //            stripTabsQty = currentQty.ToString("0.##");
            //        }

            //        return new
            //        {
            //            batch = batchNo,
            //            expiry = expiry,
            //            rate = ratePerUnit,
            //            mrp = mrp,
            //            qty = currentQty,               // ORIGINAL qty (unchanged)
            //            stripTabsQty = stripTabsQty,    // "25:5" format
            //            purchaseItemId = g.Select(x => x.Id).FirstOrDefault(),
            //            conversion = conversion        
            //        };
            //    })
            //    .OrderBy(x => x.expiry)
            //    .ToList();

            var groupedResult = currentStocks
            .GroupBy(x => new { x.Batch, x.ExpiryDate, x.Mrp, x.PurchaseRate })
            .Select(g =>
            {
                var batchNo = g.Key.Batch;
                var expiry = g.Key.ExpiryDate;
                var mrp = g.Key.Mrp;
                var purchaseRate = g.Key.PurchaseRate;
                decimal conversion = item.Conversion > 0 ? item.Conversion : 1;
            
                var currentQty = g.Sum(x => x.Qty);
            
                // ✅ TabletWise rate
                decimal ratePerUnit = purchaseRate;
            
                if (setting != null && setting.ItemConversion == "TabletWise")
                {
                    ratePerUnit = conversion != 0 ? ratePerUnit / conversion : ratePerUnit;
                }
            
                // ✅ Tablet-wise qty display (UNCHANGED)
                string stripTabsQty;
            
                if (setting != null && setting.ItemConversion == "TabletWise" && conversion > 0)
                {
                    int conversionInt = (int)conversion;
            
                    int totalTablets = (int)Math.Round(
                        currentQty * conversionInt,
                        MidpointRounding.AwayFromZero
                    );
            
                    int strips = totalTablets / conversionInt;
                    int tablets = totalTablets % conversionInt;
            
                    stripTabsQty = $"{strips}:{tablets}";
                }
                else
                {
                    stripTabsQty = currentQty.ToString("0.##");
                }
            
                return new
                {
                    batch = batchNo,
                    expiry = expiry,
                    rate = ratePerUnit,
                    striprate = purchaseRate,
                    mrp = mrp,
                    qty = currentQty,           
                    stripTabsQty = stripTabsQty,
                    purchaseItemId = 0,
                    conversion = conversion
                };
            }).Where(x => x.qty > 0).OrderBy(x => x.expiry).ToList();

            var result = new
            {
                gst = item.Hsn?.IGST ?? 0,
                cess = item.Hsn?.Cess ?? 0,
                groupedResult
            };

            return Json(result);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetBatchesByItemId(int id)
        //{
        //    var item = await _itemmasterservice.GetByItemMasterId(id);
        //    if (item == null)
        //        return Json(new { error = "Item not found." });
        //    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    var setting = await _salsesettingservice.GetByUserId(userId);
        //    // --- Fetch all related transactions ---
        //    var purchaseItems = await _purchaseitemservice.GetByItemMasterId(id);
        //    var purchaseReturnItems = (await _purchasereturnservice.GetAll())
        //        .Where(r => r.PurchaseReturnItems != null)
        //        .SelectMany(r => r.PurchaseReturnItems)
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    var salesItems = (await _salesService.GetAll())
        //        .Where(s => s.SalesItems != null)
        //        .SelectMany(s => s.SalesItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockIssueItems = (await _stockissueservice.GetAll())
        //        .Where(s => s.StockIssuesItems != null)
        //        .SelectMany(s => s.StockIssuesItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockReturnItems = (await _stockrturnservice.GetAll())
        //        .Where(s => s.StockReturnItems != null)
        //        .SelectMany(s => s.StockReturnItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockReceiveItems = (await _stockreceiveservice.GetAll())
        //        .Where(s => s.StockReceiveItems != null)
        //        .SelectMany(s => s.StockReceiveItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    // --- Calculate remaining qty per batch ---
        //    var groupedResult = purchaseItems
        //        .GroupBy(b => new { b.Batch, b.ExpiryDate, b.Mrp, b.Rate })
        //        .Select(g =>
        //        {
        //            var batchNo = g.Key.Batch;
        //            var expiry = g.Key.ExpiryDate;
        //            //var rate = g.Key.Rate;
        //            var mrp = g.Key.Mrp;
        //            decimal conversion = g.First().ItemMasters?.Conversion ?? 1;

        //            // Priority-based rate selection from first item in group
        //            var firstItem = g.First();

        //            //var rate = firstItem.Mrp > 0 ? firstItem.Mrp :
        //            //           firstItem.salserateA > 0 ? firstItem.salserateA :
        //            //           firstItem.salserateB;

        //            var rate = firstItem.salserateA > 0 ? firstItem.salserateA : 0;

        //            // Purchased Qty
        //            var totalPurchased = g.Sum(x => x.Qty + x.FreeQty);

        //            // Returned to supplier
        //            var purchaseReturned = purchaseReturnItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty + x.FreeQty);

        //            // Sold Qty
        //            var soldQty = salesItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // Stock issued Qty
        //            var issuedQty = stockIssueItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // Stock returned Qty
        //            var returnedQty = stockReturnItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // Stock received Qty
        //            var receivedQty = stockReceiveItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // ✅ Final available quantity
        //            var currentQty = totalPurchased + returnedQty +
        //                           -purchaseReturned - soldQty - issuedQty;
        //            //if (currentQty < 0) currentQty = 0;
        //            decimal ratePerUnit = rate; // default strip rate

        //            if (setting.ItemConversion == "TabletWise")
        //            {
        //                ratePerUnit = conversion != 0 ? rate / conversion : rate;
        //            }
        //            return new
        //            {
        //                batch = batchNo,
        //                expiry = expiry,
        //                rate = ratePerUnit,
        //                mrp = mrp,
        //                //qty = currentQty < 0 ? 0 : currentQty, // never negative
        //                qty = currentQty, // never negative
        //                purchaseItemId = g.Select(x => x.Id).FirstOrDefault()
        //            };
        //        })
        //        //.Where(x => x.qty > 0) // Only show available batches
        //        .OrderBy(x => x.expiry)
        //        .ToList();

        //    var result = new
        //    {
        //        gst = item.Hsn?.IGST ?? 0,
        //        groupedResult
        //    };

        //    return Json(result);
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

        

        //[HttpGet]
        //public async Task<IActionResult> Edit(int Id)
        //{
        //    SalesOrder model = await _salesOrderRepo.GetById(Id);
        //    SalesOrderVM VM = new SalesOrderVM();

        //    if (model != null)
        //    {
        //        VM.Id = model.Id;
        //        VM.CustomerId = model.CustomerId;
        //        VM.CustomerName = model.Customers?.Name;
        //        VM.BillNo = model.BillNo;
        //        VM.BillDate = model.BillDate;
        //        VM.MobileNo = model.MobileNo;

        //        #region Doctor Required
        //        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //        var setting = await _salsesettingservice.GetByUserId(userId);
        //        VM.DoctorRequired = setting?.DoctorRequired ?? false;
        //        #endregion

        //        VM.PharmacyDoctorId = model.PharmacyDoctorId;
        //        VM.DoctorName = model.PharmacyDoctor?.Name;
        //        VM.DoctorMobileNumber = model.DoctorMobileNumber;
        //        VM.DoctorRegNumber = model.DoctorRegNumber;
        //        VM.Address = model.Address;
        //        VM.Total = model.Total;
        //        VM.TotalGstAmt = model.TotalGstAmt;
        //        VM.discountPercent = model.discountPercent;
        //        VM.discountAmount = model.discountAmount;
        //        VM.Totaldiscount = model.Totaldiscount;
        //        VM.TotalPayable = model.TotalPayable;
        //        VM.PaidAmount = model.PaidAmount;
        //        VM.ReturnAmount = model.ReturnAmount;
        //        VM.OfferId = model.OfferId;
        //        VM.Balance = model.Balance;
        //        VM.SalesOrderItemVMs = model.salesOrderItems != null
        //            ? model.salesOrderItems.Select(x => new SalesOrderItemVM
        //            {
        //                Id = x.Id,
        //                ItemMasterId = x.ItemMasterId,
        //                ItemBarcodeNumber = x.ItemMaster?.Name,
        //                PurchaseItemId = x.PurchaseItemId,
        //                Batch = x.Batch,
        //                Expirydate = x.Expirydate,
        //                Mrp = x.Mrp,
        //                Qty = x.Qty,
        //                Rate = x.Rate,
        //                Gst = x.Gst,
        //                Discount = x.Discount,
        //                Amount = x.Amount,
        //            }).ToList() : new List<SalesOrderItemVM>();

        //        VM.SalsePaymentDetails = model.SalsePaymentDetails?.Select(pd => new SalsePaymentDetailsVM
        //        {
        //            Id = pd.Id,
        //            PaymentModeId = pd.PaymentModeId,
        //            Amount = pd.Amount,
        //            ReferenceNo = pd.ReferenceNo,
        //            Description = pd.Description,
        //            CustomerId = pd.CustomerId
        //        }).ToList() ?? new List<SalsePaymentDetailsVM>();
        //    }
        //    ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
        //    ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
        //    ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");

        //    // CHECK BUSINESS TYPE
        //    var tenantId = User?.FindFirst("TenantId")?.Value;
        //    var user = (await _tenantRepository.GetAll())
        //        .FirstOrDefault(x => x.Id == tenantId);
        //    var businessType = user?.BusinessType ?? 0;
        //    ViewBag.BusinessType = (int)businessType;

        //    return View(VM);
        //}

        
        [HttpGet]
        public async Task<IActionResult> GetItemByBarcode(string? barcode)
        {

            var item = await _itemmasterservice.GetByBarcode(barcode);
            if (item == null)
                return Json(new { error = "Item not found" });

            return Json(new { itemId = item.Id, itemname = item.Name });
        }
        [HttpGet]
        public async Task<IActionResult> GetItemById(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
                return Json(new { error = "Item not found" });

            return Json(new { itemId = item.Id, itemname = item.Name, category = item.Category?.CategoryName });
        }
        [HttpGet]
        public async Task<IActionResult> GetCustomerByMobile(string mobileNo)
        {
            var customer = await _customerservice.GetCustomerByMobileno(mobileNo); // your service logic

            if (customer == null)
                return Json(new { found = false });

            return Json(new
            {
                found = true,
                data = new
                {
                    customer.Name,
                    customer.Id,
                    customer.Address
                }
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
                        x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new
                {
                    success = false,
                    errors = errors
                });
            }

            var customerList = await _customerservice.GetAll();

            // ✅ Duplicate mobile check
            if (!string.IsNullOrEmpty(Vm.PhoneNo))
            {
                var isExist = customerList.Any(x =>
                    !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                    x.PhoneNo.Trim() == Vm.PhoneNo.Trim()
                );

                if (isExist)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Customer's phone number already exists."
                    });
                }
            }

            var Customer = new Customer
            {
                Name = Vm.Name,
                PhoneNo = Vm.PhoneNo,
                Address = Vm.Address,
                Email = Vm.Email
            };

            var result = await _customerservice.Create(Customer);

            return Json(new
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
        public async Task<IActionResult> GetOpticalDetails(int salesOrderId, int itemmasterId)
        {
            var opticalDetails = (await _opticalRepo.GetBySalesOrder(salesOrderId, itemmasterId))
                .Select(o => new
                {
                    o.Id,
                    o.ItemMasterId,
                    o.Rx,
                    o.Sphere,
                    o.Cylinder,
                    o.Axis,
                    o.Prism,
                    o.Add
                })
                .ToList();

            return Json(opticalDetails);
        }

    }
}
