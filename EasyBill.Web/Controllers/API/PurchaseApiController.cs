using AOne.DataAccess.ProfileService;
using DocumentFormat.OpenXml.Spreadsheet;
using EasyBill.DataAccess.Repository;
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
    public class PurchaseApiController : ControllerBase
    {
        private readonly IPurchaseRepository _purchaseservice;
        private readonly ISupplierRepository _supplierservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IHSNRepository _hsnservice;
        private readonly IStockRepository _stockservice;
        private readonly IPurchaseReturnRepository _purchasereturnservice;
        private readonly ISalesRepository _salesService;
        private readonly IStockIssueRepository _stockissueservice;
        private readonly IStockReturnRepository _stockrturnservice;
        private readonly IStockReceiveRepository _stockreceiveservice;
        private readonly ICompanyRepository _companyServices;
        private readonly IProfileService _profileService;

        public PurchaseApiController(
            IPurchaseRepository purchaseservice,
            ISupplierRepository supplierservice,
            IItemMasterRepository itemmasterservice,
            IHSNRepository hsnservice,
            IStockRepository stockservice,
            IPurchaseReturnRepository purchasereturnservice,
            ISalesRepository salesService,
            IStockIssueRepository stockissueservice,
            IStockReturnRepository stockrturnservice,
            IStockReceiveRepository stockReceiveservice,
            ICompanyRepository companyServices,
            IProfileService profileService)
        {
            _purchaseservice = purchaseservice;
            _supplierservice = supplierservice;
            _itemmasterservice = itemmasterservice;
            _hsnservice = hsnservice;
            _stockservice = stockservice;
            _purchasereturnservice = purchasereturnservice;
            _salesService = salesService;
            _stockissueservice = stockissueservice;
            _stockrturnservice = stockrturnservice;
            _stockreceiveservice = stockReceiveservice;
            _companyServices = companyServices;
            _profileService = profileService;
        }
         
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            await _profileService.Set(User);

            var data = await _purchaseservice.GetAll();

            var purchasedata = data.Select(x => new
            {
                Id = x.Id,
                SupplierId = x.SupplierId,
                SupplierName = x.Suppliers?.FirstName,
                BillNo = x.BillNo,
                BillDate = x.BillDate,
                PartyBillNo = x.PartyBillNo,
                PartyBillDate = x.PartyBillDate,
                TotalGstAmt = x.TotalGstAmt,
                Totaldiscount = x.Totaldiscount,
                TotalPayable = x.TotalPayable,
                discountPercent = x.discountPercent,
                discountAmount = x.discountAmount,
                Total = x.Total,
                PaidAmount = x.PaidAmount,
                Balance = x.Balance,
                PaymentStatus = x.PaymentStatus,
                SalesItems = x.PurchaseItems?.Select(item => new
                {
                    Id = item.Id,
                    PurchaseId = item.PurchaseId,
                    Batch = item.Batch,
                    ItemId = item.ItemId,
                    ItemName = item.ItemMasters?.Name,
                    Qty = item.Qty,
                    FreeQty = item.FreeQty,
                    Unit = item.Unit,
                    Rate = item.Rate,
                    HsnId = item.HsnId,
                    GST = item.Gst,
                    GstAmount = item.GstAmount,
                    Discount = item.Discount,
                    DiscountAmt = item.DiscountAmt,
                    ExpiryDate = item.ExpiryDate,
                    Amount = item.Amount,
                    TotalAmt = item.TotalAmt,
                    BatchWiseCose = item.BatchWiseCose,
                    Mrp = item.Mrp,
                    salserateA = item.salserateA,
                    salserateB = item.salserateB 
                }).ToList(),
            }).ToList();

            return Ok(new
            {
                success = true,
                data = purchasedata
            });
        } 

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _purchaseservice.GetById(id);
            if (model == null) return NotFound();
            var vm = new PurchaseVM
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
                PurchaseItemVms = model.PurchaseItems?.Select(x => new PurchaseItemVM
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
                }).ToList() ?? new List<PurchaseItemVM>()
            };

            return Ok(model);
        }

        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] PurchaseVM VM)
        //{
        //    if (VM == null) return BadRequest();

        //    var model = new Purchase
        //    {
        //        SupplierId = VM.SupplierId,
        //        BillNo = VM.BillNo,
        //        BillDate = VM.BillDate,
        //        PartyBillDate = VM.PartyBillDate,
        //        PartyBillNo = VM.PartyBillNo,
        //        TotalGstAmt = VM.TotalGstAmt,
        //        Totaldiscount = VM.Totaldiscount,
        //        TotalPayable = VM.TotalPayable,
        //        discountPercent = VM.discountPercent,
        //        discountAmount = VM.discountAmount,
        //        Total = VM.Total,
        //        PaymentAmt = 0,
        //        PaidAmount = 0,
        //        Balance = VM.Balance,
        //        PaymentStatus = "Unpaid",
        //        PurchaseItems = VM.PurchaseItemVms.Select(x => new PurchaseItem
        //        {
        //            ItemId = x.ItemId,
        //            Batch = x.Batch,
        //            ExpiryDate = x.ExpiryDate,
        //            Mrp = x.Mrp,
        //            Qty = x.Qty,
        //            FreeQty = x.FreeQty,
        //            Unit = x.Unit,
        //            Rate = x.Rate,
        //            HsnId = x.HsnId,
        //            Gst = x.Gst,
        //            GstAmount = x.GstAmount,
        //            Discount = x.Discount,
        //            DiscountAmt = x.DiscountAmt,
        //            Amount = x.Amount,
        //            TotalAmt = x.TotalAmt,
        //            BatchWiseCose = x.BatchWiseCose,
        //            salserateA = x.salserateA,
        //            salserateB = x.salserateB
        //        }).ToList()
        //    };
        //    await _purchaseservice.Update(model);
        //    return Ok(new { success = true, data = model });
        //}
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PurchaseVM VM)
        {
            if (VM == null) return BadRequest();

            var model = new Purchase
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
                PaidAmount = VM.PaidAmount,
                Balance = VM.TotalPayable - VM.PaidAmount,
                PaymentStatus = VM.PaidAmount == 0 ? "Unpaid" :
                VM.PaidAmount < VM.TotalPayable ? "Partial" : "Paid",
                PurchaseItems = VM.PurchaseItemVms?.Select(x => new PurchaseItem
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
                }).ToList() ?? new List<PurchaseItem>()
            };

            await _purchaseservice.Create(model);
            return Ok(new { success = true, data = model });
        }
        //[HttpPut("{id}")]
        //public async Task<IActionResult> Edit(int id, [FromBody] PurchaseVM VM)
        //{
        //    if (VM == null || id != VM.Id) return BadRequest();

        //    var model = await _purchaseservice.GetById(id);
        //    if (model == null) return NotFound();

        //    model.SupplierId = VM.SupplierId;
        //    model.BillNo = VM.BillNo;
        //    model.BillDate = VM.BillDate;
        //    model.PartyBillNo = VM.PartyBillNo;
        //    model.PartyBillDate = VM.PartyBillDate;
        //    model.TotalGstAmt = VM.TotalGstAmt;
        //    model.discountPercent = VM.discountPercent;
        //    model.discountAmount = VM.discountAmount;
        //    model.Totaldiscount = VM.Totaldiscount;
        //    model.TotalPayable = VM.TotalPayable;
        //    model.Total = VM.Total;

        //    // Remove deleted items
        //    var removedItems = model.PurchaseItems.Where(dbItem => !VM.PurchaseItemVms.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();
        //    removedItems.ForEach(x => model.PurchaseItems.Remove(x));

        //    // Update existing or add new items
        //    foreach (var item in VM.PurchaseItemVms)
        //    {
        //        if (item.Id > 0)
        //        {
        //            var existingItem = model.PurchaseItems.FirstOrDefault(x => x.Id == item.Id);
        //            if (existingItem != null)
        //            {
        //                existingItem.ItemId = item.ItemId;
        //                existingItem.Batch = item.Batch;
        //                existingItem.ExpiryDate = item.ExpiryDate;
        //                existingItem.Mrp = item.Mrp;
        //                existingItem.Qty = item.Qty;
        //                existingItem.FreeQty = item.FreeQty;
        //                existingItem.Unit = item.Unit;
        //                existingItem.Rate = item.Rate;
        //                existingItem.HsnId = item.HsnId;
        //                existingItem.Gst = item.Gst;
        //                existingItem.GstAmount = item.GstAmount;
        //                existingItem.Discount = item.Discount;
        //                existingItem.DiscountAmt = item.DiscountAmt;
        //                existingItem.Amount = item.Amount;
        //                existingItem.TotalAmt = item.TotalAmt;
        //                existingItem.BatchWiseCose = item.BatchWiseCose;
        //                existingItem.salserateA = item.salserateA;
        //                existingItem.salserateB = item.salserateB;
        //            }
        //        }
        //        else
        //        {
        //            model.PurchaseItems.Add(new PurchaseItem
        //            {
        //                ItemId = item.ItemId,
        //                Batch = item.Batch,
        //                ExpiryDate = item.ExpiryDate,
        //                Mrp = item.Mrp,
        //                Qty = item.Qty,
        //                FreeQty = item.FreeQty,
        //                Unit = item.Unit,
        //                Rate = item.Rate,
        //                HsnId = item.HsnId,
        //                Gst = item.Gst,
        //                GstAmount = item.GstAmount,
        //                Discount = item.Discount,
        //                DiscountAmt = item.DiscountAmt,
        //                Amount = item.Amount,
        //                TotalAmt = item.TotalAmt,
        //                BatchWiseCose = item.BatchWiseCose,
        //                salserateA = item.salserateA,
        //                salserateB = item.salserateB
        //            });
        //        }
        //    }
        //    await _purchaseservice.Update(model);
        //    return Ok(new { success = true, data = model });
        //}
        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] PurchaseVM VM)
        {
            if (VM == null || id != VM.Id) return BadRequest();

            var model = await _purchaseservice.GetById(id);
            if (model == null) return NotFound();

            // Update main fields
            model.SupplierId = VM.SupplierId;
            model.BillNo = VM.BillNo;
            model.BillDate = VM.BillDate;
            model.PartyBillNo = VM.PartyBillNo;
            model.PartyBillDate = VM.PartyBillDate;
            model.TotalGstAmt = VM.TotalGstAmt;
            model.Totaldiscount = VM.Totaldiscount;
            model.TotalPayable = VM.TotalPayable;
            model.discountPercent = VM.discountPercent;
            model.discountAmount = VM.discountAmount;
            model.Total = VM.Total;

            // Remove deleted items
            var removedItems = model.PurchaseItems
                .Where(dbItem => !VM.PurchaseItemVms.Any(vmItem => vmItem.Id == dbItem.Id))
                .ToList();
            removedItems.ForEach(x => model.PurchaseItems.Remove(x));

            // Update existing or add new items
            foreach (var item in VM.PurchaseItemVms)
            {
                if (item.Id > 0)
                {
                    var existingItem = model.PurchaseItems.FirstOrDefault(x => x.Id == item.Id);
                    if (existingItem != null)
                    {
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
                        existingItem.GstAmount = item.GstAmount;
                        existingItem.Discount = item.Discount;
                        existingItem.DiscountAmt = item.DiscountAmt;
                        existingItem.Amount = item.Amount;
                        existingItem.TotalAmt = item.TotalAmt;
                        existingItem.BatchWiseCose = item.BatchWiseCose;
                        existingItem.salserateA = item.salserateA;
                        existingItem.salserateB = item.salserateB;
                    }
                }
                else
                {
                    model.PurchaseItems.Add(new PurchaseItem
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
                    });
                }
            }

            // Update Payment Status
            model.Balance = model.TotalPayable - model.PaidAmount;

            model.PaymentStatus = model.PaidAmount == 0 ? "Unpaid" :
                                  model.PaidAmount < model.TotalPayable ? "Partial" : "Paid";
            await _purchaseservice.Update(model);
            return Ok(new { success = true, data = model });
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest();
            var model = await _purchaseservice.GetById(id);
            if (model == null) return NotFound();
            await _purchaseservice.Delete(model);
            return Ok(new { success = true });
        }


        [HttpGet("GetItemDetails/{id}")]
        public async Task<IActionResult> GetItemDetails(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null) return NotFound();
            return Ok(new { gst = item.Hsn?.SGST, qty = item.MaximumQty, rate = item.Mrp, hsn = item.HsnId });
        }

        [HttpGet("GetBatchesByItemId/{id}")]
        public async Task<IActionResult> GetBatchesByItemId(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null) return NotFound();

            var purchaseData = await _purchaseservice.GetAll();
            var purchaseItems = purchaseData.SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                                            .Where(x => x.ItemId == id)
                                            .ToList();

            var grouped = purchaseItems.GroupBy(p => new { p.Batch, p.ExpiryDate, p.Mrp, p.Rate })
                                       .Select(g => new
                                       {
                                           batch = g.Key.Batch,
                                           expiry = g.Key.ExpiryDate?.ToString("yyyy-MM-dd"),
                                           rate = g.Key.Rate,
                                           mrp = g.Key.Mrp,
                                           qty = g.Sum(x => x.Qty),
                                           purchaseItemId = g.FirstOrDefault()?.Id ?? 0,
                                           SalserateA = g.FirstOrDefault()?.salserateA ?? 0,
                                           SalserateB = g.FirstOrDefault()?.salserateB ?? 0,
                                       }).ToList();

            return Ok(new { gst = item.Hsn?.IGST ?? 0, hsn = item.HsnId, groupedResult = grouped });
        }

        //[HttpGet("CurrentStock")]
        //public async Task<IActionResult> CurrentStock()
        //{
        //    var itemMasters = await _itemmasterservice.GetAll();
        //    var purchases = await _purchaseservice.GetAll();
        //    var purchaseItems = purchases.SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>());

        //    var stockList = itemMasters.Select(item => new StockVM
        //    {
        //        ItemMasterId = item.Id,
        //        ItemCode = item.Code,
        //        CategoryName = item.Category?.CategoryName ?? "",
        //        ItemName = item.Name,
        //        Unit1 = item.Unit1 ?? "",
        //        Unit2 = item.Unit2 ?? "",
        //        Stocks = purchaseItems.Where(x => x.ItemId == item.Id).Sum(x => x.Qty + x.FreeQty)
        //    }).ToList();

        //    return Ok(stockList);
        //}


        //Sakshi Chnage
        [HttpGet("CurrentStock")]
        public async Task<IActionResult> CurrentStock()
        {
            // 1. Saara data fetch karein
            var itemMasters = await _itemmasterservice.GetAll();
            var purchases = await _purchaseservice.GetAll();
            var purchasereturns = await _purchasereturnservice.GetAll();
            var sales = await _salesService.GetAll();
            var stockIssue = await _stockissueservice.GetAll();
            var stockReturn = await _stockrturnservice.GetAll();
            var stockreceive = await _stockreceiveservice.GetAll();

            var purchaseItems = purchases.Where(p => p.PurchaseItems != null).SelectMany(p => p.PurchaseItems).ToList();
            var purchaseReturnItems = purchasereturns.Where(r => r.PurchaseReturnItems != null).SelectMany(r => r.PurchaseReturnItems).ToList();
            var salesItems = sales.Where(s => s.SalesItems != null).SelectMany(s => s.SalesItems).ToList();
            var stockissueItems = stockIssue.Where(s => s.StockIssuesItems != null).SelectMany(s => s.StockIssuesItems).ToList();
            var stockreturnItems = stockReturn.Where(s => s.StockReturnItems != null).SelectMany(s => s.StockReturnItems).ToList();
            var stockreceiveItems = stockreceive.Where(s => s.StockReceiveItems != null).SelectMany(s => s.StockReceiveItems).ToList();

            var usedItemIds = purchaseItems.Select(x => x.ItemId)
                .Union(purchaseReturnItems.Select(x => x.ItemId))
                .Union(salesItems.Select(x => x.ItemMasterId))
                .Distinct()
                .ToHashSet();

            var filteredItemMasters = itemMasters.Where(i => usedItemIds.Contains(i.Id)).ToList();

            var stockList = filteredItemMasters.Select(item =>
            {
                var itemId = item.Id;

                // Stock Calculation Logic (Existing)
                var totalPurchasedQty = purchaseItems.Where(x => x.ItemId == itemId).Sum(x => x.Qty + x.FreeQty);
                var totalpurchaseReturnedQty = purchaseReturnItems.Where(x => x.ItemId == itemId).Sum(x => x.Qty + x.FreeQty);
                var totalsalseQty = salesItems.Where(x => x.ItemMasterId == itemId).Sum(x => x.Qty);
                var totalstockissueQty = stockissueItems.Where(x => x.ItemMasterId == itemId).Sum(x => x.Qty);
                var totalstockreturnQty = stockreturnItems.Where(x => x.ItemMasterId == itemId).Sum(x => x.Qty);
                var totalstockreceiveQty = stockreceiveItems.Where(x => x.ItemMasterId == itemId).Sum(x => x.Qty);

                var currentStock = totalPurchasedQty + totalstockreturnQty + totalstockreceiveQty - totalpurchaseReturnedQty - totalsalseQty - totalstockissueQty;

                // --- NEW LOGIC: Latest Purchase Details ---
                // Item ki sabse last wali purchase nikal rahe hain taaki Rate/MRP dikh sake
                var latestPurchase = purchaseItems
                    .Where(x => x.ItemId == itemId)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();

                return new StockVM
                {
                    ItemMasterId = itemId,
                    ItemCode = item.Code,
                    CategoryName = item.Category?.CategoryName ?? "Unknown",
                    ItemName = item.Name,
                    Unit1 = item.Unit1 ?? "",
                    Unit2 = item.Unit2 ?? "",
                    Stocks = currentStock,

                    // Assigning Values
                    Mrp = latestPurchase?.Mrp ?? 0,
                    Rate = latestPurchase?.Rate ?? 0,
                    GstAmount = latestPurchase?.Gst ?? 0, // Per unit GST percentage ya amount
                    Amount = (latestPurchase?.Rate ?? 0) * currentStock // Total stock value
                };
            }).ToList();

            return Ok(stockList);
        }

        //[HttpGet("CurrentStock")]
        //public async Task<IActionResult> CurrentStock()
        //{
        //    // 1. Get all required data
        //    var itemMasters = await _itemmasterservice.GetAll();
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
        //    // 2. Get all distinct item IDs used in purchase, return, or sales
        //    var usedItemIds = purchaseItems.Select(x => x.ItemId)
        //        .Union(purchaseReturnItems.Select(x => x.ItemId))
        //        .Union(salesItems.Select(x => x.ItemMasterId))
        //        .Union(stockissueItems.Select(x => x.ItemMasterId))
        //        .Union(stockreturnItems.Select(x => x.ItemMasterId))
        //        .Union(stockreceiveItems.Select(x => x.ItemMasterId))
        //        .Distinct()
        //        .ToHashSet();

        //    // 3. Filter itemMasters based on usedItemIds
        //    var filteredItemMasters = itemMasters.Where(i => usedItemIds.Contains(i.Id)).ToList();

        //    // 4. Build stock list
        //    var stockList = filteredItemMasters.Select(item =>
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
        //           .Where(x => x.ItemMasterId == itemId)
        //           .Sum(x => x.Qty);

        //        var totalstockreturnQty = stockreturnItems
        //          .Where(x => x.ItemMasterId == itemId)
        //          .Sum(x => x.Qty);

        //        var totalstockreceiveQty = stockreceiveItems
        //          .Where(x => x.ItemMasterId == itemId)
        //          .Sum(x => x.Qty);

        //        var currentStock = totalPurchasedQty + totalstockreturnQty + totalstockreceiveQty - totalpurchaseReturnedQty - totalsalseQty - totalstockissueQty;

        //        return new StockVM
        //        {
        //            ItemMasterId = itemId,
        //            ItemCode = item.Code,
        //            CategoryName = item.Category?.CategoryName ?? "Unknown",
        //            ItemName = item.Name,
        //            Unit1 = item.Unit1 ?? "",
        //            Unit2 = item.Unit2 ?? "",
        //            Stocks = currentStock
        //        };
        //    })
        //    // .Where(x => x.Stocks > 0) // Optional: show only items with stock > 0
        //    .ToList();

        //    return Ok(stockList);
        //}




        // Reports: ItemWise, BillWise, SupplierWise, CompanyWise, NearExpiry, Expired
        // ------------------ ItemWise ------------------
        [HttpGet("PurchaseReportItemWise")]
        public async Task<IActionResult> PurchaseReportItemWise()
        {
            var itemMasters = await _itemmasterservice.GetAll();
            var purchase = await _purchaseservice.GetAll();
            var purchaseItems = purchase.SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>());

            var groupedpurchase = purchaseItems
                .GroupBy(x => x.ItemId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.TotalAmt),
                    TotalGst = g.Sum(x => ((x.Rate * x.Qty - ((x.Rate * x.Qty) * x.Discount / 100)) * x.Gst / 100))
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

        // ------------------ BillWise ------------------
        [HttpGet("PurchaseReportBillWise")]
        public async Task<IActionResult> PurchaseReportBillWise()
        {
            var purchase = await _purchaseservice.GetAll();
            var billwiseList = purchase.Select(sale => new PurchaseVM
            {
                Id = sale.Id,
                BillNo = sale.BillNo,
                BillDate = sale.BillDate,
                SupplierName = sale.Suppliers?.FirstName ?? "Unknown",
                TotalGstAmt = sale.TotalGstAmt,
                TotalPayable = sale.PurchaseItems?.Sum(item => item.TotalAmt) ?? 0
            }).Where(x => x.TotalPayable > 0).OrderByDescending(x => x.BillDate).ToList();

            return Ok(billwiseList);
        }

        // ------------------ SupplierWise ------------------
        [HttpGet("PurchaseReportSupplierWise/{SupplierId?}")]
        public async Task<IActionResult> PurchaseReportSupplierWise(int? SupplierId)
        {
            var purchase = await _purchaseservice.GetAll();
            if (SupplierId.HasValue && SupplierId.Value > 0) purchase = purchase.Where(p => p.SupplierId == SupplierId.Value).ToList();

            var billwiseList = purchase.Select(sale => new PurchaseVM
            {
                Id = sale.Id,
                BillNo = sale.BillNo,
                BillDate = sale.BillDate,
                SupplierName = sale.Suppliers?.FirstName ?? "Unknown",
                TotalGstAmt = sale.TotalGstAmt,
                TotalPayable = sale.PurchaseItems?.Sum(item => item.TotalAmt) ?? 0
            }).OrderByDescending(x => x.BillDate).ToList();

            return Ok(billwiseList);
        }

        // ------------------ CompanyWise ------------------
        [HttpGet("PurchaseReportCompanyWise/{companyId?}")]
        public async Task<IActionResult> PurchaseReportCompanyWise(int? companyId)
        {
            var itemMasters = await _itemmasterservice.GetAll();
            var companies = await _companyServices.GetAll();
            var purchase = await _purchaseservice.GetAll();
            var salesItems = purchase.SelectMany(s => s.PurchaseItems ?? new List<PurchaseItem>());

            if (companyId.HasValue && companyId.Value > 0)
                salesItems = salesItems.Where(x => x.ItemMasters?.CompanyId == companyId.Value).ToList();

            var groupedSales = salesItems
                .GroupBy(x => x.ItemId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.TotalAmt),
                    TotalGst = g.Sum(x => ((x.Rate * x.Qty - ((x.Rate * x.Qty) * x.Discount / 100)) * x.Gst / 100))
                }).ToList();

            var stockList = (from item in itemMasters
                             join g in groupedSales on item.Id equals g.ItemMasterId
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

            return Ok(stockList);
        }

        // ------------------ NearExpiry ------------------
        [HttpGet("NearExpiryReport")]
        public async Task<IActionResult> NearExpiryReport(DateTime? expiryFrom, DateTime? expiryTo)
        {
            var today = DateTime.Today;
            var expiryFromDate = expiryFrom ?? today;
            var expiryToDate = expiryTo ?? today.AddDays(90);

            var itemMasters = (await _itemmasterservice.GetAll()).ToDictionary(x => x.Id);
            var purchases = await _purchaseservice.GetAll();
            var purchaseItems = purchases.SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>());

            var nearExpiry = purchaseItems
                .Where(x => x.ExpiryDate >= expiryFromDate && x.ExpiryDate <= expiryToDate)
                .Select(x => new NearExpiryVM
                {
                    ItemId = x.ItemId,
                    ItemCode = itemMasters[x.ItemId].Code,
                    ItemName = itemMasters[x.ItemId].Name,
                    CategoryName = itemMasters[x.ItemId].Category?.CategoryName ?? "",
                    BatchNo = x.Batch,
                    ExpiryDate = x.ExpiryDate ?? today,
                    DaysToExpire = ((x.ExpiryDate ?? today) - today).Days,
                    Stocks = x.Qty + x.FreeQty
                }).OrderBy(x => x.ExpiryDate).ToList();

            return Ok(nearExpiry);
        }

        // ------------------ Expired Items ------------------
        [HttpGet("ExpiredItemReport")]
        public async Task<IActionResult> ExpiredItemReport()
        {
            var today = DateTime.Today;
            var itemMasters = (await _itemmasterservice.GetAll()).ToDictionary(x => x.Id);
            var purchases = await _purchaseservice.GetAll();
            var purchaseItems = purchases.SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>());

            var expired = purchaseItems
                .Where(x => x.ExpiryDate <= today)
                .Select(x => new NearExpiryVM
                {
                    ItemId = x.ItemId,
                    ItemCode = itemMasters[x.ItemId].Code,
                    ItemName = itemMasters[x.ItemId].Name,
                    CategoryName = itemMasters[x.ItemId].Category?.CategoryName ?? "",
                    BatchNo = x.Batch,
                    ExpiryDate = x.ExpiryDate ?? today,
                    DaysToExpire = ((x.ExpiryDate ?? today) - today).Days,
                    Stocks = x.Qty + x.FreeQty
                }).OrderBy(x => x.ExpiryDate).ToList();

            return Ok(expired);
        }


        [HttpGet("GetUnpaidPurchases")]
        public async Task<IActionResult> GetUnpaidPurchases(int supplierId)
        {
            var purchaseList = (await _purchaseservice.GetAll())
                .Where(x => x.SupplierId == supplierId
                         && (x.TotalPayable - x.PaidAmount) > 0) // ✅ MAIN FIX
                .OrderBy(x => x.BillDate)
                .Select(x => new
                {
                    x.Id,
                    x.BillNo,
                    x.BillDate,
                    x.TotalPayable,
                    x.PaidAmount,
                    Balance = x.TotalPayable - x.PaidAmount
                })
                .ToList();

            return Ok(purchaseList);
        }
    }
}
