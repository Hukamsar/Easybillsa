using AOne.DataAccess.ProfileService;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class StockReceiveApiController : ControllerBase
    {
        private readonly IStockReceiveRepository _stockreceiveservice;
        private readonly IProfileService _profileService;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IPurchaseItemRepository _purchaseitemservice;

        public StockReceiveApiController(IStockReceiveRepository stockreceiveservice, IProfileService profileService,
            ICustomerRepository customerservice, IItemMasterRepository itemmasterservice, IPurchaseItemRepository purchaseitemservice)
        {
            _stockreceiveservice = stockreceiveservice;
            _profileService = profileService;
            _customerservice = customerservice;
            _itemmasterservice = itemmasterservice;
            _purchaseitemservice = purchaseitemservice;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            await _profileService.Set(User);
            var data = await _stockreceiveservice.GetAll();
            return Ok(data);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new StockReceiveVM
            {
                ChallanDate = DateTime.Now,
                ChallanNo = await GenerateNxtNumber()
            };

            var customers = await _customerservice.GetAll();
            var items = await _itemmasterservice.GetAll();

            return Ok(new
            {
                ViewModel = viewModel,
                Customers = customers.Select(c => new { c.Id, c.Name }),
                Items = items.Select(i => new { i.Id, i.Name })
            });
        }

        private async Task<string> GenerateNxtNumber()
        {
            var data = await _stockreceiveservice.GetAll();
            var lastCode = data.Where(p => !string.IsNullOrEmpty(p.ChallanNo))
                               .Select(p => p.ChallanNo)
                               .LastOrDefault();
            return GenerateNextProductCode(lastCode);
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

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] StockReceiveVM Vm)
        {
            if (Vm == null) return BadRequest("Invalid data");

            var model = new StockReceive
            {
                CustomerId = Vm.CustomerId,
                ChallanDate = Vm.ChallanDate,
                ChallanNo = Vm.ChallanNo,
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
                StockReceiveItems = Vm.StockReceiveItemVMs.Select(x => new StockReceiveItem
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
                    Amount = x.Amount
                }).ToList() ?? new List<StockReceiveItem>()
            };

            await _stockreceiveservice.Create(model);
            return Ok(model);
        }

        [HttpGet("GetItemDetails/{itemId}")]
        public async Task<IActionResult> GetItemDetails(int itemId)
        {
            if (itemId <= 0) return BadRequest(new { error = "Invalid ItemId" });

            var hsndata = await _itemmasterservice.GetByItemMasterId(itemId);
            if (hsndata == null) return NotFound(new { error = "Hsn Details not found." });

            var purchaseItems = (await _purchaseitemservice.GetByItemMasterId(itemId))
                                .Select(x => new { x.Id, Text = x.Batch })
                                .ToList();

            return Ok(new { gst = hsndata.Hsn?.IGST, purchaseItems });
        }

        [HttpGet("GetPurchaseItemDetails/{Id}")]
        public async Task<IActionResult> GetPurchaseItemDetails(int Id)
        {
            if (Id <= 0) return BadRequest(new { error = "Invalid Id" });

            var purchasedata = await _purchaseitemservice.GetById(Id);
            if (purchasedata == null) return NotFound(new { error = "Purchase Item not found." });

            return Ok(new
            {
                qty = purchasedata.Qty,
                rate = purchasedata.Rate,
                mrp = purchasedata.Mrp,
                expirydate = purchasedata.ExpiryDate
            });
        }

        [HttpGet("Edit/{Id}")]
        public async Task<IActionResult> Edit(int Id)
        {
            var model = await _stockreceiveservice.GetById(Id);
            if (model == null) return NotFound();

            var VM = new StockReceiveVM
            {
                Id = model.Id,
                CustomerId = model.CustomerId,
                CustomerName = model.Customers?.Name,
                ChallanNo = model.ChallanNo,
                ChallanDate = model.ChallanDate,
                MobileNo = model.MobileNo,
                Address = model.Address,
                PharmacyDoctorId = model.PharmacyDoctorId,
                DoctorName = model.PharmacyDoctor?.Name,
                DoctorMobileNumber = model.DoctorMobileNumber,
                DoctorRegNumber = model.DoctorRegNumber,
                Total = model.Total,
                TotalGstAmt = model.TotalGstAmt,
                discountPercent = model.discountPercent,
                discountAmount = model.discountAmount,
                Totaldiscount = model.Totaldiscount,
                TotalPayable = model.TotalPayable,
                StockReceiveItemVMs = model.StockReceiveItems?.Select(x => new StockReceiveItemVM
                {
                    Id = x.Id,
                    StockReceiveId = x.StockReceiveId,
                    ItemMasterId = x.ItemMasterId,
                    PurchaseItemId = x.PurchaseItemId,
                    Batch = x.Batch,
                    Expirydate = x.Expirydate,
                    Mrp = x.Mrp,
                    Qty = x.Qty,
                    Rate = x.Rate,
                    Gst = x.Gst,
                    Discount = x.Discount,
                    Amount = x.Amount
                }).ToList() ?? new List<StockReceiveItemVM>()
            };

            var customers = await _customerservice.GetAll();
            var items = await _itemmasterservice.GetAll();

            return Ok(new
            {
                VM,
                Customers = customers.Select(c => new { c.Id, c.Name }),
                Items = items.Select(i => new { i.Id, i.Name })
            });
        }

        [HttpPost("Edit")]
        public async Task<IActionResult> Edit([FromBody] StockReceiveVM VM)
        {
            if (VM == null) return BadRequest("Invalid data");

            var model = await _stockreceiveservice.GetById(VM.Id);
            if (model == null) return NotFound();

            model.CustomerId = VM.CustomerId;
            model.ChallanDate = VM.ChallanDate;
            model.ChallanNo = VM.ChallanNo;
            model.MobileNo = VM.MobileNo;
            model.Address = VM.Address;
            model.PharmacyDoctorId = VM.PharmacyDoctorId;
            model.DoctorMobileNumber = VM.DoctorMobileNumber;
            model.DoctorRegNumber = VM.DoctorRegNumber;
            model.Total = VM.Total;
            model.TotalGstAmt = VM.TotalGstAmt;
            model.discountPercent = VM.discountPercent;
            model.discountAmount = VM.discountAmount;
            model.Totaldiscount = VM.Totaldiscount;
            model.TotalPayable = VM.TotalPayable;

            var removedItems = model.StockReceiveItems
                .Where(dbItem => !VM.StockReceiveItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

            foreach (var item in removedItems) model.StockReceiveItems.Remove(item);

            foreach (var item in VM.StockReceiveItemVMs)
            {
                if (item.Id > 0)
                {
                    var existingItem = model.StockReceiveItems.FirstOrDefault(x => x.Id == item.Id);
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
                        existingItem.Amount = item.Amount;
                    }
                }
                else
                {
                    model.StockReceiveItems.Add(new StockReceiveItem
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
                        Amount = item.Amount
                    });
                }
            }

            await _stockreceiveservice.Update(model);
            return Ok(model);
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "Invalid Id" });

            var model = await _stockreceiveservice.GetById(id);
            if (model == null) return NotFound(new { success = false, message = "Item not found" });

            await _stockreceiveservice.Delete(model);
            return Ok(new { success = true, message = "Item deleted successfully" });
        }

        [HttpGet("StockReceiveReportItemWise")]
        public async Task<IActionResult> StockReceiveReportItemWise()
        {
            var itemMasters = await _itemmasterservice.GetAll();
            var stockReceives = await _stockreceiveservice.GetAll();

            var stockreceiveItems = stockReceives.Where(s => s.StockReceiveItems != null)
                                                 .SelectMany(s => s.StockReceiveItems);

            var groupedStockReceive = stockreceiveItems.GroupBy(x => x.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Amount),
                    TotalGst = g.Sum(x =>
                    {
                        decimal gross = x.Rate * x.Qty;
                        decimal discountAmt = gross * (x.Discount / 100);
                        decimal taxable = gross - discountAmt;
                        return taxable * (x.Gst / 100);
                    })
                }).ToList();

            var stockreceiveList = (from item in itemMasters
                                    join g in groupedStockReceive on item.Id equals g.ItemMasterId
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

            return Ok(stockreceiveList);
        }

        [HttpGet("StockReceiveReportBillWise")]
        public async Task<IActionResult> StockReceiveReportBillWise()
        {
            var stockReceives = await _stockreceiveservice.GetAll();

            var billwiseList = stockReceives.Select(s => new StockReceiveVM
            {
                Id = s.Id,
                ChallanNo = s.ChallanNo,
                ChallanDate = s.ChallanDate,
                CustomerName = s.Customers?.Name ?? "Unknown",
                MobileNo = s.MobileNo,
                TotalGstAmt = s.TotalGstAmt,
                TotalPayable = s.StockReceiveItems?.Sum(item => item.Amount) ?? 0
            }).Where(x => x.TotalPayable > 0)
              .OrderByDescending(x => x.ChallanDate)
              .ToList();

            return Ok(billwiseList);
        }
    }
}
