using EasyBill.DataAccess.Repository.IRepository;
using AOne.DataAccess.ProfileService;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class StockIssueApiController : ControllerBase
    {
        private readonly IStockIssueRepository _stockissueservice;
        private readonly IProfileService _profileService;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IPurchaseItemRepository _purchaseitemservice;
        private readonly IPurchaseOrderRepository _poRepository;
        private readonly ITenantRepository _tenantRepository;

        public StockIssueApiController(IStockIssueRepository stockissueservice, IProfileService profileService,
            ICustomerRepository customerservice, IItemMasterRepository itemmasterservice,
            IPurchaseItemRepository purchaseitemservice, IPurchaseOrderRepository poRepository, ITenantRepository tenantRepository)
        {
            _stockissueservice = stockissueservice;
            _profileService = profileService;
            _customerservice = customerservice;
            _itemmasterservice = itemmasterservice;
            _purchaseitemservice = purchaseitemservice;
            _poRepository = poRepository;
            _tenantRepository = tenantRepository;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            await _profileService.Set(User);
            var data = await _stockissueservice.GetAll();
            return Ok(data);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new StockIssueVM
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
            var data = await _stockissueservice.GetAll();
            var lastCode = data.Where(p => !string.IsNullOrEmpty(p.ChallanNo))
                               .Select(p => p.ChallanNo)
                               .LastOrDefault();
            return GenerateNextProductCode(lastCode);
        }

        private string GenerateNextProductCode(string lastCode)
        {
            if (string.IsNullOrEmpty(lastCode) || lastCode.Length < 2)
                return "CH0001";

            string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray());
            string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray());
            int.TryParse(numberPart, out int number);

            return prefix + (number + 1).ToString("D" + numberPart.Length);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] StockIssueVM Vm)
        {
            if (Vm == null) return BadRequest("Invalid data");

            var model = new StockIssue
            {
                CustomerId = Vm.CustomerId,
                ChallanDate = Vm.ChallanDate,
                ChallanNo = Vm.ChallanNo,
                MobileNo = Vm.MobileNo,
                Address = Vm.Address,
                Total = Vm.Total,
                TotalGstAmt = Vm.TotalGstAmt,
                discountPercent = Vm.discountPercent,
                discountAmount = Vm.discountAmount,
                Totaldiscount = Vm.Totaldiscount,
                TotalPayable = Vm.TotalPayable,
                PharmacyDoctorId = Vm.PharmacyDoctorId,
                DoctorMobileNumber = Vm.DoctorMobileNumber,
                DoctorRegNumber = Vm.DoctorRegNumber,
                StockIssuesItems = Vm.StockIssuesItemVMs.Select(x => new StockIssueItem
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
                }).ToList() ?? new List<StockIssueItem>()
            };

            await _stockissueservice.Create(model);
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
            var model = await _stockissueservice.GetById(Id);
            if (model == null) return NotFound();

            var VM = new StockIssueVM
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
                DoctorRegNumber = model.DoctorRegNumber,
                DoctorMobileNumber = model.DoctorMobileNumber,
                Total = model.Total,
                TotalGstAmt = model.TotalGstAmt,
                discountPercent = model.discountPercent,
                discountAmount = model.discountAmount,
                Totaldiscount = model.Totaldiscount,
                TotalPayable = model.TotalPayable,
                StockIssuesItemVMs = model.StockIssuesItems?.Select(x => new StockIssueItemVM
                {
                    Id = x.Id,
                    StockIssueId = x.StockIssueId,
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
                }).ToList() ?? new List<StockIssueItemVM>()
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
        public async Task<IActionResult> Edit([FromBody] StockIssueVM VM)
        {
            if (VM == null) return BadRequest("Invalid data");

            var model = await _stockissueservice.GetById(VM.Id);
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

            var removedItems = model.StockIssuesItems
                .Where(dbItem => !VM.StockIssuesItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

            foreach (var item in removedItems)
                model.StockIssuesItems.Remove(item);

            foreach (var item in VM.StockIssuesItemVMs)
            {
                if (item.Id > 0)
                {
                    var existingItem = model.StockIssuesItems.FirstOrDefault(x => x.Id == item.Id);
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
                    model.StockIssuesItems.Add(new StockIssueItem
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

            await _stockissueservice.Update(model);
            return Ok(model);
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "Invalid Id" });

            var model = await _stockissueservice.GetById(id);
            if (model == null) return NotFound(new { success = false, message = "Item not found" });

            await _stockissueservice.Delete(model);
            return Ok(new { success = true, message = "Item deleted successfully" });
        }

        [HttpGet("StockIssueReportItemWise")]
        public async Task<IActionResult> StockIssueReportItemWise()
        {
            var itemMasters = await _itemmasterservice.GetAll();
            var stockIssue = await _stockissueservice.GetAll();

            var stockissueItems = stockIssue.Where(s => s.StockIssuesItems != null).SelectMany(s => s.StockIssuesItems);

            var groupedStockIssue = stockissueItems
                .GroupBy(x => x.ItemMasterId)
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

            var stockissueList = (from item in itemMasters
                                  join g in groupedStockIssue on item.Id equals g.ItemMasterId
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

            return Ok(stockissueList);
        }

        [HttpGet("StockIssueReportBillWise")]
        public async Task<IActionResult> StockIssueReportBillWise()
        {
            var stockissue = await _stockissueservice.GetAll();

            var billwiseList = stockissue.Select(stock => new StockIssueVM
            {
                Id = stock.Id,
                ChallanNo = stock.ChallanNo,
                ChallanDate = stock.ChallanDate,
                CustomerName = stock.Customers?.Name ?? "Unknown",
                MobileNo = stock.MobileNo,
                TotalGstAmt = stock.TotalGstAmt,
                TotalPayable = stock.StockIssuesItems?.Sum(item => item.Amount) ?? 0
            }).Where(x => x.TotalPayable > 0)
              .OrderByDescending(x => x.ChallanDate)
              .ToList();

            return Ok(billwiseList);
        }
    
        [HttpGet("pending-incoming-requests")]
        public async Task<IActionResult> GetPendingIncomingRequests()
        {
            await _profileService.Set(User);
            var currentTenantId = _profileService.Profile?.TenantId;

            var allOrders = await _poRepository.GetAll();
            var pendingRequests = allOrders
                .Where(p => p.OrderType == "SR" 
                         && p.TargetTenantId == currentTenantId 
                         && p.WorkflowStatus == "Pending")
                .ToList();

            var tenants = await _tenantRepository.GetAll();
            
            var result = pendingRequests.Select(pr => new {
                Id = pr.Id,
                BillNo = pr.BillNo,
                BillDate = pr.BillDate,
                RequesterName = tenants.FirstOrDefault(t => t.Id == pr.TenantId)?.Name ?? "Unknown Branch",
                RequesterTenantId = pr.TenantId,
                TotalQty = 0 // Removed pr.TotalQty as it doesn't exist
            }).ToList();

            return Ok(new { success = true, data = result });
        }

        [HttpGet("request-details/{id}")]
        public async Task<IActionResult> GetRequestDetails(int id)
        {
            await _profileService.Set(User);
            
            var order = await _poRepository.GetById(id);
            if (order == null) return NotFound(new { success = false, message = "Request not found" });

            var result = order.PurchaseOrderItems.Select(item => new {
                ItemId = item.ItemId,
                ItemName = item.ItemMasters?.Name,
                Qty = item.Qty,
                FreeQty = item.FreeQty,
                PurchaseRate = item.Rate,
                Mrp = item.Mrp
            }).ToList();

            return Ok(new { success = true, data = result });
        }
    }
}

