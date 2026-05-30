using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseReturnApiController : ControllerBase
    {
        private readonly IPurchaseReturnRepository _purchasereturnservice;
        private readonly ISupplierRepository _supplierservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IHSNRepository _hsnservice;
        private readonly IStockService _currentStockService;
        private readonly IPurchaseItemRepository _purchaseitemservice;
        private readonly IPurchaseRepository _purchaseservice;

        public PurchaseReturnApiController(
            IPurchaseReturnRepository purchasereturnservice,
            ISupplierRepository supplierservice,
            IItemMasterRepository itemmasterservice,
            IHSNRepository hsnservice,
            IStockService currentStockService,
            IPurchaseItemRepository purchaseitemservice,
            IPurchaseRepository purchaseservice)
        {
            _purchasereturnservice = purchasereturnservice;
            _supplierservice = supplierservice;
            _itemmasterservice = itemmasterservice;
            _hsnservice = hsnservice;
            _currentStockService = currentStockService;
            _purchaseitemservice = purchaseitemservice;
            _purchaseservice = purchaseservice;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _purchasereturnservice.GetAll();
            return Ok(data);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var suppliers = await _supplierservice.GetALL();
            var purchaseItems = await _purchaseitemservice.GetAll();
            var hsn = await _hsnservice.GetAll();

            var model = new PurchaseReturnVM()
            {
                BillDate = DateTime.Now,
                BillNo = await GenerateNxtNumber()
            };

            return Ok(new
            {
                suppliers,
                items = purchaseItems.Where(x => x.ItemMasters != null)
                        .GroupBy(x => x.ItemId)
                        .Select(g => new { ItemId = g.Key, Name = g.First().ItemMasters!.Name }),
                hsn,
                reasons = Enum.GetValues(typeof(Purchasereturnreason))
                              .Cast<Purchasereturnreason>()
                              .Select(e => new { Value = (int)e, Text = e.ToString() })
                              .ToList(),
                model
            });
        }

        [HttpGet("GenerateNxtNumber")]
        public async Task<string> GenerateNxtNumber()
        {
            var data = await _purchasereturnservice.GetAll();
            var lastCode = data.Where(p => !string.IsNullOrEmpty(p.BillNo))
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
            int.TryParse(numberPart, out int number);

            return prefix + (number + 1).ToString("D" + numberPart.Length);
        }

        [HttpGet("GetItemsBySupplierId/{supplierId}")]
        public async Task<IActionResult> GetItemsBySupplierId(int supplierId)
        {
            var purchases = (await _purchaseservice.GetAll()).Where(x => x.SupplierId == supplierId);
            var purchaseitem = purchases.SelectMany(x => x.PurchaseItems);

            if (purchaseitem == null || !purchaseitem.Any())
                return Ok(new { success = false, items = new List<object>() });

            var itemMasterIds = purchaseitem.Where(x => x.ItemMasters != null).Select(x => x.ItemId).Distinct();
            var itemMasters = (await _itemmasterservice.GetAll()).Where(x => itemMasterIds.Contains(x.Id));

            var result = itemMasters.Select(i => new { value = i.Id, text = i.Name });
            return Ok(new { success = true, items = result });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] PurchaseReturnVM VM)
        {
            if (VM == null) return BadRequest(new { success = false });

            var model = new PurchaseReturn
            {
                SupplierId = VM.SupplierId,
                BillNo = VM.BillNo,
                BillDate = VM.BillDate,
                PartyBillDate = VM.PartyBillDate,
                PartyBillNo = VM.PartyBillNo,
                TotalGstAmt = VM.TotalGstAmt,
                Totaldiscount = VM.Totaldiscount,
                TotalPayable = VM.TotalPayable,
                discountPercent = VM.discountPercent,
                discountAmount = VM.discountAmount,
                Total = VM.Total,
                PurchaseReturnItems = VM.PurchaseReturnItemVMs?.Select(x => new PurchaseReturnItem
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
                    item.Batch?.Trim() ?? string.Empty,
                    -qty,
                    item.ExpiryDate,
                    item.Mrp,
                    item.Rate);
            }

            await _purchasereturnservice.Create(model);
            return Ok(new { success = true, data = model });
        }

        [HttpGet("GetReturnedQtyData/{supplierId}")]
        public async Task<IActionResult> GetReturnedQtyData(int supplierId)
        {
            var returnedQtyData = (await _purchasereturnservice.GetBySupplierId(supplierId))
                .SelectMany(x => x.PurchaseReturnItems)
                .GroupBy(r => new { r.ItemId, r.Batch, r.ExpiryDate, r.Mrp, r.Rate })
                .Select(g => new
                {
                    g.Key.ItemId,
                    g.Key.Batch,
                    Expiry = g.Key.ExpiryDate?.ToString("yyyy-MM-dd"),
                    g.Key.Mrp,
                    g.Key.Rate,
                    ReturnedQty = g.Sum(x => x.Qty)
                })
                .ToList();

            return Ok(returnedQtyData);
        }

        [HttpGet("GetItemDetails/{id}")]
        public async Task<IActionResult> GetItemDetails(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null) return NotFound();

            return Ok(new
            {
                gst = item.Hsn?.SGST,
                qty = item.MaximumQty,
                rate = item.Mrp,
                hsn = item.HsnId
            });
        }

        [HttpGet("GetBatchesByItemId")]
        public async Task<IActionResult> GetBatchesByItemId(int id, int supplierId)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null) return Ok(new { error = "Hsn Details not found." });

            var currentStocks = await _currentStockService.GetAll();
            var purchase = (await _purchaseservice.GetAll()).Where(x => x.SupplierId == supplierId)
                                                           .SelectMany(x => x.PurchaseItems);
            if (!purchase.Any()) return Ok(new { error = "No purchase found for this supplier." });

            var purchaseItems = purchase.Where(s => s.ItemId == id).ToList();
            if (!purchaseItems.Any()) return Ok(new { error = "No purchase items found." });

            var groupedResult = purchaseItems
                .GroupBy(b => new { b.Batch, b.ExpiryDate, b.Mrp, b.Rate })
                .Select(g => new
                {
                    ItemId = g.Select(x => x.ItemId).FirstOrDefault(),
                    g.Key.Batch,
                    g.Key.ExpiryDate,
                    g.Key.Rate,
                    g.Key.Mrp,
                    qty = Math.Max(
                        currentStocks.FirstOrDefault(s =>
                            s.ItemId == id &&
                            string.Equals(s.Batch ?? string.Empty, g.Key.Batch ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                            s.Mrp == g.Key.Mrp &&
                            s.ExpiryDate?.Date == g.Key.ExpiryDate?.Date
                        )?.Qty ?? 0,
                        0),
                    purchaseItemId = g.Select(x => x.Id).FirstOrDefault()
                })
                .Where(x => x.qty > 0)
                .ToList();

            return Ok(new { gst = item.Hsn?.IGST ?? 0, hsn = item.HsnId, groupedResult });
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _purchasereturnservice.GetById(id);
            if (model == null) return NotFound();

            var vm = new PurchaseReturnVM
            {
                Id = model.Id,
                SupplierId = model.SupplierId,
                BillNo = model.BillNo,
                BillDate = model.BillDate,
                PartyBillNo = model.PartyBillNo,
                PartyBillDate = model.PartyBillDate,
                TotalGstAmt = model.TotalGstAmt,
                discountPercent = model.discountPercent,
                discountAmount = model.discountAmount,
                Totaldiscount = model.Totaldiscount,
                TotalPayable = model.TotalPayable,
                Total = model.Total,
                PurchaseReturnItemVMs = model.PurchaseReturnItems?.Select(x => new PurchaseReturnItemVM
                {
                    Id = x.Id,
                    ItemId = x.ItemId,
                    ItemName = x.ItemMasters?.Name,
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
                    salserateB = x.salserateB
                    //Reason = x.Reason
                }).ToList() ?? new List<PurchaseReturnItemVM>()
            };

            return Ok(vm);
        }

        [HttpPost("Edit")]
        public async Task<IActionResult> Edit([FromBody] PurchaseReturnVM VM)
        {
            var model = await _purchasereturnservice.GetById(VM.Id);
            if (model == null) return NotFound();

            var incomingItems = VM.PurchaseReturnItemVMs ?? new List<PurchaseReturnItemVM>();

            model.SupplierId = VM.SupplierId;
            model.BillNo = VM.BillNo;
            model.BillDate = VM.BillDate;
            model.PartyBillNo = VM.PartyBillNo;
            model.PartyBillDate = VM.PartyBillDate;
            model.TotalGstAmt = VM.TotalGstAmt;
            model.discountPercent = VM.discountPercent;
            model.discountAmount = VM.discountAmount;
            model.Totaldiscount = VM.Totaldiscount;
            model.TotalPayable = VM.TotalPayable;
            model.Total = VM.Total;

            // sync items
            var removedItems = model.PurchaseReturnItems
                .Where(dbItem => !incomingItems.Any(vmItem => vmItem.Id == dbItem.Id))
                .ToList();
            foreach (var item in removedItems)
            {
                decimal qty = item.Qty + item.FreeQty;

                await _currentStockService.UpdateStock(
                    item.ItemId,
                    item.Batch?.Trim() ?? string.Empty,
                    qty,
                    item.ExpiryDate,
                    item.Mrp,
                    item.Rate);

                model.PurchaseReturnItems.Remove(item);
            }

            foreach (var item in incomingItems)
            {
                if (item.Id > 0)
                {
                    var existing = model.PurchaseReturnItems.FirstOrDefault(x => x.Id == item.Id);
                    if (existing != null)
                    {
                        decimal oldQty = existing.Qty + existing.FreeQty;
                        decimal newQty = item.Qty + item.FreeQty;

                        await _currentStockService.UpdateStock(
                            existing.ItemId,
                            existing.Batch?.Trim() ?? string.Empty,
                            oldQty,
                            existing.ExpiryDate,
                            existing.Mrp,
                            existing.Rate);

                        await _currentStockService.UpdateStock(
                            item.ItemId,
                            item.Batch?.Trim() ?? string.Empty,
                            -newQty,
                            item.ExpiryDate,
                            item.Mrp,
                            item.Rate);

                        existing.ItemId = item.ItemId;
                        existing.Batch = item.Batch;
                        existing.ExpiryDate = item.ExpiryDate;
                        existing.Mrp = item.Mrp;
                        existing.Qty = item.Qty;
                        existing.FreeQty = item.FreeQty;
                        existing.Unit = item.Unit;
                        existing.Rate = item.Rate;
                        existing.HsnId = item.HsnId;
                        existing.Gst = item.Gst;
                        existing.GstAmount = item.GstAmount;
                        existing.Discount = item.Discount;
                        existing.DiscountAmt = item.DiscountAmt;
                        existing.Amount = item.Amount;
                        existing.TotalAmt = item.TotalAmt;
                        existing.BatchWiseCose = item.BatchWiseCose;
                        existing.salserateA = item.salserateA;
                        existing.salserateB = item.salserateB;
                        //existing.Reason = item.Reason;
                    }
                }
                else
                {
                    decimal qty = item.Qty + item.FreeQty;

                    await _currentStockService.UpdateStock(
                        item.ItemId,
                        item.Batch?.Trim() ?? string.Empty,
                        -qty,
                        item.ExpiryDate,
                        item.Mrp,
                        item.Rate);

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
            return Ok(new { success = true, data = model });
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "Invalid Id" });

            var model = await _purchasereturnservice.GetById(id);
            if (model == null) return NotFound(new { success = false, message = "Not found" });

            foreach (var item in model.PurchaseReturnItems)
            {
                decimal qty = item.Qty + item.FreeQty;

                await _currentStockService.UpdateStock(
                    item.ItemId,
                    item.Batch?.Trim() ?? string.Empty,
                    qty,
                    item.ExpiryDate,
                    item.Mrp,
                    item.Rate);
            }

            await _purchasereturnservice.Delete(model);
            return Ok(new { success = true, message = "Deleted successfully" });
        }

        [HttpGet("PurchaseReturnReportItemWise")]
        public async Task<IActionResult> PurchaseReturnReportItemWise()
        {
            var itemMasters = await _itemmasterservice.GetAll();
            var purchasereturn = await _purchasereturnservice.GetAll();

            var purchasereturnItems = purchasereturn
                .Where(s => s.PurchaseReturnItems != null)
                .SelectMany(s => s.PurchaseReturnItems);

            var groupedpurchase = purchasereturnItems
                .GroupBy(x => x.ItemId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.TotalAmt),
                    TotalGst = g.Sum(x =>
                    {
                        decimal rate = x.Rate;
                        decimal qty = x.Qty;
                        decimal discountPercent = x.Discount;
                        decimal gstPercent = x.Gst;
                        decimal gross = rate * qty;
                        decimal discountAmt = gross * (discountPercent / 100);
                        decimal taxable = gross - discountAmt;
                        return taxable * (gstPercent / 100);
                    })
                }).ToList();

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

            return Ok(purchaseList);
        }

        [HttpGet("PurchaseReturnReportSupplierWise")]
        public async Task<IActionResult> PurchaseReturnReportSupplierWise(int? SupplierId)
        {
            var supplier = await _supplierservice.GetALL();
            var purchase = await _purchasereturnservice.GetAll();

            if (SupplierId.HasValue && SupplierId.Value > 0)
                purchase = purchase.Where(p => p.SupplierId == SupplierId.Value).ToList();

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

            return Ok(new { suppliers = supplier, billwiseList });
        }
    }
}