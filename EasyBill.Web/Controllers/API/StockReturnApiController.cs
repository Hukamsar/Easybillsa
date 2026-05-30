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
    public class StockReturnApiController : ControllerBase
    {
        private readonly IStockReturnRepository _stockReturnservice;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IPurchaseItemRepository _purchaseitemservice;
        private readonly ISalesRepository _salesservice;

        public StockReturnApiController(
            IStockReturnRepository stockReturnservice,
            ICustomerRepository customerservice,
            IItemMasterRepository itemmasterservice,
            IPurchaseItemRepository purchaseitemservice,
            ISalesRepository salesservice)
        {
            _stockReturnservice = stockReturnservice;
            _customerservice = customerservice;
            _itemmasterservice = itemmasterservice;
            _purchaseitemservice = purchaseitemservice;
            _salesservice = salesservice;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _stockReturnservice.GetAll();
            return Ok(data);
        }

        [HttpGet("generate-next-number")]
        public async Task<IActionResult> GenerateNextNumber()
        {
            var data = await _stockReturnservice.GetAll();
            var lastCode = data
                .Where(p => !string.IsNullOrEmpty(p.ChallanNo))
                .Select(p => p.ChallanNo)
                .LastOrDefault();

            string nextCode = GenerateNextProductCode(lastCode);
            return Ok(nextCode);
        }

        private string GenerateNextProductCode(string lastCode)
        {
            if (string.IsNullOrEmpty(lastCode) || lastCode.Length < 2)
                return "SR0001";

            string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray());
            string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

            int.TryParse(numberPart, out int number);
            return prefix + (number + 1).ToString("D" + numberPart.Length);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] StockReturnVM vm)
        {
            if (vm == null) return BadRequest("Invalid data.");

            var model = new StockReturn
            {
                CustomerId = vm.CustomerId,
                ChallanDate = vm.ChallanDate,
                ChallanNo = vm.ChallanNo,
                MobileNo = vm.MobileNo,
                Address = vm.Address,
                PharmacyDoctorId = vm.PharmacyDoctorId,
                DoctorMobileNumber = vm.DoctorMobileNumber,
                DoctorRegNumber = vm.DoctorRegNumber,
                Total = vm.Total,
                TotalGstAmt = vm.TotalGstAmt,
                discountPercent = vm.discountPercent,
                discountAmount = vm.discountAmount,
                Totaldiscount = vm.Totaldiscount,
                TotalPayable = vm.TotalPayable,
                StockReturnItems = vm.StockReturnItemVMs.Select(x => new StockReturnItem
                {
                    ItemMasterId = x.ItemMasterId,
                    PurchaseItemId = x.PurchaseItemId,
                    Batch = x.Batch,
                    Expirydate = x.Expirydate,
                    Mrp = x.Mrp,
                    Qty = x.Qty,
                    Rate = x.Rate,
                    Gst = x.Gst,
                    Discount = x.Discount,
                    Amount = x.Amount,
                    Reason = x.Reason
                }).ToList()
            };

            await _stockReturnservice.Create(model);
            return Ok(new { success = true, message = "Stock Return created successfully.", id = model.Id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _stockReturnservice.GetById(id);
            if (model == null) return NotFound("Stock Return not found.");

            return Ok(model);
        }

        [HttpPut("edit/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] StockReturnVM vm)
        {
            var model = await _stockReturnservice.GetById(id);
            if (model == null) return NotFound("Stock Return not found.");

            model.CustomerId = vm.CustomerId;
            model.ChallanDate = vm.ChallanDate;
            model.ChallanNo = vm.ChallanNo;
            model.MobileNo = vm.MobileNo;
            model.Address = vm.Address;
            model.PharmacyDoctorId = vm.PharmacyDoctorId;
            model.DoctorRegNumber = vm.DoctorRegNumber;
            model.DoctorMobileNumber = vm.DoctorMobileNumber;
            model.Total = vm.Total;
            model.TotalGstAmt = vm.TotalGstAmt;
            model.discountPercent = vm.discountPercent;
            model.discountAmount = vm.discountAmount;
            model.Totaldiscount = vm.Totaldiscount;
            model.TotalPayable = vm.TotalPayable;

            // Remove deleted items
            var removedItems = model.StockReturnItems.Where(dbItem => !vm.StockReturnItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();
            removedItems.ForEach(i => model.StockReturnItems.Remove(i));

            // Add/update items
            foreach (var item in vm.StockReturnItemVMs)
            {
                if (item.Id > 0)
                {
                    var existingItem = model.StockReturnItems.FirstOrDefault(x => x.Id == item.Id);
                    if (existingItem != null)
                    {
                        existingItem.ItemMasterId = item.ItemMasterId;
                        existingItem.PurchaseItemId = item.PurchaseItemId;
                        existingItem.Batch = item.Batch;
                        existingItem.Expirydate = item.Expirydate;
                        existingItem.Mrp = item.Mrp;
                        existingItem.Qty = item.Qty;
                        existingItem.Rate = item.Rate;
                        existingItem.Gst = item.Gst;
                        existingItem.Discount = item.Discount;
                        existingItem.Reason = item.Reason;
                        existingItem.Amount = item.Amount;
                    }
                }
                else
                {
                    model.StockReturnItems.Add(new StockReturnItem
                    {
                        ItemMasterId = item.ItemMasterId,
                        PurchaseItemId = item.PurchaseItemId,
                        Batch = item.Batch,
                        Expirydate = item.Expirydate,
                        Mrp = item.Mrp,
                        Qty = item.Qty,
                        Rate = item.Rate,
                        Gst = item.Gst,
                        Discount = item.Discount,
                        Amount = item.Amount,
                        Reason = item.Reason
                    });
                }
            }

            await _stockReturnservice.Update(model);
            return Ok(new { success = true, message = "Stock Return updated successfully." });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _stockReturnservice.GetById(id);
            if (model == null) return NotFound("Stock Return not found.");

            await _stockReturnservice.Delete(model);
            return Ok(new { success = true, message = "Stock Return deleted successfully." });
        }

        [HttpGet("get-items-by-customer/{customerId}")]
        public async Task<IActionResult> GetItemsByCustomerId(int customerId)
        {
            var sales = await _salesservice.GetByCustomerId(customerId);
            if (sales == null) return Ok(new { success = false, items = new List<object>() });

            var itemMasterIds = sales.SelectMany(x => x.SalesItems).Where(x => x.ItemMaster != null).Select(x => x.ItemMasterId).Distinct().ToList();
            var itemMasters = (await _itemmasterservice.GetAll()).Where(x => itemMasterIds.Contains(x.Id));

            var result = itemMasters.Select(i => new { value = i.Id, text = i.Name });
            return Ok(new { success = true, items = result });
        }

        [HttpGet("get-returned-qty/{customerId}")]
        public async Task<IActionResult> GetReturnedQtyData(int customerId)
        {
            var returnedQtyData = (await _stockReturnservice.GetByCustomerId(customerId))
                .SelectMany(x => x.StockReturnItems)
                .GroupBy(r => new { r.ItemMasterId, r.Batch, Expiry = r.Expirydate, r.Mrp, r.Rate })
                .Select(g => new
                {
                    g.Key.ItemMasterId,
                    g.Key.Batch,
                    Expiry = g.Key.Expiry,
                    Mrp = g.Key.Mrp,
                    Rate = g.Key.Rate,
                    ReturnedQty = g.Sum(x => x.Qty)
                })
                .ToList();

            return Ok(returnedQtyData);
        }

        [HttpGet("get-batches/{itemId}/{customerId}")]
        public async Task<IActionResult> GetBatchesByItemId(int itemId, int customerId)
        {
            var item = await _itemmasterservice.GetByItemMasterId(itemId);
            if (item == null) return NotFound("Item details not found.");

            var sales = await _salesservice.GetByCustomerId(customerId);
            if (sales == null) return NotFound("No sales found for this customer.");

            var salesItems = sales.SelectMany(x => x.SalesItems).Where(s => s.ItemMasterId == itemId).ToList();
            if (!salesItems.Any()) return NotFound("No sales items found for this item and customer.");

            var groupedResult = salesItems
                .GroupBy(b => new { b.Batch, b.Expirydate, b.Mrp, b.Rate })
                .Select(g => new
                {
                    ItemId = g.Select(x => x.ItemMasterId).FirstOrDefault(),
                    batch = g.Key.Batch,
                    expiry = g.Key.Expirydate,
                    rate = g.Key.Rate,
                    mrp = g.Key.Mrp,
                    qty = g.Sum(x => x.Qty),
                    salesItemId = g.Select(x => x.Id).FirstOrDefault()
                }).ToList();

            return Ok(new { gst = item.Hsn?.IGST ?? 0, groupedResult });
        }

        [HttpGet("get-item-details/{itemId}")]
        public async Task<IActionResult> GetItemDetails(int itemId)
        {
            if (itemId <= 0) return BadRequest("Invalid itemId.");

            var hsndata = await _itemmasterservice.GetByItemMasterId(itemId);
            if (hsndata == null) return NotFound("Hsn Details not found.");

            var purchaseItems = (await _purchaseitemservice.GetByItemMasterId(itemId))
                .Select(x => new { x.Id, Text = x.Batch })
                .ToList();

            return Ok(new { gst = hsndata.Hsn?.IGST, purchaseItems });
        }

        [HttpGet("get-purchase-item-details/{id}")]
        public async Task<IActionResult> GetPurchaseItemDetails(int id)
        {
            if (id <= 0) return BadRequest("Invalid Id.");

            var purchasedata = await _purchaseitemservice.GetById(id);
            if (purchasedata == null) return NotFound("Purchase item not found.");

            return Ok(new
            {
                qty = purchasedata.Qty,
                rate = purchasedata.Rate,
                mrp = purchasedata.Mrp,
                expirydate = purchasedata.ExpiryDate
            });
        }

        [HttpGet("report-customer-wise")]
        public async Task<IActionResult> StockReturnReportCustomerWise()
        {
            var sales = await _stockReturnservice.GetAll();

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
                    TotalPayable = sale.SelectMany(x => x.StockReturnItems ?? new List<StockReturnItem>()).Sum(item => item.Amount)
                })
                .OrderByDescending(x => x.ChallanDate)
                .ToList();

            return Ok(customerwiseList);
        }

        [HttpGet("report-item-wise")]
        public async Task<IActionResult> StockReturnReportItemwise()
        {
            var itemMasters = await _itemmasterservice.GetAll();
            var stockReturns = await _stockReturnservice.GetAll();

            var stockreturnItems = stockReturns.Where(s => s.StockReturnItems != null).SelectMany(s => s.StockReturnItems);

            var groupedstockreturn = stockreturnItems
                .GroupBy(x => x.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Amount),
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
                                       Amount = g.TotalAmount,
                                       GstAmount = g.TotalGst
                                   }).ToList();

            return Ok(stockreturnList);
        }
    }
}
