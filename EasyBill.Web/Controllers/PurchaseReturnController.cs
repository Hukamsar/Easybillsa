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
using Microsoft.AspNetCore.Mvc;
using Microsoft.ReportingServices.Interfaces;
using OfficeOpenXml;
using System;
using static AOne.Utility.Permissions;
using Table = iText.Layout.Element.Table;
using EasyBill.UI.Filters;
namespace EasyBill.UI.Controllers
{
    public class PurchaseReturnController : Controller
    {
        private readonly IPurchaseReturnRepository _purchasereturnservice;
        private readonly ISupplierRepository _supplierservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IHSNRepository _hsnservice;
        private readonly IStockRepository _stockservice;
        private readonly IPurchaseItemRepository _purchaseitemservice;
        private readonly IPurchaseRepository _purchaseservice;
        private readonly ISalesRepository _salesService;
        private readonly IStockIssueRepository _stockissueservice;
        private readonly IStockReturnRepository _stockrturnservice;
        private readonly IStockReceiveRepository _stockreceiveservice;
        private readonly UserManager<ApplicationUsers> _userManager;
        private readonly ITenantRepository _tenantService;
        private readonly IStockService _currentStockService;
        private readonly ISalseSettingRepository _salesSettingRepo;
        public PurchaseReturnController(
            IPurchaseReturnRepository purchasereturnservice,
            ISupplierRepository supplierservice,
            IItemMasterRepository itemmasterservice,
            IHSNRepository hsnservice,
            IStockRepository stockservice,
            IPurchaseItemRepository purchaseitemservice,
            IPurchaseRepository purchaseservice,
            ISalesRepository salesService,
            IStockIssueRepository stockissueservice,
            IStockReturnRepository stockrturnservice,
            IStockReceiveRepository stockReceiveservice,
            ITenantRepository tenantService,
            UserManager<ApplicationUsers> userManager,
            IStockService currentStockService,
            ISalseSettingRepository salesSettingRepo
            )
        {
            _purchasereturnservice = purchasereturnservice;
            _supplierservice = supplierservice;
            _itemmasterservice = itemmasterservice;
            _hsnservice = hsnservice;
            _stockservice = stockservice;
            _purchaseitemservice = purchaseitemservice;
            _purchaseservice = purchaseservice;
            _salesService = salesService;
            _stockissueservice = stockissueservice;
            _stockrturnservice = stockrturnservice;
            _stockreceiveservice = stockReceiveservice;
            _userManager = userManager;
            _tenantService = tenantService;
            _currentStockService = currentStockService;
            _salesSettingRepo = salesSettingRepo;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _purchasereturnservice.GetAll();
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var tenantid = User.FindFirst("TenantId")?.Value;
            var tenantdata = await _tenantService.GetById(tenantid);
            // Check bussiness type 
            var businessType = tenantdata?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;
            ViewBag.supplier = new SelectList(await _supplierservice.GetALL(), "Id", "FirstName");
            //ViewBag.Item = new SelectList(await _purchaseitemservice.GetAll(), "ItemId", "Name");
            var purchaseItems = await _purchaseitemservice.GetAll();

            ViewBag.Item = new SelectList(purchaseItems.Where(x => x.ItemMasters != null).GroupBy(x => x.ItemId).Select(g => g.First()) // Avoid duplicates if needed
        .Select(p => new
        {
            ItemId = p.ItemId,
            Name = p.ItemMasters!.Name
        }),
               "ItemId", "Name"
            );

            ViewBag.Hsn = new SelectList(await _hsnservice.GetAll(), "Id", "HsnCode");
            ViewBag.PurchaseReturnReasons = Enum.GetValues(typeof(Purchasereturnreason)).Cast<Purchasereturnreason>()
                                             .Select(e => new SelectListItem
                                             {
                                                 Value = ((int)e).ToString(),
                                                 Text = e.ToString()
                                             }).ToList();

            var model = new PurchaseReturnVM()
            {
                BillDate = DateTime.Now,
                PartyBillDate = DateTime.Now,
                BillNo = await GenerateNxtNumber(),
                Reason = 0
            };
            return View(model);
        }


