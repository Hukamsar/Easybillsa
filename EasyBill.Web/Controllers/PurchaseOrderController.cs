using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using EasyBill.UI.Filters;

namespace EasyBill.UI.Controllers
{
    public class PurchaseOrderController : Controller
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepo;
        private readonly ISupplierRepository _supplierRepo;
        private readonly IItemMasterRepository _itemMasterRepo;
        private readonly IHSNRepository _hSNRepo;
        private readonly ITenantRepository _tenantRepo;
        private readonly IPOWithAIRepository _poWithAIRepo;
        public PurchaseOrderController(
            IPurchaseOrderRepository purchaseOrderRepo,
            ISupplierRepository supplierRepo, 
            IItemMasterRepository itemMasterRepo, 
            IHSNRepository hSNRepo,
            ITenantRepository tenantRepo,
            IPOWithAIRepository pOWithAIRepo)
        {
            _purchaseOrderRepo = purchaseOrderRepo;
            _supplierRepo = supplierRepo;
            _itemMasterRepo = itemMasterRepo;
            _hSNRepo = hSNRepo;
            _tenantRepo = tenantRepo;
            _poWithAIRepo = pOWithAIRepo;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _purchaseOrderRepo.GetAll();
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var tenantid = User.FindFirst("TenantId")?.Value;
            var tenantdata = await _tenantRepo.GetById(tenantid); 
            // Check bussiness type 
            var businessType = tenantdata?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;
            ViewBag.supplier = new SelectList(await _supplierRepo.GetALL(), "Id", "FirstName");
            ViewBag.Item = new SelectList((await _itemMasterRepo.GetAll()).OrderBy(x => x.Name), "Id", "Name");
            ViewBag.Hsn = new SelectList(await _hSNRepo.GetAll(), "Id", "HsnCode");
           // ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            
            var tenantList = await _tenantRepo.GetAll();
            if (tenantdata != null)
            {
                var hoTenantId = string.IsNullOrEmpty(tenantdata.ParentTenantId) ? tenantdata.Id : tenantdata.ParentTenantId;
                var branches = tenantList.Where(t => 
                    (string.Equals(t.ParentTenantId, hoTenantId, StringComparison.OrdinalIgnoreCase) || string.Equals(t.Id, hoTenantId, StringComparison.OrdinalIgnoreCase)) && 
                    !string.Equals(t.Id, tenantid, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => t.Name)
                    .ToList();
                ViewBag.Branches = new SelectList(branches, "Id", "Name");
            }
            else
            {
                ViewBag.Branches = new SelectList(new List<AOne.Models.Entity.Tenant>(), "Id", "Name");
            }

            var model = new PurchaseOrderVM()
            {
                BillDate = DateTime.Now,
                PartyBillDate = DateTime.Now,
                discountPercent = 0,
                BillNo = await GenerateNxtNumber(),
                TenanatGst = tenantdata.GstNo,
                PurchaseType = "Local"
            };
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Create(PurchaseOrderVM VM)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new
                    {
                        Field = x.Key,
                        Message = x.Value.Errors.First().ErrorMessage
                    })
                    .ToList();

                var firstError = errors.FirstOrDefault();
                return BadRequest(new
                {
                    success = false,
                    message = firstError?.Message ?? "Please fill all required fields correctly.",
                    errors = errors // Optional: sab errors bhejne ke liye
                });
            }

            var data = await _purchaseOrderRepo.GetAll();
            var billNo = VM.BillNo?.Trim();

            bool isDuplicate = data.Any(x =>
                x.BillNo != null &&
                x.BillNo.Trim().Equals(billNo, StringComparison.OrdinalIgnoreCase)
            );

