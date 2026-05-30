using System.ServiceModel.Channels;
using AOne.DataAccess.ProfileService;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly IInvoiceRepository _invoiceservice;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemservice;
        private readonly IInvoiceItemRepository _invoiceitemService;
        public InvoiceController(IInvoiceRepository invoiceservice, ICustomerRepository customerservice, IItemMasterRepository itemservice, IInvoiceItemRepository invoiceitemService)
        {
            _invoiceservice = invoiceservice;
            _customerservice = customerservice;
            _itemservice = itemservice;
            _invoiceitemService = invoiceitemService;
        }
        public async Task<IActionResult> Index()
        { 
            var data = await _invoiceservice.GetAll();
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            IList<Customer> customers = await _customerservice.GetAll();
            customers.Insert(0, new Customer { Id = 0, Name = "select" });
            ViewBag.Customer = new SelectList(customers, "Id", "Name");

            IList<ItemMaster> items = await _itemservice.GetAll();
            items.Insert(0, new ItemMaster { Id = 0, Name="select" });
            ViewBag.Itemdata = new SelectList(items, "Id", "Name");
            return View(new InvoiceVM { invoiceItemVms = new List<InvoiceItemVM>() });
        }
        [HttpPost]
        public async Task<IActionResult> Create(InvoiceVM Vm)
        {
            if (Vm != null)
            {
                var model = new Invoice
                {
                    CustomerId = Vm.CustomerId == 0 ? null : Vm.CustomerId,
                    PhoneNo = Vm.PhoneNo,
                    InvoiceNo = Vm.InvoiceNo,
                    InvoiceDate = Vm.InvoiceDate,
                    Address = Vm.Address,
                    TotalTaxAmount = Vm.TotalTaxAmount,
                    TotalAmount = Vm.TotalAmount,
                    Total = Vm.Total,
                    discountper = Vm.discountper,
                    discountvalue = Vm.discountvalue,
                    invoiceItems = Vm.invoiceItemVms?.Select(item => new InvoiceItem
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
                    }).ToList() ?? new List<InvoiceItem>() // Ensure it's not null

                };
                await _invoiceservice.Create(model);
                 
            }
            return RedirectToAction("Index");
        } 
        public async Task<IActionResult> GetItemDetail(int Id)
        { 
                var itemdata = await _itemservice.GetByItemMasterId(Id);
                if(itemdata != null)
                {
                    var data = new
                    {
                        unit = itemdata.Unit1,
                        mrp = itemdata.Mrp,
                        hsncode = itemdata.Hsn?.HsnCode,
                        gst = itemdata.Hsn?.IGST,
                        amount = itemdata.SalesRate1
                    };
                   return Json(data);
                }
            return Json(new { message = "not fetching details" });
             
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            Invoice model = await _invoiceservice.GetById(Id);
            InvoiceVM VM = new InvoiceVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.CustomerId = model.CustomerId;
                VM.PhoneNo = model.PhoneNo;
                VM.InvoiceNo = model.InvoiceNo;
                VM.InvoiceDate = model.InvoiceDate;
                VM.Address = model.Address;
                VM.TotalTaxAmount = model.TotalTaxAmount;
                VM.TotalAmount = model.TotalAmount;
                VM.Total = model.Total;
                VM.discountper = model.discountper;
                VM.discountvalue = model.discountvalue;
                VM.invoiceItemVms = model.invoiceItems.Select(i => new InvoiceItemVM
                {
                    Id = i.Id,
                    ItemId = i.ItemId,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    HsnCode = i.HsnCode,
                    TaxAmount = i.TaxAmount,
                    Gst = i.Gst,
                    Amount = i.Amount,
                    TotalAmount = i.TotalAmount,
                }).ToList();


            }
            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.Itemdata = new SelectList(await _itemservice.GetAll(), "Id", "Name");
            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(InvoiceVM VM)
        {
            Invoice model = await _invoiceservice.GetById(VM.Id);

            if (model != null)
            {
                // ✅ Update Invoice Header
                model.CustomerId = VM.CustomerId;
                model.PhoneNo = VM.PhoneNo;
                model.InvoiceNo = VM.InvoiceNo;
                model.InvoiceDate = VM.InvoiceDate;
                model.Address = VM.Address;
                model.TotalTaxAmount = VM.TotalTaxAmount;
                model.TotalAmount = VM.TotalAmount;
                model.Total = VM.Total;
                model.discountper = VM.discountper;
                model.discountvalue = VM.discountvalue;

                // ✅ Track Removed Items (Get only items that exist in DB but not in VM)
                var removedItems = model.invoiceItems
                    .Where(dbItem => !VM.invoiceItemVms.Any(vmItem => vmItem.Id == dbItem.Id))
                    .ToList();

                // ❌ Remove Deleted Items
                foreach (var item in removedItems)
                {
                    model.invoiceItems.Remove(item);
                }

                // ✅ Update Existing Items or Add New
                foreach (var item in VM.invoiceItemVms)
                {
                    if (item.Id > 0) // Update existing items
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
                    else // Add new items
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

                // ✅ Save Changes
                await _invoiceservice.Update(model);
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid Id for deletion." });
                }

                var model = await _invoiceservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                await _invoiceservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
