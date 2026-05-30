using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class OpeningStockController : Controller
    {
        private readonly IOpeningStockRepository _openingStockRepo;
        private readonly IItemMasterRepository _itemMasterRepo;
        private readonly IStockService _currentstockService;
        public OpeningStockController(IOpeningStockRepository openingStockRepo, IItemMasterRepository itemMasterRepo, IStockService currentstockservice)
        {
            _openingStockRepo = openingStockRepo;
            _itemMasterRepo = itemMasterRepo;
            _currentstockService = currentstockservice;
        }
        //public async Task<IActionResult> Index()
        //{
        //    var items = await _itemMasterRepo.GetAll();
        //    var openingStocks = await _openingStockRepo.GetAll();

        //    var model = items.Select(item =>
        //    {
        //        var stock = openingStocks
        //            .FirstOrDefault(x => x.ItemId == item.Id);

        //        return new OpeningStockVM
        //        {
        //            Id= stock?.Id ?? 0,
        //            ItemId = item.Id,
        //            ItemCode = item.Code,
        //            ItemName = item.Name,
        //            Packing = item.Packing,
                     
        //            Qty = stock?.Qty ?? 0,
        //            Batch = stock?.Batch,
        //            Expirydate = stock?.Expirydate,
        //            Mrp = stock?.Mrp ?? 0,
        //            RateA = stock?.RateA ?? 0,
        //            RateB = stock?.RateB ?? 0,
        //            IsSelected = stock != null
        //        };
        //    }).ToList();

        //    return View(model);
        //}
        public async Task<IActionResult> Index()
        {
            var items = await _itemMasterRepo.GetAll();
            var stocks = await _openingStockRepo.GetAll();

            var model = items.Select(i => new OpeningStockVM
            {
                ItemId = i.Id,
                ItemName = i.Name,

                Batches = stocks
                    .Where(x => x.ItemId == i.Id)
                    .Select(x => new OpeningStockBatchVM
                    {
                        Batch = x.Batch,
                        Expirydate = x.Expirydate,
                        Qty = x.Qty,
                        Mrp = x.Mrp,
                        RateA = x.RateA,
                        RateB = x.RateB
                    }).ToList()
            }).ToList();

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Save(List<OpeningStockVM> model)
        {
            if (model == null || !model.Any())
                return RedirectToAction("Index");

            var dbStocks = await _openingStockRepo.GetAll();

            foreach (var item in model)
            {
                var existingBatches = dbStocks
                    .Where(x => x.ItemId == item.ItemId)
                    .ToList();

                foreach (var batch in item.Batches ?? new List<OpeningStockBatchVM>())
                {
                    if (batch.Qty <= 0) continue;

                    var existing = existingBatches
                        .FirstOrDefault(x => x.Batch == batch.Batch);

                    if (existing != null)
                    {
                        // 🔥 CURRENT STOCK FETCH
                        var oldStock = await _currentstockService.GetStock(
                            (int)existing.ItemId,
                            existing.Batch,
                            existing.Expirydate,
                            existing.Mrp
                        );

                        decimal oldQty = existing.Qty;
                        decimal newQty = batch.Qty;

                        // ✅ SAME STOCK ENTRY (no movement happened)
                        if (oldStock != null && oldStock.Qty == oldQty)
                        {
                            await _currentstockService.OverwriteStock(
                                oldStock.Id,
                                batch.Batch ?? string.Empty,
                                batch.Expirydate,
                                batch.Mrp,
                                newQty,
                                batch.RateA
                            );
                        }
                        else
                        {
                            // 🔁 SHIFT LOGIC

                            // 🔻 remove old qty
                            await _currentstockService.UpdateStock(
                                (int)existing.ItemId,
                                existing.Batch ?? "",
                                -oldQty,
                                existing.Expirydate,
                                existing.Mrp,
                                existing.RateA
                            );

                            // 🔺 add new qty
                            await _currentstockService.UpdateStock(
                                (int)item.ItemId,
                                batch.Batch ?? "",
                                newQty,
                                batch.Expirydate,
                                batch.Mrp,
                                batch.RateA
                            );
                        }

                        // ✅ UPDATE OPENING STOCK
                        existing.Expirydate = batch.Expirydate;
                        existing.Qty = batch.Qty;
                        existing.Mrp = batch.Mrp;
                        existing.RateA = batch.RateA;
                        existing.RateB = batch.RateB;
                        existing.Date = DateTime.Now;

                        await _openingStockRepo.Update(existing);
                    }
                    else
                    {
                        // ✅ NEW STOCK ADD
                        await _currentstockService.UpdateStock(
                            (int)item.ItemId,
                            batch.Batch ?? "",
                            batch.Qty,
                            batch.Expirydate,
                            batch.Mrp,
                            batch.RateA
                        );

                        // ✅ INSERT OPENING STOCK
                        await _openingStockRepo.Create(new OpeningStock
                        {
                            ItemId = item.ItemId,
                            Batch = batch.Batch,
                            Expirydate = batch.Expirydate,
                            Qty = batch.Qty,
                            Mrp = batch.Mrp,
                            RateA = batch.RateA,
                            RateB = batch.RateB,
                            Date = DateTime.Now
                        });
                    }
                }

                // 🔥 DELETE removed batches
                var incomingBatchNames = item.Batches?
                    .Select(x => x.Batch)
                    .ToList() ?? new List<string>();

                foreach (var old in existingBatches)
                {
                    if (!incomingBatchNames.Contains(old.Batch))
                    {
                        // 🔻 REMOVE STOCK
                        await _currentstockService.UpdateStock(
                            (int)old.ItemId,
                            old.Batch?.Trim() ?? "",
                            -old.Qty,
                            old.Expirydate,
                            old.Mrp,
                            old.RateA
                        );

                        await _openingStockRepo.Delete(old);
                    }
                }
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ExportOpeningStock()
        {
            // 🔥 Join with Item table to get ItemName
            var data = (from os in (await _openingStockRepo.GetAll())
                        join item in (await _itemMasterRepo.GetAll())
                        on os.ItemId equals item.Id
                        select new
                        {
                            ItemName = item.Name,
                            Batch = os.Batch,
                            Expiry = os.Expirydate,
                            Qty = os.Qty,
                            Mrp = os.Mrp,
                            RateA = os.RateA,
                            RateB = os.RateB
                        }).ToList();

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("OpeningStock");

                // ✅ Header
                ws.Cell(1, 1).Value = "ItemName";
                ws.Cell(1, 2).Value = "Batch";
                ws.Cell(1, 3).Value = "ExpiryDate";
                ws.Cell(1, 4).Value = "Qty";
                ws.Cell(1, 5).Value = "MRP";
                ws.Cell(1, 6).Value = "RateA";
                ws.Cell(1, 7).Value = "RateB";

                int row = 2;

                foreach (var item in data)
                {
                    ws.Cell(row, 1).Value = item.ItemName;
                    ws.Cell(row, 2).Value = item.Batch;

                    // ✅ Expiry Fix
                    if (item.Expiry.HasValue)
                        ws.Cell(row, 3).Value = item.Expiry.Value.ToString("yyyy-MM-dd");
                    else
                        ws.Cell(row, 3).Value = "";

                    ws.Cell(row, 4).Value = item.Qty;
                    ws.Cell(row, 5).Value = item.Mrp;
                    ws.Cell(row, 6).Value = item.RateA;
                    ws.Cell(row, 7).Value = item.RateB;

                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "OpeningStock.xlsx");
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> ImportOpeningStock(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "No file selected";
                return RedirectToAction("Index");
            }

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);
            var rows = sheet.RowsUsed().Skip(1);

            var allItems = (await _itemMasterRepo.GetAll()).ToList(); // 🔥 load once

            foreach (var row in rows)
            {
                string itemName = row.Cell(1).GetValue<string>()?.Trim();
                string batch = row.Cell(2).GetValue<string>();

                DateTime? expiry = null;

                var cell = row.Cell(3);
                var raw = cell.GetString()?.Trim();

                // 🔥 Try direct DateTime (Excel date case)
                if (cell.TryGetValue<DateTime>(out var dt))
                {
                    expiry = dt;
                }
                else if (!string.IsNullOrEmpty(raw))
                {
                    // 🔥 Try parsing multiple formats
                    string[] formats = {
                                           "yyyy-MM-dd",
                                           "dd-MM-yyyy",
                                           "MM/dd/yyyy",
                                           "dd/MM/yyyy"
                                       };

                    if (DateTime.TryParseExact(raw, formats,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var parsedDate))
                    {
                        expiry = parsedDate;
                    }
                }

                decimal qty = row.Cell(4).GetValue<decimal>();
                decimal mrp = row.Cell(5).GetValue<decimal>();
                decimal rateA = row.Cell(6).GetValue<decimal>();
                decimal rateB = row.Cell(7).GetValue<decimal>();

                if (qty <= 0 || string.IsNullOrEmpty(itemName)) continue;

                // 🔍 Find ItemId from name
                var item = allItems.FirstOrDefault(x => x.Name.ToLower() == itemName.ToLower());

                if (item == null)
                {
                    // ❌ skip if item not found
                    continue;
                }

                int itemId = item.Id;

                var existing = (await _openingStockRepo.GetAll())
                    .FirstOrDefault(x => x.ItemId == itemId && x.Batch == batch);

                if (existing != null)
                {
                    existing.Qty = qty;
                    existing.Mrp = mrp;
                    existing.RateA = rateA;
                    existing.RateB = rateB;
                    existing.Expirydate = expiry;

                    await _openingStockRepo.Update(existing);
                }
                else
                {
                    await _openingStockRepo.Create(new OpeningStock
                    {
                        ItemId = itemId,
                        Batch = batch,
                        Expirydate = expiry,
                        Qty = qty,
                        Mrp = mrp,
                        RateA = rateA,
                        RateB = rateB,
                        Date = DateTime.Now
                    });
                }

                await _currentstockService.UpdateStock(
                    itemId,
                    batch ?? "",
                    qty,
                    expiry,
                    mrp,
                    rateA
                );
            }

            TempData["Success"] = "Import Successful";
            return RedirectToAction("Index");
        }
    }


}