        [HttpPost]
        [HeadOfficeOnly]
        public async Task<IActionResult> Create(PurchaseReturnVM VM)
        {
            if (VM != null)
            {
                var model = new Models.Entity.PurchaseReturn
                {
                    SupplierId = VM.SupplierId,
                    BillNo = VM.BillNo,
                    BillDate = VM.BillDate,
                    PartyBillDate = VM.PartyBillDate,
                    Reason = (Purchasereturnreason)VM.Reason,
                    PartyBillNo = VM.PartyBillNo,
                    TotalGstAmt = VM.TotalGstAmt,
                    Totaldiscount = VM.Totaldiscount,
                    TotalPayable = VM.TotalPayable,
                    RoundOffAmount = VM.RoundOffAmount,
                    discountPercent = VM.discountPercent,
                    discountAmount = VM.discountAmount,
                    Total = VM.Total,
                    TotalCessAmt = VM.TotalCessAmt,
                    PurchaseReturnItems = VM.PurchaseReturnItemVMs.Select(x => new PurchaseReturnItem
                    {
                        ItemId = x.ItemId,
                        Batch = x.Batch,
                        ExpiryDate = x.ExpiryDate,
                        Mrp = x.Mrp,
                        Qty = x.Qty,
                        FreeQty = x.FreeQty,
                        Unit = x.Unit,
                        Rate = x.Rate,
                        HsnId = x.HsnId,
                        Gst = x.Gst,
                        Cess = x.Cess,
                        GstAmount = x.GstAmount,
                        Discount = x.Discount,
                        DiscountAmt = x.DiscountAmt,
                        Amount = x.Amount,
                        TotalAmt = x.TotalAmt,
                        BatchWiseCose = x.BatchWiseCose,
                        salserateA = x.salserateA,
                        salserateB = x.salserateB
                        //Reason = x.Reason
                    }).ToList() ?? new List<PurchaseReturnItem>()
                };
                foreach (var item in model.PurchaseReturnItems)
                {
                    decimal qty = item.Qty + item.FreeQty;

                    await _currentStockService.UpdateStock(
                        item.ItemId,
                        item.Batch?.Trim() ?? "",
                        -qty,   // 🔥 MINUS stock
                        item.ExpiryDate,
                        item.Mrp,
                        item.Rate
                    );
                }
                await _purchasereturnservice.Create(model);
            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id, string? returnUrl)
        {
            ViewBag.ReturnUrl=returnUrl;
            Models.Entity.PurchaseReturn model = await _purchasereturnservice.GetById(Id);
            PurchaseReturnVM purchaseVM = new PurchaseReturnVM();

            if (model != null)
            {
                purchaseVM.Id = model.Id;
                purchaseVM.SupplierId = model.SupplierId;

                var suppliers = await _supplierservice.GetALL();
                var supplier = suppliers.FirstOrDefault(x => x.Id == model.SupplierId);
                purchaseVM.SupplierName = supplier?.FirstName ?? "";

                purchaseVM.BillNo = model.BillNo;
                purchaseVM.BillDate = model.BillDate;
                purchaseVM.PartyBillNo = model.PartyBillNo;
                purchaseVM.PartyBillDate = model.PartyBillDate;
                purchaseVM.Reason = (int)model.Reason;
                purchaseVM.TotalGstAmt = model.TotalGstAmt;
                purchaseVM.discountPercent = model.discountPercent;
                purchaseVM.discountAmount = model.discountAmount;
                purchaseVM.Totaldiscount = model.Totaldiscount;
                purchaseVM.TotalPayable = model.TotalPayable;
                purchaseVM.RoundOffAmount = model.RoundOffAmount;
                purchaseVM.Total = model.Total;
                purchaseVM.TotalCessAmt = model.TotalCessAmt;
                // ✅ Get all items for name lookup
                var allItems = await _itemmasterservice.GetAll();

                purchaseVM.PurchaseReturnItemVMs = model.PurchaseReturnItems?.Select(x =>
                {
                    var item = allItems.FirstOrDefault(i => i.Id == x.ItemId);

                    return new PurchaseReturnItemVM
                    {
                        Id = x.Id,
                        ItemId = x.ItemId,
                        ItemName = item?.Name ?? "", // ✅✅✅ SET THIS
                        Batch = x.Batch,
                        ExpiryDate = x.ExpiryDate,
                        Mrp = x.Mrp,
                        Qty = x.Qty,
                        FreeQty = x.FreeQty,
                        Unit = x.Unit,
                        Rate = x.Rate,
                        HsnId = x.HsnId,
                        Gst = x.Gst,
                        Cess = x.Cess,
                        GstAmount = x.GstAmount,
                        Discount = x.Discount,
                        DiscountAmt = x.DiscountAmt,
                        Amount = x.Amount,
                        TotalAmt = x.TotalAmt,
                        BatchWiseCose = x.BatchWiseCose,
                        salserateA = x.salserateA,
                        salserateB = x.salserateB
                        //Reason = x.Reason
                    };
                }).ToList() ?? new List<PurchaseReturnItemVM>();
            }

            ViewBag.supplier = new SelectList(await _supplierservice.GetALL(), "Id", "FirstName");

            var purchase = (await _purchaseservice.GetAll())
                .Where(x => x.SupplierId == purchaseVM.SupplierId)
                .SelectMany(x => x.PurchaseItems ?? new List<PurchaseItem>());

            var itemMasterIds = purchase
                .Where(x => x.ItemMasters != null)
                .Select(x => x.ItemId)
                .Distinct()
                .ToList();

            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => itemMasterIds.Contains(x.Id))
                .OrderBy(x => x.Name);

            ViewBag.Item = new SelectList(
                itemMasters.Select(i => new
                {
                    Value = i.Id,
                    Text = i.Name
                }).ToList(),
                "Value",
                "Text"
            );

            ViewBag.Hsn = new SelectList(await _hsnservice.GetAll(), "Id", "HsnCode");

            ViewBag.PurchaseReturnReasons = Enum.GetValues(typeof(Purchasereturnreason))
                .Cast<Purchasereturnreason>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                }).ToList();
            var tenantid = User.FindFirst("TenantId")?.Value;
            var tenantdata = await _tenantService.GetById(tenantid);
            // Check bussiness type 
            var businessType = tenantdata?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;
            return View(purchaseVM);
        }
        [HttpPost]
        [HeadOfficeOnly]
        public async Task<IActionResult> Edit(PurchaseReturnVM VM)
        {
            Models.Entity.PurchaseReturn model = await _purchasereturnservice.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.SupplierId = VM.SupplierId;
                model.BillNo = VM.BillNo;
                model.BillDate = VM.BillDate;
                model.PartyBillNo = VM.PartyBillNo;
                model.PartyBillDate = VM.PartyBillDate;
                model.Reason = (Purchasereturnreason)VM.Reason;
                model.TotalGstAmt = VM.TotalGstAmt;
                model.discountPercent = VM.discountPercent;
                model.discountAmount = VM.discountAmount;
                model.Totaldiscount = VM.Totaldiscount;
                model.TotalPayable = VM.TotalPayable;
                model.RoundOffAmount = VM.RoundOffAmount;
                model.Total = VM.Total;
                model.TotalCessAmt = VM.TotalCessAmt;
                var removedItems = model.PurchaseReturnItems.Where(dbItem => !VM.PurchaseReturnItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

                // ❌ Remove Deleted Items
                foreach (var item in removedItems)
                {
                    decimal qty = item.Qty + item.FreeQty;

                    await _currentStockService.UpdateStock(
                        item.ItemId,
                        item.Batch?.Trim() ?? "",
                        qty,   // 🔥 reverse (add back)
                        item.ExpiryDate,
                        item.Mrp,
                        item.Rate
                    );
                    model.PurchaseReturnItems.Remove(item);
                }
                foreach (var item in VM.PurchaseReturnItemVMs)
                {
                    if (item.Id > 0) // Update existing items
                    {
                        var existingItem = model.PurchaseReturnItems.FirstOrDefault(x => x.Id == item.Id);
                        if (existingItem != null)
                        {
                            decimal oldQty = existingItem.Qty + existingItem.FreeQty;
                            decimal newQty = item.Qty + item.FreeQty;

                            // 🟢 Step 1: old stock wapas add
                            await _currentStockService.UpdateStock(
                                existingItem.ItemId,
                                existingItem.Batch?.Trim() ?? "",
                                oldQty,
                                existingItem.ExpiryDate,
                                existingItem.Mrp,
                                existingItem.Rate
                            );

                            // 🔴 Step 2: new stock minus
                            await _currentStockService.UpdateStock(
                                item.ItemId,
                                item.Batch?.Trim() ?? "",
                                -newQty,
                                item.ExpiryDate,
                                item.Mrp,
                                item.Rate
                            );
                            existingItem.ItemId = item.ItemId;
                            existingItem.Batch = item.Batch;
                            existingItem.ExpiryDate = item.ExpiryDate;
                            existingItem.Mrp = item.Mrp;
                            existingItem.Qty = item.Qty;
                            existingItem.FreeQty = item.FreeQty;
                            existingItem.Unit = item.Unit;
                            existingItem.Rate = item.Rate;
                            existingItem.HsnId = item.HsnId;
                            existingItem.Gst = item.Gst;
                            existingItem.Cess = item.Cess;
                            existingItem.GstAmount = item.GstAmount;
                            existingItem.Discount = item.Discount;
                            existingItem.DiscountAmt = item.DiscountAmt;
                            existingItem.Amount = item.Amount;
                            existingItem.TotalAmt = item.TotalAmt;
                            existingItem.BatchWiseCose = item.BatchWiseCose;
                            existingItem.salserateA = item.salserateA;
                            existingItem.salserateB = item.salserateB;
                            //existingItem.Reason = item.Reason;
                        }
                    }
                    else // Add new items
                    {
                        decimal qty = item.Qty + item.FreeQty;

                        await _currentStockService.UpdateStock(
                            item.ItemId,
                            item.Batch?.Trim() ?? "",
                            -qty,   // 🔥 MINUS
                            item.ExpiryDate,
                            item.Mrp,
                            item.Rate
                        );
                        model.PurchaseReturnItems.Add(new PurchaseReturnItem
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
                            Cess = item.Cess,
                            GstAmount = item.GstAmount,
                            Discount = item.Discount,
                            DiscountAmt = item.DiscountAmt,
                            Amount = item.Amount,
                            TotalAmt = item.TotalAmt,
                            BatchWiseCose = item.BatchWiseCose,
                            salserateA = item.salserateA,
                            salserateB = item.salserateB
                            //Reason = item.Reason
                        });
                    }
                }
                await _purchasereturnservice.Update(model);
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        [HeadOfficeOnly]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid Id for deletion." });
                }

                var model = await _purchasereturnservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                foreach (var item in model.PurchaseReturnItems)
                {
                    await _currentStockService.UpdateStock(
                        item.ItemId,
                        item.Batch ?? "",
                        item.Qty, // 🔺 SALE delete = add back
                        item.ExpiryDate,
                        item.Mrp,
                        item.Rate
                    );
                }
                await _purchasereturnservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        public async Task<string> GenerateNxtNumber()
        {
            var data = await _purchasereturnservice.GetAll();
            var lastCode = data
                .Where(p => !string.IsNullOrEmpty(p.BillNo))
                .Select(p => p.BillNo)
                .LastOrDefault();

            return GenerateNextProductCode(lastCode);
        }

        private string GenerateNextProductCode(string lastCode)
        {
            if (string.IsNullOrEmpty(lastCode) || lastCode.Length < 2)
                return "BL0001";

            string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray());

            string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

            int number = 0;
            int.TryParse(numberPart, out number);

            string nextCode = prefix + (number + 1).ToString("D" + numberPart.Length);

            return nextCode;
        }
        [HttpGet]
        public async Task<IActionResult> GetItemsBySupplierId(int supplierId)
        {
            // ===================== CURRENT STOCK =====================
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var setting = await _salesSettingRepo.GetByUserId(userId);
            var currentStocks = await _currentStockService.GetAll();

            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            // ===================== SUPPLIER PURCHASE ITEMS =====================
            var purchases = await _purchaseservice.GetAll();

            var supplierItemIds = purchases
                .Where(x => x.SupplierId == supplierId)
                .SelectMany(x => x.PurchaseItems ?? new List<PurchaseItem>())
                .Select(x => x.ItemId)
                .Distinct()
                .ToList();

            if (!supplierItemIds.Any())
            {
                return Json(new { success = false, items = new List<object>() });
            }

            // ===================== ITEM MASTER =====================
            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => supplierItemIds.Contains(x.Id))
                .ToList();

            // ===================== FINAL RESULT =====================
            var result = itemMasters.Select(item =>
            {
                decimal baseStock = stockDict.ContainsKey(item.Id) ? stockDict[item.Id] : 0;

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
                    value = item.Id,
                    text = item.Name,
                   // stock = baseStock,
                    qty = baseStock,
                    availableQty = availableQty,
                    unit = item.Unit1 ?? "N/A",
                    mrp = item.Mrp
                };
            })
            .Where(x => x.qty > 0)
            .ToList();

            return Json(new { success = true, items = result });
        }

        //[HttpGet]
        //public async Task<IActionResult> GetItemsBySupplierId(int supplierId)
        //{
        //    // ===================== LOAD MASTER DATA =====================
        //    var purchases = await _purchaseservice.GetAll();
        //    var purchaseReturns = await _purchasereturnservice.GetAll();
        //    var sales = await _salesService.GetAll();
        //    var stockIssues = await _stockissueservice.GetAll();
        //    var stockReturns = await _stockrturnservice.GetAll();
        //    var stockReceives = await _stockreceiveservice.GetAll();

        //    // ===================== SUPPLIER PURCHASE ITEMS (SPECIFIC SUPPLIER) =====================
        //    var supplierPurchases = purchases.Where(x => x.SupplierId == supplierId).ToList();
        //    var supplierPurchaseItems = supplierPurchases
        //        .SelectMany(x => x.PurchaseItems ?? new List<PurchaseItem>())
        //        .ToList();

        //    if (!supplierPurchaseItems.Any())
        //    {
        //        return Json(new { success = false, items = new List<object>() });
        //    }

        //    // ===================== SUPPLIER PURCHASE RETURN ITEMS (SPECIFIC SUPPLIER) =====================
        //    var supplierPurchaseReturns = purchaseReturns.Where(x => x.SupplierId == supplierId).ToList();
        //    var supplierPurchaseReturnItems = supplierPurchaseReturns
        //        .SelectMany(r => r.PurchaseReturnItems ?? new List<PurchaseReturnItem>())
        //        .ToList();

        //    // ===================== FLATTEN ONLY SALES/STOCK DATA (GLOBAL) =====================
        //    var salesItems = sales
        //        .Where(s => s.SalesItems != null)
        //        .SelectMany(s => s.SalesItems);

        //    var stockIssueItems = stockIssues
        //        .Where(s => s.StockIssuesItems != null)
        //        .SelectMany(s => s.StockIssuesItems);

        //    var stockReturnItems = stockReturns
        //        .Where(s => s.StockReturnItems != null)
        //        .SelectMany(s => s.StockReturnItems);

        //    var stockReceiveItems = stockReceives
        //        .Where(s => s.StockReceiveItems != null)
        //        .SelectMany(s => s.StockReceiveItems);

        //    // ===================== UNIQUE ITEM IDS FROM THIS SUPPLIER =====================
        //    var itemMasterIds = supplierPurchaseItems
        //        .Select(x => x.ItemId)
        //        .Distinct()
        //        .ToList();

        //    var itemMasters = (await _itemmasterservice.GetAll())
        //        .Where(x => itemMasterIds.Contains(x.Id))
        //        .ToList();

        //    // ===================== CALCULATE SUPPLIER-SPECIFIC STOCK =====================
        //    var result = itemMasters.Select(item =>
        //    {
        //        var itemId = item.Id;

        //        // ===== PURCHASED FROM THIS SUPPLIER ONLY (QTY + FREE) =====
        //        var purchasedQty = supplierPurchaseItems  // ✅ Changed: Only this supplier
        //            .Where(x => x.ItemId == itemId)
        //            .Sum(x => x.Qty);
        //        //.Sum(x => x.Qty + x.FreeQty);

        //        // ===== PURCHASE RETURN TO THIS SUPPLIER ONLY =====
        //        var purchaseReturnedQty = supplierPurchaseReturnItems  // ✅ Changed: Only this supplier
        //            .Where(x => x.ItemId == itemId)
        //            .Sum(x => x.Qty);
        //        //.Sum(x => x.Qty + x.FreeQty);

        //        // ===== SALES (GLOBAL - can't track supplier-wise) =====
        //        var soldQty = salesItems
        //            .Where(x => x.ItemMasterId == itemId && x.PurchaseItemId != null)
        //            .Where(x => supplierPurchaseItems.Any(p => p.Id == x.PurchaseItemId)) // ✅ Only if sold from this supplier's stock
        //            .Sum(x => x.Qty);

        //        // ===== STOCK ISSUE (GLOBAL - filtered by supplier purchase) =====
        //        var issuedQty = stockIssueItems
        //            .Where(x => x.ItemMasterId == itemId && x.PurchaseItemId != null)
        //            .Where(x => supplierPurchaseItems.Any(p => p.Id == x.PurchaseItemId)) // ✅ Only if issued from this supplier's stock
        //            .Sum(x => x.Qty);

        //        // ===== STOCK RETURN (GLOBAL - filtered by supplier purchase) =====
        //        var stockReturnedQty = stockReturnItems
        //            .Where(x => x.ItemMasterId == itemId && x.PurchaseItemId != null)
        //            .Where(x => supplierPurchaseItems.Any(p => p.Id == x.PurchaseItemId)) // ✅ Only if returned from this supplier's stock
        //            .Sum(x => x.Qty);

        //        // ===== STOCK RECEIVE (GLOBAL - filtered by supplier purchase) =====
        //        var stockReceivedQty = stockReceiveItems
        //            .Where(x => x.ItemMasterId == itemId && x.PurchaseItemId != null)
        //            .Where(x => supplierPurchaseItems.Any(p => p.Id == x.PurchaseItemId)) // ✅ Only if received from this supplier's stock
        //            .Sum(x => x.Qty);

        //        // ===================== SUPPLIER-SPECIFIC STOCK =====================
        //        var currentStock =
        //            purchasedQty
        //          + stockReturnedQty
        //          + stockReceivedQty
        //          - purchaseReturnedQty
        //          - soldQty
        //          - issuedQty;

        //        if (currentStock < 0)
        //            currentStock = 0;

        //        return new
        //        {
        //            value = itemId,
        //            text = item.Name,
        //            stock = currentStock,
        //            unit = item.Unit1 ?? "N/A",
        //            mrp = item.Mrp
        //        };
        //    })
        //    .Where(x => x.stock > 0) // Only items with stock
        //    .ToList();

        //    return Json(new { success = true, items = result });
        //}


        //[HttpGet]
        //public async Task<IActionResult> GetItemsBySupplierId(int supplierId)
        //{
        //    var purchases = await _purchaseservice.GetAll();
        //    var purchaseReturns = await _purchasereturnservice.GetAll();

        //    // Filter by supplier
        //    var supplierPurchases = purchases.Where(x => x.SupplierId == supplierId);
        //    var purchaseItems = supplierPurchases
        //        .SelectMany(x => x.PurchaseItems ?? new List<PurchaseItem>())
        //        .ToList();

        //    if (!purchaseItems.Any())
        //    {
        //        return Json(new { success = false, items = new List<object>() });
        //    }

        //    // Get unique items
        //    var itemMasterIds = purchaseItems
        //        .Select(x => x.ItemId)
        //        .Distinct()
        //        .ToList();

        //    var itemMasters = (await _itemmasterservice.GetAll())
        //        .Where(x => itemMasterIds.Contains(x.Id))
        //        .ToList();

        //    // Calculate supplier-specific stock
        //    var result = itemMasters.Select(item =>
        //    {
        //        var itemId = item.Id;

        //        // Total purchased from this supplier
        //        var totalPurchased = purchaseItems
        //            .Where(x => x.ItemId == itemId)
        //            .Sum(x => x.Qty + x.FreeQty);

        //        // Total returned to this supplier
        //        var totalReturned = purchaseReturns
        //            .Where(pr => pr.SupplierId == supplierId)
        //            .SelectMany(pr => pr.PurchaseReturnItems ?? new List<PurchaseReturnItem>())
        //            .Where(x => x.ItemId == itemId)
        //            .Sum(x => x.Qty + x.FreeQty);

        //        // Net stock from this supplier
        //        var supplierStock = totalPurchased - totalReturned;

        //        return new
        //        {
        //            value = itemId,
        //            text = item.Name,
        //            stock = supplierStock,       // Stock from this supplier only
        //            unit = item.Unit1 ?? "N/A",
        //            mrp = item.Mrp
        //        };
        //    })
        //    .Where(x => x.stock > 0)  // Only show items with available stock
        //    .ToList();

        //    return Json(new { success = true, items = result });
        //}
        [HttpGet]
        public async Task<IActionResult> GetItemsBySupplierIdForEditOnly(int supplierId)
        {
            var purchases = await _purchaseservice.GetAll();
            var purchaseReturns = await _purchasereturnservice.GetAll();
            var itemMasters = await _itemmasterservice.GetAll();
            var currentStocks = await _currentStockService.GetAll();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salesSettingRepo.GetByUserId(userId);

            // ===================== SUPPLIER PURCHASE =====================
            var supplierPurchases = purchases.Where(p => p.SupplierId == supplierId).ToList();

            var supplierPurchaseItems = supplierPurchases
                .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                .ToList();

            if (!supplierPurchaseItems.Any())
                return Json(new { success = false, items = new List<object>() });

            // ===================== PURCHASE RETURNS =====================
            var supplierPurchaseReturns = purchaseReturns
                .Where(r => r.SupplierId == supplierId)
                .ToList();

            var supplierPurchaseReturnItems = supplierPurchaseReturns
                .SelectMany(r => r.PurchaseReturnItems ?? new List<PurchaseReturnItem>())
                .ToList();

            // ===================== UNIQUE ITEMS =====================
            var supplierItemIds = supplierPurchaseItems.Select(x => x.ItemId).Distinct().ToList();
            var stockDict = currentStocks
               .GroupBy(x => x.ItemId)
               .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));
            // ===================== FINAL =====================
            var result = itemMasters
                .Where(i => supplierItemIds.Contains(i.Id))
                .Select(item =>
                {
                    var itemId = item.Id;

                    // ===== PURCHASED =====
                    var totalPurchasedQty = supplierPurchaseItems
                        .Where(x => x.ItemId == itemId)
                        .Sum(x => x.Qty);

                    // ===== RETURNED =====
                    var totalPurchaseReturnedQty = supplierPurchaseReturnItems
                        .Where(x => x.ItemId == itemId)
                        .Sum(x => x.Qty);

                    // ===== CURRENT STOCK =====
                    var currentStock = currentStocks
                        .Where(x => x.ItemId == itemId)
                        .Sum(x => x.Qty);

                    if (currentStock < 0)
                        currentStock = 0;

                    // ===== FINAL STOCK (EDIT CASE) =====
                    var finalStock = currentStock + totalPurchaseReturnedQty;

                    if (finalStock < 0)
                        finalStock = 0;

                    // ===================== ✅ CONVERSION =====================
                    decimal baseStock = stockDict.ContainsKey(item.Id) ? stockDict[item.Id] : 0;

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
                        value = itemId,
                        text = item.Name,

                        // ✅ RAW
                        currentStock = currentStock,
                        totalReturned = totalPurchaseReturnedQty,
                        stock = finalStock,

                        // ✅ NEW (IMPORTANT)
                        availableQty = availableQty,

                        unit = item.Unit1 ?? "N/A",
                        mrp = item.Mrp
                    };
                })
                .Where(x => x.stock > 0)
                .ToList();

            return Json(new { success = true, items = result });
        }
        //[HttpGet]
        //public async Task<IActionResult> GetItemsBySupplierIdForEditOnly(int supplierId)
        //{
        //    var purchases = await _purchaseservice.GetAll();
        //    var purchaseReturns = await _purchasereturnservice.GetAll();
        //    var sales = await _salesService.GetAll();
        //    var stockIssue = await _stockissueservice.GetAll();
        //    var stockReturn = await _stockrturnservice.GetAll();
        //    var stockReceive = await _stockreceiveservice.GetAll();
        //    var itemMasters = await _itemmasterservice.GetAll();

        //    // ===================== SUPPLIER-SPECIFIC PURCHASES =====================
        //    var supplierPurchases = purchases.Where(p => p.SupplierId == supplierId).ToList();

        //    var supplierPurchaseItems = supplierPurchases
        //        .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
        //        .ToList();

        //    if (!supplierPurchaseItems.Any())
        //        return Json(new { success = false, items = new List<object>() });

        //    // ===================== SUPPLIER-SPECIFIC PURCHASE RETURNS =====================
        //    var supplierPurchaseReturns = purchaseReturns.Where(r => r.SupplierId == supplierId).ToList();

        //    var supplierPurchaseReturnItems = supplierPurchaseReturns
        //        .SelectMany(r => r.PurchaseReturnItems ?? new List<PurchaseReturnItem>())
        //        .ToList();

        //    // ===================== FLATTEN GLOBAL DATA =====================
        //    var salesItems = sales
        //        .Where(s => s.SalesItems != null)
        //        .SelectMany(s => s.SalesItems);

        //    var stockIssueItems = stockIssue
        //        .Where(s => s.StockIssuesItems != null)
        //        .SelectMany(s => s.StockIssuesItems);

        //    var stockReturnItems = stockReturn
        //        .Where(s => s.StockReturnItems != null)
        //        .SelectMany(s => s.StockReturnItems);

        //    var stockReceiveItems = stockReceive
        //        .Where(s => s.StockReceiveItems != null)
        //        .SelectMany(s => s.StockReceiveItems);

        //    // ===================== GET SUPPLIER PURCHASE ITEM IDs =====================
        //    var supplierPurchaseItemIds = supplierPurchaseItems.Select(p => p.Id).ToHashSet();

        //    // ===================== UNIQUE ITEM IDs FROM THIS SUPPLIER =====================
        //    var supplierItemIds = supplierPurchaseItems
        //        .Select(x => x.ItemId)
        //        .Distinct()
        //        .ToList();

        //    // ===================== CALCULATE SUPPLIER-SPECIFIC STOCK =====================
        //    var result = itemMasters
        //        .Where(i => supplierItemIds.Contains(i.Id))
        //        .Select(item =>
        //        {
        //            var itemId = item.Id;

        //            // ===== PURCHASED FROM THIS SUPPLIER (QTY + FREE) =====
        //            var totalPurchasedQty = supplierPurchaseItems  // ✅ Only this supplier
        //                .Where(x => x.ItemId == itemId)
        //                .Sum(x => x.Qty);

        //            // ===== PURCHASE RETURN TO THIS SUPPLIER =====
        //            var totalPurchaseReturnedQty = supplierPurchaseReturnItems  // ✅ Only this supplier
        //                .Where(x => x.ItemId == itemId)
        //                .Sum(x => x.Qty);

        //            // ===== SALES (Only from this supplier's stock) =====
        //            var totalSalesQty = salesItems
        //                .Where(x => x.ItemMasterId == itemId
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only this supplier
        //                .Sum(x => x.Qty);

        //            // ===== STOCK ISSUE (Only from this supplier's stock) =====
        //            var totalStockIssueQty = stockIssueItems
        //                .Where(x => x.ItemMasterId == itemId
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only this supplier
        //                .Sum(x => x.Qty);

        //            // ===== STOCK RETURN (Only from this supplier's stock) =====
        //            var totalStockReturnQty = stockReturnItems
        //                .Where(x => x.ItemMasterId == itemId
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only this supplier
        //                .Sum(x => x.Qty);

        //            // ===== STOCK RECEIVE (Only from this supplier's stock) =====
        //            var totalStockReceiveQty = stockReceiveItems
        //                .Where(x => x.ItemMasterId == itemId
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only this supplier
        //                .Sum(x => x.Qty);

        //            // ===================== CURRENT STOCK (SUPPLIER-SPECIFIC) =====================
        //            var currentStock =
        //                totalPurchasedQty
        //              + totalStockReturnQty
        //              + totalStockReceiveQty
        //              - totalPurchaseReturnedQty
        //              - totalSalesQty
        //              - totalStockIssueQty;

        //            if (currentStock < 0)
        //                currentStock = 0;

        //            // ✅ FOR EDIT: Add back returned qty (already supplier-specific)
        //            var finalStock = currentStock + totalPurchaseReturnedQty;

        //            if (finalStock < 0)
        //                finalStock = 0;

        //            return new
        //            {
        //                value = itemId,
        //                text = item.Name,
        //                currentStock = currentStock,
        //                totalReturned = totalPurchaseReturnedQty,  // ✅ Already supplier-specific
        //                stock = finalStock,   // 🔥 FINAL = Current + Returned
        //                unit = item.Unit1 ?? "N/A",
        //                mrp = item.Mrp
        //            };
        //        })
        //        .Where(x => x.stock > 0)
        //        .ToList();

        //    return Json(new { success = true, items = result });
        //}

        [HttpGet]
        public async Task<IActionResult> GetReturnedQtyData(int supplierId)
        {
            var returnedQtyData = (await _purchasereturnservice.GetBySupplierId(supplierId)).SelectMany(x => x.PurchaseReturnItems)
                .GroupBy(r => new
                {
                    r.ItemId,
                    r.Batch,
                    Expiry = r.ExpiryDate,
                    r.Mrp,
                    r.Rate
                })
                .Select(g => new
                {
                    g.Key.ItemId,
                    g.Key.Batch,
                    Expiry = g.Key.Expiry?.ToString("yyyy-MM-dd"),
                    Mrp = g.Key.Mrp,
                    Rate = g.Key.Rate,
                    ReturnedQty = g.Sum(x => x.Qty)
                })
                .ToList();

            return Json(returnedQtyData);
        }

        public async Task<IActionResult> GetItemPurchaseHistory(int itemId, int supplierId)
        {
            var data = await _purchaseservice.GetItemPurchaseHistory(itemId, supplierId);
            if (data == null || !data.Any())
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetItemDetails(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
                return NotFound();

            return Json(new
            {
                gst = item.Hsn?.SGST,
                qty = item.MaximumQty,
                rate = item.Mrp,
                hsn = item.HsnId
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetBatchesByItemIdForEditOnly(int id, int supplierId)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
                return Json(new { error = "Item not found." });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salesSettingRepo.GetByUserId(userId);

            var purchases = await _purchaseservice.GetAll();
            var purchaseReturns = await _purchasereturnservice.GetAll();
            var currentStocks = await _currentStockService.GetAll();

            var supplierPurchases = purchases.Where(p => p.SupplierId == supplierId).ToList();

            var supplierPurchaseItems = supplierPurchases
                .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                .Where(x => x.ItemId == id)
                .ToList();

            if (!supplierPurchaseItems.Any())
                return Json(new { error = "No batch found for this supplier." });

            var supplierPurchaseReturns = purchaseReturns
                .Where(r => r.SupplierId == supplierId)
                .ToList();

            var supplierPurchaseReturnItems = supplierPurchaseReturns
                .SelectMany(r => r.PurchaseReturnItems ?? new List<PurchaseReturnItem>())
                .Where(x => x.ItemId == id)
                .ToList();

            var supplierBatches = supplierPurchaseItems
                .GroupBy(x => new { x.Batch, x.ExpiryDate, x.Mrp, x.Rate })
                .ToList();

            var groupedResult = supplierBatches.Select(g =>
            {
                var batchNo = g.Key.Batch;

                // ✅ PURCHASED (FIXED FILTER)
                var purchasedQty = supplierPurchaseItems
                    .Where(x => x.Batch == batchNo &&
                                x.ExpiryDate == g.Key.ExpiryDate &&
                                x.Mrp == g.Key.Mrp &&
                                x.Rate == g.Key.Rate)
                    .Sum(x => x.Qty);

                var purchasedFreeQty = supplierPurchaseItems
                    .Where(x => x.Batch == batchNo &&
                                x.ExpiryDate == g.Key.ExpiryDate &&
                                x.Mrp == g.Key.Mrp &&
                                x.Rate == g.Key.Rate)
                    .Sum(x => x.FreeQty);

                // ✅ RETURNED
                var purchaseReturnedQty = supplierPurchaseReturnItems
                    .Where(x => x.Batch == batchNo &&
                                x.ExpiryDate == g.Key.ExpiryDate &&
                                x.Mrp == g.Key.Mrp &&
                                x.Rate == g.Key.Rate)
                    .Sum(x => x.Qty);

                var purchaseReturnedFreeQty = supplierPurchaseReturnItems
                    .Where(x => x.Batch == batchNo &&
                                x.ExpiryDate == g.Key.ExpiryDate &&
                                x.Mrp == g.Key.Mrp &&
                                x.Rate == g.Key.Rate)
                    .Sum(x => x.FreeQty);

                // ✅ CURRENT STOCK (SAFE MATCH)
                var stock = currentStocks.FirstOrDefault(s =>
                    s.ItemId == id &&
                    string.Equals(s.Batch, batchNo, StringComparison.OrdinalIgnoreCase) &&
                    s.Mrp == g.Key.Mrp &&
                    s.ExpiryDate?.Date == g.Key.ExpiryDate?.Date
                );

                decimal currentStock = stock?.Qty ?? 0;
                if (currentStock < 0) currentStock = 0;

                // ✅ EDIT CASE
                var finalQty = currentStock + purchaseReturnedQty;
                if (finalQty < 0) finalQty = 0;

                var currentFreeQty = purchasedFreeQty - purchaseReturnedFreeQty;
                if (currentFreeQty < 0) currentFreeQty = 0;

                // ===================== ✅ CONVERSION =====================
                string availableQty;
                if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
                {
                    int conversion = item.Conversion;

                    int totalTablets = (int)Math.Round(finalQty * conversion, MidpointRounding.AwayFromZero);

                    int strips = totalTablets / conversion;
                    int tablets = totalTablets % conversion;

                    availableQty = $"{strips}:{tablets}";
                }
                else
                {
                    availableQty = finalQty.ToString("0.##");
                }

                return new
                {
                    ItemId = id,
                    batch = batchNo,
                    expiry = g.Key.ExpiryDate,
                    rate = g.Key.Rate,
                    mrp = g.Key.Mrp,

                    qty = finalQty,
                    freeQty = currentFreeQty,
                    currentStock = currentStock,
                    supplierReturned = purchaseReturnedQty,

                    // ✅ NEW FIELD
                    availableQty = availableQty
                };
            })
            .Where(x => x.qty > 0 || x.freeQty > 0)
            .ToList();

            return Json(new
            {
                gst = item.Hsn?.IGST ?? 0,
                cess = item.Hsn?.Cess ?? 0,
                hsn = item.HsnId,
                groupedResult = groupedResult
            });
        }
        //public async Task<IActionResult> GetBatchesByItemIdForEditOnly(int id, int supplierId)
        //{
        //    var item = await _itemmasterservice.GetByItemMasterId(id);
        //    if (item == null)
        //        return Json(new { error = "Item not found." });

        //    var purchases = await _purchaseservice.GetAll();
        //    var purchaseReturns = await _purchasereturnservice.GetAll();
        //    var sales = await _salesService.GetAll();
        //    var stockIssues = await _stockissueservice.GetAll();
        //    var stockReturns = await _stockrturnservice.GetAll();
        //    var stockReceives = await _stockreceiveservice.GetAll();

        //    // ===================== SUPPLIER-SPECIFIC PURCHASES =====================
        //    var supplierPurchases = purchases.Where(p => p.SupplierId == supplierId).ToList();

        //    var supplierPurchaseItems = supplierPurchases
        //        .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    if (!supplierPurchaseItems.Any())
        //        return Json(new { error = "No batch found for this supplier." });

        //    // ===================== SUPPLIER-SPECIFIC PURCHASE RETURNS =====================
        //    var supplierPurchaseReturns = purchaseReturns.Where(r => r.SupplierId == supplierId).ToList();

        //    var supplierPurchaseReturnItems = supplierPurchaseReturns
        //        .SelectMany(r => r.PurchaseReturnItems ?? new List<PurchaseReturnItem>())
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    // ===================== FLATTEN GLOBAL DATA =====================
        //    var salesItems = sales
        //        .Where(s => s.SalesItems != null)
        //        .SelectMany(s => s.SalesItems);

        //    var stockIssueItems = stockIssues
        //        .Where(s => s.StockIssuesItems != null)
        //        .SelectMany(s => s.StockIssuesItems);

        //    var stockReturnItems = stockReturns
        //        .Where(s => s.StockReturnItems != null)
        //        .SelectMany(s => s.StockReturnItems);

        //    var stockReceiveItems = stockReceives
        //        .Where(s => s.StockReceiveItems != null)
        //        .SelectMany(s => s.StockReceiveItems);

        //    // ===================== GET SUPPLIER PURCHASE ITEM IDs =====================
        //    var supplierPurchaseItemIds = supplierPurchaseItems.Select(p => p.Id).ToHashSet();

        //    // ===================== GROUP BY BATCH (SUPPLIER-SPECIFIC) =====================
        //    var supplierBatches = supplierPurchaseItems
        //        .GroupBy(x => new { x.Batch, x.ExpiryDate, x.Mrp, x.Rate })
        //        .ToList();

        //    var groupedResult = supplierBatches.Select(g =>
        //    {
        //        var batch = g.Key.Batch;

        //        // ===== PURCHASED FROM THIS SUPPLIER (QTY) =====
        //        var purchasedQty = supplierPurchaseItems  // ✅ Only this supplier
        //            .Where(x => x.Batch == batch)
        //            .Sum(x => x.Qty);

        //        // ===== PURCHASED FROM THIS SUPPLIER (FREE QTY) =====
        //        var purchasedFreeQty = supplierPurchaseItems  // ✅ Only this supplier
        //            .Where(x => x.Batch == batch)
        //            .Sum(x => x.FreeQty);

        //        // ===== PURCHASE RETURN TO THIS SUPPLIER (QTY) =====
        //        var purchaseReturnedQty = supplierPurchaseReturnItems  // ✅ Only this supplier
        //            .Where(x => x.Batch == batch
        //                     && x.ExpiryDate == g.Key.ExpiryDate
        //                     && x.Mrp == g.Key.Mrp
        //                     && x.Rate == g.Key.Rate)
        //            .Sum(x => x.Qty);

        //        // ===== PURCHASE RETURN TO THIS SUPPLIER (FREE QTY) =====
        //        var purchaseReturnedFreeQty = supplierPurchaseReturnItems  // ✅ Only this supplier
        //            .Where(x => x.Batch == batch
        //                     && x.ExpiryDate == g.Key.ExpiryDate
        //                     && x.Mrp == g.Key.Mrp
        //                     && x.Rate == g.Key.Rate)
        //            .Sum(x => x.FreeQty);

        //        // ===== SALES (Only from this supplier's batches) =====
        //        var soldQty = salesItems
        //            .Where(x => x.ItemMasterId == id
        //                     && x.Batch == batch
        //                     && x.PurchaseItemId.HasValue
        //                     && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only this supplier
        //            .Sum(x => x.Qty);

        //        // ===== STOCK ISSUE (Only from this supplier's batches) =====
        //        var issuedQty = stockIssueItems
        //            .Where(x => x.ItemMasterId == id
        //                     && x.Batch == batch
        //                     && x.PurchaseItemId.HasValue
        //                     && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only this supplier
        //            .Sum(x => x.Qty);

        //        // ===== STOCK RETURN (Only from this supplier's batches) =====
        //        var stockReturnedQty = stockReturnItems
        //            .Where(x => x.ItemMasterId == id
        //                     && x.Batch == batch
        //                     && x.PurchaseItemId.HasValue
        //                     && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only this supplier
        //            .Sum(x => x.Qty);

        //        // ===== STOCK RECEIVE (Only from this supplier's batches) =====
        //        var stockReceivedQty = stockReceiveItems
        //            .Where(x => x.ItemMasterId == id
        //                     && x.Batch == batch
        //                     && x.PurchaseItemId.HasValue
        //                     && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only this supplier
        //            .Sum(x => x.Qty);

        //        // ===================== CURRENT STOCK (SUPPLIER-SPECIFIC) =====================
        //        var currentStock =
        //            purchasedQty
        //          + stockReturnedQty
        //          + stockReceivedQty
        //          - purchaseReturnedQty
        //          - soldQty
        //          - issuedQty;

        //        // ✅ FINAL QTY FOR EDIT (Add back the returned qty to show what was originally available)
        //        var finalQty = currentStock + purchaseReturnedQty;  // ✅ Already supplier-specific
        //        if (finalQty < 0) finalQty = 0;

        //        // ===================== FREE QTY =====================
        //        var currentFreeQty = purchasedFreeQty;  // ✅ Already supplier-specific
        //        if (currentFreeQty < 0)
        //            currentFreeQty = 0;

        //        return new
        //        {
        //            ItemId = id,
        //            batch = batch,
        //            expiry = g.Key.ExpiryDate,
        //            rate = g.Key.Rate,
        //            mrp = g.Key.Mrp,
        //            qty = finalQty,
        //            freeQty = currentFreeQty,
        //            currentStock = currentStock,
        //            supplierReturned = purchaseReturnedQty  // ✅ Already supplier-specific
        //        };
        //    })
        //    .Where(x => x.qty > 0 || x.freeQty > 0)  // ✅ Show if either available
        //    .ToList();

        //    return Json(new
        //    {
        //        gst = item.Hsn?.IGST ?? 0,
        //        hsn = item.HsnId,
        //        groupedResult = groupedResult
        //    });
        //}
        [HttpGet]
        public async Task<IActionResult> GetBatchesByItemId(int id, int supplierId)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);

            if (item == null)
                return Json(new { error = "Hsn Details not found." });
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var setting = await _salesSettingRepo.GetByUserId(userId);
            // ===================== LOAD MASTER DATA =====================
            var purchases = await _purchaseservice.GetAll();
            var purchaseReturns = await _purchasereturnservice.GetAll();

            // ✅ CURRENT STOCK TABLE (IMPORTANT)
            var currentStocks = await _currentStockService.GetAll();

            // ===================== SUPPLIER PURCHASE =====================
            var supplierPurchases = purchases.Where(p => p.SupplierId == supplierId).ToList();

            var supplierPurchaseItems = supplierPurchases
                .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                .Where(x => x.ItemId == id)
                .ToList();

            if (!supplierPurchaseItems.Any())
                return Json(new { error = "No purchase found for this supplier." });

            // ===================== PURCHASE RETURNS =====================
            var supplierPurchaseReturns = purchaseReturns
                .Where(r => r.SupplierId == supplierId)
                .ToList();

            var supplierPurchaseReturnItems = supplierPurchaseReturns
                .SelectMany(r => r.PurchaseReturnItems ?? new List<PurchaseReturnItem>())
                .Where(x => x.ItemId == id)
                .ToList(); 
            // ===================== GROUP BY (SAME AS YOUR LOGIC) =====================
            var groupedResult = supplierPurchaseItems
                .GroupBy(x => new { x.Batch, x.ExpiryDate, x.Mrp, x.Rate, x.ItemId })
                .Select(g =>
                {
                    var batchNo = g.Key.Batch;

                    // ✅ PURCHASED (SAME)
                    var purchasedQty = supplierPurchaseItems
                        .Where(x => x.Batch == batchNo)
                        .Sum(x => x.Qty);

                    var purchasedFreeQty = supplierPurchaseItems
                        .Where(x => x.Batch == batchNo)
                        .Sum(x => x.FreeQty);

                    // ✅ RETURNED (SAME)
                    var purchaseReturnedQty = supplierPurchaseReturnItems
                        .Where(x => x.Batch == batchNo)
                        .Sum(x => x.Qty);

                    var purchaseReturnedFreeQty = supplierPurchaseReturnItems
                        .Where(x => x.Batch == batchNo)
                        .Sum(x => x.FreeQty);

                    // ===================== ✅ CURRENT STOCK FROM TABLE =====================
                    var stock = currentStocks.FirstOrDefault(s =>
                        s.ItemId == id &&
                        string.Equals(s.Batch, batchNo, StringComparison.OrdinalIgnoreCase) &&
                        s.Mrp == g.Key.Mrp &&
                        s.ExpiryDate?.Date == g.Key.ExpiryDate?.Date
                    );

                    decimal currentStock = stock?.Qty ?? 0;

                    if (currentStock < 0)
                        currentStock = 0;

                    // ===================== FREE QTY =====================
                    var currentFreeQty = purchasedFreeQty - purchaseReturnedFreeQty;

                    if (currentFreeQty < 0)
                        currentFreeQty = 0;

                    //string availableQty;

                    //if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
                    //{
                    //    int conversion = item.Conversion;

                    //    int totalTablets = (int)Math.Round(currentStock, MidpointRounding.AwayFromZero);

                    //    int strips = totalTablets / conversion;
                    //    int tablets = totalTablets % conversion;

                    //    availableQty = $"{strips}:{tablets}";
                    //}
                    //else
                    //{
                    //    availableQty = currentStock.ToString("0.##");
                    //}

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
                        ItemId = g.Key.ItemId,
                        batch = batchNo,
                        expiry = g.Key.ExpiryDate,
                        rate = g.Key.Rate,
                        mrp = g.Key.Mrp,

                        // ✅ KEEP ALL YOUR ORIGINAL FIELDS
                        purchasedQty = purchasedQty,
                        purchasedFreeQty = purchasedFreeQty,
                        returnedQty = purchaseReturnedQty,
                        returnedFreeQty = purchaseReturnedFreeQty,

                        // ✅ ONLY THIS CHANGED
                        qty = currentStock,
                        freeQty = currentFreeQty,
                        availableQty = availableQty,
                        currentStock = currentStock,
                        purchaseItemId = g.Select(x => x.Id).FirstOrDefault()
                    };
                })
                .Where(x => x.qty > 0 || x.freeQty > 0)
                .ToList();

            return Json(new
            {
                gst = item.Hsn?.IGST ?? 0,
                cess = item.Hsn?.Cess ?? 0,
                hsn = item.HsnId,
                groupedResult = groupedResult
            });
        }
        //public async Task<IActionResult> GetBatchesByItemId(int id, int supplierId)
        //{
        //    var item = await _itemmasterservice.GetByItemMasterId(id);

        //    if (item == null)
        //        return Json(new { error = "Hsn Details not found." });

        //    // ===================== LOAD MASTER DATA =====================
        //    var purchases = await _purchaseservice.GetAll();
        //    var purchaseReturns = await _purchasereturnservice.GetAll();
        //    var sales = await _salesService.GetAll();
        //    var stockIssues = await _stockissueservice.GetAll();
        //    var stockReturns = await _stockrturnservice.GetAll();
        //    var stockReceives = await _stockreceiveservice.GetAll();

        //    // ===================== SUPPLIER-SPECIFIC PURCHASES =====================
        //    var supplierPurchases = purchases.Where(p => p.SupplierId == supplierId).ToList();

        //    var supplierPurchaseItems = supplierPurchases
        //        .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    if (!supplierPurchaseItems.Any())
        //        return Json(new { error = "No purchase found for this supplier." });

        //    // ===================== SUPPLIER-SPECIFIC PURCHASE RETURNS =====================
        //    var supplierPurchaseReturns = purchaseReturns.Where(r => r.SupplierId == supplierId).ToList();

        //    var supplierPurchaseReturnItems = supplierPurchaseReturns
        //        .SelectMany(r => r.PurchaseReturnItems ?? new List<PurchaseReturnItem>())
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    // ===================== FLATTEN GLOBAL DATA (for tracking movements) =====================
        //    var salesItems = sales
        //        .Where(s => s.SalesItems != null)
        //        .SelectMany(s => s.SalesItems);

        //    var stockIssueItems = stockIssues
        //        .Where(s => s.StockIssuesItems != null)
        //        .SelectMany(s => s.StockIssuesItems);

        //    var stockReturnItems = stockReturns
        //        .Where(s => s.StockReturnItems != null)
        //        .SelectMany(s => s.StockReturnItems);

        //    var stockReceiveItems = stockReceives
        //        .Where(s => s.StockReceiveItems != null)
        //        .SelectMany(s => s.StockReceiveItems);

        //    // ===================== GET PURCHASE ITEM IDs FROM THIS SUPPLIER =====================
        //    var supplierPurchaseItemIds = supplierPurchaseItems.Select(p => p.Id).ToHashSet();

        //    // ===================== GROUP BY BATCH (SUPPLIER-SPECIFIC) =====================
        //    var groupedResult = supplierPurchaseItems
        //        .GroupBy(x => new { x.Batch, x.ExpiryDate, x.Mrp, x.Rate, x.ItemId })
        //        .Select(g =>
        //        {
        //            var batchNo = g.Key.Batch;

        //            // ===== PURCHASED FROM THIS SUPPLIER (QTY) =====
        //            var purchasedQty = supplierPurchaseItems  // ✅ Only this supplier
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // ===== PURCHASED FROM THIS SUPPLIER (FREE QTY) =====
        //            var purchasedFreeQty = supplierPurchaseItems  // ✅ Only this supplier
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.FreeQty);

        //            // ===== PURCHASE RETURN TO THIS SUPPLIER (QTY) =====
        //            var purchaseReturnedQty = supplierPurchaseReturnItems  // ✅ Only this supplier
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // ===== PURCHASE RETURN TO THIS SUPPLIER (FREE QTY) =====
        //            var purchaseReturnedFreeQty = supplierPurchaseReturnItems  // ✅ Only this supplier
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.FreeQty);

        //            // ===== SALES (Only from this supplier's batches) =====
        //            var soldQty = salesItems
        //                .Where(x => x.ItemMasterId == id
        //                         && x.Batch == batchNo
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only if sold from this supplier
        //                .Sum(x => x.Qty);

        //            // ===== STOCK ISSUE (Only from this supplier's batches) =====
        //            var issuedQty = stockIssueItems
        //                .Where(x => x.ItemMasterId == id
        //                         && x.Batch == batchNo
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only if issued from this supplier
        //                .Sum(x => x.Qty);

        //            // ===== STOCK RETURN (Only from this supplier's batches) =====
        //            var stockReturnedQty = stockReturnItems
        //                .Where(x => x.ItemMasterId == id
        //                         && x.Batch == batchNo
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only if returned from this supplier
        //                .Sum(x => x.Qty);

        //            // ===== STOCK RECEIVE (Only from this supplier's batches) =====
        //            var stockReceivedQty = stockReceiveItems
        //                .Where(x => x.ItemMasterId == id
        //                         && x.Batch == batchNo
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only if received from this supplier
        //                .Sum(x => x.Qty);

        //            // ===================== CURRENT STOCK (SUPPLIER-SPECIFIC) =====================
        //            var currentStock =
        //                purchasedQty
        //              + stockReturnedQty
        //              + stockReceivedQty
        //              - purchaseReturnedQty
        //              - soldQty
        //              - issuedQty;

        //            if (currentStock < 0)
        //                currentStock = 0;

        //            // ===================== FREE QTY STOCK =====================
        //            var currentFreeQty =
        //                purchasedFreeQty
        //              - purchaseReturnedFreeQty;

        //            if (currentFreeQty < 0)
        //                currentFreeQty = 0;

        //            return new
        //            {
        //                ItemId = g.Key.ItemId,
        //                batch = batchNo,
        //                expiry = g.Key.ExpiryDate,
        //                rate = g.Key.Rate,
        //                mrp = g.Key.Mrp,

        //                // ✅ ORIGINAL PURCHASED QUANTITIES (for validation)
        //                purchasedQty = purchasedQty,           // Total purchased from this supplier
        //                purchasedFreeQty = purchasedFreeQty,   // Total free qty purchased

        //                // ✅ ALREADY RETURNED QUANTITIES
        //                returnedQty = purchaseReturnedQty,     // Already returned to this supplier
        //                returnedFreeQty = purchaseReturnedFreeQty,

        //                // ✅ CURRENT AVAILABLE STOCK (for display)
        //                qty = currentStock,                    // Current remaining stock
        //                freeQty = currentFreeQty,              // Current free qty

        //                currentStock = currentStock,
        //                purchaseItemId = g.Select(x => x.Id).FirstOrDefault()
        //            };
        //        })
        //        .Where(x => x.qty > 0 || x.freeQty > 0)  // ✅ Show if either qty or freeQty available
        //        .ToList();

        //    // ===================== FINAL RESPONSE =====================
        //    return Json(new
        //    {
        //        gst = item.Hsn?.IGST ?? 0,
        //        hsn = item.HsnId,
        //        groupedResult = groupedResult
        //    });
        //}

        //public async Task<IActionResult> GetBatchesByItemId(int id, int supplierId)
        //{
        //    var item = await _itemmasterservice.GetByItemMasterId(id);

        //    if (item == null)
        //        return Json(new { error = "Hsn Details not found." });

        //    // ===================== LOAD MASTER DATA =====================
        //    var purchases = await _purchaseservice.GetAll();
        //    var purchaseReturns = await _purchasereturnservice.GetAll();
        //    var sales = await _salesService.GetAll();
        //    var stockIssues = await _stockissueservice.GetAll();
        //    var stockReturns = await _stockrturnservice.GetAll();
        //    var stockReceives = await _stockreceiveservice.GetAll();

        //    // ===================== SUPPLIER-SPECIFIC PURCHASES =====================
        //    var supplierPurchases = purchases.Where(p => p.SupplierId == supplierId).ToList();

        //    var supplierPurchaseItems = supplierPurchases
        //        .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    if (!supplierPurchaseItems.Any())
        //        return Json(new { error = "No purchase found for this supplier." });

        //    // ===================== SUPPLIER-SPECIFIC PURCHASE RETURNS =====================
        //    var supplierPurchaseReturns = purchaseReturns.Where(r => r.SupplierId == supplierId).ToList();

        //    var supplierPurchaseReturnItems = supplierPurchaseReturns
        //        .SelectMany(r => r.PurchaseReturnItems ?? new List<PurchaseReturnItem>())
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    // ===================== FLATTEN GLOBAL DATA (for tracking movements) =====================
        //    var salesItems = sales
        //        .Where(s => s.SalesItems != null)
        //        .SelectMany(s => s.SalesItems);

        //    var stockIssueItems = stockIssues
        //        .Where(s => s.StockIssuesItems != null)
        //        .SelectMany(s => s.StockIssuesItems);

        //    var stockReturnItems = stockReturns
        //        .Where(s => s.StockReturnItems != null)
        //        .SelectMany(s => s.StockReturnItems);

        //    var stockReceiveItems = stockReceives
        //        .Where(s => s.StockReceiveItems != null)
        //        .SelectMany(s => s.StockReceiveItems);

        //    // ===================== GET PURCHASE ITEM IDs FROM THIS SUPPLIER =====================
        //    var supplierPurchaseItemIds = supplierPurchaseItems.Select(p => p.Id).ToHashSet();

        //    // ===================== GROUP BY BATCH (SUPPLIER-SPECIFIC) =====================
        //    var groupedResult = supplierPurchaseItems
        //        .GroupBy(x => new { x.Batch, x.ExpiryDate, x.Mrp, x.Rate, x.ItemId })
        //        .Select(g =>
        //        {
        //            var batchNo = g.Key.Batch;

        //            // ===== PURCHASED FROM THIS SUPPLIER (QTY) =====
        //            var purchasedQty = supplierPurchaseItems  // ✅ Only this supplier
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // ===== PURCHASED FROM THIS SUPPLIER (FREE QTY) =====
        //            var purchasedFreeQty = supplierPurchaseItems  // ✅ Only this supplier
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.FreeQty);

        //            // ===== PURCHASE RETURN TO THIS SUPPLIER (QTY) =====
        //            var purchaseReturnedQty = supplierPurchaseReturnItems  // ✅ Only this supplier
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // ===== PURCHASE RETURN TO THIS SUPPLIER (FREE QTY) =====
        //            var purchaseReturnedFreeQty = supplierPurchaseReturnItems  // ✅ Only this supplier
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.FreeQty);

        //            // ===== SALES (Only from this supplier's batches) =====
        //            var soldQty = salesItems
        //                .Where(x => x.ItemMasterId == id
        //                         && x.Batch == batchNo
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only if sold from this supplier
        //                .Sum(x => x.Qty);

        //            // ===== STOCK ISSUE (Only from this supplier's batches) =====
        //            var issuedQty = stockIssueItems
        //                .Where(x => x.ItemMasterId == id
        //                         && x.Batch == batchNo
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only if issued from this supplier
        //                .Sum(x => x.Qty);

        //            // ===== STOCK RETURN (Only from this supplier's batches) =====
        //            var stockReturnedQty = stockReturnItems
        //                .Where(x => x.ItemMasterId == id
        //                         && x.Batch == batchNo
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only if returned from this supplier
        //                .Sum(x => x.Qty);

        //            // ===== STOCK RECEIVE (Only from this supplier's batches) =====
        //            var stockReceivedQty = stockReceiveItems
        //                .Where(x => x.ItemMasterId == id
        //                         && x.Batch == batchNo
        //                         && x.PurchaseItemId.HasValue
        //                         && supplierPurchaseItemIds.Contains(x.PurchaseItemId.Value))  // ✅ Only if received from this supplier
        //                .Sum(x => x.Qty);

        //            // ===================== CURRENT STOCK (SUPPLIER-SPECIFIC) =====================
        //            var currentStock =
        //                purchasedQty
        //              + stockReturnedQty
        //              + stockReceivedQty
        //              - purchaseReturnedQty
        //              - soldQty
        //              - issuedQty;

        //            if (currentStock < 0)
        //                currentStock = 0;

        //            // ===================== FREE QTY STOCK =====================
        //            var currentFreeQty =
        //                purchasedFreeQty
        //              - purchaseReturnedFreeQty;

        //            if (currentFreeQty < 0)
        //                currentFreeQty = 0;

        //            return new
        //            {
        //                ItemId = g.Key.ItemId,
        //                batch = batchNo,
        //                expiry = g.Key.ExpiryDate,
        //                rate = g.Key.Rate,
        //                mrp = g.Key.Mrp,
        //                qty = currentStock,
        //                freeQty = currentFreeQty,
        //                currentStock = currentStock,
        //                purchaseItemId = g.Select(x => x.Id).FirstOrDefault()
        //            };
        //        })
        //        .Where(x => x.qty > 0 || x.freeQty > 0)  // ✅ Show if either qty or freeQty available
        //        .ToList();

        //    // ===================== FINAL RESPONSE =====================
        //    return Json(new
        //    {
        //        gst = item.Hsn?.IGST ?? 0,
        //        hsn = item.HsnId,
        //        groupedResult = groupedResult
        //    });
        //}

        //public async Task<IActionResult> GetBatchesByItemId(int id, int supplierId)
        //{
        //    var item = await _itemmasterservice.GetByItemMasterId(id);

        //    if (item != null)
        //    {
        //        var purchase = (await _purchaseservice.GetAll()).Where(x => x.SupplierId == supplierId).SelectMany(x => x.PurchaseItems);


        //        if (purchase == null || !purchase.Any())
        //        {
        //            return Json(new { error = "No purchase found for this supplier." });
        //        }

        //        var purchaseItems = purchase.Where(s => s.ItemId == id).ToList();

        //        if (!purchaseItems.Any())
        //        {
        //            return Json(new { error = "No purchase items found for this item and supplier." });
        //        }

        //        var purchasereturns = await _purchasereturnservice.GetAll();
        //        var purchaseReturnItems = purchasereturns.Where(r => r.PurchaseReturnItems != null).SelectMany(r => r.PurchaseReturnItems);
        //        var groupedResult = purchaseItems
        //       .GroupBy(b => new { b.Batch, b.ExpiryDate, b.Mrp, b.Rate, b.ItemId })
        //       .Select(g =>
        //       {
        //           var batchNo = g.Key.Batch;

        //           // Total Purchased Qty
        //           var totalPurchasedQty = g.Sum(x => x.Qty + x.FreeQty);

        //           // Total Returned Qty (match by batch + item)
        //           var returnedQty = purchaseReturnItems
        //               .Where(x => x.Batch == batchNo && x.ItemId == g.Key.ItemId)
        //               .Sum(x => x.Qty + x.FreeQty);

        //           // Final available qty
        //           var finalQty = totalPurchasedQty - returnedQty;
        //           if (finalQty < 0) finalQty = 0;

        //           return new
        //           {
        //               ItemId = g.Key.ItemId,
        //               batch = batchNo,
        //               expiry = g.Key.ExpiryDate,
        //               rate = g.Key.Rate,
        //               mrp = g.Key.Mrp,
        //               qty = finalQty,
        //               purchaseItemId = g.Select(x => x.Id).FirstOrDefault()
        //           };
        //       })
        //       .Where(x => x.qty > 0)
        //       .ToList();


        //        var model = new
        //        {
        //            gst = item.Hsn?.IGST ?? 0, // Null check with default 0
        //            hsn = item.HsnId,
        //            groupedResult = groupedResult
        //        };

        //        return Json(model);
        //    }
        //    else
        //    {
        //        return Json(new { error = "Hsn Details not found." });
        //    }
        //}





        [HttpGet]
        public async Task<IActionResult> PurchaseReturnReportItemWise(DateTime? fromDate , DateTime? toDate)
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
            var purchasereturn = (await _purchasereturnservice.GetAll())
         .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate)  
         .ToList();

            // Flatten all SalesItems
            var purchasereturnItems = purchasereturn
                .Where(s => s.PurchaseReturnItems != null)
                .SelectMany(s => s.PurchaseReturnItems);

            // Group by ItemMasterId to calculate total sales
            var groupedpurchase = purchasereturnItems
                              .GroupBy(x => x.ItemId)
                              .Select(g => new
                              {
                                  ItemMasterId = g.Key,
                                  TotalQty = g.Sum(x => x.Qty),
                                  TotalAmount = g.Sum(x => x.TotalAmt), // Assuming already discounted
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
            var purchaseList = (from item in itemMasters
                                join g in groupedpurchase on item.Id equals g.ItemMasterId
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

            ViewBag.TotalQty = purchaseList.Sum(x => x.Stocks);
            ViewBag.TotalAmount = purchaseList.Sum(x => x.Amount);
            ViewBag.TotalGst = purchaseList.Sum(x => x.GstAmount);
            return View(purchaseList);
        }

        //Export To excel
        [HttpGet]
        public async Task<IActionResult> ExportPurchaseReturnItemWiseExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();
            var purchaseReturns = (await _purchasereturnservice.GetAll())
                .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate);

            var items = purchaseReturns
                .Where(p => p.PurchaseReturnItems != null)
                .SelectMany(p => p.PurchaseReturnItems);

            var grouped = items
                .GroupBy(x => x.ItemId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    Qty = g.Sum(x => x.Qty),
                    Amount = g.Sum(x => x.TotalAmt),
                    Gst = g.Sum(x =>
                    {
                        var gross = x.Rate * x.Qty;
                        var discount = gross * (x.Discount / 100);
                        var taxable = gross - discount;
                        return taxable * (x.Gst / 100);
                    })
                }).ToList();

            var list = (from item in itemMasters
                        join g in grouped on item.Id equals g.ItemMasterId
                        where g.Qty > 0
                        select new StockVM
                        {
                            ItemCode = item.Code,
                            ItemName = item.Name,
                            CategoryName = item.Category?.CategoryName ?? "Unknown",
                            Stocks = g.Qty,
                            GstAmount = g.Gst,
                            Amount = g.Amount
                        }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Purchase Return Item Wise");
            var user = await _userManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }
            // COMPANY NAME
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 6).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // REPORT NAME
            ws.Cell(2, 1).Value = "Purchase Return Item Wise Report";
            ws.Range(2, 1, 2, 6).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // DATE RANGE
            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 6).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;
            string[] headers = { "Item Code", "Item Name", "Category", "Total Qty", "GST Amount", "Total Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 6).Style.Font.SetBold();
            row++;

            foreach (var x in list)
            {
                ws.Cell(row, 1).Value = x.ItemCode;
                ws.Cell(row, 2).Value = x.ItemName;
                ws.Cell(row, 3).Value = x.CategoryName;
                ws.Cell(row, 4).Value = x.Stocks;
                ws.Cell(row, 5).Value = x.GstAmount;
                ws.Cell(row, 6).Value = x.Amount;
                row++;
            }

            // ✅ TOTAL ROW
            ws.Cell(row, 3).Value = "TOTAL";
            ws.Cell(row, 4).Value = list.Sum(x => x.Stocks);
            ws.Cell(row, 5).Value = list.Sum(x => x.GstAmount);
            ws.Cell(row, 6).Value = list.Sum(x => x.Amount);

            ws.Range(row, 3, row, 6).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.LightGray);

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PurchaseReturnItemWise.xlsx"
            );
        }

        //Export to pdf
        [HttpGet]
        public async Task<IActionResult> ExportPurchaseReturnItemWisePdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();
            var purchaseReturns = (await _purchasereturnservice.GetAll())
                .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate);

            var items = purchaseReturns
                .Where(p => p.PurchaseReturnItems != null)
                .SelectMany(p => p.PurchaseReturnItems);

            var grouped = items
                .GroupBy(x => x.ItemId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    Qty = g.Sum(x => x.Qty),
                    Amount = g.Sum(x => x.TotalAmt),
                    Gst = g.Sum(x =>
                    {
                        var gross = x.Rate * x.Qty;
                        var discount = gross * (x.Discount / 100);
                        var taxable = gross - discount;
                        return taxable * (x.Gst / 100);
                    })
                }).ToList();

            var list = (from item in itemMasters
                        join g in grouped on item.Id equals g.ItemMasterId
                        where g.Qty > 0
                        select new StockVM
                        {
                            ItemCode = item.Code,
                            ItemName = item.Name,
                            CategoryName = item.Category?.CategoryName ?? "Unknown",
                            Stocks = g.Qty,
                            GstAmount = g.Gst,
                            Amount = g.Amount
                        }).ToList();

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());
            var user = await _userManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }
            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Company Name
            doc.Add(new Paragraph(companyName)
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER));

            // Report Name
            doc.Add(new Paragraph("Purchase Return Item Wise Report")
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER));

            // Date Range
            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));
            Table table = new Table(6).UseAllAvailableWidth();

            string[] headers = { "Item Code", "Item Name", "Category", "Total Qty", "GST Amount", "Total Amount" };

            foreach (var h in headers)
                table.AddHeaderCell(new Cell().Add(new Paragraph(h)
                    .SetFont(bold)
                    .SetFontSize(9)));

            foreach (var x in list)
            {
                table.AddCell(new Cell().Add(new Paragraph(x.ItemCode)
                    .SetFont(normal).SetFontSize(9)));

                table.AddCell(new Cell().Add(new Paragraph(x.ItemName)
                    .SetFont(normal).SetFontSize(9)));

                table.AddCell(new Cell().Add(new Paragraph(x.CategoryName)
                    .SetFont(normal).SetFontSize(9)));

                table.AddCell(new Cell().Add(new Paragraph(x.Stocks.ToString())
                    .SetFont(normal).SetFontSize(9)));

                table.AddCell(new Cell().Add(new Paragraph($"{x.GstAmount:0.00}")
                    .SetFont(normal).SetFontSize(9)));

                table.AddCell(new Cell().Add(new Paragraph($"{x.Amount:0.00}")
                    .SetFont(normal).SetFontSize(9)));
            }
            // ✅ TOTAL ROW (only font size added)
            table.AddCell(new Cell(1, 3).Add(new Paragraph("TOTAL")
                .SetFont(bold).SetFontSize(9)));

            table.AddCell(new Cell().Add(new Paragraph(list.Sum(x => x.Stocks).ToString())
                .SetFont(bold).SetFontSize(9)));

            table.AddCell(new Cell().Add(new Paragraph($"{list.Sum(x => x.GstAmount):0.00}")
                .SetFont(bold).SetFontSize(9)));

            table.AddCell(new Cell().Add(new Paragraph($"{list.Sum(x => x.Amount):0.00}")
                .SetFont(bold).SetFontSize(9)));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "PurchaseReturnItemWise.pdf");
        }


        //public async Task<IActionResult> PurchaseReturnReportSupplierWise(DateTime? fromDate, DateTime? toDate ,int? SupplierId)
        //{
        //    var supplier = await _supplierservice.GetALL();

        //    ViewBag.Supplier = supplier;
        //    ViewBag.SelectedSupplierId = SupplierId;
        //    var purchase = await _purchasereturnservice.GetAll();

        //    if (SupplierId.HasValue && SupplierId.Value > 0)
        //    {
        //        purchase = purchase.Where(p => p.SupplierId == SupplierId.Value).ToList();
        //    }

        //    var billwiseList = purchase.Select(sale => new PurchaseReturnVM
        //    {
        //        Id = sale.Id,
        //        BillNo = sale.BillNo,
        //        BillDate = sale.BillDate,
        //        SupplierName = sale.Suppliers?.FirstName ?? "Unknown",
        //        TotalGstAmt = sale.TotalGstAmt,
        //        TotalPayable = sale.PurchaseReturnItems?.Sum(item => item.TotalAmt) ?? 0
        //    })
        //    .OrderByDescending(x => x.BillDate)
        //    .ToList();

        //    return View(billwiseList);
        //}

        [HttpGet]
        public async Task<IActionResult> PurchaseReturnReportSupplierWise(DateTime? fromDate, DateTime? toDate,int? SupplierId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            var supplier = await _supplierservice.GetALL();
            ViewBag.Supplier = supplier;
            ViewBag.SelectedSupplierId = SupplierId;

            var purchase = await _purchasereturnservice.GetAll();

            // 🔹 Date filter
            purchase = purchase
                .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate)
                .ToList();

            // 🔹 Supplier filter (unchanged)
            if (SupplierId.HasValue && SupplierId.Value > 0)
            {
                purchase = purchase
                    .Where(p => p.SupplierId == SupplierId.Value)
                    .ToList();
            }

            var billwiseList = purchase.Select(sale => new PurchaseReturnVM
            {
                Id = sale.Id,
                BillNo = sale.BillNo,
                BillDate = sale.BillDate,
                SupplierName = sale.Suppliers?.FirstName ?? "Unknown",
                TotalGstAmt = sale.TotalGstAmt,
                TotalPayable = sale.PurchaseReturnItems?.Sum(item => item.TotalAmt) ?? 0
            })
            .OrderByDescending(x => x.BillDate)
            .ToList();
            ViewBag.TotalGst= billwiseList.Sum(x => x.TotalGstAmt);
            ViewBag.TotalPayable= billwiseList.Sum(x => x.TotalPayable);
            return View(billwiseList);
        }
        //Export to excel
        [HttpGet]
        public async Task<IActionResult> PurchaseReturnSupplierWiseExcel(DateTime? fromDate, DateTime? toDate,int? SupplierId)
        {
            var purchase = await _purchasereturnservice.GetAll();

            if (fromDate.HasValue && toDate.HasValue)
            {
                purchase = purchase
                    .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate)
                    .ToList();
            }

            if (SupplierId.HasValue && SupplierId.Value > 0)
            {
                purchase = purchase
                    .Where(p => p.SupplierId == SupplierId.Value)
                    .ToList();
            }

            var data = purchase.Select(x => new
            {
                x.BillNo,
                BillDate = x.BillDate?.ToString("dd-MM-yyyy"),
                Supplier = x.Suppliers?.FirstName ?? "Unknown",
                Gst = x.TotalGstAmt,
                Amount = x.PurchaseReturnItems?.Sum(i => i.TotalAmt) ?? 0
            }).ToList();

            using var package = new ExcelPackage();
            var user = await _userManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }
            var ws = package.Workbook.Worksheets.Add("Purchase Return");

            // COMPANY NAME
            ws.Cells["A1"].Value = companyName;
            ws.Cells["A1:E1"].Merge = true;
            ws.Cells["A1:E1"].Style.Font.Bold = true;
            ws.Cells["A1:E1"].Style.Font.Size = 16;
            ws.Cells["A1:E1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // REPORT NAME
            ws.Cells["A2"].Value = "Purchase Return Supplier Wise Report";
            ws.Cells["A2:E2"].Merge = true;
            ws.Cells["A2:E2"].Style.Font.Bold = true;
            ws.Cells["A2:E2"].Style.Font.Size = 13;
            ws.Cells["A2:E2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // DATE RANGE
            ws.Cells["A3"].Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Cells["A3:E3"].Merge = true;
            ws.Cells["A3:E3"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // DATA TABLE
            ws.Cells["A5"].LoadFromCollection(data, true);

            ws.Cells["D1"].Value = "GST Amount";
            ws.Cells["E1"].Value = "Total Amount";
            int totalRow = data.Count + 6;

            ws.Cells["C" + totalRow].Value = "TOTAL";
            ws.Cells["D" + totalRow].Value = data.Sum(x => x.Gst);
            ws.Cells["E" + totalRow].Value = data.Sum(x => x.Amount);

            ws.Cells["C" + totalRow + ":E" + totalRow].Style.Font.Bold = true;
            ws.Cells.AutoFitColumns();

            return File(
                package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PurchaseReturnSupplierWise.xlsx"
            );
        }

        //Export to pdf
        [HttpGet]
        public async Task<IActionResult> PurchaseReturnSupplierWisePdf(DateTime? fromDate, DateTime? toDate, int? SupplierId)
        {
            var purchase = await _purchasereturnservice.GetAll();

            if (fromDate.HasValue && toDate.HasValue)
            {
                purchase = purchase
                    .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate)
                    .ToList();
            }

            if (SupplierId.HasValue && SupplierId.Value > 0)
            {
                purchase = purchase
                    .Where(p => p.SupplierId == SupplierId.Value)
                    .ToList();
            }

            var list = purchase.Select(x => new
            {
                x.BillNo,
                x.BillDate,
                Supplier = x.Suppliers?.FirstName ?? "Unknown",
                Gst = x.TotalGstAmt,
                Amount = x.PurchaseReturnItems?.Sum(i => i.TotalAmt) ?? 0
            }).ToList();

            decimal totalGst = list.Sum(x => x.Gst);
            decimal totalAmount = list.Sum(x => x.Amount);
            var user = await _userManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }
            using var ms = new MemoryStream();
            var writer = new PdfWriter(ms);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            // Fonts
            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Header
            doc.Add(new Paragraph(companyName)
                .SetFont(bold)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph("Purchase Return Supplier Wise Report")
                .SetFont(bold)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // Table
            var table = new Table(5).UseAllAvailableWidth();

            // Headers
            table.AddHeaderCell(new Cell().Add(new Paragraph("Bill No").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Date").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Supplier").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("GST").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Amount").SetFont(bold).SetFontSize(9)));

            // Rows
            foreach (var item in list)
            {
                table.AddCell(new Cell().Add(new Paragraph(item.BillNo ?? "").SetFont(normal).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(item.BillDate?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(item.Supplier).SetFont(normal).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph($"{item.Gst:0.00}").SetFont(normal).SetFontSize(9)).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Cell().Add(new Paragraph($"{item.Amount:0.00}").SetFont(normal).SetFontSize(9)).SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL ROW
            table.AddCell(new Cell().Add(new Paragraph("").SetFont(normal)));
            table.AddCell(new Cell().Add(new Paragraph("").SetFont(normal)));

            table.AddCell(new Cell().Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(9)));

            table.AddCell(new Cell().Add(new Paragraph($"{totalGst:0.00}").SetFont(bold).SetFontSize(9))
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Cell().Add(new Paragraph($"{totalAmount:0.00}").SetFont(bold).SetFontSize(9))
                .SetTextAlignment(TextAlignment.RIGHT));

            doc.Add(table);
            doc.Close();

            return File(
                ms.ToArray(),
                "application/pdf",
                "PurchaseReturnSupplierWise.pdf"
            );
        }

    }
}