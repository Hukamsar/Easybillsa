using Microsoft.AspNetCore.Mvc;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using DocumentFormat.OpenXml.Office2010.Excel;
using EasyBill.Models.Entity;
using DocumentFormat.OpenXml.Vml;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class SalesOrderApiController : ControllerBase
    {
        private readonly ISalesOrderRepository _salesOrderRepo;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IModeOfPaymentRepository _modeofpaymentservice;
        private readonly ISalseSettingRepository _salsesettingservice;
        private readonly IPurchaseItemRepository _purchaseitemservice;
        private readonly IOpticalRepository _opticalRepo;

        public SalesOrderApiController(
            ISalesOrderRepository salesOrderRepo,
            ICustomerRepository customerservice,
            IItemMasterRepository itemmasterservice,
            IModeOfPaymentRepository modeofpaymentservice,
            ISalseSettingRepository salsesettingservice,
            IPurchaseItemRepository purchaseitemservice,
            IOpticalRepository opticalRepo)
        {
            _salesOrderRepo = salesOrderRepo;
            _customerservice = customerservice;
            _itemmasterservice = itemmasterservice;
            _modeofpaymentservice = modeofpaymentservice;
            _salsesettingservice = salsesettingservice;
            _purchaseitemservice = purchaseitemservice;
            _opticalRepo = opticalRepo;
        }

        // GET: api/SalesOrder
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _salesOrderRepo.GetAll();

            var salesOrderData = data.Select(x => new
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                BillNo = x.BillNo,
                BillDate = x.BillDate,
                MobileNo = x.MobileNo ?? string.Empty,
                Address = x.Address ?? string.Empty,
                Total = x.Total,
                TotalGstAmt = x.TotalGstAmt,
                TotalPayable = x.TotalPayable,
                DoctorMobileNumber = x.DoctorMobileNumber ?? string.Empty,
                DoctorRegNumber = x.DoctorRegNumber ?? string.Empty,
                TotalDiscount = x.Totaldiscount,
                DiscountPercent = x.discountPercent,
                DiscountAmount = x.discountAmount,
                PaidAmount = x.PaidAmount,
                ReturnAmount = x.ReturnAmount,
                OfferId = x.OfferId
            }).ToList();

            return Ok(salesOrderData);
        }
         
        [HttpGet("GenerateNextBillNo")]
        public async Task<IActionResult> GenerateNextBillNo()
        {
            try
            {
                var data = await _salesOrderRepo.GetAll();
                var lastCode = data
                    .Where(p => !string.IsNullOrEmpty(p.BillNo))
                    .Select(p => p.BillNo)
                    .OrderBy(p => p)
                    .LastOrDefault();

                var nextBillNo = GenerateNextProductCode(lastCode);

                return Ok(new
                {
                    success = true,
                    nextBillNo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating next bill number.",
                    error = ex.Message
                });
            }
        }

        private string GenerateNextProductCode(string lastCode)
        {
            if (string.IsNullOrEmpty(lastCode) || lastCode.Length < 2)
                return "SO0001";

            string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray());
            string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

            if (!int.TryParse(numberPart, out int number))
                number = 0;

            return prefix + (number + 1).ToString("D" + numberPart.Length);
        }

        // POST: api/SalesOrder/Create
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] SalesOrderVM Vm)
        {
            if (Vm == null)
                return BadRequest(new { success = false, message = "Invalid data." });

            var model = new SalesOrder
            {
                CustomerId = Vm.CustomerId,
                BillDate = Vm.BillDate,
                BillNo = Vm.BillNo,
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
                PaidAmount = Vm.PaidAmount,
                ReturnAmount = Vm.ReturnAmount,
                OfferId = Vm.OfferId,
                salesOrderItems = Vm.SalesOrderItemVMs?.Select(x => new SalesOrderItem()
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
                }).ToList() ?? new List<SalesOrderItem>(),
                SalsePaymentDetails = Vm.SalsePaymentDetails?.Select(pd => new SalsePaymentDetails
                {
                    PaymentModeId = pd.PaymentModeId,
                    Amount = pd.Amount,
                    ReferenceNo = pd.ReferenceNo,
                    Description = pd.Description,
                    CustomerId = Vm.CustomerId,
                }).ToList() ?? new List<SalsePaymentDetails>(),
                Opticals = Vm.OpticalVMs?.Select(pd => new Optical
                {
                    ItemMasterId = pd.ItemMasterId,
                    Rx = pd.Rx,
                    Sphere = pd.Sphere,
                    Cylinder = pd.Cylinder,
                    Axis = pd.Axis,
                    Prism = pd.Prism,
                    Add = pd.Add
                }).ToList() ?? new List<Optical>()
            };

            var createdSale = await _salesOrderRepo.Create(model);
            return Ok(new { success = true, saleId = createdSale.Id });
        }
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _salesOrderRepo.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Sale not found." });

            var VM = new SalesOrderVM
            {
                Id = model.Id,
                CustomerId = model.CustomerId,
                BillDate = model.BillDate,
                BillNo = model.BillNo,
                MobileNo = model.MobileNo,
                Address = model.Address,
                PharmacyDoctorId = model.PharmacyDoctorId,
                DoctorMobileNumber = model.DoctorMobileNumber,
                DoctorRegNumber = model.DoctorRegNumber,
                Total = model.Total,
                TotalGstAmt = model.TotalGstAmt,
                TotalPayable = model.TotalPayable,
                discountPercent = model.discountPercent,
                discountAmount = model.discountAmount,
                Totaldiscount = model.Totaldiscount,
                PaidAmount = model.PaidAmount,
                ReturnAmount = model.ReturnAmount,
                OfferId = model.OfferId,
                SalesOrderItemVMs = model.salesOrderItems?.Select(x => new SalesOrderItemVM
                {
                    Id = x.Id,
                    ItemMasterId = x.ItemMasterId,
                    Batch = x.Batch,
                    Expirydate = x.Expirydate,
                    Mrp = x.Mrp,
                    Qty = x.Qty,
                    Rate = x.Rate,
                    Gst = x.Gst,
                    Discount = x.Discount,
                    Amount = x.Amount
                }).ToList() ?? new List<SalesOrderItemVM>(),
                SalsePaymentDetails = model.SalsePaymentDetails?.Select(x => new SalsePaymentDetailsVM
                {
                    Id = x.Id,
                    PaymentModeId = x.PaymentModeId,
                    Amount = x.Amount,
                    ReferenceNo = x.ReferenceNo,
                    Description = x.Description,
                    CustomerId = x.CustomerId
                }).ToList() ?? new List<SalsePaymentDetailsVM>()
            };

            return Ok(new
            {
                success = true,
                message = "Sales order retrieved successfully.",
                data = VM
            });
        }

        // PUT: api/SalesOrder/Edit
        [HttpPut("Edit")]
        public async Task<IActionResult> Edit([FromBody] SalesOrderVM VM)
        {
            if (VM == null)
                return BadRequest(new { success = false, message = "Invalid data." });

            var model = await _salesOrderRepo.GetById(VM.Id);
            if (model == null)
                return NotFound(new { success = false, message = "Sale not found." });

            // Same update logic as MVC version (items, payments, opticals)
            model.CustomerId = VM.CustomerId;
            model.BillDate = VM.BillDate;
            model.BillNo = VM.BillNo;
            model.MobileNo = VM.MobileNo;
            model.Address = VM.Address;
            model.PharmacyDoctorId = VM.PharmacyDoctorId;
            model.DoctorMobileNumber = VM.DoctorMobileNumber;
            model.DoctorRegNumber = VM.DoctorRegNumber;
            model.Total = VM.Total;
            model.TotalGstAmt = VM.TotalGstAmt;
            model.TotalPayable = VM.TotalPayable;
            model.discountPercent = VM.discountPercent;
            model.discountAmount = VM.discountAmount;
            model.Totaldiscount = VM.Totaldiscount;
            model.PaidAmount = VM.PaidAmount;
            model.ReturnAmount = VM.ReturnAmount;
            model.OfferId = VM.OfferId;

            // Update Items
            if (model.salesOrderItems == null) model.salesOrderItems = new List<SalesOrderItem>();
            if (VM.SalesOrderItemVMs == null) VM.SalesOrderItemVMs = new List<SalesOrderItemVM>();

            var removedItems = model.salesOrderItems.Where(dbItem => !VM.SalesOrderItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();
            removedItems.ForEach(item => model.salesOrderItems.Remove(item));

            foreach (var item in VM.SalesOrderItemVMs)
            {
                if (item.Id > 0)
                {
                    var existing = model.salesOrderItems.FirstOrDefault(x => x.Id == item.Id);
                    if (existing != null)
                    {
                        existing.ItemMasterId = item.ItemMasterId;
                        existing.PurchaseItemId = item.PurchaseItemId;
                        existing.Batch = item.Batch;
                        existing.Expirydate = item.Expirydate;
                        existing.Mrp = item.Mrp;
                        existing.Qty = item.Qty;
                        existing.Rate = item.Rate;
                        existing.Gst = item.Gst;
                        existing.Discount = item.Discount;
                        existing.Amount = item.Amount;
                    }
                }
                else
                {
                    model.salesOrderItems.Add(new SalesOrderItem
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

            // Update Payments
            if (model.SalsePaymentDetails == null) model.SalsePaymentDetails = new List<SalsePaymentDetails>();
            if (VM.SalsePaymentDetails == null) VM.SalsePaymentDetails = new List<SalsePaymentDetailsVM>();

            var removedPayments = model.SalsePaymentDetails.Where(db => !VM.SalsePaymentDetails.Any(vm => vm.Id == db.Id)).ToList();
            removedPayments.ForEach(p => model.SalsePaymentDetails.Remove(p));

            foreach (var pd in VM.SalsePaymentDetails)
            {
                if (pd.Id > 0)
                {
                    var existing = model.SalsePaymentDetails.FirstOrDefault(x => x.Id == pd.Id);
                    if (existing != null)
                    {
                        existing.PaymentModeId = pd.PaymentModeId;
                        existing.Amount = pd.Amount;
                        existing.ReferenceNo = pd.ReferenceNo;
                        existing.Description = pd.Description;
                        existing.CustomerId = VM.CustomerId;
                    }
                }
                else
                {
                    model.SalsePaymentDetails.Add(new SalsePaymentDetails
                    {
                        PaymentModeId = pd.PaymentModeId,
                        Amount = pd.Amount,
                        ReferenceNo = pd.ReferenceNo,
                        Description = pd.Description,
                        CustomerId = VM.CustomerId
                    });
                }
            }
 
            await _salesOrderRepo.Update(model);
            return Ok(new { success = true, message = "Sale updated successfully." });
        }

        // DELETE: api/SalesOrder/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "Invalid Id." });

            var model = await _salesOrderRepo.GetById(id);
            if (model == null) return NotFound(new { success = false, message = "Sale not found." });

            await _salesOrderRepo.Delete(model);
            return Ok(new { success = true, message = "Sale deleted successfully." });
        }

        // GET: api/SalesOrder/GetBatchesByItemId/{id}
        [HttpGet("GetBatchesByItemId/{id}")]
        public async Task<IActionResult> GetBatchesByItemId(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
                return NotFound(new { error = "Item not found." });

            var purchaseItems = await _purchaseitemservice.GetByItemMasterId(id);
            var groupedResult = purchaseItems
                .GroupBy(b => new { b.Batch, b.ExpiryDate, b.Mrp, b.Rate })
                .Select(g => new
                {
                    batch = g.Key.Batch,
                    expiry = g.Key.ExpiryDate,
                    rate = g.Key.Rate,
                    mrp = g.Key.Mrp,
                    qty = g.Select(x => x.Qty).FirstOrDefault(),
                    purchaseItemId = g.Select(x => x.Id).FirstOrDefault()
                }).ToList();

            return Ok(new
            {
                gst = item.Hsn?.IGST ?? 0,
                groupedResult
            });
        }

        // GET: api/SalesOrder/GetItemDetails/{itemId}
        [HttpGet("GetItemDetails/{itemId}")]
        public async Task<IActionResult> GetItemDetails(int itemId)
        {
            if (itemId <= 0)
                return BadRequest(new { error = "Invalid ItemId" });

            var hsndata = await _itemmasterservice.GetByItemMasterId(itemId);
            if (hsndata == null)
                return NotFound(new { error = "Hsn Details not found." });

            var purchaseItems = await _purchaseitemservice.GetByItemMasterId(itemId);

            return Ok(new
            {
                gst = hsndata.Hsn?.IGST ?? 0,
                purchaseItems = purchaseItems.Select(x => new { x.Id, Text = x.Batch }).ToList()
            });
        }

        // GET: api/SalesOrder/GetPurchaseItemDetails/{id}
        [HttpGet("GetPurchaseItemDetails/{id}")]
        public async Task<IActionResult> GetPurchaseItemDetails(int id)
        {
            if (id <= 0) return BadRequest(new { error = "Invalid Id" });

            var purchasedata = await _purchaseitemservice.GetById(id);
            if (purchasedata == null) return NotFound(new { error = "Purchase Item not found." });

            return Ok(new
            {
                qty = purchasedata.Qty,
                rate = purchasedata.Rate,
                mrp = purchasedata.Mrp,
                expirydate = purchasedata.ExpiryDate
            });
        }

        // GET: api/SalesOrder/OpticalDetails
        [HttpGet("OpticalDetails")]
        public async Task<IActionResult> GetOpticalDetails(int salesOrderId, int itemmasterId)
        {
            var opticalDetails = (await _opticalRepo.GetBySalesOrder(salesOrderId, itemmasterId))
                .Select(o => new
                {
                    o.Id,
                    o.ItemMasterId,
                    o.Rx,
                    o.Sphere,
                    o.Cylinder,
                    o.Axis,
                    o.Prism,
                    o.Add
                }).ToList();

            return Ok(opticalDetails);
        }

        // GET: api/SalesOrder/ItemByBarcode
        [HttpGet("ItemByBarcode")]
        public async Task<IActionResult> GetItemByBarcode(string? barcode)
        {
            var item = await _itemmasterservice.GetByBarcode(barcode);
            if (item == null) return NotFound(new { error = "Item not found" });

            return Ok(new { itemId = item.Id, itemname = item.Name });
        }

        // GET: api/SalesOrder/ItemById/{id}
        [HttpGet("ItemById/{id}")]
        public async Task<IActionResult> GetItemById(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null) return NotFound(new { error = "Item not found" });

            return Ok(new { itemId = item.Id, itemname = item.Name, category = item.Category?.CategoryName });
        }

        // GET: api/SalesOrder/CustomerByMobile
        [HttpGet("CustomerByMobile")]
        public async Task<IActionResult> GetCustomerByMobile(string mobileNo)
        {
            var customer = await _customerservice.GetCustomerByMobileno(mobileNo);
            if (customer == null) return Ok(new { found = false });

            return Ok(new
            {
                found = true,
                data = new
                {
                    customer.Name,
                    customer.Id,
                    customer.Address
                }
            });
        }

        // POST: api/SalesOrder/CreateCustomer
        [HttpPost("CreateCustomer")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerVM Vm)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid customer data");

            var Customer = new Customer
            {
                Name = Vm.Name,
                PhoneNo = Vm.PhoneNo,
                Address = Vm.Address,
                Email = Vm.Email
            };

            var result = await _customerservice.Create(Customer);

            return Ok(new
            {
                success = true,
                data = new
                {
                    result.Id,
                    result.Name,
                    result.PhoneNo,
                    result.Address
                }
            });
        }

        [HttpGet("OrderHistory/All/{CustomerId}")]
        public async Task<IActionResult> GetAllOrderHistory(int CustomerId)
        {
            if (CustomerId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid CustomerId."
                });
            }

            var orders = await _salesOrderRepo.GetByCustomerId(CustomerId);

            if (orders == null || !orders.Any())
            {
                return NotFound(new
                {
                    success = false,
                    message = "No orders found for this customer."
                });
            }

            var orderList = orders.Select(model => new SalesOrderVM
            {
                Id = model.Id,
                CustomerId = model.CustomerId,
                BillDate = model.BillDate,
                BillNo = model.BillNo,
                MobileNo = model.MobileNo,
                Address = model.Address,
                PharmacyDoctorId = model.PharmacyDoctorId,
                DoctorMobileNumber = model.DoctorMobileNumber,
                DoctorRegNumber = model.DoctorRegNumber,
                Total = model.Total,
                TotalGstAmt = model.TotalGstAmt,
                TotalPayable = model.TotalPayable,
                discountPercent = model.discountPercent,
                discountAmount = model.discountAmount,
                Totaldiscount = model.Totaldiscount,
                PaidAmount = model.PaidAmount,
                ReturnAmount = model.ReturnAmount,
                OfferId = model.OfferId,

                SalesOrderItemVMs = model.salesOrderItems?.Select(x => new SalesOrderItemVM
                {
                    Id = x.Id,
                    ItemMasterId = x.ItemMasterId,
                    Batch = x.Batch,
                    Expirydate = x.Expirydate,
                    Mrp = x.Mrp,
                    Qty = x.Qty,
                    Rate = x.Rate,
                    Gst = x.Gst,
                    Discount = x.Discount,
                    Amount = x.Amount
                }).ToList() ?? new List<SalesOrderItemVM>(),

                SalsePaymentDetails = model.SalsePaymentDetails?.Select(x => new SalsePaymentDetailsVM
                {
                    Id = x.Id,
                    PaymentModeId = x.PaymentModeId,
                    Amount = x.Amount,
                    ReferenceNo = x.ReferenceNo,
                    Description = x.Description,
                    CustomerId = x.CustomerId
                }).ToList() ?? new List<SalsePaymentDetailsVM>()

            }).ToList();

            return Ok(new
            {
                success = true,
                message = "Order history loaded successfully.",
                count = orderList.Count,
                data = orderList
            });
        }

    }
}
