using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EasyBill.UI.Filters;

namespace EasyBill.UI.Controllers
{
    public class PurchaseChallanController : Controller
    {
        private readonly IPurchaseChallanRepository _challanRepository;
        private readonly ISupplierRepository _supplierservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IHSNRepository _hsnservice;
        private readonly ITenantRepository _tenantService;
        private readonly IModeOfPaymentRepository _modeofpaymentservice;
        private readonly IStockService _currentstockservice;
        private readonly ISalseSettingRepository _salsesettingservice;

        // 🔥 StockService को यहाँ से हटा दिया गया है (TL के आर्किटेक्चर के हिसाब से)

        public PurchaseChallanController(
            IPurchaseChallanRepository challanRepository,
            ISupplierRepository supplierservice,
            IItemMasterRepository itemmasterservice,
            IHSNRepository hsnservice,
            ITenantRepository tenantService,
            IModeOfPaymentRepository modeofpaymentservice,
            IStockService currentstockservice,
            ISalseSettingRepository salsesettingservice)
        {
            _challanRepository = challanRepository;
            _supplierservice = supplierservice;
            _itemmasterservice = itemmasterservice;
            _hsnservice = hsnservice;
            _tenantService = tenantService;
            _modeofpaymentservice = modeofpaymentservice;
            _currentstockservice = currentstockservice;
            _salsesettingservice = salsesettingservice;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _challanRepository.GetAll();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllItems(string? purchaseType)
        {
            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var setting = await _salsesettingservice.GetByUserId(userId);
            var currentStocks = await _currentstockservice.GetAll();

            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var stockList = itemMasters.Select(item =>
            {
                var baseStock = stockDict.TryGetValue(item.Id, out var qty) ? qty : 0;

                string availableQty;
                if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
                {
                    int conversion = item.Conversion;
                    int totalTablets = (int)Math.Round(baseStock * conversion, MidpointRounding.AwayFromZero);
                    int strips = totalTablets / conversion;
                    int tablets = totalTablets % conversion;
                    availableQty = $"{strips}:{tablets}";
                }
                else
                {
                    availableQty = baseStock.ToString("0.##");
                }

                return new
                {
                    id = item.Id,
                    barcode = item.Barcode,
                    code = item.Code,
                    name = item.Name,
                    gst = item.Hsn?.IGST ?? 0,
                    cgst = item.Hsn?.CGST ?? 0,
                    sgst = item.Hsn?.SGST ?? 0,
                    hsn = item.HsnId ?? 0,
                    cess = item.Hsn?.Cess,
                    qty = baseStock,
                    availableQty = availableQty,
                    unit = item.Unit1 ?? "N/A",
                    mrp = item.Mrp
                };
            }).ToList();

            return Json(stockList);
        }

        [HttpGet]
        public async Task<IActionResult> GetBatchesByItemId(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
                return Json(new { error = "Item not found." });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var setting = await _salsesettingservice.GetByUserId(userId);
            var currentStocks = await _currentstockservice.GetAll();

            var batches = currentStocks
                .Where(x => x.ItemId == id)
                .GroupBy(x => new { x.Batch, x.ExpiryDate, x.Mrp, x.PurchaseRate })
                .Select(g =>
                {
                    var currentStock = g.Sum(x => x.Qty);
                    if (currentStock < 0)
                        currentStock = 0;

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
                        batch = g.Key.Batch,
                        expiry = g.Key.ExpiryDate?.ToString("yyyy-MM-dd"),
                        rate = g.Key.PurchaseRate,
                        mrp = g.Key.Mrp,
                        qty = currentStock,
                        availableQty = availableQty,
                        salserateA = 0,
                        salserateB = 0,
                    };
                })
                .ToList();

            var model = new
            {
                gst = item.Hsn?.IGST ?? 0,
                hsn = item.HsnId,
                barcode = item.Barcode,
                groupedResult = batches
            };

            return Json(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulatePurchaseViewDataAsync();
            ViewData["Title"] = "Create Purchase Challan";

            var model = new PurchaseChallanVM
            {
                BillDate = DateTime.Now,
                PartyBillDate = DateTime.Now,
                discountPercent = 0,
                BillNo = await GenerateNxtNumber(),
                billingType = "registered",
                PurchaseType = "Local",
                PaymentType = "cash",
                Status = "Pending",
                TenanatGst = await GetTenantGstAsync()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PurchaseChallanVM VM)
        {
            NormalizeCollections(VM);
            ValidateItemExpiryDates(VM);

            if (await _challanRepository.ExistsByBillNoAsync(VM.BillNo))
            {
                ModelState.AddModelError(nameof(VM.BillNo), $"{VM.BillNo?.Trim()} - This Challan Number already exists.");
            }

            if (!ModelState.IsValid)
            {
                await PopulatePurchaseViewDataAsync();
                ViewData["Title"] = "Create Purchase Challan";
                VM.TenanatGst = await GetTenantGstAsync();
                return View(VM);
            }

            var model = MapToEntity(VM);

            // 🔥 यहाँ Create कॉल होते ही Repository के अंदर ऑटोमैटिकली स्टॉक भी अपडेट हो जाएगा!
            await _challanRepository.Create(model);

            TempData["SuccessMessage"] = "Purchase Challan saved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var challan = await _challanRepository.GetById(id);
            if (challan == null)
            {
                return NotFound();
            }

            if (challan.ConvertedPurchaseId.HasValue)
            {
                TempData["ErrorMessage"] = "Converted challan cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            await PopulatePurchaseViewDataAsync();
            ViewData["Title"] = "Edit Purchase Challan";
            var model = MapToViewModel(challan);
            model.TenanatGst = await GetTenantGstAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PurchaseChallanVM VM)
        {
            NormalizeCollections(VM);
            ValidateItemExpiryDates(VM);

            if (!ModelState.IsValid)
            {
                await PopulatePurchaseViewDataAsync();
                ViewData["Title"] = "Edit Purchase Challan";
                VM.TenanatGst = await GetTenantGstAsync();
                return View(VM);
            }

            var challan = await _challanRepository.GetById(VM.Id);
            if (challan == null)
            {
                return NotFound();
            }

            if (challan.ConvertedPurchaseId.HasValue)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Converted challan cannot be edited."
                });
            }

            if (await _challanRepository.ExistsByBillNoAsync(VM.BillNo, VM.Id))
            {
                ModelState.AddModelError(nameof(VM.BillNo), $"{VM.BillNo?.Trim()} - This Challan Number already exists.");
                await PopulatePurchaseViewDataAsync();
                ViewData["Title"] = "Edit Purchase Challan";
                VM.TenanatGst = await GetTenantGstAsync();
                return View(VM);
            }

            ApplyToEntity(challan, VM);
            await _challanRepository.Update(challan);

            TempData["SuccessMessage"] = "Purchase Challan updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ConvertToPurchase(int id)
        {
            var challan = await _challanRepository.GetById(id);
            if (challan == null)
            {
                return NotFound();
            }

            if (challan.ConvertedPurchaseId.HasValue)
            {
                TempData["ErrorMessage"] = "This challan is already converted.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Purchase conversion is disabled so the purchase module stays unchanged.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var challan = await _challanRepository.GetById(id);
                if (challan == null)
                {
                    return Json(new { success = false, message = "Record not found." });
                }

                if (challan.ConvertedPurchaseId.HasValue)
                {
                    return Json(new { success = false, message = "Converted challan cannot be deleted." });
                }

                await _challanRepository.Delete(challan);

                return Json(new { success = true, message = "Purchase Challan deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public async Task<string> GenerateNxtNumber()
        {
            var nextNumber = (await _challanRepository.GetAll())
                .Where(x => !string.IsNullOrWhiteSpace(x.BillNo))
                .Select(x => ExtractTrailingNumber(x.BillNo))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"CH-{nextNumber:D4}";
        }

        private PurchaseChallan MapToEntity(PurchaseChallanVM VM)
        {
            return new PurchaseChallan
            {
                SupplierId = VM.SupplierId,
                BillNo = VM.BillNo?.Trim() ?? string.Empty,
                BillDate = VM.BillDate,
                PartyBillNo = VM.PartyBillNo ?? string.Empty,
                PartyBillDate = VM.PartyBillDate,
                TotalGstAmt = VM.TotalGstAmt,
                Totaldiscount = VM.Totaldiscount,
                TotalPayable = VM.TotalPayable,
                RoundOffAmount = VM.RoundOffAmount,
                discountPercent = VM.discountPercent,
                discountAmount = VM.discountAmount,
                Total = VM.Total,
                billingType = string.IsNullOrWhiteSpace(VM.billingType) ? "registered" : VM.billingType,
                PaymentType = string.IsNullOrWhiteSpace(VM.PaymentType) ? "cash" : VM.PaymentType,
                PurchaseType = VM.PurchaseType,
                TotalCGstAmt = VM.TotalCGstAmt,
                TotalSGstAmt = VM.TotalSGstAmt,
                PaidAmount = 0,
                ReturnAmount = 0,
                Balance = 0,
                PaymentAmt = 0,
                PaymentStatus = "Pending",
                Status = "Pending",
                PurchaseChallanItems = VM.PurchaseItemVms.Select(MapToItemEntity).ToList(),
                PaymentDetails = new List<SalsePaymentDetails>()
            };
        }

        private void ApplyToEntity(PurchaseChallan challan, PurchaseChallanVM VM)
        {
            challan.SupplierId = VM.SupplierId;
            challan.BillNo = VM.BillNo?.Trim() ?? string.Empty;
            challan.BillDate = VM.BillDate;
            challan.PartyBillNo = VM.PartyBillNo ?? string.Empty;
            challan.PartyBillDate = VM.PartyBillDate;
            challan.TotalGstAmt = VM.TotalGstAmt;
            challan.Totaldiscount = VM.Totaldiscount;
            challan.TotalPayable = VM.TotalPayable;
            challan.RoundOffAmount = VM.RoundOffAmount;
            challan.discountPercent = VM.discountPercent;
            challan.discountAmount = VM.discountAmount;
            challan.Total = VM.Total;
            challan.billingType = string.IsNullOrWhiteSpace(VM.billingType) ? "registered" : VM.billingType;
            challan.PaymentType = string.IsNullOrWhiteSpace(VM.PaymentType) ? "cash" : VM.PaymentType;
            challan.PurchaseType = VM.PurchaseType;
            challan.TotalCGstAmt = VM.TotalCGstAmt;
            challan.TotalSGstAmt = VM.TotalSGstAmt;
            challan.PaidAmount = 0;
            challan.ReturnAmount = 0;
            challan.Balance = 0;
            challan.PaymentAmt = 0;
            challan.PaymentStatus = "Pending";

            challan.PurchaseChallanItems ??= new List<PurchaseChallanItem>();
            challan.PaymentDetails ??= new List<SalsePaymentDetails>();

            var removedItems = challan.PurchaseChallanItems
                .Where(dbItem => !VM.PurchaseItemVms.Any(vmItem => vmItem.Id == dbItem.Id))
                .ToList();

            foreach (var item in removedItems)
            {
                challan.PurchaseChallanItems.Remove(item);
            }

            foreach (var item in VM.PurchaseItemVms)
            {
                if (item.Id > 0)
                {
                    var existingItem = challan.PurchaseChallanItems.FirstOrDefault(x => x.Id == item.Id);
                    if (existingItem != null)
                    {
                        ApplyToItemEntity(existingItem, item);
                    }
                }
                else
                {
                    challan.PurchaseChallanItems.Add(MapToItemEntity(item));
                }
            }

            challan.PaymentDetails.Clear();
        }

        private PurchaseChallanVM MapToViewModel(PurchaseChallan challan)
        {
            return new PurchaseChallanVM
            {
                Id = challan.Id,
                SupplierId = challan.SupplierId,
                SupplierName = challan.Suppliers?.FirstName,
                BillNo = challan.BillNo,
                BillDate = challan.BillDate,
                PartyBillNo = challan.PartyBillNo,
                PartyBillDate = challan.PartyBillDate,
                TotalGstAmt = challan.TotalGstAmt,
                Totaldiscount = challan.Totaldiscount,
                TotalPayable = challan.TotalPayable,
                RoundOffAmount = challan.RoundOffAmount,
                discountPercent = challan.discountPercent,
                discountAmount = challan.discountAmount,
                Total = challan.Total,
                billingType = string.IsNullOrWhiteSpace(challan.billingType) ? "registered" : challan.billingType,
                PaymentType = challan.PaymentType,
                PurchaseType = challan.PurchaseType,
                TotalCGstAmt = challan.TotalCGstAmt,
                TotalSGstAmt = challan.TotalSGstAmt,
                PaidAmount = challan.PaidAmount,
                ReturnAmount = challan.ReturnAmount,
                Balance = challan.Balance,
                Status = challan.Status,
                ConvertedPurchaseId = challan.ConvertedPurchaseId,
                TenanatGst = null,
                PurchaseItemVms = challan.PurchaseChallanItems.Select(x => new PurchaseItemVM
                {
                    Id = x.Id,
                    ItemId = x.ItemId,
                    ItemName = x.ItemMasters?.Name ?? string.Empty,
                    Batch = x.Batch,
                    ExpiryDate = x.ExpiryDate,
                    Mrp = x.Mrp,
                    Qty = x.Qty,
                    FreeQty = x.FreeQty,
                    Unit = x.Unit,
                    Rate = x.Rate,
                    HsnId = x.HsnId,
                    Gst = x.Gst,
                    GstAmount = x.GstAmount,
                    Discount = x.Discount,
                    DiscountAmt = x.DiscountAmt,
                    Amount = x.Amount,
                    TotalAmt = x.TotalAmt,
                    BatchWiseCose = x.BatchWiseCose,
                    salserateA = x.salserateA,
                    salserateB = x.salserateB,
                    Barcode = x.Barcode,
                    CGst = x.CGst,
                    SGst = x.SGst,
                    CGstAmount = x.CGstAmount,
                    SGstAmount = x.SGstAmount
                }).ToList(),
                PaymentDetails = challan.PaymentDetails?.Select(x => new SalsePaymentDetailsVM
                {
                    Id = x.Id,
                    PaymentModeId = x.PaymentModeId,
                    Amount = x.Amount,
                    ReferenceNo = x.ReferenceNo,
                    Description = x.Description
                }).ToList() ?? new List<SalsePaymentDetailsVM>()
            };
        }

        private static PurchaseChallanItem MapToItemEntity(PurchaseItemVM item)
        {
            return new PurchaseChallanItem
            {
                ItemId = item.ItemId,
                Batch = item.Batch,
                ExpiryDate = item.ExpiryDate,
                Mrp = item.Mrp,
                Qty = item.Qty,
                FreeQty = item.FreeQty,
                Unit = item.Unit,
                Rate = item.Rate,
                HsnId = item.HsnId,
                Gst = item.Gst,
                GstAmount = item.GstAmount,
                Discount = item.Discount,
                DiscountAmt = item.DiscountAmt,
                Amount = item.Amount,
                TotalAmt = item.TotalAmt,
                BatchWiseCose = item.BatchWiseCose,
                salserateA = item.salserateA,
                salserateB = item.salserateB,
                Barcode = item.Barcode,
                CGst = item.CGst,
                SGst = item.SGst,
                CGstAmount = item.CGstAmount,
                SGstAmount = item.SGstAmount,
                ConvertedQty = 0
            };
        }

        private static void ApplyToItemEntity(PurchaseChallanItem target, PurchaseItemVM item)
        {
            target.ItemId = item.ItemId;
            target.Batch = item.Batch;
            target.ExpiryDate = item.ExpiryDate;
            target.Mrp = item.Mrp;
            target.Qty = item.Qty;
            target.FreeQty = item.FreeQty;
            target.Unit = item.Unit;
            target.Rate = item.Rate;
            target.HsnId = item.HsnId;
            target.Gst = item.Gst;
            target.GstAmount = item.GstAmount;
            target.Discount = item.Discount;
            target.DiscountAmt = item.DiscountAmt;
            target.Amount = item.Amount;
            target.TotalAmt = item.TotalAmt;
            target.BatchWiseCose = item.BatchWiseCose;
            target.salserateA = item.salserateA;
            target.salserateB = item.salserateB;
            target.Barcode = item.Barcode;
            target.CGst = item.CGst;
            target.SGst = item.SGst;
            target.CGstAmount = item.CGstAmount;
            target.SGstAmount = item.SGstAmount;
        }

        private async Task PopulatePurchaseViewDataAsync()
        {
            ViewBag.supplier = new SelectList(await _supplierservice.GetALL(), "Id", "FirstName");
            ViewBag.Item = new SelectList((await _itemmasterservice.GetAll()).OrderBy(x => x.Name), "Id", "Name");
            ViewBag.Hsn = new SelectList(await _hsnservice.GetAll(), "Id", "HsnCode");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
        }

        private async Task<string?> GetTenantGstAsync()
        {
            var tenantid = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrWhiteSpace(tenantid))
            {
                return null;
            }

            var tenantdata = await _tenantService.GetById(tenantid);
            return tenantdata?.GstNo;
        }

        private static void NormalizeCollections(PurchaseChallanVM vm)
        {
            vm.PurchaseItemVms ??= new List<PurchaseItemVM>();
            vm.PaymentDetails ??= new List<SalsePaymentDetailsVM>();
        }

        private void ValidateItemExpiryDates(PurchaseChallanVM vm)
        {
            for (var index = 0; index < vm.PurchaseItemVms.Count; index++)
            {
                var item = vm.PurchaseItemVms[index];
                if (item.ItemId <= 0)
                {
                    continue;
                }

                if (!item.ExpiryDate.HasValue)
                {
                    ModelState.AddModelError(
                        $"PurchaseItemVms[{index}].ExpiryDate",
                        "Expiry date is required in MM/YYYY format.");
                    continue;
                }

                var expiry = item.ExpiryDate.Value;
                var effectiveExpiry = new DateTime(
                    expiry.Year,
                    expiry.Month,
                    DateTime.DaysInMonth(expiry.Year, expiry.Month));

                if (effectiveExpiry.Date < DateTime.Today)
                {
                    ModelState.AddModelError(
                        $"PurchaseItemVms[{index}].ExpiryDate",
                        "Back date expiry is not allowed.");
                }
            }
        }

        private static int? ExtractTrailingNumber(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var match = Regex.Match(code.Trim(), @"(\d+)(?!.*\d)");
            if (!match.Success)
            {
                return null;
            }

            if (int.TryParse(match.Value, out int number))
            {
                return number;
            }
            return null;
        }
    }
}
