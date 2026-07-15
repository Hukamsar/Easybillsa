using AOne.DataAccess.ProfileService;
using ClosedXML.Excel;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Table = iText.Layout.Element.Table;
namespace EasyBill.UI.Controllers
{
    public class StockReceiveController : Controller
    {
        private readonly IStockReceiveRepository _stockreceiveservice;
        private readonly IProfileService _profileService;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IPurchaseItemRepository _purchaseitemservice;
        private readonly UserManager<ApplicationUsers> _usersManager;
        private readonly ITenantRegistrationRepository _tenantRepository;
        private readonly IStockService _currentStockService;
        private readonly ISupplierRepository _supplierservice;
        private readonly IModeOfPaymentRepository _modeofpaymentservice;
        private readonly ISalsePaymentDetailsRepository _paymentDetailsRepository;
        private readonly IStockIssueRepository _stockIssueService;
        public StockReceiveController(
            IStockReceiveRepository stockreceiveservice,
            IProfileService profileService,
            ICustomerRepository customerservice,
            IItemMasterRepository itemmasterservice,
            IPurchaseItemRepository purchaseitemservice,
            UserManager<ApplicationUsers> userManager,
            ITenantRegistrationRepository tenantRepository,
            IStockService currentStockService,
            ISupplierRepository supplierservice,
            IModeOfPaymentRepository modeofpaymentservice,
            ISalsePaymentDetailsRepository paymentDetailsRepository,
            IStockIssueRepository stockIssueService
            )
        {
            _stockreceiveservice = stockreceiveservice;
            _profileService = profileService;
            _customerservice = customerservice;
            _itemmasterservice = itemmasterservice;
            _purchaseitemservice = purchaseitemservice;
            _usersManager = userManager;
            _tenantRepository=tenantRepository;
            _currentStockService = currentStockService;
            _supplierservice = supplierservice;
            _modeofpaymentservice = modeofpaymentservice;
            _paymentDetailsRepository = paymentDetailsRepository;
            _stockIssueService = stockIssueService;
        }
        public async Task<IActionResult> Index()
        {
            await _profileService.Set(User);
            var data = await _stockreceiveservice.GetAll();

            var tenantId = User.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                var pendingTransfers = await _stockIssueService.GetPendingTransfersForBranch(tenantId);
                ViewBag.PendingTransfers = pendingTransfers;
            }
            else
            {
                ViewBag.PendingTransfers = new List<StockIssue>();
            }

            var allTenants = await _tenantRepository.GetAll();
            ViewBag.Tenants = allTenants.ToDictionary(t => t.Id, t => t);

            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create(string? returnUrl, int? pendingIssueId)
        {
            ViewBag.ReturnUrl=returnUrl;
            ViewBag.AutoLoadIssueId = pendingIssueId;
            var tenantId = User.FindFirst("TenantId")?.Value;
            var tenant = string.IsNullOrWhiteSpace(tenantId) ? null : await _tenantRepository.GetById(tenantId);

            ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            ViewBag.Hsn = new SelectList(Enumerable.Empty<SelectListItem>(), "Value", "Text");
            ViewBag.BusinessType = (int)(tenant?.BusinessType ?? 0);

            // Fetch related branches for branch transfer
            var tenantList = await _tenantRepository.GetAll();
            if (tenant != null)
            {
                var hoTenantId = string.IsNullOrEmpty(tenant.ParentTenantId) ? tenant.Id : tenant.ParentTenantId;
                var branches = tenantList.Where(t => 
                    (t.ParentTenantId == hoTenantId || t.Id == hoTenantId) && 
                    t.Id != tenantId)
                    .OrderBy(t => t.Name)
                    .ToList();
                ViewBag.Branches = new SelectList(branches, "Id", "Name");
            }
            else
            {
                ViewBag.Branches = new SelectList(new List<AOne.Models.Entity.Tenant>(), "Id", "Name");
            }

            var viewModel = new StockReceiveVM
            {
                ChallanDate = DateTime.Now,
                PartyBillDate = DateTime.Now,
                ChallanNo = await GenerateNxtNumber(),
                PurchaseType = "Local",
                TenanatGst = tenant?.GstNo,
                billingType = "walkin",
                PaymentType = "cash",
                TaxCalculation = "Yes"
            };

            return View(viewModel);
        }
        public async Task<string> GenerateNxtNumber()
        {
            var nextNumber = (await _stockreceiveservice.GetAll())
                .Where(x => !string.IsNullOrWhiteSpace(x.ChallanNo))
                .Select(x =>
                {
                    var digits = new string((x.ChallanNo ?? string.Empty)
                        .Trim()
                        .Reverse()
                        .TakeWhile(char.IsDigit)
                        .Reverse()
                        .ToArray());

                    return int.TryParse(digits, out var number) ? number : 0;
                })
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"SR{nextNumber:D4}";
        }
        [HttpGet]
        public async Task<IActionResult> IsBillNumberDuplicate(string billNumber, int id = 0)
        {
            var normalizedBillNumber = billNumber?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedBillNumber))
            {
                return Json(new { isDuplicate = false });
            }

            var exists = (await _stockreceiveservice.GetAll())
                .Any(x => x.Id != id
                    && !string.IsNullOrWhiteSpace(x.ChallanNo)
                    && string.Equals(x.ChallanNo.Trim(), normalizedBillNumber, StringComparison.OrdinalIgnoreCase));

