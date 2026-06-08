using AOne.DataAccess.Data;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBill.UI.Controllers
{
    public class StockConversionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockService _stockService;

        public StockConversionController(ApplicationDbContext context, IStockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        // GET: StockConversion
        public async Task<IActionResult> Index(string search = "", int page = 1, int pageSize = 10)
        {
            var query = _context.StockConversions
                .Include(x => x.BulkItem)
                .Include(x => x.RetailItem)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.VoucherNo.Contains(search) 
                    || (x.BulkItem != null && x.BulkItem.Name.Contains(search))
                    || (x.RetailItem != null && x.RetailItem.Name.Contains(search)));
            }

            int totalItems = await query.CountAsync();
            var paginatedData = await query
                .OrderByDescending(x => x.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Search = search;

            return View(paginatedData);
        }

        // GET: StockConversion/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Generate sequential VoucherNo
            var lastConversion = await _context.StockConversions
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            string voucherNo = "SC-0001";
            if (lastConversion != null)
            {
                var numPart = lastConversion.VoucherNo.Replace("SC-", "");
                if (int.TryParse(numPart, out int num))
                {
                    voucherNo = "SC-" + (num + 1).ToString("D4");
                }
            }

            var bulkItems = await _context.ItemMasters
                .Where(x => x.ItemType == "Bulk" && x.Deleted == null && x.IsActive == true)
                .ToListAsync();

            ViewBag.BulkItems = new SelectList(bulkItems, "Id", "Name");
            ViewBag.VoucherNo = voucherNo;

            return View();
        }

        // GET: StockConversion/GetBatches
        [HttpGet]
        public async Task<IActionResult> GetBatches(int itemId)
        {
            var batches = await _context.CurrentStocks
                .Where(x => x.ItemId == itemId && x.Qty > 0 && x.Deleted == null)
                .Select(x => new
                {
                    batch = x.Batch,
                    expiry = x.ExpiryDate.HasValue ? x.ExpiryDate.Value.ToString("yyyy-MM-dd") : "",
                    qty = x.Qty,
                    mrp = x.Mrp,
                    purchaseRate = x.PurchaseRate
                })
                .ToListAsync();

            return Json(batches);
        }

        // GET: StockConversion/GetChildItems
        [HttpGet]
        public async Task<IActionResult> GetChildItems(int parentItemId)
        {
            var children = await _context.ItemMasters
                .Where(x => x.ParentItemId == parentItemId && x.Deleted == null && x.IsActive == true)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    conversionFactor = x.ConversionFactor ?? 1,
                    barcode = x.Barcode ?? "",
                    packing = x.Packing ?? "",
                    unit1 = x.Unit1 ?? ""
                })
                .ToListAsync();

            return Json(children);
        }

        // POST: StockConversion/QuickCreateChild
        [HttpPost]
        public async Task<IActionResult> QuickCreateChild(int parentItemId, string packing, string unit, decimal conversionFactor, decimal mrp, decimal salesRate)
        {
            if (parentItemId <= 0 || string.IsNullOrEmpty(packing) || string.IsNullOrEmpty(unit) || conversionFactor <= 0)
            {
                return Json(new { success = false, message = "Please fill in all details correctly to create a repack size." });
            }

            var parent = await _context.ItemMasters.FindAsync(parentItemId);
            if (parent == null)
            {
                return Json(new { success = false, message = "Parent bulk item not found." });
            }

            var tenantId = User?.FindFirst("TenantId")?.Value;

            // Generate unique product code
            var nextCode = "IT0001";
            var lastItem = await _context.ItemMasters
                .Where(x => x.Code != null && x.Code.StartsWith("IT"))
                .OrderByDescending(x => x.Code)
                .FirstOrDefaultAsync();
            if (lastItem != null && lastItem.Code.Length > 2)
            {
                var numPart = lastItem.Code.Substring(2);
                if (int.TryParse(numPart, out int num))
                {
                    nextCode = "IT" + (num + 1).ToString("D4");
                }
            }

            var child = new ItemMaster
            {
                Name = parent.Name + " " + packing,
                Code = nextCode,
                ItemType = "Repacked",
                ParentItemId = parentItemId,
                ConversionFactor = conversionFactor,
                Packing = packing,
                Unit1 = unit,
                Mrp = mrp,
                SalesRate1 = salesRate,
                SalesRate2 = salesRate,
                CategoryId = parent.CategoryId,
                SubCategoryId = parent.SubCategoryId,
                DivisionId = parent.DivisionId,
                CompanyId = parent.CompanyId,
                HsnId = parent.HsnId,
                Local = parent.Local,
                Central = parent.Central,
                IsActive = true,
                TenantId = tenantId
            };

            _context.ItemMasters.Add(child);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Repacked child size created successfully!",
                item = new
                {
                    id = child.Id,
                    name = child.Name,
                    conversionFactor = child.ConversionFactor ?? 1,
                    barcode = child.Barcode ?? "",
                    packing = child.Packing ?? "",
                    unit1 = child.Unit1 ?? ""
                }
            });
        }

        // POST: StockConversion/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockConversionPayload payload)
        {
            if (payload == null || payload.BulkItemId <= 0 || string.IsNullOrEmpty(payload.Batch) || payload.Targets == null || !payload.Targets.Any())
            {
                return Json(new { success = false, message = "Please fill in all required fields correctly and add at least one repackaging row." });
            }

            var tenantId = User?.FindFirst("TenantId")?.Value;

            // Fetch bulk stock details to get purchase rates, mrp, expiry
            var bulkStock = await _context.CurrentStocks
                .FirstOrDefaultAsync(x => x.ItemId == payload.BulkItemId && x.Batch == payload.Batch && x.Deleted == null);

            if (bulkStock == null)
            {
                return Json(new { success = false, message = "Selected bulk item batch stock not found." });
            }

            decimal totalBulkQtyNeeded = payload.Targets.Sum(t => t.BulkQtyUsed);
            if (bulkStock.Qty < totalBulkQtyNeeded)
            {
                return Json(new { success = false, message = $"Insufficient stock available for the selected bulk item batch. Available: {bulkStock.Qty}, Required: {totalBulkQtyNeeded}" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Generate sequential VoucherNo (ensuring no concurrency clashes)
                var lastConversion = await _context.StockConversions
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                string voucherNo = "SC-0001";
                if (lastConversion != null)
                {
                    var numPart = lastConversion.VoucherNo.Replace("SC-", "");
                    if (int.TryParse(numPart, out int num))
                    {
                        voucherNo = "SC-" + (num + 1).ToString("D4");
                    }
                }

                // 1. Deduct total bulk item stock
                await _stockService.UpdateStock(
                    payload.BulkItemId,
                    payload.Batch,
                    -totalBulkQtyNeeded,
                    bulkStock.ExpiryDate,
                    bulkStock.Mrp,
                    bulkStock.SalesRateA,
                    bulkStock.SalesRateB,
                    bulkStock.PurchaseRate,
                    bulkStock.Barcode
                );

                // 2. Add retail item stock and log conversion logs
                foreach (var target in payload.Targets)
                {
                    var childItem = await _context.ItemMasters.FindAsync(target.RetailItemId);
                    if (childItem == null)
                    {
                        throw new Exception($"Selected retail item with ID {target.RetailItemId} not found.");
                    }

                    // Auto-generate barcode for child item if empty
                    if (string.IsNullOrWhiteSpace(childItem.Barcode))
                    {
                        var baseCode = "200" + target.RetailItemId.ToString().PadLeft(7, '0') + "00"; // 12 digits
                        int sum = 0;
                        for (int i = 0; i < 12; i++)
                        {
                            int val = baseCode[i] - '0';
                            sum += (i % 2 == 0) ? val : val * 3;
                        }
                        int checkDigit = (10 - (sum % 10)) % 10;
                        childItem.Barcode = baseCode + checkDigit;
                        _context.Entry(childItem).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }

                    // Rates division based on Conversion Factor
                    decimal factor = childItem.ConversionFactor ?? 1;
                    decimal retailMrp = childItem.Mrp > 0 ? childItem.Mrp : (bulkStock.Mrp / factor);
                    decimal retailSalesRateA = childItem.SalesRate1 > 0 ? childItem.SalesRate1 : (bulkStock.SalesRateA / factor);
                    decimal retailSalesRateB = childItem.SalesRate2 > 0 ? childItem.SalesRate2 : (bulkStock.SalesRateB / factor);
                    decimal retailPurchaseRate = bulkStock.PurchaseRate / factor;

                    // Add retail item stock
                    await _stockService.UpdateStock(
                        target.RetailItemId,
                        payload.Batch,
                        target.RetailQty,
                        bulkStock.ExpiryDate,
                        retailMrp,
                        retailSalesRateA,
                        retailSalesRateB,
                        retailPurchaseRate,
                        childItem.Barcode
                    );

                    // Log Stock Conversion record
                    var conversion = new StockConversion
                    {
                        VoucherNo = voucherNo,
                        TransactionDate = DateTime.Now,
                        BulkItemId = payload.BulkItemId,
                        BulkQty = target.BulkQtyUsed,
                        RetailItemId = target.RetailItemId,
                        RetailQty = target.RetailQty,
                        WastageQty = target.WastageQty,
                        Remarks = payload.Remarks,
                        TenantId = tenantId
                    };

                    _context.StockConversions.Add(conversion);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Stock conversions processed successfully!", voucherNo = voucherNo });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "An error occurred during conversion: " + ex.Message });
            }
        }

        // GET: StockConversion/PrintStickers
        public async Task<IActionResult> PrintStickers(int? id, string? voucherNo)
        {
            IQueryable<StockConversion> query = _context.StockConversions
                .Include(x => x.BulkItem)
                .Include(x => x.RetailItem);

            List<StockConversion> conversions;
            if (!string.IsNullOrEmpty(voucherNo))
            {
                conversions = await query.Where(x => x.VoucherNo == voucherNo).ToListAsync();
            }
            else if (id.HasValue)
            {
                var single = await query.FirstOrDefaultAsync(x => x.Id == id.Value);
                if (single == null) return NotFound();
                conversions = await query.Where(x => x.VoucherNo == single.VoucherNo).ToListAsync();
            }
            else
            {
                return BadRequest("Provide voucherNo or id");
            }

            if (!conversions.Any())
            {
                return NotFound();
            }

            // Get stock details for each target item to show correct MRP/expiry in the sticker view
            var itemIds = conversions.Select(x => x.RetailItemId).Distinct().ToList();
            var stocks = await _context.CurrentStocks
                .Where(x => itemIds.Contains(x.ItemId) && x.Deleted == null)
                .ToListAsync();

            ViewBag.Stocks = stocks;
            ViewBag.VoucherNo = conversions.First().VoucherNo;

            return View(conversions);
        }
    }
}
