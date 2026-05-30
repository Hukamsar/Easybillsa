using AOne.Models.Entity;
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
    public class InvoiceApiController : ControllerBase
    {
        private readonly IInvoiceRepository _invoiceservice;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemservice;
        private readonly IInvoiceItemRepository _invoiceitemService;

        public InvoiceApiController(
            IInvoiceRepository invoiceservice,
            ICustomerRepository customerservice,
            IItemMasterRepository itemservice,
            IInvoiceItemRepository invoiceitemService)
        {
            _invoiceservice = invoiceservice;
            _customerservice = customerservice;
            _itemservice = itemservice;
            _invoiceitemService = invoiceitemService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _invoiceservice.GetAll();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _invoiceservice.GetById(id);
            if (model == null)
                return NotFound(new { message = "Invoice not found." });

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InvoiceVM vm)
        {
            if (vm == null)
                return BadRequest(new { message = "Invalid invoice data." });

            var model = new Invoice
            {
                CustomerId = vm.CustomerId == 0 ? null : vm.CustomerId,
                PhoneNo = vm.PhoneNo,
                InvoiceNo = vm.InvoiceNo,
                InvoiceDate = vm.InvoiceDate,
                Address = vm.Address,
                TotalTaxAmount = vm.TotalTaxAmount,
                TotalAmount = vm.TotalAmount,
                Total = vm.Total,
                discountper = vm.discountper,
                discountvalue = vm.discountvalue,
                invoiceItems = vm.invoiceItemVms?.Select(item => new InvoiceItem
                {
                    ItemId = item.ItemId,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Discount = item.Discount,
                    HsnCode = item.HsnCode,
                    Gst = item.Gst,
                    TaxAmount = item.TaxAmount,
                    Amount = item.Amount,
                    TotalAmount = item.TotalAmount,
                }).ToList() ?? new List<InvoiceItem>()
            };

            await _invoiceservice.Create(model);
            return Ok(new { success = true, message = "Invoice created successfully." });
        }

        [HttpGet("itemdetail/{id}")]
        public async Task<IActionResult> GetItemDetail(int id)
        {
            var itemdata = await _itemservice.GetByItemMasterId(id);
            if (itemdata != null)
            {
                var data = new
                {
                    unit = itemdata.Unit1,
                    mrp = itemdata.Mrp,
                    hsncode = itemdata.Hsn?.HsnCode,
                    gst = itemdata.Hsn?.IGST,
                    amount = itemdata.SalesRate1
                };
                return Ok(data);
            }

            return NotFound(new { message = "Item details not found." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] InvoiceVM vm)
        {
            if (vm == null || vm.Id != id)
                return BadRequest(new { message = "Invalid request." });

            var model = await _invoiceservice.GetById(vm.Id);

            if (model == null)
                return NotFound(new { message = "Invoice not found." });

            // Update Invoice Header
            model.CustomerId = vm.CustomerId;
            model.PhoneNo = vm.PhoneNo;
            model.InvoiceNo = vm.InvoiceNo;
            model.InvoiceDate = vm.InvoiceDate;
            model.Address = vm.Address;
            model.TotalTaxAmount = vm.TotalTaxAmount;
            model.TotalAmount = vm.TotalAmount;
            model.Total = vm.Total;
            model.discountper = vm.discountper;
            model.discountvalue = vm.discountvalue;

            // Track Removed Items
            var removedItems = model.invoiceItems
                .Where(dbItem => !vm.invoiceItemVms.Any(vmItem => vmItem.Id == dbItem.Id))
                .ToList();

            foreach (var item in removedItems)
            {
                model.invoiceItems.Remove(item);
            }

            // Update or Add Items
            foreach (var item in vm.invoiceItemVms)
            {
                if (item.Id > 0)
                {
                    var existingItem = model.invoiceItems.FirstOrDefault(x => x.Id == item.Id);
                    if (existingItem != null)
                    {
                        existingItem.ItemId = item.ItemId;
                        existingItem.Quantity = item.Quantity;
                        existingItem.Unit = item.Unit;
                        existingItem.UnitPrice = item.UnitPrice;
                        existingItem.Discount = item.Discount;
                        existingItem.Gst = item.Gst;
                        existingItem.HsnCode = item.HsnCode;
                        existingItem.TaxAmount = item.TaxAmount;
                        existingItem.Amount = item.Amount;
                        existingItem.TotalAmount = item.TotalAmount;
                    }
                }
                else
                {
                    model.invoiceItems.Add(new InvoiceItem
                    {
                        ItemId = item.ItemId,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Discount = item.Discount,
                        Gst = item.Gst,
                        HsnCode = item.HsnCode,
                        TaxAmount = item.TaxAmount,
                        Amount = item.Amount,
                        TotalAmount = item.TotalAmount,
                    });
                }
            }

            await _invoiceservice.Update(model);
            return Ok(new { success = true, message = "Invoice updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "Invalid Id for deletion." });

                var model = await _invoiceservice.GetById(id);

                if (model == null)
                    return NotFound(new { success = false, message = "Invoice not found." });

                await _invoiceservice.Delete(model);
                return Ok(new { success = true, message = "Invoice deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