            return Json(new { isDuplicate = exists });
        }
        [HttpPost]
        public async Task<IActionResult> Create(StockReceiveVM Vm, string? returnUrl)
        {
            try
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

                    return Json(new
                    {
                        success = false,
                        message = errors.FirstOrDefault()?.Message ?? "Validation failed. Please check the form.",
                        errors
                    });
                }

                if (string.IsNullOrWhiteSpace(Vm.PartyBillNo))
                {
                    return Json(new { success = false, message = "Party Bill No is required." });
                }

                if (!Vm.PartyBillDate.HasValue)
                {
                    return Json(new { success = false, message = "Party Bill Date is required." });
                }

                Vm.ChallanNo = Vm.ChallanNo?.Trim();
                if (string.IsNullOrWhiteSpace(Vm.ChallanNo))
                {
                    return Json(new { success = false, message = "Challan number is required." });
                }

                var duplicateChallan = (await _stockreceiveservice.GetAll())
                    .Any(x => !string.IsNullOrWhiteSpace(x.ChallanNo)
                        && string.Equals(x.ChallanNo.Trim(), Vm.ChallanNo, StringComparison.OrdinalIgnoreCase));

                if (duplicateChallan)
                {
                    return Json(new { success = false, message = "Challan number already exists." });
                }

                Vm.TaxCalculation = string.Equals(Vm.TaxCalculation, "No", StringComparison.OrdinalIgnoreCase) ? "No" : "Yes";
                Vm.billingType = string.Equals(Vm.billingType, "registered", StringComparison.OrdinalIgnoreCase) ? "registered" : "walkin";
                Vm.PaymentType = string.IsNullOrWhiteSpace(Vm.PaymentType) ? "cash" : Vm.PaymentType;
                Vm.PurchaseType = string.IsNullOrWhiteSpace(Vm.PurchaseType) ? "Local" : Vm.PurchaseType;
                Vm.StockReceiveItemVMs ??= new List<StockReceiveItemVM>();

                var items = new List<StockReceiveItemVM>();
                for (int i = 0; i < Vm.StockReceiveItemVMs.Count; i++)
                {
                    var item = Vm.StockReceiveItemVMs[i];
                    bool hasAnyItemData =
                        item.ItemMasterId > 0
                        || (item.PurchaseItemId ?? 0) > 0
                        || !string.IsNullOrWhiteSpace(item.Batch)
                        || item.Expirydate.HasValue
                        || item.Mrp > 0
                        || item.Qty > 0
                        || item.Rate > 0
                        || item.Amount > 0
                        || item.SalesRateA > 0
                        || (item.SalesRateB ?? 0) > 0
                        || !string.IsNullOrWhiteSpace(item.Barcode);

                    if (!hasAnyItemData)
                    {
                        continue;
                    }

                    var rowNumber = i + 1;
                    if (item.ItemMasterId <= 0)
                    {
                        return Json(new { success = false, message = $"Please select an item on row {rowNumber}." });
                    }

                    if (string.IsNullOrWhiteSpace(item.Batch))
                    {
                        return Json(new { success = false, message = $"Please enter batch on row {rowNumber}." });
                    }

                    if (item.Mrp <= 0)
                    {
                        return Json(new { success = false, message = $"Please enter MRP on row {rowNumber}." });
                    }

                    if (item.Qty <= 0)
                    {
                        return Json(new { success = false, message = $"Please enter quantity on row {rowNumber}." });
                    }

                    if (item.Rate < 0)
                    {
                        return Json(new { success = false, message = $"Please enter a valid rate on row {rowNumber}." });
                    }

                    item.Batch = item.Batch?.Trim();
                    items.Add(item);
                }

                if (!items.Any())
                {
                    return Json(new { success = false, message = "Please add at least one valid item." });
                }

                var paymentDetails = (Vm.PaymentDetails ?? new List<SalsePaymentDetailsVM>())
                    .Where(x => x.Amount > 0
                        || x.PaymentModeId > 0
                        || !string.IsNullOrWhiteSpace(x.ReferenceNo)
                        || !string.IsNullOrWhiteSpace(x.Description))
                    .ToList();

                if (paymentDetails.Any(x => x.PaymentModeId <= 0))
                {
                    return Json(new { success = false, message = "Please select payment mode for each payment row." });
                }

                paymentDetails = paymentDetails.Where(x => x.Amount > 0).ToList();

                Supplier? supplier = null;

                bool isRegisteredSupplier = Vm.billingType == "registered" && Vm.ReceiveFromType != "Branch";

                if (isRegisteredSupplier)
                {
                    if (!Vm.SupplierId.HasValue || Vm.SupplierId.Value <= 0)
                    {
                        return Json(new { success = false, message = "Please select a supplier." });
                    }

                    supplier = await _supplierservice.GetBySupplierId(Vm.SupplierId);
                    if (supplier == null)
                    {
                        return Json(new { success = false, message = "Selected supplier was not found." });
                    }
                }

                var paidAmount = paymentDetails.Sum(x => x.Amount);
                var totalPayable = Vm.TotalPayable;
                var returnAmount = Math.Max(0, paidAmount - totalPayable);
                var balance = paidAmount > totalPayable ? 0 : totalPayable - paidAmount;

                var model = new StockReceive
                {
                    CustomerId = null,
                    SupplierId = isRegisteredSupplier ? Vm.SupplierId : null,
                    ChallanDate = Vm.ChallanDate ?? DateTime.Now,
                    ChallanNo = Vm.ChallanNo,
                    PartyBillNo = Vm.PartyBillNo?.Trim(),
                    PartyBillDate = Vm.PartyBillDate,
                    MobileNo = isRegisteredSupplier
                        ? (string.IsNullOrWhiteSpace(Vm.MobileNo) ? supplier?.PhoneNO : Vm.MobileNo)
                        : null,
                    Address = isRegisteredSupplier
                        ? (string.IsNullOrWhiteSpace(Vm.Address) ? supplier?.Address : Vm.Address)
                        : null,
                    PharmacyDoctorId = Vm.PharmacyDoctorId,
                    DoctorMobileNumber = Vm.DoctorMobileNumber,
                    DoctorRegNumber = Vm.DoctorRegNumber,
                    Total = Vm.Total,
                    TotalGstAmt = Vm.TaxCalculation == "No" ? 0 : Vm.TotalGstAmt,
                    discountPercent = Vm.discountPercent,
                    discountAmount = Vm.discountAmount,
                    Totaldiscount = Vm.Totaldiscount,
                    TotalPayable = Vm.TotalPayable,
                    RoundOffAmount = Vm.RoundOffAmount,
                    PaidAmount = paidAmount,
                    ReturnAmount = returnAmount,
                    Balance = balance,
                    TotalCessAmount = Vm.TotalCessAmount,
                    billingType = Vm.billingType,
                    PaymentType = Vm.PaymentType,
                    TaxCalculation = Vm.TaxCalculation,
                    SourceStockIssueId = Vm.SourceStockIssueId,
                    TransferFromTenantId = Vm.TransferFromTenantId,
                    IsPendingTransfer = false, // Always false once it is actually received and saved
                    StockReceiveItems = items.Select(x => new StockReceiveItem
                    {
                        ItemMasterId = x.ItemMasterId,
                        PurchaseItemId = x.PurchaseItemId,
                        Batch = x.Batch?.Trim(),
                        Expirydate = x.Expirydate,
                        Mrp = x.Mrp,
                        Qty = x.Qty,
                        Rate = x.Rate,
                        Gst = Vm.TaxCalculation == "No" ? 0 : x.Gst,
                        Cess = x.Cess,
                        Discount = x.Discount,
                        Amount = x.Amount
                    }).ToList(),
                    PaymentDetails = paymentDetails.Select(pd => new SalsePaymentDetails
                    {
                        Date = DateTime.Now,
                        PaymentModeId = pd.PaymentModeId,
                        Amount = pd.Amount,
                        ReferenceNo = pd.ReferenceNo,
                        Description = pd.Description
                    }).ToList()
                };

                await _stockreceiveservice.Create(model);

                // If this receive is satisfying a pending branch transfer, mark the issue as received
                if (Vm.SourceStockIssueId.HasValue && Vm.SourceStockIssueId.Value > 0)
                {
                    var sourceIssue = await _stockIssueService.GetByIdBypassTenant(Vm.SourceStockIssueId.Value);
                    if (sourceIssue != null && !sourceIssue.IsReceived)
                    {
                        sourceIssue.IsReceived = true;
                        sourceIssue.TransferStatus = "Accepted";
                        await _stockIssueService.Update(sourceIssue);
                    }
                }

                foreach (var item in items)
                {
                    await _currentStockService.UpdateStock(
                        item.ItemMasterId,
                        item.Batch?.Trim() ?? "",
                        item.Qty,
                        item.Expirydate,
                        item.Mrp,
                        item.SalesRateA > 0 ? item.SalesRateA : item.Rate,
                        item.SalesRateB,
                        item.Rate,
                        item.Barcode
                    );
                }

                //return Json(new { success = true, id = model.Id });
                return Json(new
                {
                    success = true,
                    redirectUrl = !string.IsNullOrEmpty(returnUrl)
        ? returnUrl
        : Url.Action("Index")
                });
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }

                return Json(new { success = false, message = $"Unable to save stock receive. {ex.Message}" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> RejectTransfer(int issueId)
        {
            try
            {
                var issue = await _stockIssueService.GetByIdBypassTenant(issueId);
                if (issue == null)
                {
                    return Json(new { success = false, message = "Transfer not found." });
                }

                issue.TransferStatus = "Rejected";
                await _stockIssueService.Update(issue);

                // Revert stock for the issuing branch
                if (issue.StockIssuesItems != null && !string.IsNullOrEmpty(issue.TenantId))
                {
                    foreach (var item in issue.StockIssuesItems)
                    {
                        // Add stock back to the issuing branch
                        await _currentStockService.UpdateStock(
                            item.ItemMasterId,
                            item.Batch?.Trim() ?? "",
                            item.Qty, // Positive quantity to add back
                            item.Expirydate,
                            item.Mrp,
                            item.Rate, // SalesRateA fallback
                            null, // SalesRateB
                            item.Rate, // purchaseRate fallback
                            null, // Barcode
                            false,
                            issue.TenantId // Override TenantId to update issuing branch's stock
                        );
                    }
                }

                return Json(new { success = true, message = "Transfer rejected successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Unable to reject transfer. {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemDetails(int itemId)
        {
            if (itemId <= 0)
            {
                return Json(new { error = "Item details not found." });
            }

            var itemMaster = await _itemmasterservice.GetByItemMasterId(itemId);
            if (itemMaster == null)
            {
                return Json(new { error = "Item details not found." });
            }

            var purchaseItems = await _purchaseitemservice.GetByItemMasterId(itemId);
            return Json(new
            {
                gst = itemMaster.Hsn?.IGST ?? 0,
                cess = itemMaster.Hsn?.Cess ?? 0,
                purchaseItems = purchaseItems.Select(x => new
                {
                    id = x.Id,
                    batch = x.Batch,
                    qty = x.Qty,
                    rate = x.Rate,
                    mrp = x.Mrp,
                    expirydate = x.ExpiryDate,
                    salesRateA = x.salserateA,
                    salesRateB = x.salserateB,
                    barcode = x.Barcode
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingBranchTransfers()
        {
            var currentUser = await _usersManager.GetUserAsync(User);
            var tenantId = currentUser?.TenantId;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Json(new { success = false, message = "Tenant not found." });
            }

            // A SuperAdmin has TenantId = null and can see everything, but realistically 
            // a branch transfer is meant for a specific branch. If SA is acting on behalf of a branch,
            // they would switch tenants. So we match TransferToTenantId exactly.
            var pendingIssues = (await _stockIssueService.GetPendingTransfersForBranch(tenantId))
                .Select(x => new
                {
                    Id = x.Id,
                    ChallanNo = x.ChallanNo,
                    ChallanDate = x.ChallanDate?.ToString("dd-MMM-yyyy"),
                    TotalAmount = x.TotalPayable,
                    IssuingBranch = x.Tenant?.Name ?? "Head Office"
                }).ToList();

            return Json(new { success = true, data = pendingIssues });
        }

        [HttpGet]
        public async Task<IActionResult> GetStockIssueDetails(int issueId)
        {
            var issue = await _stockIssueService.GetByIdBypassTenant(issueId);
            if (issue == null || issue.IsReceived)
            {
                return Json(new { success = false, message = "Issue not found or already received." });
            }

            var items = issue.StockIssuesItems.Select(x => new
            {
                itemId = x.ItemMasterId,
                itemName = x.ItemMaster?.Name,
                qty = x.Qty,
                freeQty = 0, // No FreeQty in StockIssueItem
                rate = x.Rate,
                mrp = x.Mrp,
                discountPercent = x.Discount,
                taxPercent = x.Gst,
                taxAmount = (x.Amount * x.Gst) / (100 + x.Gst), // Rough estimate, or we can just let JS recalculate it
                totalAmount = x.Amount,
                batch = x.Batch,
                expiryDate = x.Expirydate?.ToString("MM/yyyy")
            }).ToList();

            return Json(new { 
                success = true, 
                data = new {
                    issueId = issue.Id,
                    challanNo = issue.ChallanNo,
                    challanDate = issue.ChallanDate?.ToString("yyyy-MM-dd"),
                    total = issue.Total,
                    totalGstAmt = issue.TotalGstAmt,
                    totalPayable = issue.TotalPayable,
                    discountPercent = issue.discountPercent,
                    discountAmount = issue.discountAmount,
                    roundOffAmount = issue.RoundOffAmount,
                    transferFromTenantId = issue.TenantId,
                    items = items
                } 
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetSupplierDetails(int supplierId)
        {
            if (supplierId <= 0)
            {
                return Json(new { success = false, message = "Invalid supplier." });
            }

            var supplier = await _supplierservice.GetBySupplierId(supplierId);
            if (supplier == null)
            {
                return Json(new { success = false, message = "Supplier not found." });
            }

            return Json(new
            {
                success = true,
                phoneNo = supplier.PhoneNO,
                address = supplier.Address,
                gstNo = supplier.GstNO
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetPurchaseItemDetails(int Id)
        {
            if (Id > 0)
            {
                var purchasedata = await _purchaseitemservice.GetById(Id);
                if (purchasedata != null)
                {
                    var model = new
                    {
                        qty = purchasedata.Qty,
                        rate = purchasedata.Rate,
                        mrp = purchasedata.Mrp,
                        expirydate = purchasedata.ExpiryDate
                    };
                    return Json(model);
                }
                else
                {
                    return Json(new { error = "Hsn Details not found." });
                }

            }
            else
            {
                return Json(new { error = "Hsn Details not found." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            var model = await _stockreceiveservice.GetById(Id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }

            var currentStocks = await _currentStockService.GetAll();
            var billingType = string.Equals(model.billingType, "registered", StringComparison.OrdinalIgnoreCase)
                ? "registered"
                : "walkin";

            var vm = new StockReceiveVM
            {
                Id = model.Id,
                CustomerId = model.CustomerId,
                CustomerName = model.Customers?.Name,
                SupplierId = model.SupplierId,
                SupplierName = billingType == "registered"
                    ? (model.Supplier?.FirstName ?? model.Customers?.Name)
                    : string.Empty,
                ChallanNo = model.ChallanNo,
                ChallanDate = model.ChallanDate,
                PartyBillNo = model.PartyBillNo,
                PartyBillDate = model.PartyBillDate,
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
                RoundOffAmount = model.RoundOffAmount,
                PaidAmount = model.PaidAmount,
                ReturnAmount = model.ReturnAmount,
                Balance = model.Balance,
                TotalCessAmount = model.TotalCessAmount,
                PurchaseType = "Local",
                billingType = billingType,
                PaymentType = string.IsNullOrWhiteSpace(model.PaymentType) ? "cash" : model.PaymentType,
                TaxCalculation = string.Equals(model.TaxCalculation, "No", StringComparison.OrdinalIgnoreCase) ? "No" : "Yes",
                StockReceiveItemVMs = model.StockReceiveItems?.Select(x => new StockReceiveItemVM
                {
                    SalesRateA = currentStocks
                        .Where(stock => stock.ItemId == x.ItemMasterId
                            && stock.Batch == x.Batch
                            && stock.ExpiryDate == x.Expirydate
                            && stock.Mrp == x.Mrp)
                        .OrderByDescending(stock => stock.Id)
                        .Select(stock => stock.SalesRateA)
                        .FirstOrDefault(),
                    SalesRateB = currentStocks
                        .Where(stock => stock.ItemId == x.ItemMasterId
                            && stock.Batch == x.Batch
                            && stock.ExpiryDate == x.Expirydate
                            && stock.Mrp == x.Mrp)
                        .OrderByDescending(stock => stock.Id)
                        .Select(stock => (decimal?)stock.SalesRateB)
                        .FirstOrDefault(),
                    Barcode = currentStocks
                        .Where(stock => stock.ItemId == x.ItemMasterId
                            && stock.Batch == x.Batch
                            && stock.ExpiryDate == x.Expirydate
                            && stock.Mrp == x.Mrp)
                        .OrderByDescending(stock => stock.Id)
                        .Select(stock => stock.Barcode)
                        .FirstOrDefault(),
                    Id = x.Id,
                    StockReceiveId = x.StockReceiveId,
                    ItemMasterId = x.ItemMasterId,
                    ItemName = x.ItemMaster?.Name,
                    HsnId = x.ItemMaster?.HsnId,
                    CGst = x.ItemMaster?.Hsn?.CGST ?? 0,
                    SGst = x.ItemMaster?.Hsn?.SGST ?? 0,
                    SCGst = (x.ItemMaster?.Hsn?.CGST ?? 0) + (x.ItemMaster?.Hsn?.SGST ?? 0),
                    PurchaseItemId = x.PurchaseItemId,
                    Batch = x.Batch,
                    Expirydate = x.Expirydate,
                    Mrp = x.Mrp,
                    FreeQty = 0,
                    Qty = x.Qty,
                    Rate = x.Rate,
                    Gst = x.Gst,
                    Cess = x.Cess,
                    Discount = x.Discount,
                    Amount = x.Amount,
                    BatchWiseCose = 0
                }).ToList() ?? new List<StockReceiveItemVM>(),
                PaymentDetails = model.PaymentDetails?.Select(x => new SalsePaymentDetailsVM
                {
                    Id = x.Id,
                    PaymentModeId = x.PaymentModeId,
                    PaymentModeName = x.ModeOfPayment?.Name,
                    Amount = x.Amount,
                    ReferenceNo = x.ReferenceNo,
                    Description = x.Description,
                    StockReceiveId = x.StockReceiveId
                }).ToList() ?? new List<SalsePaymentDetailsVM>()
            };

            var tenantId = User.FindFirst("TenantId")?.Value;
            var tenant = string.IsNullOrWhiteSpace(tenantId) ? null : await _tenantRepository.GetById(tenantId);
            vm.TenanatGst = tenant?.GstNo;

            ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            ViewBag.Hsn = new SelectList(Enumerable.Empty<SelectListItem>(), "Value", "Text");
            ViewBag.BusinessType = (int)(tenant?.BusinessType ?? 0);

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(StockReceiveVM VM)
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

                return Json(new
                {
                    success = false,
                    message = errors.FirstOrDefault()?.Message ?? "Validation failed. Please check the form.",
                    errors
                });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(VM.PartyBillNo))
                {
                    return Json(new { success = false, message = "Party Bill No is required." });
                }

                if (!VM.PartyBillDate.HasValue)
                {
                    return Json(new { success = false, message = "Party Bill Date is required." });
                }

                var model = await _stockreceiveservice.GetById(VM.Id);
                if (model == null)
                {
                    return Json(new { success = false, message = "Stock receive record not found." });
                }

                if (model.SourceStockIssueId != null && model.SourceStockIssueId > 0)
                {
                    return Json(new { success = false, message = "Cannot edit a stock receipt that was generated from a branch transfer." });
                }

                VM.ChallanNo = VM.ChallanNo?.Trim();
                if (string.IsNullOrWhiteSpace(VM.ChallanNo))
                {
                    return Json(new { success = false, message = "Challan number is required." });
                }

                var duplicateChallan = (await _stockreceiveservice.GetAll())
                    .Any(x => x.Id != VM.Id
                        && !string.IsNullOrWhiteSpace(x.ChallanNo)
                        && string.Equals(x.ChallanNo.Trim(), VM.ChallanNo, StringComparison.OrdinalIgnoreCase));

                if (duplicateChallan)
                {
                    return Json(new { success = false, message = "Challan number already exists." });
                }

                VM.TaxCalculation = string.Equals(VM.TaxCalculation, "No", StringComparison.OrdinalIgnoreCase) ? "No" : "Yes";
                VM.billingType = string.Equals(VM.billingType, "registered", StringComparison.OrdinalIgnoreCase) ? "registered" : "walkin";
                VM.PaymentType = string.IsNullOrWhiteSpace(VM.PaymentType) ? "cash" : VM.PaymentType;
                VM.PurchaseType = string.IsNullOrWhiteSpace(VM.PurchaseType) ? "Local" : VM.PurchaseType;
                VM.StockReceiveItemVMs ??= new List<StockReceiveItemVM>();

                var items = new List<StockReceiveItemVM>();
                for (int i = 0; i < VM.StockReceiveItemVMs.Count; i++)
                {
                    var item = VM.StockReceiveItemVMs[i];
                    bool hasAnyItemData =
                        item.ItemMasterId > 0
                        || (item.PurchaseItemId ?? 0) > 0
                        || !string.IsNullOrWhiteSpace(item.Batch)
                        || item.Expirydate.HasValue
                        || item.Mrp > 0
                        || item.Qty > 0
                        || item.Rate > 0
                        || item.Amount > 0
                        || item.SalesRateA > 0
                        || (item.SalesRateB ?? 0) > 0
                        || !string.IsNullOrWhiteSpace(item.Barcode);

                    if (!hasAnyItemData)
                    {
                        continue;
                    }

                    var rowNumber = i + 1;
                    if (item.ItemMasterId <= 0)
                    {
                        return Json(new { success = false, message = $"Please select an item on row {rowNumber}." });
                    }

                    if (string.IsNullOrWhiteSpace(item.Batch))
                    {
                        return Json(new { success = false, message = $"Please enter batch on row {rowNumber}." });
                    }

                    if (item.Mrp <= 0)
                    {
                        return Json(new { success = false, message = $"Please enter MRP on row {rowNumber}." });
                    }

                    if (item.Qty <= 0)
                    {
                        return Json(new { success = false, message = $"Please enter quantity on row {rowNumber}." });
                    }

                    if (item.Rate < 0)
                    {
                        return Json(new { success = false, message = $"Please enter a valid rate on row {rowNumber}." });
                    }

                    item.Batch = item.Batch?.Trim();
                    items.Add(item);
                }

                if (!items.Any())
                {
                    return Json(new { success = false, message = "Please add at least one valid item." });
                }

                var paymentDetails = (VM.PaymentDetails ?? new List<SalsePaymentDetailsVM>())
                    .Where(x => x.Amount > 0
                        || x.PaymentModeId > 0
                        || !string.IsNullOrWhiteSpace(x.ReferenceNo)
                        || !string.IsNullOrWhiteSpace(x.Description))
                    .ToList();

                if (paymentDetails.Any(x => x.PaymentModeId <= 0))
                {
                    return Json(new { success = false, message = "Please select payment mode for each payment row." });
                }

                paymentDetails = paymentDetails.Where(x => x.Amount > 0).ToList();

                Supplier? supplier = null;

                bool isRegisteredSupplier = VM.billingType == "registered" && VM.ReceiveFromType != "Branch";

                if (isRegisteredSupplier)
                {
                    if (!VM.SupplierId.HasValue || VM.SupplierId.Value <= 0)
                    {
                        return Json(new { success = false, message = "Please select a supplier." });
                    }

                    supplier = await _supplierservice.GetBySupplierId(VM.SupplierId);
                    if (supplier == null)
                    {
                        return Json(new { success = false, message = "Selected supplier was not found." });
                    }
                }

                var paidAmount = paymentDetails.Sum(x => x.Amount);
                var totalPayable = VM.TotalPayable;
                var returnAmount = Math.Max(0, paidAmount - totalPayable);
                var balance = paidAmount > totalPayable ? 0 : totalPayable - paidAmount;

                model.CustomerId = null;
                model.SupplierId = isRegisteredSupplier ? VM.SupplierId : null;
                model.ChallanDate = VM.ChallanDate ?? model.ChallanDate ?? DateTime.Now;
                model.ChallanNo = VM.ChallanNo;
                model.PartyBillNo = VM.PartyBillNo?.Trim();
                model.PartyBillDate = VM.PartyBillDate;
                model.MobileNo = isRegisteredSupplier
                    ? (string.IsNullOrWhiteSpace(VM.MobileNo) ? supplier?.PhoneNO : VM.MobileNo)
                    : null;
                model.Address = isRegisteredSupplier
                    ? (string.IsNullOrWhiteSpace(VM.Address) ? supplier?.Address : VM.Address)
                    : null;
                model.PharmacyDoctorId = VM.PharmacyDoctorId;
                model.DoctorMobileNumber = VM.DoctorMobileNumber;
                model.DoctorRegNumber = VM.DoctorRegNumber;
                model.Total = VM.Total;
                model.TotalGstAmt = VM.TaxCalculation == "No" ? 0 : VM.TotalGstAmt;
                model.discountPercent = VM.discountPercent;
                model.discountAmount = VM.discountAmount;
                model.Totaldiscount = VM.Totaldiscount;
                model.TotalPayable = VM.TotalPayable;
                model.RoundOffAmount = VM.RoundOffAmount;
                model.PaidAmount = paidAmount;
                model.ReturnAmount = returnAmount;
                model.Balance = balance;
                model.TotalCessAmount = VM.TotalCessAmount;
                model.billingType = VM.billingType;
                model.PaymentType = VM.PaymentType;
                model.TaxCalculation = VM.TaxCalculation;
                
                // If it was a pending branch transfer, mark it as received
                if (model.IsPendingTransfer)
                {
                    model.IsPendingTransfer = false;
                }

                model.StockReceiveItems ??= new List<StockReceiveItem>();
                var removedItems = model.StockReceiveItems
                    .Where(dbItem => !items.Any(vmItem => vmItem.Id == dbItem.Id))
                    .ToList();

                foreach (var item in removedItems)
                {
                    var stockLayer = await _currentStockService.GetStock(
                        item.ItemMasterId,
                        item.Batch?.Trim() ?? "",
                        item.Expirydate,
                        item.Mrp);

                    await _currentStockService.UpdateStock(
                        item.ItemMasterId,
                        item.Batch?.Trim() ?? "",
                        -item.Qty,
                        item.Expirydate,
                        item.Mrp,
                        stockLayer?.SalesRateA ?? item.Rate,
                        stockLayer?.SalesRateB,
                        item.Rate,
                        stockLayer?.Barcode
                    );
                    model.StockReceiveItems.Remove(item);
                }

                foreach (var item in items)
                {
                    if (item.Id > 0)
                    {
                        var existingItem = model.StockReceiveItems.FirstOrDefault(x => x.Id == item.Id);
                        if (existingItem == null)
                        {
                            continue;
                        }

                        await _currentStockService.UpdateStock(
                            existingItem.ItemMasterId,
                            existingItem.Batch?.Trim() ?? "",
                            -existingItem.Qty,
                            existingItem.Expirydate,
                            existingItem.Mrp,
                            item.SalesRateA > 0 ? item.SalesRateA : existingItem.Rate,
                            item.SalesRateB,
                            existingItem.Rate,
                            item.Barcode
                        );

                        await _currentStockService.UpdateStock(
                            item.ItemMasterId,
                            item.Batch?.Trim() ?? "",
                            item.Qty,
                            item.Expirydate,
                            item.Mrp,
                            item.SalesRateA > 0 ? item.SalesRateA : item.Rate,
                            item.SalesRateB,
                            item.Rate,
                            item.Barcode
                        );

                        existingItem.ItemMasterId = item.ItemMasterId;
                        existingItem.PurchaseItemId = item.PurchaseItemId;
                        existingItem.Batch = item.Batch?.Trim();
                        existingItem.Expirydate = item.Expirydate;
                        existingItem.Mrp = item.Mrp;
                        existingItem.Qty = item.Qty;
                        existingItem.Rate = item.Rate;
                        existingItem.Gst = VM.TaxCalculation == "No" ? 0 : item.Gst;
                        existingItem.Cess = item.Cess;
                        existingItem.Discount = item.Discount;
                        existingItem.Amount = item.Amount;
                    }
                    else
                    {
                        await _currentStockService.UpdateStock(
                            item.ItemMasterId,
                            item.Batch?.Trim() ?? "",
                            item.Qty,
                            item.Expirydate,
                            item.Mrp,
                            item.SalesRateA > 0 ? item.SalesRateA : item.Rate,
                            item.SalesRateB,
                            item.Rate,
                            item.Barcode
                        );

                        model.StockReceiveItems.Add(new StockReceiveItem
                        {
                            ItemMasterId = item.ItemMasterId,
                            PurchaseItemId = item.PurchaseItemId,
                            Batch = item.Batch?.Trim(),
                            Expirydate = item.Expirydate,
                            Mrp = item.Mrp,
                            Qty = item.Qty,
                            Rate = item.Rate,
                            Gst = VM.TaxCalculation == "No" ? 0 : item.Gst,
                            Cess = item.Cess,
                            Discount = item.Discount,
                            Amount = item.Amount
                        });
                    }
                }

                model.PaymentDetails ??= new List<SalsePaymentDetails>();
                var removedPaymentItems = model.PaymentDetails
                    .Where(dbItem => !paymentDetails.Any(vmItem => vmItem.Id == dbItem.Id))
                    .ToList();

                foreach (var item in removedPaymentItems)
                {
                    model.PaymentDetails.Remove(item);
                }

                foreach (var item in paymentDetails)
                {
                    if (item.Id > 0)
                    {
                        var existingItem = model.PaymentDetails.FirstOrDefault(x => x.Id == item.Id);
                        if (existingItem == null)
                        {
                            continue;
                        }

                        existingItem.PaymentModeId = item.PaymentModeId;
                        existingItem.Amount = item.Amount;
                        existingItem.ReferenceNo = item.ReferenceNo;
                        existingItem.Description = item.Description;
                    }
                    else
                    {
                        model.PaymentDetails.Add(new SalsePaymentDetails
                        {
                            Date = DateTime.Now,
                            PaymentModeId = item.PaymentModeId,
                            Amount = item.Amount,
                            ReferenceNo = item.ReferenceNo,
                            Description = item.Description
                        });
                    }
                }

                await _stockreceiveservice.Update(model);
                return Json(new { success = true, id = VM.Id });
            }
            catch (Exception ex)
            { 
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }

                return Json(new { success = false, message = $"Unable to update stock receive. {ex.Message}" });
            }
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

                var model = await _stockreceiveservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                if (model.SourceStockIssueId != null && model.SourceStockIssueId > 0)
                {
                    return Json(new { success = false, message = "Cannot delete a stock receipt that was generated from a branch transfer." });
                }

                await _stockreceiveservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> StockreceiveReportItemWise(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            var itemMasters = await _itemmasterservice.GetAll();
            var Stockreceive = (await _stockreceiveservice.GetAll())
                .Where(s => s.ChallanDate >= fromDate && s.ChallanDate <= toDate);
            // Flatten all SalesItems
            var stockreceiveItems = Stockreceive
                .Where(s => s.StockReceiveItems != null)
                .SelectMany(s => s.StockReceiveItems);

            // Group by ItemMasterId to calculate total sales
            var groupedStockreceive = stockreceiveItems
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
                                      decimal cessPercent = x.Cess;

                                      decimal gross = rate * qty;
                                      decimal discountAmt = gross * (discountPercent / 100);
                                      decimal taxable = gross - discountAmt;

                                      decimal gstAmt = taxable * (gstPercent / 100);
                                      decimal cessAmt = cessPercent > 0 ? taxable * (cessPercent / 100) : 0;

                                 
                                      return gstAmt + cessAmt;
                                  })
                              })
                              .ToList();

            var stockreceiveList = (from item in itemMasters
                                    join g in groupedStockreceive on item.Id equals g.ItemMasterId
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

            ViewBag.TotalQty = stockreceiveList.Sum(x => x.Stocks);
            ViewBag.TotalAmount = stockreceiveList.Sum(x => x.Amount);
            ViewBag.GstAmount = stockreceiveList.Sum(x => x.GstAmount);
            return View(stockreceiveList);
        }
        //Export to Excel
        [HttpGet]
        public async Task<IActionResult> ExportStockReceiveItemWiseExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();

            var stockReceive = (await _stockreceiveservice.GetAll())
                .Where(s => s.ChallanDate >= fromDate && s.ChallanDate <= toDate);

            var items = stockReceive
                .Where(s => s.StockReceiveItems != null)
                .SelectMany(s => s.StockReceiveItems);

            var grouped = items
                .GroupBy(x => x.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    Qty = g.Sum(x => x.Qty),
                    Amount = g.Sum(x => x.Amount),
                    Gst = g.Sum(x =>
                    {
                        decimal gross = x.Rate * x.Qty;
                        decimal disc = gross * (x.Discount / 100);
                        decimal taxable = gross - disc;

                        decimal gst = taxable * (x.Gst / 100);
                        decimal cess = taxable * (x.Cess / 100);

                        return Math.Round(gst + cess, 2);
                    })
                }).ToList();

            var list = (from item in itemMasters
                        join g in grouped on item.Id equals g.ItemMasterId
                        where g.Qty > 0
                        select new StockVM
                        {
                            ItemCode = item.Code,
                            ItemName = item.Name,
                            CategoryName = item.Category?.CategoryName ?? "Unknown",
                            Stocks = g.Qty,
                            GstAmount = g.Gst,
                            Amount = g.Amount
                        }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stock Receive Item Wise");

            var user = await _usersManager.GetUserAsync(User);
            var tenant = await _tenantRepository.GetById(user.TenantId);
            string companyName = tenant?.Name ?? "Company Name";

            // HEADER
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Stock Receive Item Wise Report";
            ws.Range(2, 1, 2, 6).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 6).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "Item Code", "Item Name", "Category", "Total Qty", "Gst Amount", "Total Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 6).Style.Font.SetBold();
            row++;

            foreach (var x in list)
            {
                ws.Cell(row, 1).Value = x.ItemCode;
                ws.Cell(row, 2).Value = x.ItemName;
                ws.Cell(row, 3).Value = x.CategoryName;
                ws.Cell(row, 4).Value = Math.Round(x.Stocks, 2);
                ws.Cell(row, 5).Value = Math.Round(x.GstAmount ?? 0, 2);
                ws.Cell(row, 6).Value = Math.Round(x.Amount ?? 0, 2);
                row++;
            }

            // TOTAL ROW
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 4).Value = Math.Round(list.Sum(x => x.Stocks), 2);
            ws.Cell(row, 5).Value = Math.Round(list.Sum(x => x.GstAmount ?? 0), 2);
            ws.Cell(row, 6).Value = Math.Round(list.Sum(x => x.Amount ?? 0), 2);

            ws.Range(row, 1, row, 6).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "StockReceiveItemWise.xlsx");
        }

        //Export to Pdf
        [HttpGet]
        public async Task<IActionResult> ExportStockReceiveItemWisePdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();

            var stockReceive = (await _stockreceiveservice.GetAll())
                .Where(s => s.ChallanDate >= fromDate && s.ChallanDate <= toDate);

            var items = stockReceive
                .Where(s => s.StockReceiveItems != null)
                .SelectMany(s => s.StockReceiveItems);

            var grouped = items
                .GroupBy(x => x.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    Qty = g.Sum(x => x.Qty),
                    Amount = g.Sum(x => x.Amount),
                    Gst = g.Sum(x =>
                    {
                        decimal gross = x.Rate * x.Qty;
                        decimal disc = gross * (x.Discount / 100);
                        decimal taxable = gross - disc;

                        decimal gst = taxable * (x.Gst / 100);
                        decimal cess = taxable * (x.Cess / 100);

                        return Math.Round(gst + cess, 2);
                    })
                }).ToList();

            var list = (from item in itemMasters
                        join g in grouped on item.Id equals g.ItemMasterId
                        where g.Qty > 0
                        select new StockVM
                        {
                            ItemCode = item.Code,
                            ItemName = item.Name,
                            CategoryName = item.Category?.CategoryName ?? "Unknown",
                            Stocks = g.Qty,
                            GstAmount = g.Gst,
                            Amount = g.Amount
                        }).ToList();

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _usersManager.GetUserAsync(User);
            var tenant = await _tenantRepository.GetById(user.TenantId);
            string companyName = tenant?.Name ?? "Company Name";

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fs = 9;

            // HEADER
            doc.Add(new Paragraph(companyName).SetFont(bold).SetFontSize(16).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph("Stock Receive Item Wise Report").SetFont(bold).SetFontSize(13).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(10));

            Table table = new Table(6).UseAllAvailableWidth();

            string[] headers = { "Item Code", "Item Name", "Category", "Total Qty", "Gst Amount", "Total Amount" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(fs))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            foreach (var x in list)
            {
                table.AddCell(new Paragraph(x.ItemCode ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.ItemName ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.CategoryName ?? "").SetFont(normal).SetFontSize(fs));

                table.AddCell(new Paragraph(Math.Round(x.Stocks, 2).ToString("0.00"))
                    .SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Paragraph(Math.Round(x.GstAmount ?? 0, 2).ToString("0.00"))
                    .SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Paragraph(Math.Round(x.Amount ?? 0, 2).ToString("0.00"))
                    .SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 3)
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(fs)));

            table.AddCell(new Paragraph(Math.Round(list.Sum(x => x.Stocks), 2).ToString("0.00"))
                .SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(Math.Round(list.Sum(x => x.GstAmount ?? 0), 2).ToString("0.00"))
                .SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(Math.Round(list.Sum(x => x.Amount ?? 0), 2).ToString("0.00"))
                .SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "StockReceiveItemWise.pdf");
        }


        public async Task<IActionResult> StockReceiveReportBillWise(DateTime? fromDate, DateTime ? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            var sales = (await _stockreceiveservice.GetAll())
                .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
                .ToList();

            var billwiseList = sales.Select(sale => new StockReceiveVM
            {
                Id = sale.Id,
                ChallanNo = sale.ChallanNo,
                ChallanDate = sale.ChallanDate,
                CustomerName = sale.Customers?.Name ?? "Unknown",
                MobileNo = sale.MobileNo,
                TotalGstAmt = sale.TotalGstAmt,
                TotalPayable = sale.StockReceiveItems?.Sum(item => item.Amount) ?? 0
            })
            .Where(x => x.TotalPayable > 0) // Optional filter
            .OrderByDescending(x => x.ChallanDate)
            .ToList();
            ViewBag.TotalGst=billwiseList.Sum(x => x.TotalGstAmt);
            ViewBag.TotalPayable=billwiseList.Sum(x => x.TotalPayable);
            return View(billwiseList);
        }

        //EXPORT TO EXCEL
        [HttpGet]
        public async Task<IActionResult> ExportStockReceiveBillWiseExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = (await _stockreceiveservice.GetAll())
                .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
                .ToList();

            var list = sales.Select(sale => new StockReceiveVM
            {
                ChallanNo = sale.ChallanNo,
                ChallanDate = sale.ChallanDate,
                CustomerName = sale.Customers?.Name ?? "Unknown",
                TotalGstAmt = sale.TotalGstAmt,
                TotalPayable = sale.StockReceiveItems?.Sum(x => x.Amount) ?? 0
            })
            .Where(x => x.TotalPayable > 0)
            .OrderByDescending(x => x.ChallanDate)
            .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stock Receive Bill Wise");

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";
            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 5).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Stock Receive Bill Wise Report";
            ws.Range(2, 1, 2, 5).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 5).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "Bill No", "Bill Date", "Customer", "Gst Amount", "Total Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 5).Style.Font.SetBold();
            row++;

            foreach (var x in list)
            {
                ws.Cell(row, 1).Value = x.ChallanNo;
                ws.Cell(row, 2).Value = x.ChallanDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 3).Value = x.CustomerName;
                ws.Cell(row, 4).Value = Math.Round(x.TotalGstAmt, 2);
                ws.Cell(row, 5).Value = Math.Round(x.TotalPayable, 2);
                row++;
            }

            // TOTAL ROW
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 4).Value = Math.Round(list.Sum(x => x.TotalGstAmt), 2);
            ws.Cell(row, 5).Value = Math.Round(list.Sum(x => x.TotalPayable), 2);

            ws.Range(row, 1, row, 5).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "StockReceiveBillWise.xlsx");
        }
        //EXPORT To PDF
        [HttpGet]
        public async Task<IActionResult> ExportStockReceiveBillWisePdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = (await _stockreceiveservice.GetAll())
                .Where(x => x.ChallanDate >= fromDate && x.ChallanDate <= toDate)
                .ToList();

            var list = sales.Select(sale => new StockReceiveVM
            {
                ChallanNo = sale.ChallanNo,
                ChallanDate = sale.ChallanDate,
                CustomerName = sale.Customers?.Name ?? "Unknown",
                TotalGstAmt = sale.TotalGstAmt,
                TotalPayable = sale.StockReceiveItems?.Sum(x => x.Amount) ?? 0
            })
            .Where(x => x.TotalPayable > 0)
            .OrderByDescending(x => x.ChallanDate)
            .ToList();

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";
            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // HEADER
            doc.Add(new Paragraph(companyName).SetFont(bold).SetFontSize(16).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph("Stock Receive Bill Wise Report").SetFont(bold).SetFontSize(13).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(10));

            float fs = 9;

            Table table = new Table(5).UseAllAvailableWidth();

            string[] headers = { "Bill No", "Bill Date", "Customer", "Gst Amount", "Total Amount" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(fs))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            foreach (var x in list)
            {
                table.AddCell(new Paragraph(x.ChallanNo ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.ChallanDate?.ToString("dd-MM-yyyy")).SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.CustomerName ?? "").SetFont(normal).SetFontSize(fs));

                table.AddCell(new Paragraph(x.TotalGstAmt.ToString("0.00"))
                    .SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Paragraph(x.TotalPayable.ToString("0.00"))
                    .SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 3)
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(fs)));

            table.AddCell(new Paragraph(list.Sum(x => x.TotalGstAmt).ToString("0.00"))
                .SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(list.Sum(x => x.TotalPayable).ToString("0.00"))
                .SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "StockReceiveBillWise.pdf");
        }

    }
}