            if (isDuplicate)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"{billNo} - This Bill Number already exists."
                });
            }


            if (VM != null)
            {
                var model = new PurchaseOrder
                {
                    SupplierId = VM.SupplierId,
                    BillNo = VM.BillNo,
                    BillDate = VM.BillDate,
                    PartyBillDate = VM.PartyBillDate,
                    PartyBillNo = VM.PartyBillNo,
                    TotalGstAmt = VM.TotalGstAmt,
                    Totaldiscount = VM.Totaldiscount,
                    TotalPayable = VM.TotalPayable,
                    RoundOffAmount = VM.RoundOffAmount,
                    discountPercent = VM.discountPercent,
                    discountAmount = VM.discountAmount,
                    Total = VM.Total,
                    billingType = VM.billingType,
                    PaymentType = VM.PaymentType,
                    PurchaseType = VM.PurchaseType,
                    TotalCGstAmt = VM.TotalCGstAmt,
                    TotalSGstAmt = VM.TotalSGstAmt,
                    PaidAmount = VM.PaidAmount,
                    ReturnAmount = VM.ReturnAmount,
                    Balance = VM.Balance,
                    PurchaseOrderItems = VM.PurchaseOrderItemVMs.Select(x => new PurchaseOrderItem
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
                        salserateB = x.salserateB,
                        Barcode = x.Barcode,
                        CGst = x.CGst,
                        SGst = x.SGst,
                        CGstAmount = x.CGstAmount,
                        SGstAmount = x.SGstAmount
                    }).ToList() ?? new List<PurchaseOrderItem>(),
                    //PaymentDetails = VM.PaymentDetails?.Select(pd => new SalsePaymentDetails
                    //{
                    //    Id = pd.Id,
                    //    Date = DateTime.Now,
                    //    PaymentModeId = pd.PaymentModeId,
                    //    Amount = pd.Amount,
                    //    ReferenceNo = pd.ReferenceNo,
                    //    Description = pd.Description
                    //}).ToList() ?? new List<SalsePaymentDetails>()
                };
                await _purchaseOrderRepo.Create(model);

            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            PurchaseOrder model = await _purchaseOrderRepo.GetById(Id);
            PurchaseOrderVM purchaseVM = new PurchaseOrderVM();

            if (model != null)
            {
                purchaseVM.Id = model.Id;
                purchaseVM.SupplierId = model.SupplierId;

                // ✅ Supplier Name Set
                var suppliers = await _supplierRepo.GetALL();
                var supplier = suppliers.FirstOrDefault(x => x.Id == model.SupplierId);
                purchaseVM.SupplierName = supplier?.FirstName ?? "";

                purchaseVM.BillNo = model.BillNo;
                purchaseVM.BillDate = model.BillDate;
                purchaseVM.PartyBillNo = model.PartyBillNo;
                purchaseVM.PartyBillDate = model.PartyBillDate;
                purchaseVM.TotalGstAmt = model.TotalGstAmt;
                purchaseVM.discountPercent = model.discountPercent;
                purchaseVM.discountAmount = model.discountAmount;
                purchaseVM.Totaldiscount = model.Totaldiscount;
                purchaseVM.TotalPayable = model.TotalPayable;
                purchaseVM.RoundOffAmount = model.RoundOffAmount;
                purchaseVM.Total = model.Total;
                purchaseVM.billingType = model.billingType;
                purchaseVM.PaymentType = model.PaymentType;
                purchaseVM.PurchaseType = model.PurchaseType;
                purchaseVM.TotalCGstAmt = model.TotalCGstAmt;
                purchaseVM.TotalSGstAmt = model.TotalSGstAmt;
                purchaseVM.PaidAmount = model.PaidAmount;
                purchaseVM.ReturnAmount = model.ReturnAmount;
                purchaseVM.Balance = model.Balance;
                // Get all items for name lookup
                var allItems = await _itemMasterRepo.GetAll();

                purchaseVM.PurchaseOrderItemVMs = model.PurchaseOrderItems?.Select(x =>
                {
                    var item = allItems.FirstOrDefault(i => i.Id == x.ItemId);

                    return new PurchaseOrderItemVM
                    {
                        Id = x.Id,
                        ItemId = x.ItemId,
                        ItemName = item?.Name ?? "",
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
                        salserateB = x.salserateB,
                        Barcode = x.Barcode,
                        CGst = x.CGst,
                        SGst = x.SGst,
                        CGstAmount = x.CGstAmount,
                        SGstAmount = x.SGstAmount
                    };
                }).ToList() ?? new List<PurchaseOrderItemVM>();

                //purchaseVM.PaymentDetails = model.PaymentDetails?.Select(x => new SalsePaymentDetailsVM
                //{
                //    Id = x.Id,
                //    PaymentModeId = x.PaymentModeId,
                //    Amount = x.Amount,
                //    ReferenceNo = x.ReferenceNo,
                //    Description = x.Description

                //}).ToList() ?? new List<SalsePaymentDetailsVM>();
            }
            var tenantid = User.FindFirst("TenantId")?.Value;
            var tenantdata = await _tenantRepo.GetById(tenantid);
            // Check bussiness type 
            var businessType = tenantdata?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;
            ViewBag.supplier = new SelectList(await _supplierRepo.GetALL(), "Id", "FirstName");
            ViewBag.Item = new SelectList((await _itemMasterRepo.GetAll()).OrderBy(x => x.Name), "Id", "Name");
            ViewBag.Hsn = new SelectList(await _hSNRepo.GetAll(), "Id", "HsnCode");
           // ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            
            var tenantList = await _tenantRepo.GetAll();
            if (tenantdata != null)
            {
                var hoTenantId = string.IsNullOrEmpty(tenantdata.ParentTenantId) ? tenantdata.Id : tenantdata.ParentTenantId;
                var branches = tenantList.Where(t => 
                    (string.Equals(t.ParentTenantId, hoTenantId, StringComparison.OrdinalIgnoreCase) || string.Equals(t.Id, hoTenantId, StringComparison.OrdinalIgnoreCase)) && 
                    !string.Equals(t.Id, tenantid, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => t.Name)
                    .ToList();
                ViewBag.Branches = new SelectList(branches, "Id", "Name");
            }
            else
            {
                ViewBag.Branches = new SelectList(new List<AOne.Models.Entity.Tenant>(), "Id", "Name");
            }

            return View(purchaseVM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(PurchaseOrderVM VM)
        {
            PurchaseOrder model = await _purchaseOrderRepo.GetById(VM.Id);
            if (model != null)
            {
                if (model.WorkflowStatus == "Issued")
                {
                    TempData["ErrorMessage"] = "Cannot update an issued stock request.";
                    return RedirectToAction("Index");
                }
                model.Id = VM.Id;
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
                model.RoundOffAmount = VM.RoundOffAmount;
                model.Total = VM.Total;
                model.billingType = VM.billingType;
                model.PaymentType = VM.PaymentType;
                model.PurchaseType = VM.PurchaseType;
                model.TotalCGstAmt = VM.TotalCGstAmt;
                model.TotalSGstAmt = VM.TotalSGstAmt;
                model.PaidAmount = VM.PaidAmount;
                model.ReturnAmount = VM.ReturnAmount;
                model.Balance = VM.Balance;
                var removedItems = model.PurchaseOrderItems.Where(dbItem => !VM.PurchaseOrderItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

                // ❌ Remove Deleted Items
                foreach (var item in removedItems)
                {
                    model.PurchaseOrderItems.Remove(item);
                }
                foreach (var item in VM.PurchaseOrderItemVMs)
                {
                    if (item.Id > 0) // Update existing items
                    {
                        var existingItem = model.PurchaseOrderItems.FirstOrDefault(x => x.Id == item.Id);
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
                            existingItem.Barcode = item.Barcode;
                            existingItem.CGst = item.CGst;
                            existingItem.SGst = item.SGst;
                            existingItem.CGstAmount = item.CGstAmount;
                            existingItem.SGstAmount = item.SGstAmount;
                        }
                    }
                    else
                    {
                        model.PurchaseOrderItems.Add(new PurchaseOrderItem
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
                            salserateB = item.salserateB,
                            Barcode = item.Barcode,
                            CGst = item.CGst,
                            SGst = item.SGst,
                            CGstAmount = item.CGstAmount,
                            SGstAmount = item.SGstAmount,
                        });
                    }
                }

                //var removedpaymentItems = model.PaymentDetails.Where(dbItem => !VM.PaymentDetails.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

                //// ❌ Remove Deleted Items
                //foreach (var item in removedpaymentItems)
                //{
                //    model.PaymentDetails.Remove(item);
                //}
                //foreach (var item in VM.PaymentDetails)
                //{
                //    if (item.Id > 0) // Update existing items
                //    {
                //        var existingItem = model.PaymentDetails.FirstOrDefault(x => x.Id == item.Id);
                //        if (existingItem != null)
                //        {
                //            existingItem.PaymentModeId = item.PaymentModeId;
                //            existingItem.Amount = item.Amount;
                //            existingItem.ReferenceNo = item.ReferenceNo;
                //            existingItem.Description = item.Description;

                //        }
                //    }
                //    else
                //    {
                //        model.PaymentDetails.Add(new SalsePaymentDetails
                //        {
                //            Date = DateTime.Now,
                //            PaymentModeId = item.PaymentModeId,
                //            Amount = item.Amount,
                //            ReferenceNo = item.ReferenceNo,
                //            Description = item.Description
                //        });
                //    }
                //}
                await _purchaseOrderRepo.Update(model);
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

                var model = await _purchaseOrderRepo.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                if (model.WorkflowStatus == "Issued")
                {
                    return Json(new { success = false, message = "Cannot delete an issued stock request." });
                }

                await _purchaseOrderRepo.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        public async Task<string> GenerateNxtNumber()
        {
            var lastCode = (await _purchaseOrderRepo.GetAll())
                .Where(x => !string.IsNullOrWhiteSpace(x.BillNo))
                .OrderByDescending(x => x.Created)
                .Select(x => x.BillNo)
                .FirstOrDefault();

            return GenerateNextProductCode(lastCode);
        }

        private string GenerateNextProductCode(string lastCode)
        {
            if (string.IsNullOrWhiteSpace(lastCode))
                return "BL0001";

            // Last numeric sequence only
            var match = Regex.Match(lastCode, @"(\d+)(?!.*\d)");

            if (!match.Success)
                return "BL0001";

            if (!long.TryParse(match.Value, out long number))
            {
                return "BL0001";
            }

            string prefix = lastCode[..match.Index];
            string suffix = lastCode[(match.Index + match.Length)..];

            string nextNumber = (number + 1).ToString($"D{match.Length}");

            return $"{prefix}{nextNumber}{suffix}";
        }
        [HttpGet]
        public async Task<IActionResult> GeneratePoWithAI()
        {
            var tenantid = User.FindFirst("TenantId")?.Value;
            var tenantdata = await _tenantRepo.GetById(tenantid);
            ViewBag.supplier = new SelectList(await _supplierRepo.GetALL(), "Id", "FirstName");
            ViewBag.Item = new SelectList((await _itemMasterRepo.GetAll()).OrderBy(x => x.Name), "Id", "Name");
            ViewBag.Hsn = new SelectList(await _hSNRepo.GetAll(), "Id", "HsnCode"); 
            var model = new POWithAIVM()
            {
                
            };
            return View(model);
        }
        [HttpPost]
        public IActionResult GenerateReorder(POWithAIVM VM)
        {
            if (!ModelState.IsValid)
                return View(VM);
            var model = new POWithAI
            {

            };

            var result = _poWithAIRepo.Create(model);

            return View("ReorderResult", result);
        }
    }
}
