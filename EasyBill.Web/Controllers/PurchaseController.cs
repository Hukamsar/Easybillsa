using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office.CustomUI;
using AOne.Utility.Enums;
using EasyBill.DataAccess.Migrations;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Service.PurchaseImportService;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Reporting.NETCore;
using Microsoft.ReportingServices.Interfaces;
using NPOI.SS.Formula.Functions;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Twilio.TwiML.Voice;
using static iText.Kernel.Pdf.Colorspace.PdfPattern.Tiling;
using Table = iText.Layout.Element.Table;
using Text = iText.Layout.Element.Text;
namespace EasyBill.UI.Controllers
{
    public partial class PurchaseController : Controller
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
        private readonly ISalseSettingRepository _salsesettingservice;
        private readonly UserManager<ApplicationUsers> _userManager;
        private readonly ITenantRepository _tenantService;
        private readonly IModeOfPaymentRepository _modeofpaymentservice;
        private readonly IPurchaseOrderRepository _purchaseOrderRepo;
        private readonly IPurchaseChallanRepository _purchaseChallanRepository;

        private readonly IStockService _currenstockService;
        private readonly ISupplierAdvanceRepository _supplierAdvanceRepo;
        private readonly IAccountGroupRepository _accountgroupRepo;
        private readonly ICountryRepository _countryRepo;
        private readonly ICurrencyRepository _currencyRepo;
        private readonly IOpeningStockRepository _openingStockRepo;
        public PurchaseController(
            UserManager<ApplicationUsers> userManager,
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
            ISalseSettingRepository salsesettingservice,
            ITenantRepository tenantService,
            IModeOfPaymentRepository modeofpaymentservice,
            IPurchaseOrderRepository purchaseOrderRepo,
            IPurchaseChallanRepository purchaseChallanRepository,
            IStockService currenstockService,
            ISupplierAdvanceRepository supplierAdvanceRepo,
            IAccountGroupRepository accountgroupRepo,
            ICountryRepository countryRepo,
            ICurrencyRepository currencyRepo, 
            IOpeningStockRepository openingStockRepo
            )
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
            _salsesettingservice = salsesettingservice;
            _userManager = userManager;
            _tenantService = tenantService;
            _modeofpaymentservice = modeofpaymentservice;
            _purchaseOrderRepo = purchaseOrderRepo;
            _purchaseChallanRepository = purchaseChallanRepository;
            _currenstockService = currenstockService;
            _supplierAdvanceRepo = supplierAdvanceRepo;
            _accountgroupRepo = accountgroupRepo;
            _countryRepo = countryRepo;
            _currencyRepo = currencyRepo;
            _openingStockRepo = openingStockRepo;
        }

        public async Task<IActionResult> Index()
        {
            var data = (await _purchaseservice.GetAll()).OrderBy(x => x.BillDate).ThenBy(x => x.Id);
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {

            var tenantid = User.FindFirst("TenantId")?.Value;
            var tenantdata = await _tenantService.GetById(tenantid);
            // Check bussiness type 
            var businessType = tenantdata?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;
            ViewBag.supplier = new SelectList(await _supplierservice.GetALL(), "Id", "FirstName");
            ViewBag.Item = new SelectList((await _itemmasterservice.GetAll()).OrderBy(x => x.Name), "Id", "Name");
            ViewBag.Hsn = new SelectList(await _hsnservice.GetAll(), "Id", "HsnCode");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            ViewBag.CountryList = new SelectList(await _countryRepo.GetAll(), "Id", "Name");
            ViewBag.CurrencyList = new SelectList(await _currencyRepo.GetAll(), "Id", "Name");
            var model = new PurchaseVM()
            {
                BillDate = DateTime.Now,
                PartyBillDate = DateTime.Now,
                discountPercent = 0,
                BillNo = await GenerateNxtNumber(),
                TenanatGst = tenantdata.GstNo,
                PurchaseType = "Local",
                SourcePurchaseChallanId = null
            };
            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Create(PurchaseVM VM)
        {
            if (HasCollectionIndexGap(nameof(PurchaseVM.PurchaseItemVms)) ||
                HasCollectionIndexGap(nameof(PurchaseVM.PaymentDetails)))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Purchase rows were not submitted correctly. Please refresh and try again."
                });
            }

            VM.PurchaseItemVms ??= new List<PurchaseItemVM>();
            VM.PaymentDetails ??= new List<SalsePaymentDetailsVM>();

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
                    errors = errors
                });
            }

            if (!VM.PurchaseItemVms.Any(x => x.ItemId > 0))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please add at least one valid purchase item."
                });
            }

            var data = await _purchaseservice.GetAll();
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

            var sourceChallanIds = VM.PurchaseItemVms?
                .Where(x => x.SourcePurchaseChallanId.HasValue)
                .Select(x => x.SourcePurchaseChallanId!.Value)
                .Distinct()
                .ToList() ?? new List<int>();

            if (!sourceChallanIds.Any() && VM.SourcePurchaseChallanId.HasValue)
            {
                sourceChallanIds.Add(VM.SourcePurchaseChallanId.Value);
            }

            var sourceChallans = new List<PurchaseChallan>();
            foreach (var sourceChallanId in sourceChallanIds)
            {
                var sourceChallan = await _purchaseChallanRepository.GetById(sourceChallanId);

                if (sourceChallan == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Selected purchase challan was not found."
                    });
                }

                if (sourceChallan.ConvertedPurchaseId.HasValue ||
                    string.Equals(sourceChallan.Status, "Converted", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Selected purchase challan is already converted."
                    });
                }

                if (sourceChallan.SupplierId != VM.SupplierId)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Selected purchase challan does not belong to the chosen supplier."
                    });
                }

                sourceChallans.Add(sourceChallan);
            }

            if (sourceChallans.Any())
            {
                VM.PurchaseOrderId = null;
            }

            // MERGED FROM TL: Decimal Qty and Expiry validations
            if (!TryValidateDecimalQtyFreeQtyRule(VM.PurchaseItemVms, out var qtyRuleMessage))
            {
                return BadRequest(new
                {
                    success = false,
                    message = qtyRuleMessage
                });
            }

            if (await IsCurrentTenantPharmacyAsync() &&
                !TryValidateRequiredExpiry(VM.PurchaseItemVms, out var expiryRuleMessage))
            {
                return BadRequest(new
                {
                    success = false,
                    message = expiryRuleMessage
                });
            }

            ApplyDefaultExpiryIfMissing(VM.PurchaseItemVms, DateTime.Today.AddYears(1));

            if (VM != null)
            {
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
                    PurchaseOrderId = VM.PurchaseOrderId,
                    TotalCessAmt = VM.TotalCessAmt,
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
                        salserateB = x.salserateB,
                        Barcode = x.Barcode,
                        CGst = x.CGst,
                        SGst = x.SGst,
                        CGstAmount = x.CGstAmount,
                        SGstAmount = x.SGstAmount,
                        Cess = x.Cess,
                        SourcePurchaseChallanId = x.SourcePurchaseChallanId > 0 ? x.SourcePurchaseChallanId : null

                    }).ToList() ?? new List<PurchaseItem>(),
                    PaymentDetails = VM.PaymentDetails?.Select(pd => new SalsePaymentDetails
                    {
                        Id = pd.Id,
                        Date = DateTime.Now,
                        PaymentModeId = pd.PaymentModeId,
                        Amount = pd.Amount,
                        ReferenceNo = pd.ReferenceNo,
                        Description = pd.Description
                    }).ToList() ?? new List<SalsePaymentDetails>()
                };

                var createdPurchase = await _purchaseservice.Create(model);

                // ============================================
                // AUTO ADJUST SUPPLIER ADVANCE
                // ============================================

                if (createdPurchase.SupplierId != null)
                {
                    var supplierAdvance =
                        (await _supplierAdvanceRepo
                        .GetBySupplierId(createdPurchase.SupplierId))
                        .FirstOrDefault();

                    if (supplierAdvance != null &&
                        supplierAdvance.AdvanceAmount > 0)
                    {
                        decimal purchaseBalance =
                            createdPurchase.TotalPayable -
                            createdPurchase.PaidAmount;

                        if (purchaseBalance > 0)
                        {
                            decimal advanceToAdjust =
                                Math.Min(
                                    purchaseBalance,
                                    supplierAdvance.AdvanceAmount);

                            // PAYMENT HISTORY ENTRY

                            createdPurchase.PaymentDetails.Add(
                                new SalsePaymentDetails
                                {
                                    Date = DateTime.Now,
                                    PaymentModeId = 1, // Advance Adjust
                                    Amount = advanceToAdjust,
                                    Description = "Adjusted from supplier advance"
                                });

                            // UPDATE PURCHASE

                            createdPurchase.PaidAmount += advanceToAdjust;

                            createdPurchase.Balance =
                                createdPurchase.TotalPayable -
                                createdPurchase.PaidAmount;

                            // STATUS

                            if (createdPurchase.Balance <= 0)
                            {
                                createdPurchase.PaymentStatus = "Paid";
                            }
                            else
                            {
                                createdPurchase.PaymentStatus = "Partial";
                            }

                            // REDUCE ADVANCE

                            supplierAdvance.AdvanceAmount -=
                                advanceToAdjust;

                            await _supplierAdvanceRepo
                                .Update(supplierAdvance);

                            // SAVE PURCHASE AGAIN

                            await _purchaseservice.Update(createdPurchase);
                        }
                    }
                }

                foreach (var sourceChallan in sourceChallans)
                {
                    await _purchaseChallanRepository.MarkConvertedAsync(sourceChallan.Id, createdPurchase.Id);
                }
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult DownloadImportTemplate()
        {
            var fileBytes = PurchaseImportBuilder.BuildTemplateFile();

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PurchaseImportTemplate.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> PreviewImportItems(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please select an Excel, CSV, or PDF file first."
                });
            }

            try
            {
                // Import only prepares the form rows; actual save still goes through the normal Purchase Create action.
                var preview = await PurchaseImportBuilder.BuildAsync(
                    file,
                    (await _itemmasterservice.GetAll()).Where(x => x.IsActive),
                    await _supplierservice.GetALL());

                if (!preview.Items.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No valid purchase items were found in the selected file.",
                        errors = preview.Warnings
                    });
                }

                return Json(new
                {
                    success = true,
                    message = $"{preview.Items.Count} item(s) are ready to load.",
                    data = preview
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
       
        [HttpGet]
        public async Task<IActionResult> IsBillNumberDuplicate(string billNumber, int id = 0)
        {
            var exists = await _purchaseservice.IsBillNoDuplicateAsync(billNumber, id);

            return Json(new { isDuplicate = exists });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id, string? returnUrl)
        {
            ViewBag.ReturnUrl=returnUrl;
            Purchase model = await _purchaseservice.GetById(Id);
            PurchaseVM purchaseVM = new PurchaseVM();

            if (model != null)
            {
                purchaseVM.Id = model.Id;
                purchaseVM.SupplierId = model.SupplierId;

                // ✅ Supplier Name Set
                var suppliers = await _supplierservice.GetALL();
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
                purchaseVM.PurchaseOrderId = model.PurchaseOrderId;
                purchaseVM.TotalCessAmt = model.TotalCessAmt;
                // Get all items for name lookup
                var allItems = await _itemmasterservice.GetAll();

                purchaseVM.PurchaseItemVms = model.PurchaseItems?.Select(x =>
                {
                    var item = allItems.FirstOrDefault(i => i.Id == x.ItemId);

                    return new PurchaseItemVM
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
                        SGstAmount = x.SGstAmount,
                        Cess = x.Cess
                    };
                }).ToList() ?? new List<PurchaseItemVM>();

                purchaseVM.PaymentDetails = model.PaymentDetails?.Select(x => new SalsePaymentDetailsVM
                {
                    Id = x.Id,
                    PaymentModeId = x.PaymentModeId,
                    Amount = x.Amount,
                    ReferenceNo = x.ReferenceNo,
                    Description = x.Description

                }).ToList() ?? new List<SalsePaymentDetailsVM>();
            }
            var tenantid = User.FindFirst("TenantId")?.Value;
            var tenantdata = await _tenantService.GetById(tenantid);
            // Check bussiness type 
            var businessType = tenantdata?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;
            ViewBag.supplier = new SelectList(await _supplierservice.GetALL(), "Id", "FirstName");
            ViewBag.Item = new SelectList((await _itemmasterservice.GetAll()).OrderBy(x => x.Name), "Id", "Name");
            ViewBag.Hsn = new SelectList(await _hsnservice.GetAll(), "Id", "HsnCode");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            ViewBag.CountryList = new SelectList(await _countryRepo.GetAll(), "Id", "Name");
            ViewBag.CurrencyList = new SelectList(await _currencyRepo.GetAll(), "Id", "Name");
            return View(purchaseVM);
        }


        //[HttpPost]
        //public async Task<IActionResult> Edit(PurchaseVM VM)
        //{
        //    Purchase model = await _purchaseservice.GetById(VM.Id);
        //    if (model != null)
        //    {
        //        model.Id = VM.Id;
        //        model.SupplierId = VM.SupplierId;
        //        model.BillNo = VM.BillNo;
        //        model.BillDate = VM.BillDate;
        //        model.PartyBillNo = VM.PartyBillNo;
        //        model.PartyBillDate = VM.PartyBillDate;
        //        model.TotalGstAmt = VM.TotalGstAmt;
        //        model.discountPercent = VM.discountPercent;
        //        model.discountAmount = VM.discountAmount;
        //        model.Totaldiscount = VM.Totaldiscount;
        //        model.TotalPayable = VM.TotalPayable;
        //        model.RoundOffAmount = VM.RoundOffAmount;
        //        model.Total = VM.Total;
        //        model.billingType = VM.billingType;
        //        model.PaymentType = VM.PaymentType;
        //        model.PurchaseType = VM.PurchaseType;
        //        model.TotalCGstAmt = VM.TotalCGstAmt;
        //        model.TotalSGstAmt = VM.TotalSGstAmt;
        //        model.PaidAmount = VM.PaidAmount;
        //        model.ReturnAmount = VM.ReturnAmount;
        //        model.Balance = VM.Balance;
        //        model.PurchaseOrderId = VM.PurchaseOrderId;
        //        var removedItems = model.PurchaseItems.Where(dbItem => !VM.PurchaseItemVms.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

        //        // ❌ Remove Deleted Items
        //        foreach (var item in removedItems)
        //        {
        //            await _currenstockService.UpdateStock(
        //                item.ItemId,
        //                item.Batch?.Trim() ?? "",
        //                -(item.Qty + item.FreeQty),
        //                item.ExpiryDate,
        //                item.Mrp,
        //                item.Rate
        //            );
        //            model.PurchaseItems.Remove(item);
        //        }
        //        foreach (var item in VM.PurchaseItemVms)
        //        {
        //            item.Batch = item.Batch?.Trim(); // 🔥 normalize
        //            if (item.Id > 0) // Update existing items
        //            {
        //                var existingItem = model.PurchaseItems.FirstOrDefault(x => x.Id == item.Id);
        //                if (existingItem != null)
        //                {
        //                    //decimal oldQty = existingItem.Qty + existingItem.FreeQty;
        //                    //decimal newQty = item.Qty + item.FreeQty;

        //                    //// 🔴 Step 1: REMOVE OLD STOCK
        //                    //await _currenstockService.UpdateStock(
        //                    //    existingItem.ItemId,
        //                    //    existingItem.Batch ?? "",
        //                    //    -oldQty,
        //                    //    existingItem.ExpiryDate,
        //                    //    existingItem.Mrp,
        //                    //    existingItem.Rate
        //                    //);

        //                    //// 🟢 Step 2: ADD NEW STOCK
        //                    //await _currenstockService.UpdateStock(
        //                    //    item.ItemId,
        //                    //    item.Batch ?? "",
        //                    //    newQty,
        //                    //    item.ExpiryDate,
        //                    //    item.Mrp,
        //                    //    item.Rate
        //                    //);

        //                    var oldStock = await _currenstockService.GetStock(
        //                        existingItem.ItemId,
        //                        existingItem.Batch,
        //                        existingItem.ExpiryDate,
        //                        existingItem.Mrp
        //                    );

        //                    // conversion logic (optional but recommended)
        //                    decimal oldQty = existingItem.Qty + existingItem.FreeQty;
        //                    decimal newQty = item.Qty + item.FreeQty;

        //                    // ✅ CONDITION: stock untouched hai
        //                    if (oldStock != null && oldStock.Qty == oldQty)
        //                    {
        //                        // 🟢 DIRECT UPDATE (NO NEW ROW)
        //                        oldStock.Batch = item.Batch;
        //                        oldStock.ExpiryDate = item.ExpiryDate;
        //                        oldStock.Mrp = item.Mrp;
        //                        oldStock.PurchaseRate = item.Rate;
        //                        oldStock.Qty = newQty;
        //                    }
        //                    else
        //                    {
        //                        // 🔁 SHIFT LOGIC
        //                        await _currenstockService.UpdateStock(
        //                            existingItem.ItemId,
        //                            existingItem.Batch ?? "",
        //                            -oldQty,
        //                            existingItem.ExpiryDate,
        //                            existingItem.Mrp,
        //                            existingItem.Rate
        //                        );

        //                        await _currenstockService.UpdateStock(
        //                            item.ItemId,
        //                            item.Batch ?? "",
        //                            newQty,
        //                            item.ExpiryDate,
        //                            item.Mrp,
        //                            item.Rate
        //                        );
        //                    }

        //                    existingItem.ItemId = item.ItemId;
        //                    existingItem.Batch = item.Batch;
        //                    existingItem.ExpiryDate = item.ExpiryDate;
        //                    existingItem.Mrp = item.Mrp;
        //                    existingItem.Qty = item.Qty;
        //                    existingItem.FreeQty = item.FreeQty;
        //                    existingItem.Unit = item.Unit;
        //                    existingItem.Rate = item.Rate;
        //                    existingItem.HsnId = item.HsnId;
        //                    existingItem.Gst = item.Gst;
        //                    existingItem.GstAmount = item.GstAmount;
        //                    existingItem.Discount = item.Discount;
        //                    existingItem.DiscountAmt = item.DiscountAmt;
        //                    existingItem.Amount = item.Amount;
        //                    existingItem.TotalAmt = item.TotalAmt;
        //                    existingItem.BatchWiseCose = item.BatchWiseCose;
        //                    existingItem.salserateA = item.salserateA;
        //                    existingItem.salserateB = item.salserateB;
        //                    existingItem.Barcode = item.Barcode;
        //                    existingItem.CGst = item.CGst;
        //                    existingItem.SGst = item.SGst;
        //                    existingItem.CGstAmount = item.CGstAmount;
        //                    existingItem.SGstAmount = item.SGstAmount;
        //                }
        //            }
        //            else
        //            {
        //                // 🔥 NEW ITEM → FULL STOCK ADD
        //                await _currenstockService.UpdateStock(
        //                    item.ItemId,
        //                    item.Batch,
        //                    item.Qty + item.FreeQty,
        //                    item.ExpiryDate,
        //                    item.Mrp,
        //                    item.Rate
        //                );
        //                model.PurchaseItems.Add(new PurchaseItem
        //                {
        //                    ItemId = item.ItemId,
        //                    Batch = item.Batch,
        //                    ExpiryDate = item.ExpiryDate,
        //                    Mrp = item.Mrp,
        //                    Qty = item.Qty,
        //                    FreeQty = item.FreeQty,
        //                    Unit = item.Unit,
        //                    Rate = item.Rate,
        //                    HsnId = item.HsnId,
        //                    Gst = item.Gst,
        //                    GstAmount = item.GstAmount,
        //                    Discount = item.Discount,
        //                    DiscountAmt = item.DiscountAmt,
        //                    Amount = item.Amount,
        //                    TotalAmt = item.TotalAmt,
        //                    BatchWiseCose = item.BatchWiseCose,
        //                    salserateA = item.salserateA,
        //                    salserateB = item.salserateB,
        //                    Barcode = item.Barcode,
        //                    CGst = item.CGst,
        //                    SGst = item.SGst,
        //                    CGstAmount = item.CGstAmount,
        //                    SGstAmount = item.SGstAmount,
        //                });
        //            }
        //        }

        //        var removedpaymentItems = model.PaymentDetails.Where(dbItem => !VM.PaymentDetails.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

        //        // ❌ Remove Deleted Items
        //        foreach (var item in removedpaymentItems)
        //        {
        //            model.PaymentDetails.Remove(item);
        //        }
        //        foreach (var item in VM.PaymentDetails)
        //        {
        //            if (item.Id > 0) // Update existing items
        //            {
        //                var existingItem = model.PaymentDetails.FirstOrDefault(x => x.Id == item.Id);
        //                if (existingItem != null)
        //                {
        //                    existingItem.PaymentModeId = item.PaymentModeId;
        //                    existingItem.Amount = item.Amount;
        //                    existingItem.ReferenceNo = item.ReferenceNo;
        //                    existingItem.Description = item.Description;

        //                }
        //            }
        //            else
        //            {
        //                model.PaymentDetails.Add(new SalsePaymentDetails
        //                {
        //                    Date = DateTime.Now,
        //                    PaymentModeId = item.PaymentModeId,
        //                    Amount = item.Amount,
        //                    ReferenceNo = item.ReferenceNo,
        //                    Description = item.Description
        //                });
        //            }
        //        }
        //        await _purchaseservice.Update(model);
        //    }

        //    return RedirectToAction("Index");
        //} 


        [HttpPost]
        public async Task<IActionResult> Edit(PurchaseVM VM)
        {
            if (HasCollectionIndexGap(nameof(PurchaseVM.PurchaseItemVms)) ||
                HasCollectionIndexGap(nameof(PurchaseVM.PaymentDetails)))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Purchase rows were not submitted correctly. Please refresh and try again."
                });
            }

            VM.PurchaseItemVms ??= new List<PurchaseItemVM>();
            VM.PaymentDetails ??= new List<SalsePaymentDetailsVM>();

            if (!VM.PurchaseItemVms.Any(x => x.ItemId > 0))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please add at least one valid purchase item."
                });
            }

            if (!TryValidateDecimalQtyFreeQtyRule(VM.PurchaseItemVms, out var qtyRuleMessage))
            {
                return BadRequest(new
                {
                    success = false,
                    message = qtyRuleMessage
                });
            }

            if (await IsCurrentTenantPharmacyAsync() &&
                !TryValidateRequiredExpiry(VM.PurchaseItemVms, out var expiryRuleMessage))
            {
                return BadRequest(new
                {
                    success = false,
                    message = expiryRuleMessage
                });
            }

            ApplyDefaultExpiryIfMissing(VM.PurchaseItemVms, DateTime.Today.AddYears(1));

            Purchase model = await _purchaseservice.GetById(VM.Id);
            if (model != null)
            {
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
                model.PurchaseOrderId = VM.PurchaseOrderId;
                model.TotalCessAmt = VM.TotalCessAmt;
                var removedItems = model.PurchaseItems.Where(dbItem => !VM.PurchaseItemVms.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

                // ❌ Remove Deleted Items
                foreach (var item in removedItems)
                {
                    await _currenstockService.UpdateStock(
                        item.ItemId,
                        item.Batch?.Trim() ?? "",
                        -(item.Qty + item.FreeQty),
                        item.ExpiryDate,
                        item.Mrp,
                        item.salserateA,
                        item.salserateB,
                        item.Rate,
                        item.Barcode
                    );
                    model.PurchaseItems.Remove(item);
                }

                foreach (var item in VM.PurchaseItemVms)
                {
                    item.Batch = item.Batch?.Trim(); // 🔥 normalize
                    if (item.Id > 0) // Update existing items
                    {
                        var existingItem = model.PurchaseItems.FirstOrDefault(x => x.Id == item.Id);
                        if (existingItem != null)
                        {
                            var oldStock = await _currenstockService.GetStock(
                                existingItem.ItemId,
                                existingItem.Batch,
                                existingItem.ExpiryDate,
                                existingItem.Mrp
                            );

                            decimal oldQty = existingItem.Qty + existingItem.FreeQty;
                            decimal newQty = item.Qty + item.FreeQty;

                            var canUpdateBatchRates = oldStock != null && oldStock.Qty == oldQty;
                            if (canUpdateBatchRates && oldStock != null)
                            {
                                await _currenstockService.OverwriteStock(
                                    oldStock.Id,
                                    item.Batch ?? string.Empty,
                                    item.ExpiryDate,
                                    item.Mrp,
                                    newQty,
                                    item.Rate,
                                    item.salserateA,
                                    item.salserateB,
                                    item.Barcode
                                );
                            }
                            else
                            {
                                var isSameStockLayer = IsSameStockLayer(existingItem, item);
                                var salesRateAToApply = isSameStockLayer ? existingItem.salserateA : item.salserateA;
                                var salesRateBToApply = isSameStockLayer ? existingItem.salserateB : item.salserateB;
                                var barcodeToApply = isSameStockLayer ? existingItem.Barcode : item.Barcode;

                                await _currenstockService.UpdateStock(
                                    existingItem.ItemId,
                                    existingItem.Batch ?? "",
                                    -oldQty,
                                    existingItem.ExpiryDate,
                                    existingItem.Mrp,
                                    existingItem.salserateA,
                                    existingItem.salserateB,
                                    existingItem.Rate,
                                    existingItem.Barcode
                                );

                                await _currenstockService.UpdateStock(
                                    item.ItemId,
                                    item.Batch ?? "",
                                    newQty,
                                    item.ExpiryDate,
                                    item.Mrp,
                                    salesRateAToApply,
                                    salesRateBToApply,
                                    item.Rate,
                                    barcodeToApply
                                );
                            }

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
                            existingItem.Cess = item.Cess;
                            existingItem.SourcePurchaseChallanId = item.SourcePurchaseChallanId > 0 ? item.SourcePurchaseChallanId : null;
                        }
                    }
                    else
                    {
                        bool isExtraItem = item.SourcePurchaseChallanId == null || item.SourcePurchaseChallanId <= 0;

                        if (isExtraItem)
                        {
                            await _currenstockService.UpdateStock(
                                item.ItemId,
                                item.Batch,
                                item.Qty + item.FreeQty,
                                item.ExpiryDate,
                                item.Mrp,
                                item.salserateA,
                                item.salserateB,
                                item.Rate,
                                item.Barcode
                            );
                        }

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
                            salserateB = item.salserateB,
                            Barcode = item.Barcode,
                            CGst = item.CGst,
                            SGst = item.SGst,
                            CGstAmount = item.CGstAmount,
                            SGstAmount = item.SGstAmount,
                            Cess = item.Cess,
                            SourcePurchaseChallanId = item.SourcePurchaseChallanId > 0 ? item.SourcePurchaseChallanId : null
                        });
                    }
                }

                var removedpaymentItems = model.PaymentDetails.Where(dbItem => !VM.PaymentDetails.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

                // ❌ Remove Deleted Items
                foreach (var item in removedpaymentItems)
                {
                    model.PaymentDetails.Remove(item);
                }
                foreach (var item in VM.PaymentDetails)
                {
                    if (item.Id > 0) // Update existing items
                    {
                        var existingItem = model.PaymentDetails.FirstOrDefault(x => x.Id == item.Id);
                        if (existingItem != null)
                        {
                            existingItem.PaymentModeId = item.PaymentModeId;
                            existingItem.Amount = item.Amount;
                            existingItem.ReferenceNo = item.ReferenceNo;
                            existingItem.Description = item.Description;
                        }
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
                await _purchaseservice.Update(model);
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

                var model = await _purchaseservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                // 🔥 1. STOCK REVERSE
                foreach (var item in model.PurchaseItems)
                {
                    await _currenstockService.UpdateStock(
                        item.ItemId,
                        item.Batch ?? "",
                        -(item.Qty + item.FreeQty), // 🔻 PURCHASE delete = minus (qty + free qty)
                        item.ExpiryDate,
                        item.Mrp,
                        item.salserateA,
                        item.salserateB,
                        item.Rate,
                        item.Barcode
                    );
                }
                await _purchaseservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        public async Task<string> GenerateNxtNumber()
        {
            var allPurchases = await _purchaseservice.GetAll();

            var nextNumber = allPurchases
                .Where(x => !string.IsNullOrWhiteSpace(x.BillNo) && x.BillNo.StartsWith("BL", StringComparison.OrdinalIgnoreCase))
                .Select(x => ExtractTrailingNumber(x.BillNo))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"BL{nextNumber:D4}";
        }

        private string GenerateNextProductCode(string lastCode)
        {
            if (string.IsNullOrWhiteSpace(lastCode))
                return "BL0001";

            // Last numeric sequence only
            var match = Regex.Match(lastCode, @"(\d+)(?!.*\d)");

            if (!match.Success)
                return "BL0001";

            int number = int.Parse(match.Value);
            string prefix = lastCode[..match.Index];
            string suffix = lastCode[(match.Index + match.Length)..];

            string nextNumber = (number + 1).ToString($"D{match.Length}");

            return $"{prefix}{nextNumber}{suffix}";
        }

        private int? ExtractTrailingNumber(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var match = System.Text.RegularExpressions.Regex.Match(code, @"\d+$");
            if (match.Success)
            {
                if (int.TryParse(match.Value, out int result))
                {
                    return result;
                }
                return null;
            }

            return null;
        }


        //[HttpGet]
        //public async Task<IActionResult> GetAllItems(string? purchaseType)
        //{
        //    //var itemMasters = (await _itemmasterservice.GetAll()).OrderBy(x => x.Name);

        //    var itemMasters = (await _itemmasterservice.GetAll())
        //        .Where(x => x.IsActive)
        //        .OrderBy(x => x.Name);

        //    var purchasereturns = await _purchasereturnservice.GetAll();
        //    var sales = await _salesService.GetAll();
        //    var stockIssue = await _stockissueservice.GetAll();
        //    var stockReturn = await _stockrturnservice.GetAll();
        //    var stockreceive = await _stockreceiveservice.GetAll();
        //    var receivedQtyDict = await BuildReceivedQtyDictionaryAsync();

        //    var purchaseReturnItems = purchasereturns.Where(r => r.PurchaseReturnItems != null).SelectMany(r => r.PurchaseReturnItems);
        //    var salesItems = sales.Where(s => s.SalesItems != null).SelectMany(s => s.SalesItems);
        //    var stockissueItems = stockIssue.Where(s => s.StockIssuesItems != null).SelectMany(s => s.StockIssuesItems);
        //    var stockreturnItems = stockReturn.Where(s => s.StockReturnItems != null).SelectMany(s => s.StockReturnItems);
        //    var stockreceiveItems = stockreceive.Where(s => s.StockReceiveItems != null).SelectMany(s => s.StockReceiveItems);

        //    var stockList = itemMasters.Select(item =>
        //    {
        //        var itemId = item.Id;

        //        var totalPurchasedQty = receivedQtyDict.ContainsKey(itemId) ? receivedQtyDict[itemId] : 0;

        //        var totalpurchaseReturnedQty = purchaseReturnItems
        //            .Where(x => x.ItemId == itemId)
        //            .Sum(x => x.Qty + x.FreeQty);

        //        var totalsalseQty = salesItems
        //            .Where(x => x.ItemMasterId == itemId)
        //            .Sum(x => x.Qty);

        //        var totalstockissueQty = stockissueItems
        //            .Where(x => x.ItemMasterId == itemId)
        //            .Sum(x => x.Qty);

        //        var totalstockreturnQty = stockreturnItems
        //            .Where(x => x.ItemMasterId == itemId)
        //            .Sum(x => x.Qty);

        //        var totalstockreceiveQty = stockreceiveItems
        //            .Where(x => x.ItemMasterId == itemId)
        //            .Sum(x => x.Qty);

        //        var currentStock = totalPurchasedQty + totalstockreturnQty + totalstockreceiveQty
        //                         - totalpurchaseReturnedQty - totalsalseQty - totalstockissueQty;

        //        return new
        //        {
        //            id = itemId,
        //            barcode = item.Barcode,
        //            code = item.Code,
        //            name = item.Name,
        //            gst = item.Hsn?.IGST ?? 0,
        //            cgst = item.Hsn?.CGST ?? 0,
        //            sgst = item.Hsn?.SGST ?? 0,
        //            hsn = item.HsnId ?? 0,
        //            stock = currentStock,
        //            unit = item.Unit1 ?? "N/A",
        //            mrp = item.Mrp
        //        };
        //    }).ToList();

        //    return Json(stockList);
        //}
        [HttpGet]
        public async Task<IActionResult> GetAllItems(string? purchaseType)
        {
            var itemMasters = (await _itemmasterservice.GetAll()).Where(x => x.IsActive).OrderBy(x => x.Name);
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var setting = await _salsesettingservice.GetByUserId(userId);
            var currentStocks = await _currenstockService.GetAll();

            //var stockDict = currentStocks
            //    .GroupBy(x => x.ItemId)
            //    .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var stockDict = currentStocks
                             .GroupBy(x => x.ItemId)
                             .ToDictionary(
                                 g => g.Key,
                                 g => new
                                 {
                                     Qty = g.Sum(x => x.Qty),

                                     PurchaseRate = g
                                         .Where(x => x.PurchaseRate > 0) // ✅ ignore 0
                                         .OrderByDescending(x => x.Id)   // latest valid
                                         .Select(x => x.PurchaseRate)
                                         .FirstOrDefault()
                                 }
                             );

            var stockList = itemMasters.Select(item =>
            {
                var stockData = stockDict.ContainsKey(item.Id) ? stockDict[item.Id] : new { Qty = 0m, PurchaseRate = 0m };
                // var baseStock = stockDict.ContainsKey(item.Id) ? stockDict[item.Id] : 0;
                var baseStock = stockData.Qty;
                var purchaseRate = stockData.PurchaseRate;

                string availableQty;
                if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
                {
                    int conversion = item.Conversion;

                    int totalTablets = (int)Math.Round(baseStock * conversion, MidpointRounding.AwayFromZero);

                    int strips = totalTablets / conversion;
                    int tablets = totalTablets % conversion;

                    availableQty = $"{strips}:{tablets}";
                }
                else
                {
                    availableQty = baseStock.ToString("0.##");
                }
                //return new
                //{
                //    id = item.Id,
                //    name = item.Name,
                //    stock = $"{strip}:{tablet}",   // ✅ 5:10 format
                //    unit = item.Unit1 ?? "N/A"
                //};

                return new
                {
                    id = item.Id,
                    barcode = item.Barcode,
                    code = item.Code,
                    name = item.Name,
                    gst = item.Hsn?.IGST ?? 0,
                    cgst = item.Hsn?.CGST ?? 0,
                    sgst = item.Hsn?.SGST ?? 0,
                    hsn = item.HsnId ?? 0,
                    cess = item.Hsn?.Cess,
                    // stock = currentStock,
                    qty = baseStock,
                    availableQty = availableQty,
                    purchaseRate = purchaseRate,
                    unit = item.Unit1 ?? "N/A",
                    mrp = item.Mrp
                };
            }).ToList();

            return Json(stockList);
        }

        [HttpGet]
        public async Task<IActionResult> GetItemDetails(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
                return NotFound();

            return Json(new
            {
                gst = item.Hsn?.IGST,
                qty = item.MaximumQty,
                rate = item.Mrp,
                hsn = item.HsnId
            });
        }

        //public async Task<IActionResult> GetBatchesByItemId(int id)
        //{
        //    var item = await _itemmasterservice.GetByItemMasterId(id);
        //    if (item == null)
        //        return Json(new { error = "HSN Details not found." });

        //    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    var setting = await _salsesettingservice.GetByUserId(userId);

        //    // Get all transaction data
        //    var purchases = await _purchaseservice.GetAll();
        //    var purchaseReturns = await _purchasereturnservice.GetAll();
        //    var sales = await _salesService.GetAll();
        //    var stockIssues = await _stockissueservice.GetAll();
        //    var stockReturns = await _stockrturnservice.GetAll();
        //    var stockReceives = await _stockreceiveservice.GetAll();

        //    // Flatten all items by ItemId
        //    var purchaseItems = purchases
        //        .Where(p => p.PurchaseItems != null)
        //        .SelectMany(p => p.PurchaseItems)
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    var purchaseReturnItems = purchaseReturns
        //        .Where(r => r.PurchaseReturnItems != null)
        //        .SelectMany(r => r.PurchaseReturnItems)
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    var salesItems = sales
        //        .Where(s => s.SalesItems != null)
        //        .SelectMany(s => s.SalesItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockIssueItems = stockIssues
        //        .Where(s => s.StockIssuesItems != null)
        //        .SelectMany(s => s.StockIssuesItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockReturnItems = stockReturns
        //        .Where(s => s.StockReturnItems != null)
        //        .SelectMany(s => s.StockReturnItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockReceiveItems = stockReceives
        //        .Where(s => s.StockReceiveItems != null)
        //        .SelectMany(s => s.StockReceiveItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    // Group purchases by batch
        //    var purchaseBatches = purchaseItems
        //        .GroupBy(p => new { p.Batch, p.ExpiryDate, p.Mrp, p.Rate })
        //        .Select(g => new
        //        {
        //            g.Key.Batch,
        //            g.Key.ExpiryDate,
        //            g.Key.Mrp,
        //            g.Key.Rate,
        //            TotalQty = g.Sum(x => x.Qty + x.FreeQty),
        //            SalserateA = g.FirstOrDefault()?.salserateA ?? 0,
        //            SalserateB = g.FirstOrDefault()?.salserateB ?? 0,
        //            Barcode = g.FirstOrDefault()?.Barcode
        //        })
        //        .ToList();

        //    // Calculate batch-wise stock (inline calculation - no separate helper method)
        //    var groupedResult = purchaseBatches
        //        .Select(p =>
        //        {
        //            // Purchase Returns (deduct from stock)
        //            var purchaseReturnQty = purchaseReturnItems
        //                .Where(x => x.Batch == p.Batch
        //                    && x.ExpiryDate == p.ExpiryDate
        //                    && x.Mrp == p.Mrp
        //                    //&& x.Rate == p.Rate
        //                    )
        //                .Sum(x => x.Qty + x.FreeQty);

        //            // Sales (deduct from stock)
        //            var salesQty = salesItems
        //                .Where(x => x.Batch == p.Batch
        //                    && x.Expirydate == p.ExpiryDate
        //                    && x.Mrp == p.Mrp
        //                    //&& x.Rate == p.Rate)
        //                    //&& x.Rate == p.SalserateA
        //                    )
        //                .Sum(x => x.Qty);

        //            // Stock Issue (deduct from stock)
        //            var stockIssueQty = stockIssueItems
        //                .Where(x => x.Batch == p.Batch
        //                    && x.Expirydate == p.ExpiryDate
        //                    && x.Mrp == p.Mrp
        //                    //&& x.Rate == p.Rate
        //                    )
        //                .Sum(x => x.Qty);

        //            // Stock Return (add to stock)
        //            var stockReturnQty = stockReturnItems
        //                .Where(x => x.Batch == p.Batch
        //                    && x.Expirydate == p.ExpiryDate
        //                    && x.Mrp == p.Mrp
        //                    //&& x.Rate == p.Rate
        //                    )
        //                .Sum(x => x.Qty);

        //            // Stock Receive (add to stock)
        //            var stockReceiveQty = stockReceiveItems
        //                .Where(x => x.Batch == p.Batch
        //                    && x.Expirydate == p.ExpiryDate
        //                    && x.Mrp == p.Mrp
        //                    //&& x.Rate == p.Rate
        //                    )
        //                .Sum(x => x.Qty);

        //            // Final Stock Calculation
        //            var finalQty = p.TotalQty + stockReturnQty + stockReceiveQty
        //                          - purchaseReturnQty - salesQty - stockIssueQty;

        //            return new
        //            {
        //                batch = p.Batch,
        //                expiry = p.ExpiryDate?.ToString("yyyy-MM-dd"),
        //                rate = p.Rate,
        //                mrp = p.Mrp,
        //                qty = finalQty,
        //                salserateA = p.SalserateA,
        //                salserateB = p.SalserateB,
        //                barcode = p.Barcode
        //            };
        //        })
        //        //.Where(x => x.qty > 0) // Only show batches with available stock (by Sir)
        //        .ToList();

        //    var model = new
        //    {
        //        gst = item.Hsn?.IGST ?? 0,
        //        hsn = item.HsnId,
        //        barcode = item.Barcode,
        //        groupedResult
        //    };

        //    return Json(model);
        //}

        [HttpGet]
        public async Task<IActionResult> GetBatchesByItemId(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
                return Json(new { error = "Item not found." });
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var setting = await _salsesettingservice.GetByUserId(userId);
            var currentStocks = await _currenstockService.GetAll();

            var batches = currentStocks
                .Where(x => x.ItemId == id)
                .GroupBy(x => new { x.Batch, x.ExpiryDate })
                .Select(g =>
                {
                    var currentStock = g.Sum(x => x.Qty);
                    var latestStockLayer = g
                        .OrderByDescending(x => x.LastModified ?? x.Created ?? DateTime.MinValue)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefault();

                    //if (currentStock < 0)
                    //    currentStock = 0;
                    string availableQty;
                    if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
                    {
                        int conversion = item.Conversion;

                        int totalTablets = (int)Math.Round(currentStock * conversion, MidpointRounding.AwayFromZero);

                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;

                        availableQty = $"{strips}:{tablets}";
                    }
                    else
                    {
                        availableQty = currentStock.ToString("0.##");
                    }

                    return new
                    {
                        batch = g.Key.Batch,
                        expiry = g.Key.ExpiryDate?.ToString("yyyy-MM-dd"),
                        rate = latestStockLayer?.PurchaseRate ?? 0,
                        mrp = latestStockLayer?.Mrp ?? 0,
                        qty = currentStock,   // ✅ 5:10 format
                        availableQty = availableQty,
                        salserateA = latestStockLayer?.SalesRateA ?? 0,
                        salserateB = latestStockLayer?.SalesRateB ?? 0,
                        barcode = latestStockLayer?.Barcode
                    };
                })
                //.Where(x => x.qty != "0:0") // optional
                .ToList();

            var model = new
            {
                gst = item.Hsn?.IGST ?? 0,
                hsn = item.HsnId,
                barcode = item.Barcode,
                groupedResult = batches
            };

            return Json(model);
        }

        //public async Task<IActionResult> GetBatchesByItemId(int id)
        //{
        //    var item = await _itemmasterservice.GetByItemMasterId(id);
        //    if (item == null)
        //        return Json(new { error = "HSN Details not found." });

        //    // ✅ Get all transaction data
        //    var purchases = await _purchaseservice.GetAll();
        //    var purchaseReturns = await _purchasereturnservice.GetAll();
        //    var sales = await _salesService.GetAll();
        //    var stockIssues = await _stockissueservice.GetAll();
        //    var stockReturns = await _stockrturnservice.GetAll();
        //    var stockReceives = await _stockreceiveservice.GetAll();

        //    // ✅ Flatten all items by ItemId
        //    var purchaseItems = purchases
        //        .Where(p => p.PurchaseItems != null)
        //        .SelectMany(p => p.PurchaseItems)
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    var purchaseReturnItems = purchaseReturns
        //        .Where(r => r.PurchaseReturnItems != null)
        //        .SelectMany(r => r.PurchaseReturnItems)
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    var salesItems = sales
        //        .Where(s => s.SalesItems != null)
        //        .SelectMany(s => s.SalesItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockIssueItems = stockIssues
        //        .Where(s => s.StockIssuesItems != null)
        //        .SelectMany(s => s.StockIssuesItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockReturnItems = stockReturns
        //        .Where(s => s.StockReturnItems != null)
        //        .SelectMany(s => s.StockReturnItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockReceiveItems = stockReceives
        //        .Where(s => s.StockReceiveItems != null)
        //        .SelectMany(s => s.StockReceiveItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    // ✅ Group purchases by batch
        //    var purchaseBatches = purchaseItems
        //        .GroupBy(p => new { p.Batch, p.ExpiryDate, p.Mrp, p.Rate })
        //        .Select(g => new
        //        {
        //            g.Key.Batch,
        //            g.Key.ExpiryDate,
        //            g.Key.Mrp,
        //            g.Key.Rate,
        //            TotalQty = g.Sum(x => x.Qty + x.FreeQty), // Qty + FreeQty
        //            SalserateA = g.FirstOrDefault()?.salserateA ?? 0,
        //            SalserateB = g.FirstOrDefault()?.salserateB ?? 0,
        //            Barcode = g.FirstOrDefault()?.Barcode
        //        })
        //        .ToList();

        //    // Calculate batch-wise stock
        //    var groupedResult = purchaseBatches
        //        .Select(p => new
        //        {
        //            batch = p.Batch,
        //            expiry = p.ExpiryDate?.ToString("yyyy-MM-dd"),
        //            rate = p.Rate,
        //            mrp = p.Mrp,
        //            qty = CalculateBatchStock(
        //                p.Batch,
        //                p.ExpiryDate,
        //                p.Mrp,
        //                // p.Rate,
        //                p.SalserateA,
        //                p.TotalQty,
        //                purchaseReturnItems,
        //                salesItems,
        //                stockIssueItems,
        //                stockReturnItems,
        //                stockReceiveItems
        //            ),
        //            salserateA = p.SalserateA,
        //            salserateB = p.SalserateB,
        //            barcode = p.Barcode
        //        })
        //        //.Where(x => x.qty > 0) // Only show batches with available stock (by Sir)
        //        .ToList();

        //    var model = new
        //    {
        //        gst = item.Hsn?.IGST ?? 0,
        //        hsn = item.HsnId,
        //        barcode = item.Barcode,
        //        groupedResult
        //    };

        //    return Json(model);
        //}

        //// Helper method to calculate batch-wise stock
        //private decimal CalculateBatchStock(
        //    string batch,
        //    DateTime? expiryDate,
        //    decimal mrp,
        //    decimal rate,
        //    decimal purchasedQty,
        //    List<PurchaseReturnItem> purchaseReturnItems,
        //    List<SalesItem> salesItems,
        //    List<StockIssueItem> stockIssueItems,
        //    List<StockReturnItem> stockReturnItems,
        //    List<StockReceiveItem> stockReceiveItems)
        //{
        //    // ✅ Purchase Returns (deduct from stock)
        //    var purchaseReturnQty = purchaseReturnItems
        //        .Where(x => x.Batch == batch
        //            && x.ExpiryDate == expiryDate
        //            && x.Mrp == mrp
        //            && x.Rate == rate)
        //        .Sum(x => x.Qty + x.FreeQty);

        //    // ✅ Sales (deduct from stock)
        //    var salesQty = salesItems
        //        .Where(x => x.Batch == batch
        //            && x.Expirydate == expiryDate
        //            && x.Mrp == mrp
        //            && x.Rate == rate)
        //        .Sum(x => x.Qty);

        //    // ✅ Stock Issue (deduct from stock)
        //    var stockIssueQty = stockIssueItems
        //        .Where(x => x.Batch == batch
        //            && x.Expirydate == expiryDate
        //            && x.Mrp == mrp
        //            && x.Rate == rate)
        //        .Sum(x => x.Qty);

        //    // ✅ Stock Return (add to stock)
        //    var stockReturnQty = stockReturnItems
        //        .Where(x => x.Batch == batch
        //            && x.Expirydate == expiryDate
        //            && x.Mrp == mrp
        //            && x.Rate == rate)
        //        .Sum(x => x.Qty);

        //    // ✅ Stock Receive (add to stock)
        //    var stockReceiveQty = stockReceiveItems
        //        .Where(x => x.Batch == batch
        //            && x.Expirydate == expiryDate
        //            && x.Mrp == mrp
        //            && x.Rate == rate)
        //        .Sum(x => x.Qty);

        //    // ✅ Final Stock Calculation
        //    return purchasedQty + stockReturnQty + stockReceiveQty
        //           - purchaseReturnQty - salesQty - stockIssueQty;
        //}
        public async Task<IActionResult> GetItemPurchaseHistory(int id)
        {
            var data = await _purchaseservice.GetItemSupplierPurchaseInfoByItemIdAsync(id);

            if (data == null || !data.Any())
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSuppliers()
        {
            try
            {
                var suppliers = await _supplierservice.GetALL();

                var supplierList = suppliers
                    .Select(s => new
                    {
                        id = s.Id,
                        firstName = s.FirstName,
                        area = s.Area ?? string.Empty
                    })
                    .ToList();

                return Json(supplierList);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        //[HttpPost]
        //public async Task<IActionResult> edit(PurchaseVM VM)
        //{
        //    Purchase model = await _purchaseservice.GetById(VM.Id);
        //    if (model != null)
        //    {
        //        model.Id = VM.Id;
        //        model.SupplierId = VM.SupplierId;
        //        model.BillNo = VM.BillNo;
        //        model.BillDate = VM.BillDate;
        //        model.PartyBillNo = VM.PartyBillNo;
        //        model.PartyBillDate = VM.PartyBillDate;
        //        model.TotalGstAmt = VM.TotalGstAmt;
        //        model.discountPercent = VM.discountPercent;
        //        model.discountAmount = VM.discountAmount;
        //        model.Totaldiscount = VM.Totaldiscount;
        //        model.TotalPayable = VM.TotalPayable;
        //        model.Total = VM.Total;
        //        var removedItems = model.PurchaseItems.Where(dbItem => !VM.PurchaseItemVms.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

        //        // ❌ Remove Deleted Items
        //        foreach (var item in removedItems)
        //        {
        //            model.PurchaseItems.Remove(item);
        //        }
        //        foreach (var item in VM.PurchaseItemVms)
        //        {
        //            if (item.Id > 0) // Update existing items
        //            {
        //                var existingItem = model.PurchaseItems.FirstOrDefault(x => x.Id == item.Id);
        //                if (existingItem != null)
        //                {
        //                    existingItem.ItemId = item.ItemId;
        //                    existingItem.Batch = item.Batch;
        //                    existingItem.ExpiryDate = item.ExpiryDate;
        //                    existingItem.Mrp = item.Mrp;
        //                    existingItem.Qty = item.Qty;
        //                    existingItem.FreeQty = item.FreeQty;
        //                    existingItem.Unit = item.Unit;
        //                    existingItem.Rate = item.Rate;
        //                    existingItem.HsnId = item.HsnId;
        //                    existingItem.Gst = item.Gst;
        //                    existingItem.GstAmount = item.GstAmount;
        //                    existingItem.Discount = item.Discount;
        //                    existingItem.DiscountAmt = item.DiscountAmt;
        //                    existingItem.Amount = item.Amount;
        //                    existingItem.TotalAmt = item.TotalAmt;
        //                    existingItem.BatchWiseCose = item.BatchWiseCose;
        //                    existingItem.salserateA = item.salserateA;
        //                    existingItem.salserateB = item.salserateB;
        //                    existingItem.Barcode = item.Barcode;
        //                }
        //            }
        //            else // Add new items
        //            {
        //                model.PurchaseItems.Add(new PurchaseItem
        //                {
        //                    ItemId = item.ItemId,
        //                    Batch = item.Batch,
        //                    ExpiryDate = item.ExpiryDate,
        //                    Mrp = item.Mrp,
        //                    Qty = item.Qty,
        //                    FreeQty = item.FreeQty,
        //                    Unit = item.Unit,
        //                    Rate = item.Rate,
        //                    HsnId = item.HsnId,
        //                    Gst = item.Gst,
        //                    GstAmount = item.GstAmount,
        //                    Discount = item.Discount,
        //                    DiscountAmt = item.DiscountAmt,
        //                    Amount = item.Amount,
        //                    TotalAmt = item.TotalAmt,
        //                    BatchWiseCose = item.BatchWiseCose,
        //                    salserateA = item.salserateA,
        //                    salserateB = item.salserateB,
        //                    Barcode = item.Barcode
        //                });
        //            }
        //        }
        //        await _purchaseservice.Update(model);
        //    }

        //    return RedirectToAction("Index");
        //}


        public async Task<IActionResult> CurrentStock()
        {
            // Get user setting
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            var today = DateTime.Today;
            var itemMasters = await _itemmasterservice.GetAll();
            var currentStocks = await _currenstockService.GetAll();
            var stockDict = currentStocks.GroupBy(x => x.ItemId).ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));
            //var purchaseReturns = (await _purchasereturnservice.GetAll())
            //    .Where(x => x.BillDate <= today)
            //    .ToList();

            //var sales = (await _salesService.GetAll())
            //    .Where(x => x.BillDate <= today)
            //    .ToList();

            //var stockIssues = (await _stockissueservice.GetAll())
            //    .Where(x => x.ChallanDate <= today)
            //    .ToList();

            //var stockReturns = (await _stockrturnservice.GetAll())
            //    .Where(x => x.ChallanDate <= today)
            //    .ToList();

            //var stockReceives = (await _stockreceiveservice.GetAll())
            //    .Where(x => x.ChallanDate <= today)
            //    .ToList();

            //var receivedQtyDict = await BuildReceivedQtyDictionaryAsync(today);
            //var purchaseReturnItems = purchaseReturns.SelectMany(x => x.PurchaseReturnItems ?? new List<PurchaseReturnItem>());
            //var salesItems = sales.SelectMany(x => x.SalesItems ?? new List<SalesItem>());
            //var stockIssueItems = stockIssues.SelectMany(x => x.StockIssuesItems ?? new List<StockIssueItem>());
            //var stockReturnItems = stockReturns.SelectMany(x => x.StockReturnItems ?? new List<StockReturnItem>());
            //var stockReceiveItems = stockReceives.SelectMany(x => x.StockReceiveItems ?? new List<StockReceiveItem>());

            //var purchaseReturnDict = purchaseReturnItems
            //    .GroupBy(x => x.ItemId)
            //    .ToDictionary(g => g.Key,
            //                  g => g.Sum(x => (x.Qty) + (x.FreeQty)));

            //var salesDict = salesItems
            //    .GroupBy(x => x.ItemMasterId)
            //    .ToDictionary(g => g.Key,
            //                  g => g.Sum(x => x.Qty));

            //var issueDict = stockIssueItems
            //    .GroupBy(x => x.ItemMasterId)
            //    .ToDictionary(g => g.Key,
            //                  g => g.Sum(x => x.Qty));

            //var returnDict = stockReturnItems
            //    .GroupBy(x => x.ItemMasterId)
            //    .ToDictionary(g => g.Key,
            //                  g => g.Sum(x => x.Qty));

            //var receiveDict = stockReceiveItems
            //    .GroupBy(x => x.ItemMasterId)
            //    .ToDictionary(g => g.Key,
            //                  g => g.Sum(x => x.Qty));

            var stockList = itemMasters.Select(item =>
            {
                var itemId = item.Id;

                //decimal totalPurchased = receivedQtyDict.ContainsKey(itemId) ? receivedQtyDict[itemId] : 0;
                //decimal totalPurchaseReturn = purchaseReturnDict.ContainsKey(itemId) ? purchaseReturnDict[itemId] : 0;
                //decimal totalSales = salesDict.ContainsKey(itemId) ? salesDict[itemId] : 0;
                //decimal totalIssue = issueDict.ContainsKey(itemId) ? issueDict[itemId] : 0;
                //decimal totalReturn = returnDict.ContainsKey(itemId) ? returnDict[itemId] : 0;
                //decimal totalReceive = receiveDict.ContainsKey(itemId) ? receiveDict[itemId] : 0;

                //decimal currentStock =
                //    totalPurchased
                //    + totalReturn
                //    + totalReceive
                //    - totalPurchaseReturn
                //    - totalSales
                //    - totalIssue;
                decimal currentStock = stockDict.ContainsKey(itemId) ? stockDict[itemId] : 0;

                return new StockVM
                {
                    ItemMasterId = itemId,
                    ItemCode = item.Code,
                    CategoryName = item.Category?.CategoryName ?? "",
                    ItemName = item.Name,
                    Unit1 = item.Unit1 ?? "",
                    Stocks = currentStock,
                    MinQty = item.MinimumQty,
                    MaxQty = item.MaximumQty,
                    Conversion = item.Conversion
                };
            })
            .Where(x => x.Stocks != 0)
            .OrderBy(x => x.ItemName)
            .ToList();

            ViewBag.IsTabletWise = isTabletWise;
            return View(stockList);
        }

        [HttpGet]
        public async Task<IActionResult> CurrentStockRegister(int itemId)
        {
            var ledger = new List<CurrentStockRegisterRowVM>();

            // =========================
            // PURCHASE
            // =========================
            var purchases = await _purchaseservice.GetAll();

            foreach (var purchase in purchases)
            {
                if (purchase.PurchaseItems == null)
                    continue;

                foreach (var item in purchase.PurchaseItems)
                {
                    if (item.ItemId != itemId)
                        continue;

                    ledger.Add(new CurrentStockRegisterRowVM
                    {
                        BillNo = purchase.BillNo,
                        BillDate = purchase.BillDate,
                        Particulars = purchase.billingType == "registered"
    ? $"{purchase.Suppliers?.FirstName} (Supplier)"
    : "Cash ",
                        Receive = item.Qty + item.FreeQty,
                        Issue = 0,
                        NavigateUrl = Url.Action("Edit", "Purchase", new { id = purchase.Id }),
                        Type = "Purchase"
                    });
                }
            }

            // =========================
            // SALES
            // =========================
            var sales = await _salesService.GetAll();

            foreach (var sale in sales)
            {
                if (sale.SalesItems == null)
                    continue;

                foreach (var item in sale.SalesItems)
                {
                    if (item.ItemMasterId != itemId)
                        continue;

                    ledger.Add(new CurrentStockRegisterRowVM
                    {
                        BillNo = sale.BillNo,
                        BillDate = sale.BillDate,
                        Particulars = sale.billingType == "registered"
    ? $"{sale.Customers?.Name} (Customer)"
    : "Cash",
                        Receive = 0,
                        Issue = item.Qty,
                        NavigateUrl = Url.Action("Edit", "Sales", new { id = sale.Id }),
                        Type="Sales"
                    });
                }
            }

            // =========================
            // PURCHASE RETURN
            // =========================
            var purchaseReturns = await _purchasereturnservice.GetAll();

            foreach (var purchaseReturn in purchaseReturns)
            {
                if (purchaseReturn.PurchaseReturnItems == null)
                    continue;

                foreach (var item in purchaseReturn.PurchaseReturnItems)
                {
                    if (item.ItemId != itemId)
                        continue;

                    ledger.Add(new CurrentStockRegisterRowVM
                    {
                        BillNo = purchaseReturn.BillNo,
                        BillDate = purchaseReturn.BillDate,
                        Particulars = $"{purchaseReturn.Suppliers?.FirstName} (Supplier)",
                        Receive = 0,
                        Issue = item.Qty + item.FreeQty,
                        NavigateUrl = Url.Action("Edit", "PurchaseReturn", new { id = purchaseReturn.Id }),
                        Type="Purchase Return"
                    });
                }
            }

            // =========================
            // STOCK RECEIVE
            // =========================
            var stockReceives = await _stockreceiveservice.GetAll();

            foreach (var stockReceive in stockReceives)
            {
                if (stockReceive.StockReceiveItems == null)
                    continue;

                foreach (var item in stockReceive.StockReceiveItems)
                {
                    if (item.ItemMasterId != itemId)
                        continue;

                    ledger.Add(new CurrentStockRegisterRowVM
                    {
                        BillNo = stockReceive.ChallanNo,
                        BillDate = stockReceive.ChallanDate,
                        Particulars = stockReceive.billingType == "registered"
    ? $"{stockReceive.Supplier?.FirstName} (Supplier)"
    : "Cash",
                        Receive = item.Qty,
                        Issue = 0,
                        NavigateUrl = Url.Action("Edit", "StockReceive", new { id = stockReceive.Id }),
                        Type= "Stock Receive"
                    });
                }
            }

            // =========================
            // STOCK ISSUE
            // =========================
            var stockIssues = await _stockissueservice.GetAll();

            foreach (var stockIssue in stockIssues)
            {
                if (stockIssue.StockIssuesItems == null)
                    continue;

                foreach (var item in stockIssue.StockIssuesItems)
                {
                    if (item.ItemMasterId != itemId)
                        continue;

                    ledger.Add(new CurrentStockRegisterRowVM
                    {
                        BillNo = stockIssue.ChallanNo,
                        BillDate = stockIssue.ChallanDate,
                        Particulars = stockIssue.billingType == "registered"
    ? $"{stockIssue.Customers?.Name} (Customer)"
    : "Cash",
                        Receive = 0,
                        Issue = item.Qty,
                        NavigateUrl = Url.Action("Edit", "StockIssue", new { id = stockIssue.Id }),
                        Type="Stock Issue"
                    });
                }
            }
            // =========================
            // SALES RETURN / STOCK RETURN
            // =========================
            var stockReturns = await _stockrturnservice.GetAll();

            foreach (var stockReturn in stockReturns)
            {
                if (stockReturn.StockReturnItems == null)
                    continue;

                foreach (var item in stockReturn.StockReturnItems)
                {
                    if (item.ItemMasterId != itemId)
                        continue;

                    ledger.Add(new CurrentStockRegisterRowVM
                    {
                        BillNo = stockReturn.ChallanNo,
                        BillDate = stockReturn.ChallanDate,

                        Particulars = stockReturn.billingType == "registered"
                            ? $"{stockReturn.Customers?.Name} (Customer)"
                            : "Cash",

                        Receive = item.Qty,
                        Issue = 0,

                        NavigateUrl = Url.Action("Edit", "StockReturn",
                            new { id = stockReturn.Id }),
                        Type= "Sales Return"
                    });
                }
            }
            // =========================
            // SORTING
            // =========================
            ledger = ledger
                .OrderBy(x => x.BillDate)
                .ToList();

            // =========================
            // RUNNING BALANCE
            // =========================
            decimal balance = 0;

            foreach (var row in ledger)
            {
                balance += row.Receive - row.Issue;

                row.Balance = balance;

                row.ReceiveDisplay = row.Receive.ToString("0.##");
                row.IssueDisplay = row.Issue.ToString("0.##");
                row.BalanceDisplay = row.Balance.ToString("0.##");
            }

            // =========================
            // ITEM DETAILS
            // =========================
            var itemMaster = (await _itemmasterservice.GetAll())
                .FirstOrDefault(x => x.Id == itemId);

            var model = new CurrentStockRegisterVM
            {
                ItemId = itemId,
                ItemName = itemMaster?.Name,
                Unit = itemMaster?.Unit1,

                // YE CURRENTSTOCK TABLE KA SAVED DATA H
                CurrentStock = (await _currenstockService.GetAll())
                    .Where(x => x.ItemId == itemId)
                    .Sum(x => x.Qty),

                CurrentStockDisplay = (await _currenstockService.GetAll())
                    .Where(x => x.ItemId == itemId)
                    .Sum(x => x.Qty)
                    .ToString("0.##"),

                Rows = ledger
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CurrentStockBatchDetails(int itemId)
        {
            var itemMaster = (await _itemmasterservice.GetAll())
                .FirstOrDefault(x => x.Id == itemId);

            if (itemMaster == null)
            {
                return RedirectToAction("CurrentStock");
            }

            var currentStocks = await _currenstockService.GetAll();

            var rows = currentStocks
                .Where(x => x.ItemId == itemId && x.Qty != 0)
                .Select(x => new CurrentStockBatchDetailsRowVM
                {
                    Batch = x.Batch,
                    Stock = x.Qty,
                    StockDisplay = x.Qty.ToString("0.##"),
                    Unit = itemMaster.Unit1,
                    SalesRateA = x.SalesRateA,
                    PurchaseRate = x.PurchaseRate,
                    Mrp = x.Mrp,
                    ExpiryDate = x.ExpiryDate,

                    NavigateUrl = Url.Action(
                        "CurrentStockRegister",
                        "Purchase",
                        new { itemId = itemId }
                    )
                })
                .OrderBy(x => x.Batch)
                .ToList();

            var model = new CurrentStockBatchDetailsVM
            {
                ItemId = itemId,
                ItemName = itemMaster.Name,
                Unit = itemMaster.Unit1,
                Rows = rows
            };

            return View(model);
        }

        //Current Stock Excel
        [HttpGet]
        public async Task<IActionResult> CurrentStockExcel()
        {
            var today = DateTime.Today;
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            var itemMasters = await _itemmasterservice.GetAll();
            var currentStocks = await _currenstockService.GetAll();

            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var stockList = itemMasters.Select(item =>
            {
                decimal stock = stockDict.ContainsKey(item.Id) ? stockDict[item.Id] : 0;

                return new
                {
                    item.Name,
                    Category = item.Category?.CategoryName ?? "",
                    Unit = item.Unit1 ?? "",
                    Stock = stock,
                    Conversion = item.Conversion
                };
            })
            .Where(x => x.Stock != 0)
            .OrderBy(x => x.Name)
            .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Current Stock");

            var user = await _userManager.GetUserAsync(User);
            string companyName = (await _tenantService.GetById(user.TenantId))?.Name ?? "Company";

            // COMPANY
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 4).Merge().Style
                .Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TITLE
            ws.Cell(2, 1).Value = "Current Stock Report";
            ws.Range(2, 1, 2, 4).Merge().Style
                .Font.SetBold().Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // DATE
            ws.Cell(3, 1).Value = $"Date : {today:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 4).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "Item Name", "Category", "Unit", "Stock" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 4).Style.Font.SetBold();
            row++;

            decimal totalStock = 0;

            foreach (var x in stockList)
            {
                ws.Cell(row, 1).Value = x.Name;
                ws.Cell(row, 2).Value = x.Category;
                ws.Cell(row, 3).Value = x.Unit;

                // ✅ Tablet wise format
                if (isTabletWise && x.Conversion > 0)
                {
                    int totalTabs = (int)Math.Round(x.Stock * x.Conversion);
                    int strips = totalTabs / x.Conversion;
                    int tablets = totalTabs % x.Conversion;

                    ws.Cell(row, 4).Value = $"{strips}:{tablets}";
                }
                else
                {
                    ws.Cell(row, 4).Value = x.Stock;
                }

                totalStock += x.Stock;
                row++;
            }


            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "CurrentStock.xlsx");
        }

        //Current Stock PDF 
        [HttpGet]
        public async Task<IActionResult> CurrentStockPdf()
        {
            var today = DateTime.Today;
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            var itemMasters = await _itemmasterservice.GetAll();
            var currentStocks = await _currenstockService.GetAll();

            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var stockList = itemMasters.Select(item =>
            {
                decimal stock = stockDict.ContainsKey(item.Id) ? stockDict[item.Id] : 0;

                return new
                {
                    item.Name,
                    Category = item.Category?.CategoryName ?? "",
                    Unit = item.Unit1 ?? "",
                    Stock = stock,
                    Conversion = item.Conversion
                };
            })
            .Where(x => x.Stock != 0)
            .OrderBy(x => x.Name)
            .ToList();

            using var ms = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(ms));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _userManager.GetUserAsync(User);
            string companyName = (await _tenantService.GetById(user.TenantId))?.Name ?? "Company";

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // COMPANY
            doc.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            // TITLE
            doc.Add(new Paragraph("Current Stock Report")
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            // DATE
            doc.Add(new Paragraph($"Date : {today:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            Table table = new Table(new float[] { 6, 4, 3, 3 }).UseAllAvailableWidth();

            string[] headers = { "Item Name", "Category", "Unit", "Stock" };

            foreach (var h in headers)
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(9)));

            decimal totalStock = 0;

            foreach (var x in stockList)
            {
                table.AddCell(new Paragraph(x.Name).SetFontSize(9));
                table.AddCell(new Paragraph(x.Category).SetFontSize(9));
                table.AddCell(new Paragraph(x.Unit).SetFontSize(9));

                string stockValue;

                if (isTabletWise && x.Conversion > 0)
                {
                    int totalTabs = (int)Math.Round(x.Stock * x.Conversion);
                    int strips = totalTabs / x.Conversion;
                    int tablets = totalTabs % x.Conversion;

                    stockValue = $"{strips}:{tablets}";
                }
                else
                {
                    stockValue = x.Stock.ToString("0.##");
                }

                table.AddCell(new Paragraph(stockValue).SetFontSize(9));

                totalStock += x.Stock;
            }


            doc.Add(table);
            doc.Close();

            return File(ms.ToArray(), "application/pdf", "CurrentStock.pdf");
        }

        //private sealed class ReceivedStockLine
        //{
        //    public int ItemId { get; init; }
        //    public int? SupplierId { get; init; }
        //    public string? Batch { get; init; }
        //    public DateTime? ExpiryDate { get; init; }
        //    public decimal Mrp { get; init; }
        //    public decimal Qty { get; init; }
        //}

        private async Task<List<ReceivedStockLine>> BuildReceivedStockLinesAsync(DateTime? uptoDate = null)
        {
            var purchases = await _purchaseservice.GetAll();
            var purchaseLines = purchases
                .Where(x => !uptoDate.HasValue || x.BillDate <= uptoDate)
                .SelectMany(x => (x.PurchaseItems ?? new List<PurchaseItem>()).Select(item => new ReceivedStockLine
                {
                    ItemId = item.ItemId,
                    SupplierId = x.SupplierId,
                    Batch = item.Batch,
                    ExpiryDate = item.ExpiryDate,
                    Mrp = item.Mrp,
                    Qty = item.Qty + item.FreeQty
                }));

            var purchaseChallans = await _purchaseChallanRepository.GetAll();
            var challanLines = purchaseChallans
                .Where(x =>
                    (!uptoDate.HasValue || x.BillDate <= uptoDate) &&
                    !x.ConvertedPurchaseId.HasValue &&
                    !string.Equals(x.Status, "Converted", StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => (x.PurchaseChallanItems ?? new List<PurchaseChallanItem>()).Select(item => new ReceivedStockLine
                {
                    ItemId = item.ItemId,
                    SupplierId = x.SupplierId,
                    Batch = item.Batch,
                    ExpiryDate = item.ExpiryDate,
                    Mrp = item.Mrp,
                    Qty = item.Qty + item.FreeQty
                }));

            return purchaseLines.Concat(challanLines).ToList();
        }

        private async Task<Dictionary<int, decimal>> BuildReceivedQtyDictionaryAsync(DateTime? uptoDate = null)
        {
            var receivedLines = await BuildReceivedStockLinesAsync(uptoDate);

            return receivedLines
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));
        }
        public async Task<IActionResult> PurchaseReportItemWise(DateTime? fromDate, DateTime? toDate, int? supplierId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            ViewBag.Suppliers = (await _supplierservice.GetALL())
                .Select(s => new { s.Id, Text = s.FirstName })
                .ToList();
            ViewBag.SelectedSupplierId = supplierId;

            var itemMasters = await _itemmasterservice.GetAll();

            var purchasesQuery = (await _purchaseservice.GetAll())
                .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate);

            if (supplierId.HasValue && supplierId.Value > 0)
                purchasesQuery = purchasesQuery.Where(p => p.SupplierId == supplierId.Value);

            var purchases = purchasesQuery.ToList();

            var flat = purchases
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems.Select(pi => new
                {
                    Item = pi,
                    //SupplierName = (p.Suppliers != null ? p.Suppliers.FirstName : "-"),
                    SupplierName = p.billingType == "walkin" ? "Cash" : (p.Suppliers != null ? p.Suppliers.FirstName : "-")
                }))
                .ToList();

            var grouped = flat
                .GroupBy(x => new { x.Item.ItemId, x.SupplierName })
                .Select(g => new
                {
                    ItemMasterId = g.Key.ItemId,
                    SupplierName = g.Key.SupplierName,
                    TotalQty = g.Sum(x => (decimal)x.Item.Qty),
                    TotalAmount = g.Sum(x => (decimal)x.Item.TotalAmt),   // ✅ saved amount
                    Rate = g.Any() ? (decimal)g.First().Item.Rate : 0     // ✅ show only
                })
                .ToList();

            var purchaseList = (from item in itemMasters
                                join g in grouped on item.Id equals g.ItemMasterId
                                where g.TotalQty > 0
                                select new StockVM
                                {
                                    ItemMasterId = item.Id,
                                    ItemCode = item.Code,
                                    CategoryName = item.Category?.CategoryName ?? "",
                                    ItemName = item.Name,
                                    Stocks = g.TotalQty,
                                    Rate = g.Rate,
                                    Amount = g.TotalAmount,
                                    SupplierName = g.SupplierName
                                }).ToList();

            ViewBag.TotalQty = purchaseList.Sum(x => x.Stocks);
            ViewBag.TotalAmount = purchaseList.Sum(x => x.Amount);
            return View(purchaseList);
        }

        //Export to excel
        [HttpGet]
        public async Task<IActionResult> ExportPurchaseItemWiseExcel(DateTime? fromDate, DateTime? toDate, int? supplierId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();

            var purchaseQuery = (await _purchaseservice.GetAll())
                .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate);

            if (supplierId.HasValue && supplierId.Value > 0)
                purchaseQuery = purchaseQuery.Where(p => p.SupplierId == supplierId.Value);

            var purchase = purchaseQuery.ToList();

            var flat = purchase
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems.Select(pi => new
                {
                    ItemId = pi.ItemId,
                    pi.Qty,
                    pi.Rate,
                    pi.TotalAmt,
                    SupplierName = p.billingType == "walkin"
                        ? "Cash"
                        : (p.Suppliers != null ? p.Suppliers.FirstName : "-")
                }))
                .ToList();

            var grouped = flat
                .GroupBy(x => new { x.ItemId, x.SupplierName })
                .Select(g => new
                {
                    ItemId = g.Key.ItemId,
                    SupplierName = g.Key.SupplierName,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.TotalAmt),
                    Rate = g.First().Rate
                })
                .ToList();

            var purchaseList = (from item in itemMasters
                                join g in grouped on item.Id equals g.ItemId
                                select new StockVM
                                {
                                    ItemCode = item.Code,
                                    ItemName = item.Name,
                                    CategoryName = item.Category?.CategoryName ?? "",
                                    SupplierName = g.SupplierName,
                                    Stocks = g.TotalQty,
                                    Rate = g.Rate,
                                    Amount = g.TotalAmount
                                }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Purchase Item Wise");

            var user = await _userManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Purchase Item Wise Report";
            ws.Range(2, 1, 2, 7).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 7).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers =
            {
        "Item Code","Item Name","Category","Supplier",
        "Total Qty","Rate","Total Amount"
    };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, headers.Length).Style.Font.SetBold();
            row++;

            foreach (var item in purchaseList)
            {
                ws.Cell(row, 1).Value = item.ItemCode;
                ws.Cell(row, 2).Value = item.ItemName;
                ws.Cell(row, 3).Value = item.CategoryName;
                ws.Cell(row, 4).Value = item.SupplierName;
                ws.Cell(row, 5).Value = item.Stocks;
                ws.Cell(row, 6).Value = item.Rate;
                ws.Cell(row, 7).Value = item.Amount;
                row++;
            }

            ws.Cell(row, 4).Value = "TOTAL";
            ws.Cell(row, 5).Value = purchaseList.Sum(x => x.Stocks);
            ws.Cell(row, 7).Value = purchaseList.Sum(x => x.Amount);

            ws.Range(row, 4, row, 7).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PurchaseItemWise.xlsx");
        }
        //Export to pdf
        [HttpGet]
        public async Task<IActionResult> ExportPurchaseItemWisePdf(DateTime? fromDate, DateTime? toDate, int? supplierId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();

            var purchaseQuery = (await _purchaseservice.GetAll())
                .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate);

            if (supplierId.HasValue && supplierId.Value > 0)
                purchaseQuery = purchaseQuery.Where(p => p.SupplierId == supplierId.Value);

            var purchase = purchaseQuery.ToList();

            var flat = purchase
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems.Select(pi => new
                {
                    ItemId = pi.ItemId,
                    pi.Qty,
                    pi.Rate,
                    pi.TotalAmt,
                    SupplierName = p.billingType == "walkin"
                        ? "Cash"
                        : (p.Suppliers != null ? p.Suppliers.FirstName : "-")
                }))
                .ToList();

            var grouped = flat
                .GroupBy(x => new { x.ItemId, x.SupplierName })
                .Select(g => new
                {
                    ItemId = g.Key.ItemId,
                    SupplierName = g.Key.SupplierName,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.TotalAmt),
                    Rate = g.First().Rate
                })
                .ToList();

            var purchaseList = (from item in itemMasters
                                join g in grouped on item.Id equals g.ItemId
                                select new StockVM
                                {
                                    ItemCode = item.Code,
                                    ItemName = item.Name,
                                    CategoryName = item.Category?.CategoryName ?? "",
                                    SupplierName = g.SupplierName,
                                    Stocks = g.TotalQty,
                                    Rate = g.Rate,
                                    Amount = g.TotalAmount
                                }).ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _userManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            document.Add(new Paragraph(companyName)
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Purchase Item Wise Report")
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(5));

            Table table = new Table(new float[] { 3, 5, 5, 4, 2, 2, 3 })
                .UseAllAvailableWidth();

            string[] headers =
            {
        "Item Code","Item Name","Category","Supplier",
        "Qty","Rate","Total Amount"
    };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(9))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            foreach (var item in purchaseList)
            {
                table.AddCell(new Cell().Add(new Paragraph(item.ItemCode).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(item.ItemName).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(item.CategoryName).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(item.SupplierName).SetFontSize(9)));

                table.AddCell(new Cell().Add(new Paragraph(item.Stocks.ToString()).SetFontSize(9))
                    .SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Cell().Add(new Paragraph(item.Rate.ToString("0.00")).SetFontSize(9))
                    .SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Cell().Add(new Paragraph(item.Amount.ToString()).SetFontSize(9))
                    .SetTextAlignment(TextAlignment.RIGHT));
            }

            table.AddCell(new Cell(1, 4)
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(9))
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Cell().Add(new Paragraph(purchaseList.Sum(x => x.Stocks).ToString()).SetFontSize(9))
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Cell());

            table.AddCell(new Cell().Add(new Paragraph(purchaseList.Sum(x => x.Amount).ToString()).SetFontSize(9))
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "PurchaseItemWise.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> PurchaseReportBillWise(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var purchase = await _purchaseservice.GetAll() ?? new List<Purchase>();

            var filtered = purchase
                .Where(p => p.BillDate.HasValue && p.BillDate.Value.Date >= fromDate.Value.Date && p.BillDate.Value.Date <= toDate.Value.Date)
                .ToList();

            var billwiseList = filtered.Select(p =>
            {
                var items = p.PurchaseItems ?? new List<PurchaseItem>();

                // ✅ STEP 1: Items se CGST + SGST nikalo
                decimal cgst = items.Sum(x => x.CGstAmount);
                decimal sgst = items.Sum(x => x.SGstAmount);

                // ✅ STEP 2: Items se CESS calculate karo (% se)
                decimal cess = items.Sum(x =>
                {
                    decimal baseAmt = (x.Qty * x.Rate) - x.DiscountAmt;
                    return baseAmt > 0 ? (baseAmt * x.Cess / 100) : 0;
                });

                // ✅ STEP 3: Total GST = CGST + SGST + CESS
                decimal gstAmt = cgst + sgst + cess;

                // ✅ STEP 4: Fallback - Agar phir bhi 0 aaye toh TotalPayable - Total se nikalo
                if (gstAmt == 0 && p.TotalPayable > 0 && p.Total > 0)
                {
                    gstAmt = p.TotalPayable - p.Total;
                    if (gstAmt < 0) gstAmt = 0;
                }

                return new PurchaseVM
                {
                    Id = p.Id,
                    BillNo = p.BillNo,
                    BillDate = p.BillDate,
                    SupplierName = p.billingType == "walkin" ? "Cash" : p.Suppliers?.FirstName ?? "",
                    TaxableAmount = p.Total,
                    TotalGstAmt = gstAmt,
                    TotalPayable = p.TotalPayable
                };
            })
              .Where(x => x.TotalPayable > 0)
            .OrderBy(x => x.BillDate)
            .ToList();

            ViewBag.TotalAmount = billwiseList.Sum(x => x.TotalPayable);
            ViewBag.TotalGstAmount = billwiseList.Sum(x => x.TotalGstAmt);
            ViewBag.TotalTaxableAmount = billwiseList.Sum(x => (decimal)x.TaxableAmount);

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            return View(billwiseList ?? new List<PurchaseVM>());
        }
        //Export to Excel
        [HttpGet]
        public async Task<IActionResult> ExportPurchaseBillWiseExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var purchases = await _purchaseservice.GetAll();

            var data = purchases
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .Select(p =>
                {
                    var items = p.PurchaseItems ?? new List<PurchaseItem>();

                    decimal cgst = items.Sum(x => x.CGstAmount);
                    decimal sgst = items.Sum(x => x.SGstAmount);
                    decimal cess = items.Sum(x =>
                    {
                        decimal baseAmt = (x.Qty * x.Rate) - x.DiscountAmt;
                        return baseAmt > 0 ? (baseAmt * x.Cess / 100) : 0;
                    });

                    decimal gstAmt = cgst + sgst + cess;

                    if (gstAmt == 0 && p.TotalPayable > 0 && p.Total > 0)
                    {
                        gstAmt = p.TotalPayable - p.Total;
                        if (gstAmt < 0) gstAmt = 0;
                    }

                    return new PurchaseVM
                    {
                        BillNo = p.BillNo,
                        BillDate = p.BillDate,
                        SupplierName = p.Suppliers?.FirstName ?? "Unknown",
                        TaxableAmount = p.Total,
                        TotalGstAmt = Math.Round(gstAmt, 2),
                        TotalPayable = p.TotalPayable
                    };
                })
                .Where(x => x.TotalPayable > 0)
                .OrderBy(x => x.BillDate)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Purchase Bill Wise");

            var user = await _userManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Purchase Bill Wise Report";
            ws.Range(2, 1, 2, 6).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 6).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "Bill No", "Bill Date", "Supplier", "Taxable Amount", "GST Amount", "Total Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 6).Style.Font.SetBold();
            row++;

            foreach (var x in data)
            {
                ws.Cell(row, 1).Value = x.BillNo;
                ws.Cell(row, 2).Value = x.BillDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 3).Value = x.SupplierName;
                ws.Cell(row, 4).Value = Math.Round(Convert.ToDecimal(x.TaxableAmount), 2);
                ws.Cell(row, 5).Value = Math.Round(Convert.ToDecimal(x.TotalGstAmt), 2);
                ws.Cell(row, 6).Value = Math.Round(Convert.ToDecimal(x.TotalPayable), 2);
                row++;
            }

            // TOTAL ROW
            ws.Cell(row, 1).Value = "Total";
            ws.Cell(row, 4).Value = Math.Round(data.Sum(x => Convert.ToDecimal(x.TaxableAmount)), 2);
            ws.Cell(row, 5).Value = Math.Round(data.Sum(x => Convert.ToDecimal(x.TotalGstAmt)), 2);
            ws.Cell(row, 6).Value = Math.Round(data.Sum(x => Convert.ToDecimal(x.TotalPayable)), 2);

            ws.Range(row, 1, row, 6).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PurchaseBillWise.xlsx");
        }
        //EXPORT TO PDF
        [HttpGet]
        public async Task<IActionResult> ExportPurchaseBillWisePdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var purchases = await _purchaseservice.GetAll();

            var data = purchases
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .Select(p =>
                {
                    var items = p.PurchaseItems ?? new List<PurchaseItem>();

                    decimal cgst = items.Sum(x => x.CGstAmount);
                    decimal sgst = items.Sum(x => x.SGstAmount);
                    decimal cess = items.Sum(x =>
                    {
                        decimal baseAmt = (x.Qty * x.Rate) - x.DiscountAmt;
                        return baseAmt > 0 ? (baseAmt * x.Cess / 100) : 0;
                    });

                    decimal gstAmt = cgst + sgst + cess;

                    if (gstAmt == 0 && p.TotalPayable > 0 && p.Total > 0)
                    {
                        gstAmt = p.TotalPayable - p.Total;
                        if (gstAmt < 0) gstAmt = 0;
                    }

                    return new PurchaseVM
                    {
                        BillNo = p.BillNo,
                        BillDate = p.BillDate,
                        SupplierName = p.Suppliers?.FirstName ?? "Unknown",
                        TaxableAmount = p.Total,
                        TotalGstAmt = Math.Round(gstAmt, 2),
                        TotalPayable = p.TotalPayable
                    };
                })
                .Where(x => x.TotalPayable > 0)
                .OrderBy(x => x.BillDate)
                .ToList();

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _userManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // HEADER (UNCHANGED)
            doc.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph("Purchase Bill Wise Report")
                .SetFont(bold).SetFontSize(13)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10)
                .SetFontSize(9));

            // TABLE
            Table table = new Table(6).UseAllAvailableWidth();

            string[] headers = { "Bill No", "Bill Date", "Supplier", "Taxable Amount", "GST Amount", "Total Amount" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(
                    new Cell().Add(new Paragraph(h)
                        .SetFont(bold)
                        .SetFontSize(9))   // ✅ FIX FONT SIZE
                );
            }

            foreach (var x in data)
            {
                table.AddCell(new Paragraph(x.BillNo).SetFont(normal).SetFontSize(9));
                table.AddCell(new Paragraph(x.BillDate?.ToString("dd-MM-yyyy")).SetFont(normal).SetFontSize(9));
                table.AddCell(new Paragraph(x.SupplierName).SetFont(normal).SetFontSize(9));

                table.AddCell(new Paragraph(x.TaxableAmount.ToString()).SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(x.TotalGstAmt.ToString("0.00")).SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(x.TotalPayable.ToString("0.00")).SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 3)
                .Add(new Paragraph("Total")
                    .SetFont(bold)
                    .SetFontSize(9)));

            table.AddCell(new Paragraph(data.Sum(x => (decimal)x.TaxableAmount).ToString())
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(data.Sum(x => x.TotalGstAmt).ToString("0.00"))
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(data.Sum(x => x.TotalPayable).ToString("0.00"))
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "PurchaseBillWise.pdf");
        }
        public async Task<IActionResult> PurchaseReportCompanyWise(DateTime? fromDate, DateTime? toDate, int? companyId, int? supplierId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            var itemMasters = await _itemmasterservice.GetAll();
            var companies = await _companyServices.GetAll();

            ViewBag.companydata = companies;
            ViewBag.SelectedCompanyId = companyId;

            // ✅ Supplier dropdown
            ViewBag.Suppliers = (await _supplierservice.GetALL())
                .Select(s => new { s.Id, Text = s.FirstName })   // ⚠️ supplier display field
                .ToList();

            ViewBag.SelectedSupplierId = supplierId;

            // ✅ Purchases (date filter)
            var purchasesQuery = (await _purchaseservice.GetAll())
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate);

            // ✅ Supplier filter
            if (supplierId.HasValue && supplierId.Value > 0)
                purchasesQuery = purchasesQuery.Where(x => x.SupplierId == supplierId.Value);

            var purchases = purchasesQuery.ToList();

            // ✅ Purchase Items flatten
            var purchaseItems = purchases
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems.Select(pi => new
                {
                    Item = pi,
                    SupplierId = p.SupplierId,
                    // SupplierName = (p.Suppliers != null ? p.Suppliers.FirstName : "-") 
                    SupplierName = p.billingType == "walkin" ? "Cash" : (p.Suppliers != null ? p.Suppliers.FirstName : "-")
                }))
                .ToList();

            // ✅ Company filter
            if (companyId.HasValue && companyId.Value > 0)
            {
                purchaseItems = purchaseItems
                    .Where(x => x.Item.ItemMasters != null && x.Item.ItemMasters.CompanyId == companyId.Value)
                    .ToList();
            }

            // ✅ Group item + supplier wise
            var grouped = purchaseItems
                .GroupBy(x => new { x.Item.ItemId, x.SupplierId, x.SupplierName })
                .Select(g =>
                {
                    decimal totalQty = g.Sum(x => (decimal)x.Item.Qty);

                    // ✅ IMPORTANT: Use saved amount from purchase entry
                    decimal totalAmount = g.Sum(x => (decimal)x.Item.TotalAmt);

                    // ✅ Rate show (no amount calculation)
                    // If you want "saved rate only", use First() rate
                    decimal rate = g.Any() ? (decimal)g.First().Item.Rate : 0;

                    return new
                    {
                        ItemMasterId = g.Key.ItemId,
                        SupplierName = g.Key.SupplierName,
                        TotalQty = totalQty,
                        TotalAmount = totalAmount,
                        Rate = rate
                    };
                })
                .ToList();

            // ✅ Join with ItemMasters
            var stockList = (from item in itemMasters
                             join g in grouped on item.Id equals g.ItemMasterId
                             where g.TotalQty > 0
                             select new StockVM
                             {
                                 ItemMasterId = item.Id,
                                 ItemCode = item.Code,
                                 CategoryName = item.Category?.CategoryName ?? "",
                                 ItemName = item.Name,
                                 Unit1 = item.Unit1 ?? "",
                                 Unit2 = item.Unit2 ?? "",
                                 Stocks = g.TotalQty,
                                 Rate = g.Rate,
                                 Amount = g.TotalAmount,
                                 SupplierName = g.SupplierName
                             }).ToList();

            // ✅ Totals
            var totalQtyAll = stockList.Sum(x => x.Stocks);
            var totalAmtAll = stockList.Sum(x => x.Amount);

            ViewBag.TotalQty = totalQtyAll;
            ViewBag.TotalAmount = totalAmtAll;

            // ✅ FIX: TotalRate ko Amount/Qty se calculate mat karo (420/20=21 issue)
            // Here: Avg of displayed rates
            ViewBag.TotalRate = stockList.Any() ? stockList.Average(x => x.Rate) : 0;

            return View(stockList);
        }

        //EXPORT TO EXCEL
        [HttpGet]
        public async Task<IActionResult> ExportPurchaseCompanyWiseExcel(DateTime? fromDate, DateTime? toDate, int? companyId, int? supplierId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();

            var purchaseQuery = (await _purchaseservice.GetAll())
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate);

            if (supplierId.HasValue && supplierId.Value > 0)
                purchaseQuery = purchaseQuery.Where(x => x.SupplierId == supplierId.Value);

            var purchases = purchaseQuery.ToList();

            var flat = purchases
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems.Select(pi => new
                {
                    ItemId = pi.ItemId,
                    pi.Qty,
                    pi.Rate,
                    pi.TotalAmt,
                    CompanyId = pi.ItemMasters?.CompanyId,
                    SupplierName = p.billingType == "walkin"
                        ? "Cash"
                        : (p.Suppliers != null ? p.Suppliers.FirstName : "-")
                }))
                .ToList();

            if (companyId.HasValue && companyId.Value > 0)
                flat = flat.Where(x => x.CompanyId == companyId.Value).ToList();

            var grouped = flat
                .GroupBy(x => new { x.ItemId, x.SupplierName })
                .Select(g => new
                {
                    ItemId = g.Key.ItemId,
                    SupplierName = g.Key.SupplierName,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.TotalAmt),
                    Rate = g.First().Rate
                }).ToList();

            var purchaseList = (from item in itemMasters
                                join g in grouped on item.Id equals g.ItemId
                                select new StockVM
                                {
                                    ItemCode = item.Code,
                                    ItemName = item.Name,
                                    CategoryName = item.Category?.CategoryName ?? "",
                                    SupplierName = g.SupplierName,
                                    Stocks = g.TotalQty,
                                    Rate = g.Rate,
                                    Amount = g.TotalAmount
                                }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Company Wise Purchase");

            var user = await _userManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // COMPANY NAME
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 7).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // REPORT NAME
            ws.Cell(2, 1).Value = "Purchase Company Wise Report";
            ws.Range(2, 1, 2, 7).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // DATE
            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 7).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "Item Code", "Category", "Item Name", "Unit", "Qty", "GST", "Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 7).Style.Font.SetBold();
            row++;

            foreach (var item in purchaseList)
            {
                ws.Cell(row, 1).Value = item.ItemCode;
                ws.Cell(row, 2).Value = item.CategoryName;
                ws.Cell(row, 3).Value = item.ItemName;
                ws.Cell(row, 4).Value = item.SupplierName;
                ws.Cell(row, 5).Value = item.Stocks;
                ws.Cell(row, 6).Value = 0;
                ws.Cell(row, 7).Value = item.Amount;
                row++;
            }

            ws.Cell(row, 4).Value = "TOTAL";
            ws.Cell(row, 5).Value = purchaseList.Sum(x => x.Stocks);
            ws.Cell(row, 7).Value = purchaseList.Sum(x => x.Amount);

            ws.Range(row, 4, row, 7).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PurchaseCompanyWise.xlsx");
        }
        // EXPORT TO PDF
        [HttpGet]
        public async Task<IActionResult> ExportPurchaseCompanyWisePdf(DateTime? fromDate, DateTime? toDate, int? companyId, int? supplierId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var itemMasters = await _itemmasterservice.GetAll();

            var purchaseQuery = (await _purchaseservice.GetAll())
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate);

            if (supplierId.HasValue && supplierId.Value > 0)
                purchaseQuery = purchaseQuery.Where(x => x.SupplierId == supplierId.Value);

            var purchases = purchaseQuery.ToList();

            var flat = purchases
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems.Select(pi => new
                {
                    ItemId = pi.ItemId,
                    pi.Qty,
                    pi.Rate,
                    pi.TotalAmt,
                    CompanyId = pi.ItemMasters?.CompanyId,
                    SupplierName = p.billingType == "walkin"
                        ? "Cash"
                        : (p.Suppliers != null ? p.Suppliers.FirstName : "-")
                }))
                .ToList();

            if (companyId.HasValue && companyId.Value > 0)
                flat = flat.Where(x => x.CompanyId == companyId.Value).ToList();

            var grouped = flat
                .GroupBy(x => new { x.ItemId, x.SupplierName })
                .Select(g => new
                {
                    ItemId = g.Key.ItemId,
                    SupplierName = g.Key.SupplierName,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.TotalAmt),
                    Rate = g.First().Rate
                }).ToList();

            var purchaseList = (from item in itemMasters
                                join g in grouped on item.Id equals g.ItemId
                                select new StockVM
                                {
                                    ItemCode = item.Code,
                                    ItemName = item.Name,
                                    CategoryName = item.Category?.CategoryName ?? "",
                                    SupplierName = g.SupplierName,
                                    Stocks = g.TotalQty,
                                    Rate = g.Rate,
                                    Amount = g.TotalAmount
                                }).ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _userManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            document.Add(new Paragraph(companyName).SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph("Purchase Company Wise Report").SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            Table table = new Table(new float[] { 3, 4, 6, 3, 2, 2, 3 }).UseAllAvailableWidth();

            string[] headers = { "Item Code", "Category", "Item Name", "Supplier", "Qty", "GST", "Amount" };

            foreach (var h in headers)
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(9)));

            foreach (var item in purchaseList)
            {
                table.AddCell(new Paragraph(item.ItemCode).SetFontSize(9));
                table.AddCell(new Paragraph(item.CategoryName).SetFontSize(9));
                table.AddCell(new Paragraph(item.ItemName).SetFontSize(9));
                table.AddCell(new Paragraph(item.SupplierName).SetFontSize(9));
                table.AddCell(new Paragraph(item.Stocks.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph("0.00").SetFontSize(9));
                table.AddCell(new Paragraph(item.Amount.ToString()).SetFontSize(9));
            }

            table.AddCell(new Cell(1, 4).Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(9)));
            table.AddCell(new Paragraph(purchaseList.Sum(x => x.Stocks).ToString()).SetFontSize(9));
            table.AddCell(new Paragraph("0.00").SetFontSize(9));
            table.AddCell(new Paragraph(purchaseList.Sum(x => x.Amount).ToString()).SetFontSize(9));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "PurchaseCompanyWise.pdf");
        }
        public async Task<IActionResult> PurchaseReportSupplierWise(DateTime? fromDate, DateTime? toDate, int? SupplierId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var supplier = await _supplierservice.GetALL();

            ViewBag.Supplier = supplier;
            ViewBag.SelectedSupplierId = SupplierId;
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            var purchase = await _purchaseservice.GetAll();

            purchase = purchase
                .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate)
                .ToList();

            if (SupplierId.HasValue && SupplierId.Value > 0)
            {
                purchase = purchase.Where(p => p.SupplierId == SupplierId.Value).ToList();
            }

            // Bill-wise data calculation
            var billwiseList = purchase.Select(sale =>
            {
                // Calculate Total Tax (GST + Cess) for this specific Bill from Items
                decimal totalTax = sale.PurchaseItems?.Sum(item =>
                {
                    decimal taxableAmount = (item.Qty * item.Rate) - item.DiscountAmt;

                    // GST from CGST+SGST if available, else from %, else 0
                    decimal gst = (item.CGstAmount + item.SGstAmount) > 0
                        ? (item.CGstAmount + item.SGstAmount)
                        : (item.Gst > 0 ? (taxableAmount * item.Gst) / 100 : 0);

                    // Cess from %
                    decimal cess = item.Cess > 0 ? (taxableAmount * item.Cess) / 100 : 0;

                    return gst + cess;
                }) ?? 0;

                return new PurchaseVM
                {
                    Id = sale.Id,
                    BillNo = sale.BillNo,
                    BillDate = sale.BillDate,
                    SupplierName = sale.Suppliers?.FirstName ?? "Unknown",
                    TotalGstAmt = totalTax,
                    TotalPayable = sale.PurchaseItems?.Sum(item => item.TotalAmt) ?? 0
                };
            })
                        .OrderBy(x => x.BillDate)
            .ToList();

            ViewBag.TotalGst = billwiseList.Sum(x => x.TotalGstAmt);
            ViewBag.TotalAmount = billwiseList.Sum(x => x.TotalPayable);

            return View(billwiseList);
        }
        //EXPORT TO EXCEL
        [HttpGet]
        public async Task<IActionResult> ExportPurchaseSupplierWiseExcel(DateTime? fromDate, DateTime? toDate, int? SupplierId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var purchase = await _purchaseservice.GetAll();

            purchase = purchase
                .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate)
                .ToList();

            if (SupplierId.HasValue && SupplierId.Value > 0)
                purchase = purchase.Where(p => p.SupplierId == SupplierId.Value).ToList();

            var list = purchase.Select(p => new PurchaseVM
            {
                BillNo = p.BillNo,
                BillDate = p.BillDate,
                SupplierName = p.Suppliers?.FirstName ?? "Unknown",
                TotalGstAmt = p.PurchaseItems?.Sum(i => i.CGstAmount + i.SGstAmount) ?? 0,
                TotalPayable = p.PurchaseItems?.Sum(i => i.TotalAmt) ?? 0
            }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Supplier Wise Purchase");

            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // COMPANY NAME
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 5).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TITLE
            ws.Cell(2, 1).Value = "Purchase Report - Supplier Wise";
            ws.Range(2, 1, 2, 5).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // DATE
            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 5).Merge().Style
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "Bill No", "Bill Date", "Supplier", "GST Amount", "Total Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 5).Style.Font.SetBold();
            row++;

            foreach (var i in list)
            {
                ws.Cell(row, 1).Value = i.BillNo;
                ws.Cell(row, 2).Value = i.BillDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 3).Value = i.SupplierName;
                ws.Cell(row, 4).Value = i.TotalGstAmt;
                ws.Cell(row, 5).Value = i.TotalPayable;
                row++;
            }

            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 4).Value = list.Sum(x => x.TotalGstAmt);
            ws.Cell(row, 5).Value = list.Sum(x => x.TotalPayable);

            ws.Range(row, 1, row, 5).Style.Font.SetBold();
            ws.Range(row, 1, row, 5).Style.Fill.SetBackgroundColor(XLColor.LightGray);

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PurchaseSupplierWise.xlsx");
        }
        //EXPORT TO PDF
        [HttpGet]
        public async Task<IActionResult> ExportPurchaseSupplierWisePdf(DateTime? fromDate, DateTime? toDate, int? SupplierId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var purchase = await _purchaseservice.GetAll();

            purchase = purchase
                .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate)
                .ToList();

            if (SupplierId.HasValue && SupplierId.Value > 0)
                purchase = purchase.Where(p => p.SupplierId == SupplierId.Value).ToList();

            var list = purchase.Select(p => new PurchaseVM
            {
                BillNo = p.BillNo,
                BillDate = p.BillDate,
                SupplierName = p.Suppliers?.FirstName ?? "Unknown",
                TotalGstAmt = p.PurchaseItems?.Sum(i => i.CGstAmount + i.SGstAmount) ?? 0,
                TotalPayable = p.PurchaseItems?.Sum(i => i.TotalAmt) ?? 0
            }).ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // COMPANY NAME
            document.Add(new Paragraph(companyName)
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER));

            // TITLE
            document.Add(new Paragraph("Purchase Report - Supplier Wise")
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER));

            // DATE
            document.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(8));

            Table table = new Table(new float[] { 3, 3, 5, 3, 3 })
                .UseAllAvailableWidth();

            string[] headers = { "Bill No", "Bill Date", "Supplier", "GST Amount", "Total Amount" };

            foreach (var h in headers)
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(9))
                    .SetTextAlignment(TextAlignment.CENTER));

            foreach (var i in list)
            {
                table.AddCell(new Paragraph(i.BillNo).SetFont(normal).SetFontSize(9));
                table.AddCell(new Paragraph(i.BillDate?.ToString("dd-MM-yyyy")).SetFont(normal).SetFontSize(9));
                table.AddCell(new Paragraph(i.SupplierName).SetFont(normal).SetFontSize(9));

                table.AddCell(new Paragraph(i.TotalGstAmt.ToString("0.00"))
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Paragraph(i.TotalPayable.ToString("0.00"))
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL
            table.AddCell(new Cell(1, 3)
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(9))
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(list.Sum(x => x.TotalGstAmt).ToString("0.00"))
                .SetFont(bold).SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(list.Sum(x => x.TotalPayable).ToString("0.00"))
                .SetFont(bold).SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "PurchaseSupplierWise.pdf");
        }
        public async Task<IActionResult> NearExpiryReport(DateTime? expiryFrom, DateTime? expiryTo)
        {
            var today = DateTime.Today;
            var expiryFromDate = expiryFrom ?? today;
            var expiryToDate = expiryTo ?? today.AddDays(90);

            var itemMasters = (await _itemmasterservice.GetAll()).ToDictionary(x => x.Id);

            // ✅ CurrentStock jaisa direct stockDict bana liya
            var currentStocks = await _currenstockService.GetAll();
            var stockDict = currentStocks.GroupBy(x => x.ItemId).ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var purchases = await _purchaseservice.GetAll();
            var purchasereturns = await _purchasereturnservice.GetAll();
            var sales = await _salesService.GetAll();
            var stockIssue = await _stockissueservice.GetAll();
            var stockReturn = await _stockrturnservice.GetAll();
            var stockReceive = await _stockreceiveservice.GetAll();

            // Flatten item collections
            var purchaseItems = purchases.SelectMany(p => p.PurchaseItems ?? []);
            var purchaseReturnItems = purchasereturns.SelectMany(r => r.PurchaseReturnItems ?? []);
            var salesItems = sales.SelectMany(s => s.SalesItems ?? []);
            var stockIssueItems = stockIssue.SelectMany(s => s.StockIssuesItems ?? []);
            var stockReturnItems = stockReturn.SelectMany(s => s.StockReturnItems ?? []);
            var stockReceiveItems = stockReceive.SelectMany(s => s.StockReceiveItems ?? []);

            // Sirf Expiry Date aur Batch nikalne ke liye (Stock calculation nahi)
            var allBatches = purchaseItems
                .Select(x => new { x.ItemId, Batch = x.Batch ?? "", ExpiryDate = x.ExpiryDate ?? DateTime.MaxValue })
                .Concat(purchaseReturnItems.Select(x => new { x.ItemId, Batch = x.Batch ?? "", ExpiryDate = x.ExpiryDate ?? DateTime.MaxValue }))
                .Concat(salesItems.Select(x => new { ItemId = x.ItemMasterId, Batch = x.Batch ?? "", ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockIssueItems.Select(x => new { ItemId = x.ItemMasterId, Batch = x.Batch ?? "", ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockReturnItems.Select(x => new { ItemId = x.ItemMasterId, Batch = x.Batch ?? "", ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockReceiveItems.Select(x => new { ItemId = x.ItemMasterId, Batch = x.Batch ?? "", ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Where(b => b.ExpiryDate.Date >= expiryFromDate && b.ExpiryDate.Date <= expiryToDate)
                .Distinct()
                .ToList();

            // Final Data Map
            var stockList = allBatches
                .Select(batch =>
                {
                    if (!itemMasters.TryGetValue(batch.ItemId, out var item)) return null;

                    // ✅ Calculation hata diya, sirf CurrentStock ka data use kiya
                    decimal currentStock = stockDict.ContainsKey(batch.ItemId) ? stockDict[batch.ItemId] : 0;

                    if (currentStock <= 0) return null;

                    return new NearExpiryVM
                    {
                        ItemId = item.Id,
                        ItemCode = item.Code,
                        ItemName = item.Name,
                        CategoryName = item.Category?.CategoryName ?? "",
                        BatchNo = batch.Batch,
                        ExpiryDate = batch.ExpiryDate,
                        DaysToExpire = (batch.ExpiryDate.Date - today).Days,
                        Stocks = currentStock  // ✅ Yahan ab CurrentStock ka exact data aayega
                    };
                })
                .Where(x => x != null)
                .OrderBy(x => x.ExpiryDate)
                .ThenBy(x => x.DaysToExpire)
                .ToList();

            return View(stockList);
        }

        //EXPORT TO Excel
        [HttpGet]
        public async Task<IActionResult> NearExpiryReportExcel(DateTime? expiryFrom, DateTime? expiryTo)
        {
            var today = DateTime.Today;
            var expiryFromDate = expiryFrom ?? today;
            var expiryToDate = expiryTo ?? today.AddDays(90);

            // 🔹 SAME LOGIC (unchanged)
            var itemMasters = (await _itemmasterservice.GetAll()).ToDictionary(x => x.Id);
            var purchases = await _purchaseservice.GetAll();
            var purchasereturns = await _purchasereturnservice.GetAll();
            var sales = await _salesService.GetAll();
            var stockIssue = await _stockissueservice.GetAll();
            var stockReturn = await _stockrturnservice.GetAll();
            var stockReceive = await _stockreceiveservice.GetAll();

            var purchaseItems = purchases.SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>());
            var purchaseReturnItems = purchasereturns.SelectMany(r => r.PurchaseReturnItems ?? new List<PurchaseReturnItem>());
            var salesItems = sales.SelectMany(s => s.SalesItems ?? new List<SalesItem>());
            var stockIssueItems = stockIssue.SelectMany(s => s.StockIssuesItems ?? new List<StockIssueItem>());
            var stockReturnItems = stockReturn.SelectMany(s => s.StockReturnItems ?? new List<StockReturnItem>());
            var stockReceiveItems = stockReceive.SelectMany(s => s.StockReceiveItems ?? new List<StockReceiveItem>());

            var allBatches =
                purchaseItems.Select(x => new { x.ItemId, x.Batch, Expiry = x.ExpiryDate ?? DateTime.MaxValue })
                .Concat(purchaseReturnItems.Select(x => new { x.ItemId, x.Batch, Expiry = x.ExpiryDate ?? DateTime.MaxValue }))
                .Concat(salesItems.Select(x => new { ItemId = x.ItemMasterId, x.Batch, Expiry = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockIssueItems.Select(x => new { ItemId = x.ItemMasterId, x.Batch, Expiry = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockReturnItems.Select(x => new { ItemId = x.ItemMasterId, x.Batch, Expiry = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockReceiveItems.Select(x => new { ItemId = x.ItemMasterId, x.Batch, Expiry = x.Expirydate ?? DateTime.MaxValue }))
                .Where(x => x.Expiry >= expiryFromDate && x.Expiry <= expiryToDate)
                .Distinct()
                .ToList();

            decimal GetQty<T>(IEnumerable<T> src, Func<T, bool> where, Func<T, decimal> qty) =>
                src.Where(where).Sum(qty);

            var report = allBatches.Select(b =>
            {
                if (!itemMasters.TryGetValue(b.ItemId, out var item)) return null;

                var purchaseQty = GetQty(purchaseItems, x => x.ItemId == b.ItemId && x.Batch == b.Batch && x.ExpiryDate == b.Expiry, x => x.Qty + x.FreeQty);
                var purchaseReturnQty = GetQty(purchaseReturnItems, x => x.ItemId == b.ItemId && x.Batch == b.Batch && x.ExpiryDate == b.Expiry, x => x.Qty + x.FreeQty);
                var salesQty = GetQty(salesItems, x => x.ItemMasterId == b.ItemId && x.Batch == b.Batch && x.Expirydate == b.Expiry, x => x.Qty);
                var stockIssueQty = GetQty(stockIssueItems, x => x.ItemMasterId == b.ItemId && x.Batch == b.Batch && x.Expirydate == b.Expiry, x => x.Qty);
                var stockReturnQty = GetQty(stockReturnItems, x => x.ItemMasterId == b.ItemId && x.Batch == b.Batch && x.Expirydate == b.Expiry, x => x.Qty);
                var stockReceiveQty = GetQty(stockReceiveItems, x => x.ItemMasterId == b.ItemId && x.Batch == b.Batch && x.Expirydate == b.Expiry, x => x.Qty);

                var stock = purchaseQty + stockReturnQty + stockReceiveQty
                            - purchaseReturnQty - salesQty - stockIssueQty;

                if (stock <= 0) return null;

                return new NearExpiryVM
                {
                    ItemName = item.Name,
                    CategoryName = item.Category?.CategoryName ?? "",
                    BatchNo = b.Batch,
                    ExpiryDate = b.Expiry,
                    DaysToExpire = (b.Expiry - today).Days,
                    Stocks = stock
                };
            })
            .Where(x => x != null)
            .OrderBy(x => x.ExpiryDate)
            .ToList();

            // ================= EXCEL =================
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Near Expiry");

            var user = await _userManager.GetUserAsync(User);
            string companyName = (await _tenantService.GetById(user.TenantId))?.Name ?? "Company";

            // COMPANY
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 6).Merge().Style
                .Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TITLE
            ws.Cell(2, 1).Value = "Near Expiry Report";
            ws.Range(2, 1, 2, 6).Merge().Style
                .Font.SetBold().Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // DATE
            ws.Cell(3, 1).Value = $"From {expiryFromDate:dd-MM-yyyy} To {expiryToDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 6).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "Item Name", "Category", "Batch No", "Expiry Date", "Days Left", "Stock" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 6).Style.Font.SetBold();
            row++;

            foreach (var x in report)
            {
                ws.Cell(row, 1).Value = x.ItemName;
                ws.Cell(row, 2).Value = x.CategoryName;
                ws.Cell(row, 3).Value = x.BatchNo;
                ws.Cell(row, 4).Value = x.ExpiryDate;
                ws.Cell(row, 5).Value = x.DaysToExpire;
                ws.Cell(row, 6).Value = x.Stocks;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "NearExpiryReport.xlsx");
        }

        //EXPORT TO PDF
        [HttpGet]
        public async Task<IActionResult> NearExpiryReportPdf(DateTime? expiryFrom, DateTime? expiryTo)
        {
            var today = DateTime.Today;
            var expiryFromDate = expiryFrom ?? today;
            var expiryToDate = expiryTo ?? today.AddDays(90);

            var data = (await NearExpiryReport(expiryFrom, expiryTo) as ViewResult)?.Model as List<NearExpiryVM>;

            using var ms = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(ms));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _userManager.GetUserAsync(User);
            string companyName = (await _tenantService.GetById(user.TenantId))?.Name ?? "Company";

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // COMPANY
            doc.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            // TITLE
            doc.Add(new Paragraph("Near Expiry Report")
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            // DATE
            doc.Add(new Paragraph($"From {expiryFromDate:dd-MM-yyyy} To {expiryToDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            Table table = new Table(new float[] { 6, 4, 4, 4, 3, 3 }).UseAllAvailableWidth();

            string[] headers = { "Item Name", "Category", "Batch No", "Expiry Date", "Days Left", "Stock" };

            foreach (var h in headers)
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(9)));

            foreach (var x in data)
            {
                table.AddCell(new Paragraph(x.ItemName).SetFontSize(9));
                table.AddCell(new Paragraph(x.CategoryName).SetFontSize(9));
                table.AddCell(new Paragraph(x.BatchNo).SetFontSize(9));
                table.AddCell(new Paragraph(x.ExpiryDate.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.DaysToExpire.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.Stocks.ToString()).SetFontSize(9));
            }

            doc.Add(table);
            doc.Close();

            return File(ms.ToArray(), "application/pdf", "NearExpiryReport.pdf");
        }

        public async Task<IActionResult> ExpiredItemReport(string fromDate, string toDate)
        {
            DateTime startDate;
            DateTime endDate;

            // Parse From Date
            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out startDate))
            {
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            // Parse To Date
            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Today;
            }

            endDate = endDate.AddDays(1).AddTicks(-1); // Full day include
            var today = DateTime.Today;

            // Load all data
            var itemMasters = (await _itemmasterservice.GetAll()).ToDictionary(x => x.Id);

            // ✅ CurrentStock ka data lekar aaye
            var currentStocks = await _currenstockService.GetAll();
            var stockDict = currentStocks.GroupBy(x => x.ItemId).ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var purchases = await _purchaseservice.GetAll();
            var purchasereturns = await _purchasereturnservice.GetAll();
            var sales = await _salesService.GetAll();
            var stockIssue = await _stockissueservice.GetAll();
            var stockReturn = await _stockrturnservice.GetAll();
            var stockReceive = await _stockreceiveservice.GetAll();

            // Flatten item collections
            var purchaseItems = purchases.SelectMany(p => p.PurchaseItems ?? []);
            var purchaseReturnItems = purchasereturns.SelectMany(r => r.PurchaseReturnItems ?? []);
            var salesItems = sales.SelectMany(s => s.SalesItems ?? []);
            var stockIssueItems = stockIssue.SelectMany(s => s.StockIssuesItems ?? []);
            var stockReturnItems = stockReturn.SelectMany(s => s.StockReturnItems ?? []);
            var stockReceiveItems = stockReceive.SelectMany(s => s.StockReceiveItems ?? []);

            // Sirf Expired Batches nikalne ke liye (Date range aur Expired check)
            var allBatches = purchaseItems
                .Select(x => new { x.ItemId, Batch = x.Batch ?? "", ExpiryDate = x.ExpiryDate ?? DateTime.MaxValue })
                .Concat(purchaseReturnItems.Select(x => new { x.ItemId, Batch = x.Batch ?? "", ExpiryDate = x.ExpiryDate ?? DateTime.MaxValue }))
                .Concat(salesItems.Select(x => new { ItemId = x.ItemMasterId, Batch = x.Batch ?? "", ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockIssueItems.Select(x => new { ItemId = x.ItemMasterId, Batch = x.Batch ?? "", ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockReturnItems.Select(x => new { ItemId = x.ItemMasterId, Batch = x.Batch ?? "", ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockReceiveItems.Select(x => new { ItemId = x.ItemMasterId, Batch = x.Batch ?? "", ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Where(x => x.ExpiryDate.Date <= today) // Sirf expired items
                .Distinct()
                .ToList();

            // Final stock mapping
            var stockList = allBatches
                .Select(batch =>
                {
                    if (!itemMasters.TryGetValue(batch.ItemId, out var item)) return null;

                    // ✅ Purani calculation hata di. Direct CurrentStock table se stock liya.
                    decimal currentStock = stockDict.ContainsKey(batch.ItemId) ? stockDict[batch.ItemId] : 0;

                    if (currentStock <= 0) return null;

                    return new NearExpiryVM
                    {
                        ItemId = item.Id,
                        ItemCode = item.Code,
                        ItemName = item.Name,
                        CategoryName = item.Category?.CategoryName ?? "",
                        BatchNo = batch.Batch,
                        ExpiryDate = batch.ExpiryDate,
                        DaysToExpire = (batch.ExpiryDate - today).Days, // Negative aayega kyunki expired hai
                        Stocks = currentStock
                    };
                })
                .Where(x => x != null)
                .OrderBy(x => x.ExpiryDate)
                .ToList();

            // Assign ViewBag before returning
            ViewBag.FromDate = startDate.ToString("dd-MM-yyyy");
            ViewBag.ToDate = endDate.ToString("dd-MM-yyyy");

            return View(stockList);
        }

        //EXPORT TO EXCEl Expired Item Report
        [HttpGet]
        public async Task<IActionResult> ExpiredItemReportExcel(string fromDate, string toDate)
        {
            DateTime startDate, endDate;
            var today = DateTime.Today;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                startDate = new DateTime(today.Year, today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                endDate = today;

            endDate = endDate.AddDays(1).AddTicks(-1);

            // ================= DATA =================
            var itemMasters = (await _itemmasterservice.GetAll()).ToDictionary(x => x.Id);
            var purchases = await _purchaseservice.GetAll();
            var purchasereturns = await _purchasereturnservice.GetAll();
            var sales = await _salesService.GetAll();
            var stockIssue = await _stockissueservice.GetAll();
            var stockReturn = await _stockrturnservice.GetAll();
            var stockReceive = await _stockreceiveservice.GetAll();

            var purchaseItems = purchases.SelectMany(p => p.PurchaseItems ?? Enumerable.Empty<PurchaseItem>());
            var purchaseReturnItems = purchasereturns.SelectMany(r => r.PurchaseReturnItems ?? Enumerable.Empty<PurchaseReturnItem>());
            var salesItems = sales.SelectMany(s => s.SalesItems ?? Enumerable.Empty<SalesItem>());
            var stockIssueItems = stockIssue.SelectMany(s => s.StockIssuesItems ?? Enumerable.Empty<StockIssueItem>());
            var stockReturnItems = stockReturn.SelectMany(s => s.StockReturnItems ?? Enumerable.Empty<StockReturnItem>());
            var stockReceiveItems = stockReceive.SelectMany(s => s.StockReceiveItems ?? Enumerable.Empty<StockReceiveItem>());

            decimal GetQty<T>(IEnumerable<T> items, Func<T, bool> where, Func<T, decimal> qty) =>
                items.Where(where).Sum(qty);

            var batches = purchaseItems
                .Select(x => new { x.ItemId, x.Batch, Expiry = x.ExpiryDate ?? DateTime.MaxValue })
                .Where(x => x.Expiry <= today)
                .Distinct()
                .ToList();

            var data = batches.Select((b, index) =>
            {
                if (!itemMasters.TryGetValue(b.ItemId, out var item)) return null;

                var stock =
                    GetQty(purchaseItems, x => x.ItemId == b.ItemId && x.Batch == b.Batch && x.ExpiryDate == b.Expiry, x => x.Qty + x.FreeQty)
                    - GetQty(salesItems, x => x.ItemMasterId == b.ItemId && x.Batch == b.Batch && x.Expirydate == b.Expiry, x => x.Qty);

                if (stock <= 0) return null;

                return new
                {
                    SNo = index + 1,
                    ItemName = item.Name,
                    Category = item.Category?.CategoryName ?? "",
                    Batch = b.Batch,
                    Expiry = b.Expiry.ToString("dd-MM-yyyy"),
                    DaysLeft = (b.Expiry - today).Days,
                    Stock = stock
                };
            })
            .Where(x => x != null)
            .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Expired Items");

            var user = await _userManager.GetUserAsync(User);
            string companyName = (await _tenantService.GetById(user.TenantId))?.Name ?? "Company";

            // COMPANY
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.SetFontSize(14);
            ws.Range(1, 1, 1, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TITLE
            ws.Cell(2, 1).Value = "Expired Item Report";
            ws.Range(2, 1, 2, 7).Merge().Style.Font.SetBold().Font.SetFontSize(12);
            ws.Range(2, 1, 2, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // DATE
            ws.Cell(3, 1).Value = $"From {startDate:dd-MM-yyyy} To {endDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 7).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "S.No", "Item Name", "Category", "Batch No", "Expiry Date", "Days Left", "Stock" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 7).Style.Font.SetBold();
            row++;

            foreach (var x in data)
            {
                ws.Cell(row, 1).Value = x.SNo;
                ws.Cell(row, 2).Value = x.ItemName;
                ws.Cell(row, 3).Value = x.Category;
                ws.Cell(row, 4).Value = x.Batch;
                ws.Cell(row, 5).Value = x.Expiry;
                ws.Cell(row, 6).Value = x.DaysLeft;
                ws.Cell(row, 7).Value = x.Stock;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ExpiredItemReport.xlsx");
        }

        //EXPORT TO PDF 
        [HttpGet]
        public async Task<IActionResult> ExpiredItemReportPdf(string fromDate, string toDate)
        {
            DateTime startDate, endDate;
            var today = DateTime.Today;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out startDate))
                startDate = new DateTime(today.Year, today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out endDate))
                endDate = today;

            var itemMasters = (await _itemmasterservice.GetAll()).ToDictionary(x => x.Id);
            var purchases = await _purchaseservice.GetAll();
            var sales = await _salesService.GetAll();

            var purchaseItems = purchases.SelectMany(p => p.PurchaseItems ?? Enumerable.Empty<PurchaseItem>());
            var salesItems = sales.SelectMany(s => s.SalesItems ?? Enumerable.Empty<SalesItem>());

            decimal GetQty<T>(IEnumerable<T> items, Func<T, bool> where, Func<T, decimal> qty) =>
                items.Where(where).Sum(qty);

            var batches = purchaseItems
                .Select(x => new { x.ItemId, x.Batch, Expiry = x.ExpiryDate ?? DateTime.MaxValue })
                .Where(x => x.Expiry <= today)
                .Distinct()
                .ToList();

            var list = batches.Select((b, index) =>
            {
                if (!itemMasters.TryGetValue(b.ItemId, out var item)) return null;

                var stock =
                    GetQty(purchaseItems, x => x.ItemId == b.ItemId && x.Batch == b.Batch && x.ExpiryDate == b.Expiry, x => x.Qty + x.FreeQty)
                    - GetQty(salesItems, x => x.ItemMasterId == b.ItemId && x.Batch == b.Batch && x.Expirydate == b.Expiry, x => x.Qty);

                if (stock <= 0) return null;

                return new
                {
                    SNo = index + 1,
                    ItemName = item.Name,
                    Category = item.Category?.CategoryName ?? "",
                    Batch = b.Batch,
                    Expiry = b.Expiry.ToString("dd-MM-yyyy"),
                    DaysLeft = (b.Expiry - today).Days,
                    Stock = stock
                };
            })
            .Where(x => x != null)
            .ToList();

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _userManager.GetUserAsync(User);
            string companyName = (await _tenantService.GetById(user.TenantId))?.Name ?? "Company";

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // COMPANY
            doc.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            // TITLE
            doc.Add(new Paragraph("Expired Item Report")
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            // DATE
            doc.Add(new Paragraph($"From {startDate:dd-MM-yyyy} To {endDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(5));

            Table table = new Table(new float[] { 2, 5, 4, 3, 3, 3, 3 }).UseAllAvailableWidth();

            string[] headers = { "S.No", "Item Name", "Category", "Batch No", "Expiry Date", "Days Left", "Stock" };

            foreach (var h in headers)
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(9)));

            foreach (var x in list)
            {
                table.AddCell(new Paragraph(x.SNo.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.ItemName).SetFontSize(9));
                table.AddCell(new Paragraph(x.Category).SetFontSize(9));
                table.AddCell(new Paragraph(x.Batch).SetFontSize(9));
                table.AddCell(new Paragraph(x.Expiry).SetFontSize(9));
                table.AddCell(new Paragraph(x.DaysLeft.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.Stock.ToString()).SetFontSize(9));
            }

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "ExpiredItemReport.pdf");
        }

        public static class Gstr2SummaryKeys
        {
            public const string B2B = "B2B";
            public const string B2C_LARGE = "B2C_LARGE";
            public const string B2C_SMALL = "B2C_SMALL";
            public const string B2BUR = "B2BUR";
            public const string NIL = "NIL";
            public const string NIL_RATED = "NIL_RATED";
            public const string EXEMPTED = "EXEMPTED";
            public const string EXPORT = "EXPORT";
            public const string ADVANCE = "ADVANCE";
            public const string TOTAL = "TOTAL";
        }

        [HttpGet]
        public async Task<IActionResult> GSTR2Report(DateTime? StartBillDate, DateTime? EndBillDate, string ViewType = "Summary")
        {
            if (!StartBillDate.HasValue)
                StartBillDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            if (!EndBillDate.HasValue)
                EndBillDate = DateTime.Today;

            ViewBag.ViewType = ViewType;
            ViewBag.StartBillDate = StartBillDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndBillDate = EndBillDate.Value.ToString("yyyy-MM-dd");

            var report = await BuildGstr2ReportAsync(StartBillDate, EndBillDate);

            if (ViewType == "Details")
            {
                var detailRows = await BuildGstr2DetailRowsAsync(StartBillDate, EndBillDate);
                return View("GSTR2DetailsView", detailRows);
            }

            return View(report.SummaryRows);
        }

        private async Task<Gstr2ReportVM> BuildGstr2ReportAsync(DateTime? startDate, DateTime? endDate)
        {
            var resolvedStart = (startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var resolvedEnd = (endDate ?? DateTime.Today).Date;

            var purchases = await _purchaseservice.GetAll();
            var suppliers = await _supplierservice.GetALL();
            var supplierLookup = suppliers.ToDictionary(x => x.Id);

            var details = purchases
                .Where(x => x.BillDate.HasValue)
                .Where(x => x.BillDate!.Value.Date >= resolvedStart && x.BillDate.Value.Date <= resolvedEnd)
                .Select(x =>
                {
                    Supplier? supplier = null;
                    if (x.SupplierId.HasValue)
                    {
                        supplierLookup.TryGetValue(x.SupplierId.Value, out supplier);
                    }

                    var items = x.PurchaseItems ?? new List<PurchaseItem>();

                    // ✅ STEP 1: Total Base after Item Discount (%)
                    var totalBase = items.Sum(i =>
                    {
                        var gross = i.Qty * i.Rate;
                        var itemDiscAmt = gross * i.Discount / 100; // % → amount
                        return gross - itemDiscAmt;
                    });

                    // ✅ STEP 2: Bill Discount (%)
                    var billDiscountPercent = x.discountPercent; // %
                    var billDiscountAmount = totalBase * billDiscountPercent / 100;

                    // ✅ STEP 3: Correct Taxable
                    var taxable = items.Sum(i =>
                    {
                        var gross = i.Qty * i.Rate;
                        var itemDiscAmt = gross * i.Discount / 100;
                        var itemBase = gross - itemDiscAmt;

                        if (itemBase <= 0 || totalBase <= 0)
                            return 0;

                        var itemBillDisc = (itemBase / totalBase) * billDiscountAmount;

                        var finalTaxable = itemBase - itemBillDisc;

                        return finalTaxable > 0 ? finalTaxable : 0;
                    });

                    // ✅ GST (as is)
                    var cgst = items.Sum(i => i.CGstAmount);
                    var sgst = items.Sum(i => i.SGstAmount);

                    var igst = items.Sum(i =>
                    {
                        var value = i.GstAmount - (i.CGstAmount + i.SGstAmount);
                        return value > 0 ? value : 0;
                    });

                    // ✅ CESS on corrected taxable
                    var cess = items.Sum(i =>
                    {
                        var gross = i.Qty * i.Rate;
                        var itemDiscAmt = gross * i.Discount / 100;
                        var itemBase = gross - itemDiscAmt;

                        if (itemBase <= 0 || totalBase <= 0)
                            return 0;

                        var itemBillDisc = (itemBase / totalBase) * billDiscountAmount;
                        var finalTaxable = itemBase - itemBillDisc;

                        return finalTaxable > 0
                            ? (finalTaxable * i.Cess / 100)
                            : 0;
                    });

                    var totalTax = cgst + sgst + igst + cess;

                    var invoiceValue = x.TotalPayable > 0
                        ? x.TotalPayable
                        : items.Sum(i => i.TotalAmt);

                    var gstin = (supplier?.GstNO ?? string.Empty).Trim();

                    var categoryKey = string.IsNullOrWhiteSpace(gstin)
                        ? Gstr2SummaryKeys.B2C_SMALL
                        : Gstr2SummaryKeys.B2B;

                    if (taxable <= 0 && totalTax <= 0)
                    {
                        categoryKey = Gstr2SummaryKeys.NIL_RATED;
                    }

                    return new Gstr2DetailVM
                    {
                        PurchaseId = x.Id,
                        Category = categoryKey,
                        SupplierName = supplier?.FirstName ?? "Cash",
                        GSTIN = gstin,
                        BillNo = x.BillNo ?? string.Empty,
                        BillDate = x.BillDate,
                        SupplyType = igst > 0 ? "Inter State" : "Intra State",
                        InvoiceValue = invoiceValue,
                        Taxable = taxable,
                        SGST = sgst,
                        CGST = cgst,
                        IGST = igst,
                        Cess = cess,
                        TotalGST = totalTax
                    };
                })
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            var summaryRows = BuildGstr2SummaryRows(details);

            return new Gstr2ReportVM
            {
                StartBillDate = resolvedStart,
                EndBillDate = resolvedEnd,
                SummaryRows = summaryRows,
                DetailRows = details
            };
        }

        private static List<Gstr2SummaryVM> BuildGstr2SummaryRows(IReadOnlyCollection<Gstr2DetailVM> details)
        {
            static Gstr2SummaryVM Aggregate(string key, string label, IEnumerable<Gstr2DetailVM> rows)
            {
                var rowList = rows.ToList();
                return new Gstr2SummaryVM
                {
                    Key = key,
                    Label = label,
                    Count = rowList.Count,
                    Taxable = rowList.Sum(x => x.Taxable),
                    SGST = rowList.Sum(x => x.SGST),
                    CGST = rowList.Sum(x => x.CGST),
                    IGST = rowList.Sum(x => x.IGST),
                    Cess = rowList.Sum(x => x.Cess),
                    InvoiceAmount = rowList.Sum(x => x.InvoiceValue)
                };
            }

            var orderedRows = new List<Gstr2SummaryVM>
            {
                Aggregate(Gstr2SummaryKeys.B2B, "B2B (Registered Supplier Invoices)", details.Where(x => x.Category == Gstr2SummaryKeys.B2B)),
                Aggregate(Gstr2SummaryKeys.B2C_LARGE, "B2C (Large) Invoice", details.Where(x => x.Category == Gstr2SummaryKeys.B2C_LARGE)),
                Aggregate(Gstr2SummaryKeys.B2C_SMALL, "B2C (Small) Invoice", details.Where(x => x.Category == Gstr2SummaryKeys.B2C_SMALL || x.Category == Gstr2SummaryKeys.B2BUR)),
                Aggregate(Gstr2SummaryKeys.NIL, "Nil Rated/Exempted", details.Where(x => x.Category == Gstr2SummaryKeys.NIL_RATED || x.Category == Gstr2SummaryKeys.EXEMPTED)),
                Aggregate(Gstr2SummaryKeys.NIL_RATED, " - Nil Rated", details.Where(x => x.Category == Gstr2SummaryKeys.NIL_RATED)),
                Aggregate(Gstr2SummaryKeys.EXEMPTED, " - Exempted", details.Where(x => x.Category == Gstr2SummaryKeys.EXEMPTED)),
                Aggregate(Gstr2SummaryKeys.EXPORT, "Export Invoices", details.Where(x => x.Category == Gstr2SummaryKeys.EXPORT)),
                Aggregate(Gstr2SummaryKeys.ADVANCE, "Tax Liability on Advance", details.Where(x => x.Category == Gstr2SummaryKeys.ADVANCE)),
                Aggregate(Gstr2SummaryKeys.TOTAL, "Total", details)
            };

            return orderedRows;
        }

        private async Task<List<Gstr2DetailVM>> BuildGstr2DetailRowsAsync(DateTime? startDate, DateTime? endDate)
        {
            var resolvedStart = (startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var resolvedEnd = (endDate ?? DateTime.Today).Date;

            var purchases = await _purchaseservice.GetAll();
            var suppliers = await _supplierservice.GetALL();
            var hsnMasters = await _hsnservice.GetAll();
            var itemMasters = await _itemmasterservice.GetAll();

            var supplierLookup = suppliers.ToDictionary(x => x.Id);
            var hsnLookup = hsnMasters.ToDictionary(x => x.Id, x => x.HsnCode ?? string.Empty);
            var itemMasterHsnLookup = itemMasters
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.HsnId);

            var detailRows = new List<Gstr2DetailVM>();

            var filteredPurchases = purchases
                .Where(x => x.BillDate.HasValue)
                .Where(x => x.BillDate!.Value.Date >= resolvedStart && x.BillDate.Value.Date <= resolvedEnd)
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            foreach (var purchase in filteredPurchases)
            {
                Supplier? supplier = null;
                if (purchase.SupplierId.HasValue)
                {
                    supplierLookup.TryGetValue(purchase.SupplierId.Value, out supplier);
                }

                var gstin = (supplier?.GstNO ?? string.Empty).Trim();
                var validItems = (purchase.PurchaseItems ?? new List<PurchaseItem>())
                    .Where(x => x.Deleted == null && (x.Qty + x.FreeQty) > 0)
                    .ToList();

                if (!validItems.Any())
                {
                    continue;
                }

                var billDiscountPer = purchase.discountPercent;

                var computedItems = validItems.Select(item =>
                {
                    var igstPer = item.Gst - (item.CGst + item.SGst);
                    if (igstPer < 0)
                    {
                        igstPer = 0;
                    }

                    var baseAmount = item.Qty * item.Rate;
                    var itemDiscountAmt = baseAmount * item.Discount / 100;
                    var afterItemDiscount = baseAmount - itemDiscountAmt;
                    var billDiscountAmt = afterItemDiscount * billDiscountPer / 100;

                    var taxable = afterItemDiscount - billDiscountAmt;
                    if (taxable < 0)
                    {
                        taxable = 0;
                    }

                    var cgstTax = taxable * item.CGst / 100;
                    var sgstTax = taxable * item.SGst / 100;
                    var igstTax = taxable * igstPer / 100;
                    var cessTax = taxable * item.Cess / 100;

                    var totalTax = cgstTax + sgstTax + igstTax + cessTax;
                    var grossAmount = taxable + totalTax;

                    string hsnCode = string.Empty;

                    if (item.HsnId.HasValue && hsnLookup.TryGetValue(item.HsnId.Value, out var mappedHsn))
                    {
                        hsnCode = mappedHsn;
                    }
                    else if (itemMasterHsnLookup.TryGetValue(item.ItemId, out var itemMasterHsnId)
                             && itemMasterHsnId.HasValue
                             && hsnLookup.TryGetValue(itemMasterHsnId.Value, out var fallbackHsn))
                    {
                        hsnCode = fallbackHsn;
                    }

                    if (string.IsNullOrWhiteSpace(hsnCode))
                    {
                        hsnCode = "-";
                    }

                    var qty = item.Qty + item.FreeQty;

                    return new
                    {
                        Item = item,
                        HsnCode = hsnCode,
                        Qty = qty,
                        Taxable = Math.Round(taxable, 2),
                        GrossAmount = Math.Round(grossAmount, 2),
                        SGSTPer = item.SGst,
                        SGSTTax = Math.Round(sgstTax, 2),
                        CGSTPer = item.CGst,
                        CGSTTax = Math.Round(cgstTax, 2),
                        IGSTPer = Math.Round(igstPer, 2),
                        IGSTTax = Math.Round(igstTax, 2),
                        CessTax = Math.Round(cessTax, 2),
                        TotalTax = Math.Round(totalTax, 2)
                    };
                }).ToList();

                var invoiceTaxable = computedItems.Sum(x => x.Taxable);
                var invoiceTax = computedItems.Sum(x => x.TotalTax);
                var invoiceValue = purchase.TotalPayable > 0
                    ? purchase.TotalPayable
                    : computedItems.Sum(x => x.GrossAmount);

                var categoryKey = string.IsNullOrWhiteSpace(gstin)
                    ? Gstr2SummaryKeys.B2C_SMALL
                    : Gstr2SummaryKeys.B2B;

                if (invoiceTaxable <= 0 && invoiceTax <= 0)
                {
                    categoryKey = Gstr2SummaryKeys.NIL_RATED;
                }

                foreach (var item in computedItems)
                {
                    detailRows.Add(new Gstr2DetailVM
                    {
                        PurchaseId = purchase.Id,
                        Category = categoryKey,
                        SupplierName = supplier?.FirstName ?? "Cash",
                        GSTIN = gstin,
                        BillNo = purchase.BillNo ?? string.Empty,
                        BillDate = purchase.BillDate,
                        SupplyType = item.IGSTTax > 0 ? "Inter State" : "Intra State",
                        HSN = item.HsnCode,
                        QtyDisplay = item.Qty.ToString(CultureInfo.InvariantCulture),
                        Amount = item.GrossAmount,
                        InvoiceValue = invoiceValue,
                        Taxable = item.Taxable,
                        SGSTPer = item.SGSTPer,
                        SGST = item.SGSTTax,
                        CGSTPer = item.CGSTPer,
                        CGST = item.CGSTTax,
                        IGSTPer = item.IGSTPer,
                        IGST = item.IGSTTax,
                        Cess = item.CessTax,
                        TotalGST = item.TotalTax
                    });
                }
            }

            return detailRows
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ThenBy(x => x.HSN)
                .ToList();
        }

        public async Task<IActionResult> ExportGSTR2Excel(DateTime? StartBillDate, DateTime? EndBillDate, string ViewType = "Summary")
        {
            if (!StartBillDate.HasValue)
                StartBillDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            if (!EndBillDate.HasValue)
                EndBillDate = DateTime.Today;

            var report = await BuildGstr2ReportAsync(StartBillDate, EndBillDate);
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("GSTR2 Report");

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _tenantService.GetById(user?.TenantId);
            var companyName = tenant?.Name ?? string.Empty;
            var address = tenant?.Address1 ?? string.Empty;
            var gstin = tenant?.GstNo ?? string.Empty;

            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 20).Merge().Style.Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = address;
            ws.Range(2, 1, 2, 20).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"GSTIN : {gstin}";
            ws.Range(3, 1, 3, 20).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(4, 1).Value = $"GSTR2 {ViewType.ToUpper()} FOR THE PERIOD {StartBillDate:dd/MM/yyyy} TO {EndBillDate:dd/MM/yyyy}";
            ws.Range(4, 1, 4, 20).Merge().Style.Font.SetBold()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            var headerRow = 6;

            if (ViewType == "Summary")
            {
                ws.Cell(headerRow, 1).Value = "Description";
                ws.Cell(headerRow, 2).Value = "Count";
                ws.Cell(headerRow, 3).Value = "Taxable";
                ws.Cell(headerRow, 4).Value = "SGST";
                ws.Cell(headerRow, 5).Value = "CGST";
                ws.Cell(headerRow, 6).Value = "IGST";
                ws.Cell(headerRow, 7).Value = "Cess";
                ws.Cell(headerRow, 8).Value = "Total GST";
                ws.Cell(headerRow, 9).Value = "Invoice Amount";

                ws.Range(headerRow, 1, headerRow, 9).Style.Font.SetBold()
                    .Fill.SetBackgroundColor(XLColor.LightGray);

                var summaryMap = report.SummaryRows.ToDictionary(x => x.Key);
                var summaryTemplate = new List<(string Key, string Text)>
                {
                    (Gstr2SummaryKeys.B2B, "B2B"),
                    (Gstr2SummaryKeys.B2C_LARGE, "B2C (Large) Invoice"),
                    (Gstr2SummaryKeys.B2C_SMALL, "B2C (Small) Invoice"),
                    (Gstr2SummaryKeys.NIL, "Nil Rated/Exempted"),
                    (Gstr2SummaryKeys.NIL_RATED, " - Nil Rated"),
                    (Gstr2SummaryKeys.EXEMPTED, " - Exempted"),
                    (Gstr2SummaryKeys.EXPORT, "Export Invoices"),
                    (Gstr2SummaryKeys.ADVANCE, "Tax Liability on Advance"),
                    (Gstr2SummaryKeys.TOTAL, "Total")
                };

                var row = headerRow + 1;
                foreach (var template in summaryTemplate)
                {
                    var item = summaryMap.ContainsKey(template.Key) ? summaryMap[template.Key] : null;
                    ws.Cell(row, 1).Value = template.Text;
                    ws.Cell(row, 2).Value = item?.Count ?? 0;
                    ws.Cell(row, 3).Value = item?.Taxable ?? 0;
                    ws.Cell(row, 4).Value = item?.SGST ?? 0;
                    ws.Cell(row, 5).Value = item?.CGST ?? 0;
                    ws.Cell(row, 6).Value = item?.IGST ?? 0;
                    ws.Cell(row, 7).Value = item?.Cess ?? 0;
                    ws.Cell(row, 8).Value = item?.TotalGST ?? 0;
                    ws.Cell(row, 9).Value = item?.InvoiceAmount ?? 0;

                    if (template.Key == Gstr2SummaryKeys.TOTAL)
                    {
                        ws.Range(row, 1, row, 9).Style.Font.SetBold();
                        ws.Range(row, 1, row, 9).Style.Fill.SetBackgroundColor(XLColor.LightYellow);
                    }

                    row++;
                }
            }
            else
            {
                ws.Cell(headerRow, 1).Value = "S.No";
                ws.Cell(headerRow, 2).Value = "Supplier";
                ws.Cell(headerRow, 3).Value = "GSTIN";
                ws.Cell(headerRow, 4).Value = "Bill Date";
                ws.Cell(headerRow, 5).Value = "Bill No";
                ws.Cell(headerRow, 6).Value = "Invoice Amount";
                ws.Cell(headerRow, 7).Value = "Supply Type";
                ws.Cell(headerRow, 8).Value = "HSN/SAC";
                ws.Cell(headerRow, 9).Value = "Qty";
                ws.Cell(headerRow, 10).Value = "Amount";
                ws.Cell(headerRow, 11).Value = "Taxable";
                ws.Cell(headerRow, 12).Value = "SGST %";
                ws.Cell(headerRow, 13).Value = "SGST Tax";
                ws.Cell(headerRow, 14).Value = "CGST %";
                ws.Cell(headerRow, 15).Value = "CGST Tax";
                ws.Cell(headerRow, 16).Value = "IGST %";
                ws.Cell(headerRow, 17).Value = "IGST Tax";
                ws.Cell(headerRow, 18).Value = "Cess";
                ws.Cell(headerRow, 19).Value = "Total GST";

                ws.Range(headerRow, 1, headerRow, 19).Style.Font.SetBold()
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                var details = await BuildGstr2DetailRowsAsync(StartBillDate, EndBillDate);
                var row = headerRow + 1;
                var srNo = 1;

                var grouped = details.GroupBy(x => new { x.BillNo, x.SupplierName, x.GSTIN, x.BillDate });

                foreach (var group in grouped)
                {
                    bool isFirst = true;

                    foreach (var item in group)
                    {
                        ws.Cell(row, 1).Value = isFirst ? srNo++ : "";

                        ws.Cell(row, 2).Value = isFirst ? item.SupplierName : "";
                        ws.Cell(row, 3).Value = isFirst ? item.GSTIN : "";
                        ws.Cell(row, 4).Value = isFirst ? item.BillDate?.ToString("dd-MM-yyyy") : "";
                        ws.Cell(row, 5).Value = isFirst ? item.BillNo : "";
                        ws.Cell(row, 6).Value = isFirst ? item.InvoiceValue : "";

                        ws.Cell(row, 7).Value = item.SupplyType;
                        ws.Cell(row, 8).Value = item.HSN;
                        ws.Cell(row, 9).Value = item.QtyDisplay;
                        ws.Cell(row, 10).Value = item.Amount;
                        ws.Cell(row, 11).Value = item.Taxable;
                        ws.Cell(row, 12).Value = item.SGSTPer;
                        ws.Cell(row, 13).Value = item.SGST;
                        ws.Cell(row, 14).Value = item.CGSTPer;
                        ws.Cell(row, 15).Value = item.CGST;
                        ws.Cell(row, 16).Value = item.IGSTPer;
                        ws.Cell(row, 17).Value = item.IGST;
                        ws.Cell(row, 18).Value = item.Cess;
                        ws.Cell(row, 19).Value = item.TotalGST;

                        isFirst = false;
                        row++;
                    }
                }

                ws.Range(row, 1, row, 10).Merge().Value = "Total";
                ws.Cell(row, 11).Value = details.Sum(x => x.Taxable);
                ws.Cell(row, 13).Value = details.Sum(x => x.SGST);
                ws.Cell(row, 15).Value = details.Sum(x => x.CGST);
                ws.Cell(row, 17).Value = details.Sum(x => x.IGST);
                ws.Cell(row, 18).Value = details.Sum(x => x.Cess);
                ws.Cell(row, 19).Value = details.Sum(x => x.TotalGST);
                ws.Range(row, 1, row, 19).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightYellow);
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"GSTR2_{ViewType}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> ExportGSTR2Csv(DateTime? StartBillDate, DateTime? EndBillDate)
        {
            var report = await BuildGstr2ReportAsync(StartBillDate, EndBillDate);

            var sb = new StringBuilder();
            sb.AppendLine("Category,Supplier,GSTIN,Bill Date,Bill No,Supply Type,Taxable,SGST,CGST,IGST,Cess,Total GST,Invoice Value");

            foreach (var row in report.DetailRows)
            {
                sb.AppendLine(
                    $"{EscapeCsvValue(row.Category)}," +
                    $"{EscapeCsvValue(row.SupplierName)}," +
                    $"{EscapeCsvValue(row.GSTIN)}," +
                    $"{EscapeCsvValue(row.BillDate?.ToString("dd-MM-yyyy"))}," +
                    $"{EscapeCsvValue(row.BillNo)}," +
                    $"{EscapeCsvValue(row.SupplyType)}," +
                    $"{row.Taxable:F2},{row.SGST:F2},{row.CGST:F2},{row.IGST:F2},{row.Cess:F2},{row.TotalGST:F2},{row.InvoiceValue:F2}");
            }

            return File(
                Encoding.UTF8.GetBytes(sb.ToString()),
                "text/csv",
                $"GSTR2_Report_{DateTime.Now:yyyyMMdd}.csv"
            );
        }

        private static string EscapeCsvValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        [HttpGet]
        public async Task<IActionResult> GetGstr2Bills(string type, DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var today = DateTime.Today;
                startDate = new DateTime(today.Year, today.Month, 1);
                endDate = today;
            }

            var report = await BuildGstr2ReportAsync(startDate, endDate);
            IEnumerable<Gstr2DetailVM> filtered = report.DetailRows;

            if (!string.IsNullOrWhiteSpace(type))
            {
                if (type == Gstr2SummaryKeys.B2B)
                    filtered = filtered.Where(x => x.Category == Gstr2SummaryKeys.B2B);
                else if (type == Gstr2SummaryKeys.B2C_LARGE)
                    filtered = filtered.Where(x => x.Category == Gstr2SummaryKeys.B2C_LARGE);
                else if (type == Gstr2SummaryKeys.B2C_SMALL || type == Gstr2SummaryKeys.B2BUR)
                    filtered = filtered.Where(x => x.Category == Gstr2SummaryKeys.B2C_SMALL || x.Category == Gstr2SummaryKeys.B2BUR);
                else if (type == Gstr2SummaryKeys.NIL)
                    filtered = filtered.Where(x => x.Category == Gstr2SummaryKeys.NIL_RATED || x.Category == Gstr2SummaryKeys.EXEMPTED);
                else if (type == Gstr2SummaryKeys.NIL_RATED)
                    filtered = filtered.Where(x => x.Category == Gstr2SummaryKeys.NIL_RATED);
                else if (type == Gstr2SummaryKeys.EXEMPTED)
                    filtered = filtered.Where(x => x.Category == Gstr2SummaryKeys.EXEMPTED);
                else if (type == Gstr2SummaryKeys.EXPORT)
                    filtered = filtered.Where(x => x.Category == Gstr2SummaryKeys.EXPORT);
                else if (type == Gstr2SummaryKeys.ADVANCE)
                    filtered = filtered.Where(x => x.Category == Gstr2SummaryKeys.ADVANCE);
            }

            var result = filtered.OrderBy(x => x.BillDate).ThenBy(x => x.BillNo)
                .Select(x => new
                {
                    id = x.PurchaseId,
                    billNo = x.BillNo,
                    billDate = x.BillDate?.ToString("dd-MM-yyyy"),
                    supplier = x.SupplierName,
                    taxable = x.Taxable,
                    gst = x.TotalGST,
                    invoiceAmount = x.InvoiceValue,
                    invoicecategory = x.Category
                });

            return Json(result);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetPurchaseBillItems(int id)
        //{
        //    var purchase = await _purchaseservice.GetById(id);
        //    if (purchase == null || purchase.PurchaseItems == null || !purchase.PurchaseItems.Any())
        //        return Json(new List<object>());

        //    var hsnLookup = (await _hsnservice.GetAll()).ToDictionary(x => x.Id, x => x.HsnCode);

        //    var items = purchase.PurchaseItems
        //        .Where(x => x.Deleted == null)
        //        .Select(x =>
        //        {
        //            var igstPer = x.Gst - (x.CGst + x.SGst);
        //            if (igstPer < 0) igstPer = 0;

        //            var igstTax = x.GstAmount - (x.CGstAmount + x.SGstAmount);
        //            if (igstTax < 0) igstTax = 0;

        //            var taxable = x.Amount;
        //            if (taxable <= 0)
        //            {
        //                var derived = x.TotalAmt - (x.CGstAmount + x.SGstAmount + igstTax + x.Cess);
        //                taxable = derived > 0 ? Math.Round(derived, 2) : 0;
        //            }

        //            var grossAmount = x.TotalAmt > 0
        //                ? x.TotalAmt
        //                : taxable + x.CGstAmount + x.SGstAmount + igstTax + x.Cess;

        //            var totalTax = x.CGstAmount + x.SGstAmount + igstTax + x.Cess;
        //            var isLocal = string.Equals(purchase.PurchaseType, "Local", StringComparison.OrdinalIgnoreCase) || igstTax <= 0;

        //            return new
        //            {
        //                hsn = x.HsnId.HasValue && hsnLookup.ContainsKey(x.HsnId.Value) ? hsnLookup[x.HsnId.Value] : string.Empty,
        //                amount = Math.Round(grossAmount, 2),
        //                taxable = Math.Round(taxable, 2),
        //                sgstPer = x.SGst,
        //                sgstTax = Math.Round(x.SGstAmount, 2),
        //                cgstPer = x.CGst,
        //                cgstTax = Math.Round(x.CGstAmount, 2),
        //                igstPer = Math.Round(igstPer, 2),
        //                igstTax = Math.Round(igstTax, 2),
        //                cess = Math.Round(x.Cess, 2),
        //                totalTax = Math.Round(totalTax, 2),
        //                taxType = isLocal ? "Local" : "Central"
        //            };
        //        })
        //        .ToList();

        //    return Json(items);
        //}
        [HttpGet]
        public async Task<IActionResult> GetPurchaseBillItems(int id)
        {
            var purchase = await _purchaseservice.GetById(id);
            if (purchase == null || purchase.PurchaseItems == null || !purchase.PurchaseItems.Any())
                return Json(new List<object>());

            var hsnLookup = (await _hsnservice.GetAll()).ToDictionary(x => x.Id, x => x.HsnCode);
            var itemMasterHsnLookup = (await _itemmasterservice.GetAll())
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.HsnId);

            var billDiscountPer = purchase.discountPercent; // ✅ purchase level %

            var items = purchase.PurchaseItems
                .Where(x => x.Deleted == null)
                .Select(x =>
                {
                    var igstPer = x.Gst - (x.CGst + x.SGst);
                    if (igstPer < 0) igstPer = 0;

                    // ✅ STEP 1: Base
                    var baseAmount = x.Qty * x.Rate;

                    // ✅ STEP 2: Item Discount (%)
                    var itemDiscountAmt = baseAmount * x.Discount / 100;
                    var afterItemDiscount = baseAmount - itemDiscountAmt;

                    // ✅ STEP 3: Bill Discount (%)
                    var billDiscountAmt = afterItemDiscount * billDiscountPer / 100;

                    // ✅ FINAL TAXABLE
                    var taxable = afterItemDiscount - billDiscountAmt;
                    taxable = taxable > 0 ? Math.Round(taxable, 2) : 0;

                    // ✅ TAX CALCULATION
                    var cgstAmount = taxable * x.CGst / 100;
                    var sgstAmount = taxable * x.SGst / 100;
                    var igstAmount = taxable * igstPer / 100;
                    var cessAmount = taxable * x.Cess / 100;

                    // ✅ TOTAL TAX
                    var totalTax = cgstAmount + sgstAmount + igstAmount + cessAmount;

                    // ✅ FINAL AMOUNT
                    var grossAmount = taxable + totalTax;

                    var isLocal = string.Equals(purchase.PurchaseType, "Local", StringComparison.OrdinalIgnoreCase) || igstAmount <= 0;

                    string hsnCode = string.Empty;
                    if (x.HsnId.HasValue && hsnLookup.TryGetValue(x.HsnId.Value, out var mappedHsn))
                    {
                        hsnCode = mappedHsn ?? string.Empty;
                    }
                    else if (itemMasterHsnLookup.TryGetValue(x.ItemId, out var itemMasterHsnId)
                             && itemMasterHsnId.HasValue
                             && hsnLookup.TryGetValue(itemMasterHsnId.Value, out var fallbackHsn))
                    {
                        hsnCode = fallbackHsn ?? string.Empty;
                    }

                    return new
                    {
                        hsn = hsnCode,

                        amount = Math.Round(grossAmount, 2),
                        taxable = Math.Round(taxable, 2),

                        sgstPer = x.SGst,
                        sgstTax = Math.Round(sgstAmount, 2),

                        cgstPer = x.CGst,
                        cgstTax = Math.Round(cgstAmount, 2),

                        igstPer = Math.Round(igstPer, 2),
                        igstTax = Math.Round(igstAmount, 2),

                        cessPer = x.Cess,
                        cess = Math.Round(cessAmount, 2),

                        totalTax = Math.Round(totalTax, 2),
                        taxType = isLocal ? "Local" : "Central"
                    };
                })
                .ToList();

            return Json(items);
        }
        private async Task<List<StockVM>> GetGSTR2Data(DateTime? startDate, DateTime? endDate)
        {
            var itemMasters = await _itemmasterservice.GetAll();
            var purchases = await _purchaseservice.GetAll();

            if (startDate.HasValue)
                purchases = purchases.Where(x => x.BillDate >= startDate.Value).ToList();

            if (endDate.HasValue)
                purchases = purchases.Where(x => x.BillDate <= endDate.Value).ToList();

            var purchaseItems = purchases
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems);

            var groupedPurchase = purchaseItems
                .GroupBy(x => x.ItemId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.TotalAmt),
                    TotalGst = g.Sum(x =>
                    {
                        decimal gross = x.Rate * x.Qty;
                        decimal discount = gross * (x.Discount / 100);
                        decimal taxable = gross - discount;
                        return taxable * (x.Gst / 100);
                    })
                }).ToList();

            return (from item in itemMasters
                    join g in groupedPurchase on item.Id equals g.ItemMasterId
                    where g.TotalQty > 0
                    select new StockVM
                    {
                        ItemCode = item.Code,
                        ItemName = item.Name,
                        CategoryName = item.Category?.CategoryName ?? "Unknown",
                        Stocks = g.TotalQty,
                        Amount = g.TotalAmount,
                        GstAmount = g.TotalGst
                    }).ToList();
        }


        [HttpGet]
        public async Task<IActionResult> GSTR3Report(DateTime? StartBillDate, DateTime? EndBillDate)
        {
            if (!StartBillDate.HasValue)
            {
                StartBillDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            if (!EndBillDate.HasValue)
            {
                EndBillDate = DateTime.Today;
            }

            ViewBag.StartBillDate = StartBillDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndBillDate = EndBillDate.Value.ToString("yyyy-MM-dd");

            var report = await BuildGstr3ReportAsync(StartBillDate, EndBillDate);
            return View(report);
        }
        private async Task<List<StockVM>> GetGSTR3Data(DateTime? startDate, DateTime? endDate)
        {
            var itemMasters = await _itemmasterservice.GetAll();
            var purchases = await _purchaseservice.GetAll();

            if (startDate.HasValue)
                purchases = purchases.Where(x => x.BillDate >= startDate.Value).ToList();

            if (endDate.HasValue)
                purchases = purchases.Where(x => x.BillDate <= endDate.Value).ToList();

            var purchaseItems = purchases
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems);

            var groupedPurchase = purchaseItems
                .GroupBy(x => x.ItemId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.TotalAmt),
                    TotalGst = g.Sum(x =>
                    {
                        decimal gross = x.Rate * x.Qty;
                        decimal discount = gross * (x.Discount / 100);
                        decimal taxable = gross - discount;
                        return taxable * (x.Gst / 100);
                    })
                }).ToList();

            return (from item in itemMasters
                    join g in groupedPurchase on item.Id equals g.ItemMasterId
                    where g.TotalQty > 0
                    select new StockVM
                    {
                        ItemCode = item.Code,
                        ItemName = item.Name,
                        CategoryName = item.Category?.CategoryName ?? "Unknown",
                        Stocks = g.TotalQty,
                        Amount = g.TotalAmount,
                        GstAmount = g.TotalGst
                    }).ToList();
        }
        public async Task<IActionResult> ExportGSTR3Excel(DateTime? StartBillDate, DateTime? EndBillDate)
        {
            return await ExportGstr3ExcelInternal(StartBillDate, EndBillDate);
        }
        public async Task<IActionResult> ExportGSTR3Csv(DateTime? StartBillDate, DateTime? EndBillDate)
        {
            return await ExportGstr3CsvInternal(StartBillDate, EndBillDate);
        }
        [HttpGet]
        public async Task<IActionResult> MinimumStockReport(string fromDate, string toDate)
        {
            DateTime startDate;
            DateTime endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out startDate))
            {
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Today;
            }

            // ✅ 1. Get Item Masters
            var itemMasters = await _itemmasterservice.GetAll();

            // ✅ 2. Get Current Stock (NO DATE FILTER)
            var currentStocks = await _currenstockService.GetAll();

            // ✅ 3. Group by ItemId
            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            // ✅ 4. Build Report
            var stockList = itemMasters.Select(item =>
            {
                var itemId = item.Id;
                decimal currentStock = stockDict.ContainsKey(itemId) ? stockDict[itemId] : 0;

                return new StockVM
                {
                    ItemMasterId = itemId,
                    ItemName = item.Name,
                    Unit1 = item.Unit1 ?? "",
                    MinQty = item.MinimumQty,
                    MaxQty = item.MaximumQty,
                    Stocks = currentStock
                };
            })
            // ✅ FILTER: Stock <= MinQty AND MinQty > 0
            .Where(x => x.MinQty > 0 && x.Stocks <= x.MinQty)
            .OrderBy(x => x.ItemName)
            .ToList();

            ViewBag.FromDate = startDate.ToString("dd-MM-yyyy");
            ViewBag.ToDate = endDate.ToString("dd-MM-yyyy");
            ViewBag.MinQty = stockList.Sum(x => x.MinQty);
            ViewBag.MaxQty = stockList.Sum(x => x.MaxQty);
            ViewBag.CurrentStock = stockList.Sum(x => x.Stocks);

            return View(stockList);
        }

        //EXPORT TO EXCEL MINIMUM STOCK REPORT 
        [HttpGet]
        public async Task<IActionResult> MinimumStockReportExcel(string fromDate, string toDate)
        {
            DateTime startDate, endDate;
            var today = DateTime.Today;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                startDate = new DateTime(today.Year, today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                endDate = today;

            // ✅ SAME LOGIC AS VIEW
            var itemMasters = await _itemmasterservice.GetAll();
            var currentStocks = await _currenstockService.GetAll();

            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var stockList = itemMasters.Select(item =>
            {
                decimal stock = stockDict.ContainsKey(item.Id) ? stockDict[item.Id] : 0;

                return new StockVM
                {
                    ItemName = item.Name,
                    Unit1 = item.Unit1 ?? "",
                    MinQty = item.MinimumQty,
                    MaxQty = item.MaximumQty,
                    Stocks = stock
                };
            })
            .Where(x => x.MinQty > 0 && x.Stocks <= x.MinQty)
            .OrderBy(x => x.ItemName)
            .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Minimum Stock");

            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null && user.TenantId != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            // HEADER
            ws.Cell("A1").Value = companyName;
            ws.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell("A2").Value = "Minimum Stock Report";
            ws.Range("A2:F2").Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell("A3").Value = $"From {startDate:dd-MM-yyyy} To {endDate:dd-MM-yyyy}";
            ws.Range("A3:F3").Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TABLE HEADER
            int row = 5;
            string[] headers = { "S.No", "Item Name", "Unit", "Min Qty", "Max Qty", "Current Stock" };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
                ws.Cell(row, i + 1).Style.Font.SetBold();
            }

            row++;
            int sr = 1;

            foreach (var x in stockList)
            {
                ws.Cell(row, 1).Value = sr++;
                ws.Cell(row, 2).Value = x.ItemName;
                ws.Cell(row, 3).Value = x.Unit1;
                ws.Cell(row, 4).Value = x.MinQty;
                ws.Cell(row, 5).Value = x.MaxQty;
                ws.Cell(row, 6).Value = x.Stocks;
                row++;
            }

            // TOTAL
            ws.Cell(row, 2).Value = "TOTAL";
            ws.Cell(row, 4).Value = stockList.Sum(x => x.MinQty);
            ws.Cell(row, 5).Value = stockList.Sum(x => x.MaxQty);
            ws.Cell(row, 6).Value = stockList.Sum(x => x.Stocks);

            ws.Range(row, 2, row, 6).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "MinimumStockReport.xlsx");
        }

        // EXPORT TO PDF MINIMUM STOCK REPORT
        [HttpGet]
        public async Task<IActionResult> MinimumStockReportPdf(string fromDate, string toDate)
        {
            DateTime startDate, endDate;
            var today = DateTime.Today;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                startDate = new DateTime(today.Year, today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                endDate = today;

            var itemMasters = await _itemmasterservice.GetAll();
            var currentStocks = await _currenstockService.GetAll();

            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var stockList = itemMasters.Select(item =>
            {
                decimal stock = stockDict.ContainsKey(item.Id) ? stockDict[item.Id] : 0;

                return new StockVM
                {
                    ItemName = item.Name,
                    Unit1 = item.Unit1 ?? "",
                    MinQty = item.MinimumQty,
                    MaxQty = item.MaximumQty,
                    Stocks = stock
                };
            })
            .Where(x => x.MinQty > 0 && x.Stocks <= x.MinQty)
            .OrderBy(x => x.ItemName)
            .ToList();

            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null && user.TenantId != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            using var ms = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(ms));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // HEADER
            doc.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph("Minimum Stock Report")
                .SetFont(bold).SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph($"From {startDate:dd-MM-yyyy} To {endDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // TABLE
            Table table = new Table(6).UseAllAvailableWidth();

            string[] headers = { "S.No", "Item Name", "Unit", "Min Qty", "Max Qty", "Current Stock" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell().Add(new Paragraph(h)
                    .SetFont(bold).SetFontSize(9)));
            }

            int sr = 1;

            foreach (var x in stockList)
            {
                table.AddCell(new Paragraph(sr++.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.ItemName).SetFontSize(9));
                table.AddCell(new Paragraph(x.Unit1).SetFontSize(9));
                table.AddCell(new Paragraph(x.MinQty.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.MaxQty.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.Stocks.ToString("0.00")).SetFontSize(9));
            }

            // TOTAL ROW FIXED
            table.AddCell(new Cell(1, 3) // ✅ 3 columns merge (S.No + Item Name + Unit)
                .Add(new Paragraph("TOTAL")
                .SetFont(bold)
                .SetFontSize(9)));

            table.AddCell(new Paragraph(stockList.Sum(x => x.MinQty).ToString())
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(stockList.Sum(x => x.MaxQty).ToString())
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(stockList.Sum(x => x.Stocks).ToString("0.00"))
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT));

            doc.Add(table);
            doc.Close();

            return File(ms.ToArray(), "application/pdf", "MinimumStockReport.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DumpStockReport(string fromDate, string toDate, int? DaysNotSold)
        {
            DateTime startDate;
            DateTime endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out startDate))
            {
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Today;
            }

            endDate = endDate.AddDays(1).AddTicks(-1);
            int daysThreshold = DaysNotSold ?? 90;

            // ✅ 1. Get Item Masters
            var itemMasters = await _itemmasterservice.GetAll();

            // ✅ 2. Get Current Stock (NO DATE FILTER - IMPORTANT)
            var currentStocks = await _currenstockService.GetAll();

            // ✅ 3. Get Sales (NO DATE FILTER - IMPORTANT)
            var sales = (await _salesService.GetAll())
                .Where(s => s.SalesItems != null)
                .ToList();

            // ✅ 4. Get Purchases (NO DATE FILTER - IMPORTANT)
            var purchases = (await _purchaseservice.GetAll())
                .Where(p => p.PurchaseItems != null)
                .ToList();

            // ✅ 5. Build Report from CurrentStocks
            var dumpStockList = currentStocks
                .GroupBy(x => new { x.ItemId, x.Batch })
                .Select(g =>
                {
                    var itemId = g.Key.ItemId;
                    var batch = g.Key.Batch;

                    var item = itemMasters.FirstOrDefault(i => i.Id == itemId);
                    if (item == null) return null;

                    var currentStock = g.Sum(x => x.Qty);

                    if (currentStock <= 0) return null;

                    // ✅ Last Sale Date
                    DateTime? lastSalesDate = sales
                        .Where(s => s.SalesItems.Any(si => si.ItemMasterId == itemId))
                        .OrderByDescending(s => s.BillDate)
                        .Select(s => s.BillDate)
                        .FirstOrDefault();

                    // ✅ Batch Purchase Date
                    DateTime? batchPurchaseDate = purchases
                        .SelectMany(p => p.PurchaseItems)
                        .Where(x => x.ItemId == itemId && x.Batch == batch)
                        .OrderByDescending(x => x.Purchases.BillDate)
                        .Select(x => x.Purchases.BillDate)
                        .FirstOrDefault();

                    int daysNotSold = 0;

                    if (lastSalesDate.HasValue)
                    {
                        daysNotSold = (endDate - lastSalesDate.Value).Days;
                    }
                    else if (batchPurchaseDate.HasValue)
                    {
                        daysNotSold = (endDate - batchPurchaseDate.Value).Days;
                    }

                    if (daysNotSold < daysThreshold)
                        return null;

                    return new StockVM
                    {
                        ItemMasterId = itemId,
                        ItemName = item.Name,
                        Batch = batch,
                        ExpiryDate = g.FirstOrDefault()?.ExpiryDate,
                        Qty = currentStock,
                        DaysSincePurchase = daysNotSold
                    };
                })
                .Where(x => x != null)
                .ToList();

            ViewBag.AsOnDate = endDate.ToString("dd-MM-yyyy");
            ViewBag.DaysThreshold = daysThreshold;
            ViewBag.TotalQty = dumpStockList.Sum(x => x.Qty);
            return View(dumpStockList);
        }
        //EXPORT TO EXCEL OF DUMP STOCK REPORT SAKSHI
        [HttpGet]
        public async Task<IActionResult> DumpStockReportExcel(string fromDate, string toDate, int? DaysNotSold)
        {
            DateTime startDate, endDate;
            var today = DateTime.Today;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                startDate = new DateTime(today.Year, today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                endDate = today;

            endDate = endDate.AddDays(1).AddTicks(-1);
            int daysThreshold = DaysNotSold ?? 90;

            var itemMasters = await _itemmasterservice.GetAll();
            var currentStocks = await _currenstockService.GetAll();
            var sales = (await _salesService.GetAll()).Where(s => s.SalesItems != null).ToList();
            var purchases = (await _purchaseservice.GetAll()).Where(p => p.PurchaseItems != null).ToList();

            // ✅ SAME LOGIC AS VIEW
            var dumpStockList = currentStocks
                .GroupBy(x => new { x.ItemId, x.Batch })
                .Select(g =>
                {
                    var item = itemMasters.FirstOrDefault(i => i.Id == g.Key.ItemId);
                    if (item == null) return null;

                    var stock = g.Sum(x => x.Qty);
                    if (stock <= 0) return null;

                    DateTime? lastSalesDate = sales
                        .Where(s => s.SalesItems.Any(si => si.ItemMasterId == g.Key.ItemId))
                        .OrderByDescending(s => s.BillDate)
                        .Select(s => s.BillDate)
                        .FirstOrDefault();

                    DateTime? purchaseDate = purchases
                        .SelectMany(p => p.PurchaseItems)
                        .Where(x => x.ItemId == g.Key.ItemId && x.Batch == g.Key.Batch)
                        .OrderByDescending(x => x.Purchases.BillDate)
                        .Select(x => x.Purchases.BillDate)
                        .FirstOrDefault();

                    int days = lastSalesDate.HasValue
                        ? (endDate - lastSalesDate.Value).Days
                        : purchaseDate.HasValue
                            ? (endDate - purchaseDate.Value).Days
                            : 0;

                    if (days < daysThreshold) return null;

                    return new StockVM
                    {
                        ItemName = item.Name,
                        Batch = g.Key.Batch,
                        ExpiryDate = g.FirstOrDefault()?.ExpiryDate,
                        Qty = stock,
                        DaysSincePurchase = days
                    };
                })
                .Where(x => x != null)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Dump Stock");

            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null && user.TenantId != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            // HEADER
            ws.Cell("A1").Value = companyName;
            ws.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell("A2").Value = "Dump Stock Report";
            ws.Range("A2:F2").Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell("A3").Value = $"As On Date : {endDate:dd-MM-yyyy}";
            ws.Range("A3:F3").Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell("A4").Value = $"Days Not Sold >= {daysThreshold}";
            ws.Range("A4:F4").Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 6;

            string[] headers = { "S.No", "Item Name", "Batch", "Expiry Date", "Qty", "Days" };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
                ws.Cell(row, i + 1).Style.Font.SetBold();
            }

            row++;
            int sr = 1;

            foreach (var x in dumpStockList)
            {
                ws.Cell(row, 1).Value = sr++;
                ws.Cell(row, 2).Value = x.ItemName;
                ws.Cell(row, 3).Value = x.Batch;
                ws.Cell(row, 4).Value = x.ExpiryDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 5).Value = x.Qty;
                ws.Cell(row, 6).Value = x.DaysSincePurchase;
                row++;
            }

            // TOTAL
            ws.Cell(row, 2).Value = "TOTAL";
            ws.Cell(row, 5).Value = dumpStockList.Sum(x => x.Qty);
            ws.Range(row, 2, row, 6).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "DumpStockReport.xlsx");
        }

        //EXPORT TO PDF OF DUMP STOCK REPORT SAKSHI
        [HttpGet]
        public async Task<IActionResult> DumpStockReportPdf(string fromDate, string toDate, int? DaysNotSold)
        {
            DateTime startDate, endDate;
            var today = DateTime.Today;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                startDate = new DateTime(today.Year, today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                endDate = today;

            endDate = endDate.AddDays(1).AddTicks(-1);
            int daysThreshold = DaysNotSold ?? 90;

            var itemMasters = await _itemmasterservice.GetAll();
            var currentStocks = await _currenstockService.GetAll();
            var sales = (await _salesService.GetAll()).Where(s => s.SalesItems != null).ToList();
            var purchases = (await _purchaseservice.GetAll()).Where(p => p.PurchaseItems != null).ToList();

            var dumpStockList = currentStocks
                .GroupBy(x => new { x.ItemId, x.Batch })
                .Select(g =>
                {
                    var item = itemMasters.FirstOrDefault(i => i.Id == g.Key.ItemId);
                    if (item == null) return null;

                    var stock = g.Sum(x => x.Qty);
                    if (stock <= 0) return null;

                    DateTime? lastSalesDate = sales
                        .Where(s => s.SalesItems.Any(si => si.ItemMasterId == g.Key.ItemId))
                        .OrderByDescending(s => s.BillDate)
                        .Select(s => s.BillDate)
                        .FirstOrDefault();

                    DateTime? purchaseDate = purchases
                        .SelectMany(p => p.PurchaseItems)
                        .Where(x => x.ItemId == g.Key.ItemId && x.Batch == g.Key.Batch)
                        .OrderByDescending(x => x.Purchases.BillDate)
                        .Select(x => x.Purchases.BillDate)
                        .FirstOrDefault();

                    int days = lastSalesDate.HasValue
                        ? (endDate - lastSalesDate.Value).Days
                        : purchaseDate.HasValue
                            ? (endDate - purchaseDate.Value).Days
                            : 0;

                    if (days < daysThreshold) return null;

                    return new StockVM
                    {
                        ItemName = item.Name,
                        Batch = g.Key.Batch,
                        ExpiryDate = g.FirstOrDefault()?.ExpiryDate,
                        Qty = stock,
                        DaysSincePurchase = days
                    };
                })
                .Where(x => x != null)
                .ToList();

            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null && user.TenantId != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            using var ms = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(ms));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // HEADER
            doc.Add(new Paragraph(companyName).SetFont(bold).SetFontSize(12).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph("Dump Stock Report").SetFont(bold).SetFontSize(10).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph($"As On Date : {endDate:dd-MM-yyyy}").SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph($"Days Not Sold >= {daysThreshold}").SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph(" "));

            Table table = new Table(6).UseAllAvailableWidth();

            string[] headers = { "S.No", "Item Name", "Batch", "Expiry Date", "Qty", "Days" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(9)));
            }

            int sr = 1;

            foreach (var x in dumpStockList)
            {
                table.AddCell(new Paragraph(sr++.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.ItemName).SetFontSize(9));
                table.AddCell(new Paragraph(x.Batch).SetFontSize(9));
                table.AddCell(new Paragraph(x.ExpiryDate?.ToString("dd-MM-yyyy")).SetFontSize(9));
                table.AddCell(new Paragraph(x.Qty.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.DaysSincePurchase.ToString()).SetFontSize(9));
            }

            // TOTAL FIX
            table.AddCell(new Cell(1, 4).Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(9)));
            table.AddCell(new Paragraph(dumpStockList.Sum(x => x.Qty).ToString()).SetFont(bold).SetFontSize(9));
            table.AddCell("");

            doc.Add(table);
            doc.Close();

            return File(ms.ToArray(), "application/pdf", "DumpStockReport.pdf");
        }
        //[HttpGet]
        //public async Task<IActionResult> SupplierWiseStock(string fromDate, string toDate, int? supplierId)
        //{
        //    // 1. Parse dates
        //    DateTime startDate;
        //    DateTime endDate;

        //    // Parse From Date
        //    if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParseExact(fromDate, "dd-MM-yyyy",
        //        System.Globalization.CultureInfo.InvariantCulture,
        //        System.Globalization.DateTimeStyles.None, out var fDate))
        //    {
        //        startDate = fDate;
        //    }
        //    else
        //    {
        //        startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        //    }

        //    // Parse To Date
        //    if (!string.IsNullOrEmpty(toDate) && DateTime.TryParseExact(toDate, "dd-MM-yyyy",
        //        System.Globalization.CultureInfo.InvariantCulture,
        //        System.Globalization.DateTimeStyles.None, out var tDate))
        //    {
        //        endDate = tDate;
        //    }
        //    else
        //    {
        //        endDate = DateTime.Today;
        //    }

        //    endDate = endDate.AddDays(1).AddTicks(-1); // Include full day

        //    // 2. Get suppliers for dropdown
        //    var suppliers = await _supplierservice.GetALL();
        //    ViewBag.SupplierList = suppliers?.ToList() ?? new List<Supplier>();
        //    ViewBag.SelectedSupplier = supplierId;
        //    ViewBag.FromDate = startDate.ToString("dd-MM-yyyy");
        //    ViewBag.ToDate = endDate.ToString("dd-MM-yyyy");

        //    // 3. Get all purchases and filter by date
        //    var purchases = (await _purchaseservice.GetAll())
        //        .Where(p => p.BillDate >= startDate && p.BillDate <= endDate)
        //        .ToList();

        //    // 4. Filter by supplier if selected
        //    if (supplierId.HasValue && supplierId > 0)
        //    {
        //        purchases = purchases.Where(p => p.SupplierId == supplierId.Value).ToList();
        //    }

        //    var model = purchases.Select(p => new PurchaseItemVM
        //    {
        //        BillDate = p.BillDate,
        //        BillNo = p.BillNo,
        //        BillValue = p.TotalPayable, // Invoice total including GST
        //        SupplierName = p.Suppliers?.FirstName ?? "",
        //        //PurchaseAmount = p.PurchaseItems?.Sum(x => x.TotalAmt) ?? 0, // Amount before GST
        //        //GstAmount = p.PurchaseItems?.Sum(x => x.GstAmount) ?? 0,   // GST separately
        //        GstAmount = p.TotalGstAmt,
        //        ExampleData = p.PurchaseItems != null
        //             ? p.PurchaseItems
        //                 .Where(x => x.ItemMasters != null)
        //                 .Select(x => x.ItemMasters.Name)
        //                 .Distinct()
        //                 .ToList()
        //             : new List<string>()
        //    }).ToList();
        //    ViewBag.TotalBillValue = model.Sum(x => x.BillValue);
        //    ViewBag.TotalGstAmt = model.Sum(x => x.GstAmount);
        //    return View(model);
        //}

        [HttpGet]
        public async Task<IActionResult> SupplierWiseStock(string fromDate, string toDate, int? supplierId)
        {
            // 1. Suppliers dropdown
            var suppliers = await _supplierservice.GetALL();
            ViewBag.SupplierList = suppliers?.ToList() ?? new List<Supplier>();
            ViewBag.SelectedSupplier = supplierId;
            ViewBag.FromDate = fromDate ?? DateTime.Today.ToString("dd-MM-yyyy");
            ViewBag.ToDate = toDate ?? DateTime.Today.ToString("dd-MM-yyyy");

            // 2. Item Masters
            var itemMasters = (await _itemmasterservice.GetAll()).Where(i => i.IsActive);
            var itemDict = itemMasters.ToDictionary(i => i.Id);

            // 3. Get Pre-calculated Stock
            var currentStocks = await _currenstockService.GetAll();

            // 4. Supplier Filter
            if (supplierId.HasValue && supplierId > 0)
            {
                var supplierItemIds = (await _purchaseservice.GetAll())
                    .Where(p => p.SupplierId == supplierId.Value)
                    .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                    .Select(pi => pi.ItemId)
                    .Distinct()
                    .ToHashSet();

                currentStocks = currentStocks.Where(x => supplierItemIds.Contains(x.ItemId)).ToList();
            }

            // 5. Get Supplier Names
            var allPurchases = await _purchaseservice.GetAll();
            var supplierNameDict = allPurchases
                .SelectMany(p => (p.PurchaseItems ?? new List<PurchaseItem>()).Select(pi => new
                {
                    ItemId = pi.ItemId,
                    SupplierName = p.Suppliers?.FirstName ?? ""
                }))
                .GroupBy(x => x.ItemId)
                .Select(g => new { ItemId = g.Key, SupplierName = g.FirstOrDefault()!.SupplierName })
                .ToDictionary(x => x.ItemId, x => x.SupplierName);

            // ✅ 6. Group by ItemId + Batch WITH REAL AVERAGE CALCULATION
            var batchStockDict = currentStocks
                .GroupBy(x => new { x.ItemId, x.Batch })
                .Select(g =>
                {
                    decimal totalQty = g.Sum(x => x.Qty);
                    decimal totalAmount = g.Sum(x => x.Qty * (x.PurchaseRate > 0 ? x.PurchaseRate : 0));

                    // ✅ REAL AVERAGE RATE FORMULA
                    decimal avgRate = totalQty > 0 ? totalAmount / totalQty : 0;

                    return new
                    {
                        Key = g.Key,
                        Qty = totalQty,
                        ExpiryDate = g.FirstOrDefault()?.ExpiryDate,
                        AvgRate = avgRate // ✅ Calculated average
                    };
                })
                .ToList();

            // 7. TabletWise Setting
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            // 8. Build Final Report
            var model = batchStockDict
                .Select(x =>
                {
                    var itemId = x.Key.ItemId;
                    var batch = x.Key.Batch;

                    if (!itemDict.ContainsKey(itemId)) return null;

                    var itemMaster = itemDict[itemId];
                    decimal currentQty = x.Qty;

                    if (currentQty <= 0) return null;

                    var supplierName = supplierNameDict.ContainsKey(itemId) ? supplierNameDict[itemId] : "Unknown";

                    int conversion = itemMaster.Conversion > 0 ? itemMaster.Conversion : 1;
                    string qtyDisplay;

                    if (isTabletWise && conversion > 0)
                    {
                        int totalTablets = (int)Math.Round(currentQty * conversion, MidpointRounding.AwayFromZero);
                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;
                        qtyDisplay = $"{strips}:{tablets}";
                    }
                    else
                    {
                        qtyDisplay = currentQty.ToString("0.##");
                    }

                    // ✅ Stock Value using AVERAGE RATE
                    decimal stockValue = currentQty * x.AvgRate;

                    return new PurchaseItemVM
                    {
                        ItemName = itemMaster.Name,
                        Batch = batch,
                        ExpiryDate = x.ExpiryDate,
                        SupplierName = supplierName,
                        CurrentQty = currentQty,
                        CurrentQtyDisplay = qtyDisplay,
                        Conversion = conversion,
                        AvgRate = Math.Round(x.AvgRate, 2), // ✅ Real Average
                        StockValue = Math.Round(stockValue, 2)
                    };
                })
                .Where(x => x != null)
                .OrderBy(x => x.SupplierName)
                .ThenBy(x => x.ItemName)
                .ToList();

            ViewBag.TotalStockValue = model.Sum(x => x.StockValue);
            ViewBag.IsTabletWise = isTabletWise;
            ViewBag.TotalQty = isTabletWise ? null : model.Sum(x => x.CurrentQty).ToString("0.##");

            return View(model);
        }

        //EXPORT TO SUPPLIER WISE STOCK
        [HttpGet]
        public async Task<IActionResult> SupplierWiseStockExcel(string fromDate, string toDate, int? supplierId)
        {
            // 🔹 SAME LOGIC AS MAIN REPORT
            var itemMasters = (await _itemmasterservice.GetAll()).Where(i => i.IsActive);
            var itemDict = itemMasters.ToDictionary(i => i.Id);

            var currentStocks = await _currenstockService.GetAll();

            if (supplierId.HasValue && supplierId > 0)
            {
                var supplierItemIds = (await _purchaseservice.GetAll())
                    .Where(p => p.SupplierId == supplierId.Value)
                    .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                    .Select(pi => pi.ItemId)
                    .Distinct()
                    .ToHashSet();

                currentStocks = currentStocks.Where(x => supplierItemIds.Contains(x.ItemId)).ToList();
            }

            var allPurchases = await _purchaseservice.GetAll();
            var supplierDict = allPurchases
                .SelectMany(p => (p.PurchaseItems ?? new List<PurchaseItem>())
                .Select(pi => new { pi.ItemId, Supplier = p.Suppliers?.FirstName ?? "" }))
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.First().Supplier);

            var batchStock = currentStocks
                .GroupBy(x => new { x.ItemId, x.Batch })
                .Select(g =>
                {
                    decimal qty = g.Sum(x => x.Qty);
                    decimal amt = g.Sum(x => x.Qty * x.PurchaseRate);
                    decimal avg = qty > 0 ? amt / qty : 0;

                    return new
                    {
                        g.Key.ItemId,
                        g.Key.Batch,
                        Qty = qty,
                        Expiry = g.FirstOrDefault()?.ExpiryDate,
                        AvgRate = avg
                    };
                }).ToList();

            var model = batchStock
                .Select(x =>
                {
                    if (!itemDict.ContainsKey(x.ItemId)) return null;

                    var item = itemDict[x.ItemId];
                    var supplier = supplierDict.ContainsKey(x.ItemId) ? supplierDict[x.ItemId] : "";

                    int conv = item.Conversion > 0 ? item.Conversion : 1;
                    int totalTabs = (int)Math.Round(x.Qty * conv);
                    int strip = totalTabs / conv;
                    int tab = totalTabs % conv;

                    return new
                    {
                        Supplier = supplier,
                        ItemName = item.Name,
                        Batch = x.Batch,
                        Expiry = x.Expiry?.ToString("MM-yyyy"),
                        Qty = $"{strip}:{tab}",
                        AvgRate = Math.Round(x.AvgRate, 2),
                        StockValue = Math.Round(x.Qty * x.AvgRate, 2)
                    };
                })
                .Where(x => x != null && x.StockValue > 0)
                .OrderBy(x => x.Supplier)
                .ThenBy(x => x.ItemName)
                .ToList();

            // 🔹 EXCEL
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Supplier Stock");

            // Company Name
            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";
            if (user?.TenantId != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                if (tenant != null) companyName = tenant.Name;
            }

            ws.Cell("A1").Value = companyName;
            ws.Range("A1:H1").Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell("A2").Value = "Supplier Wise Stock Report";
            ws.Range("A2:H2").Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // HEADER
            string[] headers = { "S.No", "Supplier", "Item Name", "Batch", "Expiry", "Current Qty", "Avg Rate", "Stock Value" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(5, i + 1).Value = headers[i];

            ws.Range("A5:H5").Style.Font.SetBold();

            int row = 6, sr = 1;

            foreach (var x in model)
            {
                ws.Cell(row, 1).Value = sr++;
                ws.Cell(row, 2).Value = x.Supplier;
                ws.Cell(row, 3).Value = x.ItemName;
                ws.Cell(row, 4).Value = x.Batch;
                ws.Cell(row, 5).Value = x.Expiry;
                ws.Cell(row, 6).Value = x.Qty;
                ws.Cell(row, 7).Value = x.AvgRate;
                ws.Cell(row, 8).Value = x.StockValue;

                ws.Cell(row, 7).Style.NumberFormat.Format = "0.00";
                ws.Cell(row, 8).Style.NumberFormat.Format = "0.00";

                row++;
            }

            // TOTAL
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 8).Value = model.Sum(x => x.StockValue);

            ws.Range(row, 1, row, 8).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SupplierWiseStock.xlsx");
        }

        //EXPORT TO PDF SUPPLIER WISE STOCK 
        [HttpGet]
        public async Task<IActionResult> SupplierWiseStockPdf(string fromDate, string toDate, int? supplierId)
        {
            // 🔹 SAME LOGIC AS MAIN REPORT
            var itemMasters = (await _itemmasterservice.GetAll()).Where(i => i.IsActive);
            var itemDict = itemMasters.ToDictionary(i => i.Id);

            var currentStocks = await _currenstockService.GetAll();

            if (supplierId.HasValue && supplierId > 0)
            {
                var supplierItemIds = (await _purchaseservice.GetAll())
                    .Where(p => p.SupplierId == supplierId.Value)
                    .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                    .Select(pi => pi.ItemId)
                    .Distinct()
                    .ToHashSet();

                currentStocks = currentStocks.Where(x => supplierItemIds.Contains(x.ItemId)).ToList();
            }

            var allPurchases = await _purchaseservice.GetAll();
            var supplierDict = allPurchases
                .SelectMany(p => (p.PurchaseItems ?? new List<PurchaseItem>())
                .Select(pi => new { pi.ItemId, Supplier = p.Suppliers?.FirstName ?? "" }))
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.First().Supplier);

            var batchStock = currentStocks
                .GroupBy(x => new { x.ItemId, x.Batch })
                .Select(g =>
                {
                    decimal qty = g.Sum(x => x.Qty);
                    decimal amt = g.Sum(x => x.Qty * (x.PurchaseRate > 0 ? x.PurchaseRate : 0));
                    decimal avg = qty > 0 ? amt / qty : 0;

                    return new
                    {
                        g.Key.ItemId,
                        g.Key.Batch,
                        Qty = qty,
                        Expiry = g.FirstOrDefault()?.ExpiryDate,
                        AvgRate = avg
                    };
                }).ToList();

            var model = batchStock
                .Select(x =>
                {
                    if (!itemDict.ContainsKey(x.ItemId)) return null;

                    var item = itemDict[x.ItemId];
                    var supplier = supplierDict.ContainsKey(x.ItemId) ? supplierDict[x.ItemId] : "";

                    int conv = item.Conversion > 0 ? item.Conversion : 1;

                    int totalTabs = (int)Math.Round(x.Qty * conv, MidpointRounding.AwayFromZero);
                    int strip = totalTabs / conv;
                    int tab = totalTabs % conv;

                    return new
                    {
                        Supplier = supplier,
                        ItemName = item.Name,
                        Batch = x.Batch ?? "",
                        Expiry = x.Expiry.HasValue ? x.Expiry.Value.ToString("MM-yyyy") : "", // ✅ FIX
                        Qty = $"{strip}:{tab}",
                        AvgRate = Math.Round(x.AvgRate, 2),
                        StockValue = Math.Round(x.Qty * x.AvgRate, 2)
                    };
                })
                .Where(x => x != null && x.StockValue > 0)
                .OrderBy(x => x.Supplier)
                .ThenBy(x => x.ItemName)
                .ToList();

            // 🔹 PDF
            using var ms = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(ms));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";
            if (user?.TenantId != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                if (tenant != null) companyName = tenant.Name;
            }

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // 🔹 HEADER
            doc.Add(new Paragraph(companyName)
                .SetFont(bold)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph("Supplier Wise Stock Report")
                .SetFont(bold)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph(" "));

            // 🔹 TABLE
            Table table = new Table(8).UseAllAvailableWidth();

            string[] headers = { "S.No", "Supplier", "Item Name", "Batch", "Expiry", "Current Qty", "Avg Rate", "Stock Value" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell().Add(new Paragraph(h)
                    .SetFont(bold)
                    .SetFontSize(9)));
            }

            int sr = 1;

            foreach (var x in model)
            {
                table.AddCell(new Paragraph(sr++.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.Supplier ?? "").SetFontSize(9));
                table.AddCell(new Paragraph(x.ItemName ?? "").SetFontSize(9));
                table.AddCell(new Paragraph(x.Batch ?? "").SetFontSize(9));
                table.AddCell(new Paragraph(x.Expiry ?? "").SetFontSize(9)); // ✅ FIX
                table.AddCell(new Paragraph(x.Qty ?? "").SetFontSize(9));
                table.AddCell(new Paragraph(x.AvgRate.ToString("0.00")).SetFontSize(9));
                table.AddCell(new Paragraph(x.StockValue.ToString("0.00")).SetFontSize(9));
            }

            // 🔹 TOTAL ROW
            table.AddCell(new Cell(1, 7)
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(9)));

            table.AddCell(new Paragraph("₹ " + model.Sum(x => x.StockValue).ToString("0.00"))
                .SetFont(bold)
                .SetFontSize(9));

            doc.Add(table);
            doc.Close();

            return File(ms.ToArray(), "application/pdf", "SupplierWiseStock.pdf");
        }

        //[HttpGet]
        //public async Task<IActionResult> CompanyWiseStock(string fromDate, string toDate, int? companyId, string reportType)
        //{
        //    DateTime startDate;
        //    DateTime endDate;

        //    if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy",
        //        System.Globalization.CultureInfo.InvariantCulture,
        //        System.Globalization.DateTimeStyles.None, out startDate))
        //    {
        //        startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        //    }

        //    if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
        //        System.Globalization.CultureInfo.InvariantCulture,
        //        System.Globalization.DateTimeStyles.None, out endDate))
        //    {
        //        endDate = DateTime.Today;
        //    }

        //    endDate = endDate.AddDays(1).AddTicks(-1); // Full day include

        //    var companies = await _companyServices.GetAll();
        //    ViewBag.companydata = companies?.ToList() ?? new List<Company>();
        //    ViewBag.SelectedCompanyId = companyId;
        //    ViewBag.ReportType = reportType ?? "Details";
        //    ViewBag.FromDate = startDate.ToString("dd-MM-yyyy");
        //    ViewBag.ToDate = endDate.ToString("dd-MM-yyyy");

        //    var purchases = await _purchaseservice.GetAll();

        //    purchases = purchases
        //        .Where(p => p.BillDate >= startDate && p.BillDate <= endDate)
        //        .ToList();

        //    var purchaseItems = purchases
        //        .Where(p => p.PurchaseItems != null)
        //        .SelectMany(p => p.PurchaseItems)
        //        .ToList();

        //    if (companyId.HasValue && companyId > 0)
        //    {
        //        purchaseItems = purchaseItems
        //            .Where(x => x.ItemMasters != null &&
        //                        x.ItemMasters.CompanyId == companyId)
        //            .ToList();
        //    }

        //    if (reportType == "Details")
        //    {
        //        var detailsList = purchaseItems
        //            .Select(x => new PurchaseItemVM
        //            {
        //                BillNo = x.Purchases?.BillNo,
        //                BillDate = x.Purchases?.BillDate,
        //                ItemName = x.ItemMasters?.Name,
        //                Batch = x.Batch,
        //                Qty = (x.Qty) + (x.FreeQty),
        //                Rate = x.Rate,
        //                Amount = x.TotalAmt
        //            })
        //            .ToList();
        //        ViewBag.TotalQty = detailsList.Sum(x => x.Qty);
        //        ViewBag.TotalRate = detailsList.Sum(x => x.Rate);
        //        ViewBag.TotalAmount = detailsList.Sum(x => x.Amount);
        //        return View(detailsList);
        //    }
        //    else
        //    {
        //            var summaryList = purchaseItems
        //                .Where(x => x.ItemMasters != null && x.ItemMasters.Company != null)
        //                .GroupBy(x => new
        //                {
        //                    CompanyName = x.ItemMasters.Company.Name,
        //                    ItemName = x.ItemMasters.Name,
        //                    Packing = x.ItemMasters.Packing,
        //                    Unit = x.ItemMasters.Unit1,
        //                })
        //                .Select(g => new PurchaseItemVM
        //                {
        //                    CompanyName = g.Key.CompanyName,
        //                    ItemName = g.Key.ItemName,
        //                    Packing = g.Key.Packing,
        //                    Unit = g.Key.Unit,

        //                    // Stock Qty = Qty + FreeQty
        //                    AvailableQty = g.Sum(x => (x.Qty + x.FreeQty)),

        //                    // Optional: Avg Rate if needed
        //                    AvgRate = g.Average(x => x.Rate),

        //                    // Total Purchase Amount
        //                    PurchaseAmount = g.Sum(x => x.TotalAmt)
        //                })
        //                .OrderBy(x => x.CompanyName)
        //                .ThenBy(x => x.ItemName)
        //                .ToList();

        //            ViewBag.AvgRate = summaryList.Sum(x => x.AvgRate);
        //            ViewBag.TotalQty = summaryList.Sum(x => x.AvailableQty);
        //            ViewBag.TotalAmount = summaryList.Sum(x => x.PurchaseAmount);

        //            return View(summaryList);
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> CompanyWiseStock(string fromDate, string toDate, int? companyId, string reportType)
        {
            DateTime startDate;
            DateTime endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out startDate))
            {
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Today;
            }

            endDate = endDate.AddDays(1).AddTicks(-1);

            var companies = await _companyServices.GetAll();
            ViewBag.companydata = companies?.ToList() ?? new List<Company>();
            ViewBag.SelectedCompanyId = companyId;

            // ✅✅✅ YE LINE ADD KARO ✅✅✅
            reportType = reportType ?? "Details";
            ViewBag.ReportType = reportType;

            ViewBag.FromDate = startDate.ToString("dd-MM-yyyy");
            ViewBag.ToDate = endDate.ToString("dd-MM-yyyy");

            // ... baaki code same rahega

            // ✅ 1. Item Masters + Dictionary
            var itemMasters = await _itemmasterservice.GetAll();
            var itemDict = itemMasters.ToDictionary(x => x.Id);

            // ✅ 2. Current Stock
            var currentStocks = await _currenstockService.GetAll();

            // ✅ 3. All Purchases (for Date + BillNo)
            var allPurchases = await _purchaseservice.GetAll();

            // ✅ 4. Company Filter - Only ItemIds
            var filteredItemIds = itemMasters.Select(i => i.Id).ToHashSet();
            if (companyId.HasValue && companyId > 0)
            {
                filteredItemIds = filteredItemIds
                    .Where(id => itemDict.ContainsKey(id) && itemDict[id].CompanyId == companyId)
                    .ToHashSet();
            }

            // ✅ 5. TabletWise Setting
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            // ==================== DETAILS MODE ====================
            if (reportType == "Details")
            {
                // Batch level stock dictionary
                var batchStockDict = currentStocks
                    .Where(x => filteredItemIds.Contains(x.ItemId))
                    .GroupBy(x => new { x.ItemId, x.Batch })
                    .ToDictionary(g => g.Key, g => new
                    {
                        TotalQty = g.Sum(x => x.Qty),
                        AvgRate = g.Sum(x => x.Qty) > 0
                            ? g.Sum(x => x.Qty * (x.PurchaseRate > 0 ? x.PurchaseRate : 0)) / g.Sum(x => x.Qty)
                            : 0
                    });

                // Purchase info (Date + BillNo) per batch
                var purchaseBatchInfo = allPurchases
                    .Where(p => p.PurchaseItems != null)
                    .SelectMany(p => p.PurchaseItems.Select(pi => new
                    {
                        ItemId = pi.ItemId,
                        Batch = pi.Batch ?? "",
                        BillNo = p.BillNo ?? "",
                        BillDate = p.BillDate
                    }))
                    .GroupBy(x => $"{x.ItemId}_{x.Batch}")
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.BillDate).First());

                var detailsList = batchStockDict
                    .Where(x => x.Value.TotalQty > 0)
                    .Select(x =>
                    {
                        var itemId = x.Key.ItemId;
                        var batch = x.Key.Batch;
                        var stock = x.Value;

                        if (!itemDict.ContainsKey(itemId)) return null;

                        var item = itemDict[itemId];
                        var key = $"{itemId}_{batch}";
                        var purchaseInfo = purchaseBatchInfo.ContainsKey(key) ? purchaseBatchInfo[key] : null;

                        int conversion = item.Conversion > 0 ? item.Conversion : 1;
                        string qtyDisplay;

                        if (isTabletWise && conversion > 0)
                        {
                            int totalTablets = (int)Math.Round(stock.TotalQty * conversion, MidpointRounding.AwayFromZero);
                            int strips = totalTablets / conversion;
                            int tablets = totalTablets % conversion;
                            qtyDisplay = $"{strips}:{tablets}";
                        }
                        else
                        {
                            qtyDisplay = stock.TotalQty.ToString("0.##");
                        }

                        return new PurchaseItemVM
                        {
                            BillNo = purchaseInfo?.BillNo ?? "-",
                            BillDate = purchaseInfo?.BillDate,
                            ItemName = item.Name,
                            Batch = batch ?? "-",
                            Qty = stock.TotalQty,
                            CurrentQtyDisplay = qtyDisplay,
                            Rate = Math.Round(stock.AvgRate, 2),
                            Amount = Math.Round(stock.TotalQty * stock.AvgRate, 2)
                        };
                    })
                    .Where(x => x != null)
                    .OrderBy(x => x.ItemName)
                    .ThenBy(x => x.Batch)
                    .ToList();

                ViewBag.TotalQty = detailsList.Sum(x => x.Qty);
                ViewBag.TotalRate = detailsList.Sum(x => x.Rate);
                ViewBag.TotalAmount = detailsList.Sum(x => x.Amount);
                ViewBag.IsTabletWise = isTabletWise;

                return View(detailsList);
            }
            // ==================== SUMMARY MODE ====================
            else
            {
                // ✅ FIX: Item level stock dictionary
                var itemStockDict = currentStocks
                    .Where(x => filteredItemIds.Contains(x.ItemId))
                    .GroupBy(x => x.ItemId)
                    .ToDictionary(g => g.Key, g => new
                    {
                        TotalQty = g.Sum(x => x.Qty),
                        AvgRate = g.Sum(x => x.Qty) > 0
                            ? g.Sum(x => x.Qty * (x.PurchaseRate > 0 ? x.PurchaseRate : 0)) / g.Sum(x => x.Qty)
                            : 0
                    });

                // ✅ FIX: Use itemMasters list, NOT filteredItems (which is HashSet<int>)
                var filteredItemList = itemMasters
                    .Where(i => filteredItemIds.Contains(i.Id))
                    .ToList();

                var summaryList = filteredItemList
                    .Where(x => x.Company != null)
                    .Select(item =>
                    {
                        var id = item.Id;
                        decimal currentStock = itemStockDict.ContainsKey(id) ? itemStockDict[id].TotalQty : 0;
                        decimal avgRate = itemStockDict.ContainsKey(id) ? itemStockDict[id].AvgRate : 0;

                        int conversion = item.Conversion > 0 ? item.Conversion : 1;
                        string qtyDisplay;

                        if (isTabletWise && conversion > 0)
                        {
                            int totalTablets = (int)Math.Round(currentStock * conversion, MidpointRounding.AwayFromZero);
                            int strips = totalTablets / conversion;
                            int tablets = totalTablets % conversion;
                            qtyDisplay = $"{strips}:{tablets}";
                        }
                        else
                        {
                            qtyDisplay = currentStock.ToString("0.##");
                        }

                        return new PurchaseItemVM
                        {
                            CompanyName = item.Company.Name,
                            ItemName = item.Name,
                            Packing = item.Packing,
                            Unit = item.Unit1,
                            AvailableQty = (int?)currentStock,
                            CurrentQtyDisplay = qtyDisplay,
                            AvgRate = Math.Round(avgRate, 2),
                            PurchaseAmount = Math.Round(currentStock * avgRate, 2)
                        };
                    })
                    .Where(x => x.AvailableQty > 0)
                    .OrderBy(x => x.CompanyName)
                    .ThenBy(x => x.ItemName)
                    .ToList();

                ViewBag.TotalQty = summaryList.Sum(x => x.AvailableQty);
                ViewBag.TotalAmount = summaryList.Sum(x => x.PurchaseAmount);
                ViewBag.IsTabletWise = isTabletWise;

                return View(summaryList);
            }
        }
        //EXPORT TO EXCEL COMPANY WISE STOCK
        [HttpGet]
        public async Task<IActionResult> CompanyWiseStockExcel(string fromDate, string toDate, int? companyId, string reportType)
        {
            reportType = reportType ?? "Details";

            DateTime startDate, endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", null, DateTimeStyles.None, out startDate))
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", null, DateTimeStyles.None, out endDate))
                endDate = DateTime.Today;

            endDate = endDate.AddDays(1).AddTicks(-1);

            var itemMasters = await _itemmasterservice.GetAll();
            var itemDict = itemMasters.ToDictionary(x => x.Id);

            var currentStocks = await _currenstockService.GetAll();
            var allPurchases = await _purchaseservice.GetAll();

            var filteredItemIds = itemMasters.Select(i => i.Id).ToHashSet();
            if (companyId.HasValue && companyId > 0)
                filteredItemIds = filteredItemIds.Where(id => itemDict[id].CompanyId == companyId).ToHashSet();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Company Wise Stock");

            int row = 1;

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _tenantService.GetById(user?.TenantId);
            string companyName = tenant?.Name ?? "Company Name";

            // ===== HEADER (CENTER) =====
            ws.Cells[row, 1].Value = companyName;
            ws.Cells[row, 1, row, 8].Merge = true;
            ws.Cells[row, 1, row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[row, 1, row, 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells[row, 1, row, 8].Style.Font.Bold = true;
            row++;

            ws.Cells[row, 1].Value = "Company Wise Stock Report";
            ws.Cells[row, 1, row, 8].Merge = true;
            ws.Cells[row, 1, row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[row, 1, row, 8].Style.Font.Bold = true;
            row++;

            ws.Cells[row, 1].Value = $"From {startDate:dd-MM-yyyy} To {endDate:dd-MM-yyyy}";
            ws.Cells[row, 1, row, 8].Merge = true;
            ws.Cells[row, 1, row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            row++;

            // ================= DETAILS =================
            if (reportType == "Details")
            {
                string[] headers = { "S.No", "Date", "Bill No", "Item", "Batch", "Qty", "Rate", "Amount" };

                for (int i = 0; i < headers.Length; i++)
                    ws.Cells[row, i + 1].Value = headers[i];

                ws.Cells[row, 1, row, 8].Style.Font.Bold = true;
                row++;

                var purchaseBatchInfo = allPurchases
                    .Where(p => p.PurchaseItems != null)
                    .SelectMany(p => p.PurchaseItems.Select(pi => new
                    {
                        pi.ItemId,
                        Batch = pi.Batch ?? "",
                        p.BillNo,
                        p.BillDate
                    }))
                    .GroupBy(x => $"{x.ItemId}_{x.Batch}")
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.BillDate).First());

                var batchStock = currentStocks
                    .Where(x => filteredItemIds.Contains(x.ItemId))
                    .GroupBy(x => new { x.ItemId, x.Batch });

                int sr = 1;
                decimal total = 0;

                foreach (var g in batchStock)
                {
                    decimal qty = g.Sum(x => x.Qty);
                    if (qty <= 0) continue;

                    decimal avg = qty > 0 ? g.Sum(x => x.Qty * x.PurchaseRate) / qty : 0;

                    var item = itemDict[g.Key.ItemId];
                    var key = $"{g.Key.ItemId}_{g.Key.Batch}";
                    var p = purchaseBatchInfo.ContainsKey(key) ? purchaseBatchInfo[key] : null;

                    ws.Cells[row, 1].Value = sr++;
                    ws.Cells[row, 2].Value = p?.BillDate?.ToString("dd-MM-yyyy") ?? "-";
                    ws.Cells[row, 3].Value = p?.BillNo ?? "-";
                    ws.Cells[row, 4].Value = item.Name;
                    ws.Cells[row, 5].Value = g.Key.Batch;
                    ws.Cells[row, 6].Value = qty;
                    ws.Cells[row, 7].Value = avg;
                    ws.Cells[row, 8].Value = qty * avg;

                    total += qty * avg;
                    row++;
                }

                ws.Cells[row, 1].Value = "TOTAL";
                ws.Cells[row, 8].Value = total;
            }

            // ================= SUMMARY =================
            else
            {
                string[] headers = { "S.No", "Company", "Item", "Packing", "Unit", "Qty", "Avg Rate", "Amount" };

                for (int i = 0; i < headers.Length; i++)
                    ws.Cells[row, i + 1].Value = headers[i];

                ws.Cells[row, 1, row, 8].Style.Font.Bold = true;
                row++;

                var summary = currentStocks
                    .Where(x => filteredItemIds.Contains(x.ItemId))
                    .GroupBy(x => x.ItemId);

                int sr = 1;
                decimal totalAmount = 0;

                foreach (var g in summary)
                {
                    var item = itemDict[g.Key];

                    decimal qty = g.Sum(x => x.Qty);
                    decimal avg = qty > 0 ? g.Sum(x => x.Qty * x.PurchaseRate) / qty : 0;

                    ws.Cells[row, 1].Value = sr++;
                    ws.Cells[row, 2].Value = item.Company?.Name;
                    ws.Cells[row, 3].Value = item.Name;
                    ws.Cells[row, 4].Value = item.Packing;
                    ws.Cells[row, 5].Value = item.Unit1;
                    ws.Cells[row, 6].Value = qty;
                    ws.Cells[row, 7].Value = avg;
                    ws.Cells[row, 8].Value = qty * avg;

                    totalAmount += qty * avg;
                    row++;
                }

                ws.Cells[row, 1].Value = "TOTAL";
                ws.Cells[row, 8].Value = totalAmount;
            }

            ws.Cells.AutoFitColumns();

            return File(package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "CompanyWiseStock.xlsx");
        }

        //EXPORT TO PDF COMPANY WISE STOCK 
        [HttpGet]
        public async Task<IActionResult> CompanyWiseStockPdf(string fromDate, string toDate, int? companyId, string reportType)
        {
            reportType = reportType ?? "Details";

            DateTime startDate, endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", null, DateTimeStyles.None, out startDate))
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", null, DateTimeStyles.None, out endDate))
                endDate = DateTime.Today;

            endDate = endDate.AddDays(1).AddTicks(-1);

            var itemMasters = await _itemmasterservice.GetAll();
            var itemDict = itemMasters.ToDictionary(x => x.Id);

            var currentStocks = await _currenstockService.GetAll();
            var allPurchases = await _purchaseservice.GetAll();

            var filteredItemIds = itemMasters.Select(i => i.Id).ToHashSet();
            if (companyId.HasValue && companyId > 0)
                filteredItemIds = filteredItemIds.Where(id => itemDict[id].CompanyId == companyId).ToHashSet();

            using var ms = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(ms));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _tenantService.GetById(user?.TenantId);
            string companyName = tenant?.Name ?? "Company Name";

            doc.Add(new Paragraph(companyName).SetFont(bold).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph("Company Wise Stock Report").SetFont(bold).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph($"From {startDate:dd-MM-yyyy} To {endDate:dd-MM-yyyy}")
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph(" "));

            if (reportType == "Details")
            {
                Table table = new Table(8).UseAllAvailableWidth();

                string[] headers = { "S.No", "Date", "Bill No", "Item", "Batch", "Qty", "Rate", "Amount" };

                foreach (var h in headers)
                    table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold)));

                var batchStock = currentStocks
                    .Where(x => filteredItemIds.Contains(x.ItemId))
                    .GroupBy(x => new { x.ItemId, x.Batch });

                var purchaseBatchInfo = allPurchases
                    .Where(p => p.PurchaseItems != null)
                    .SelectMany(p => p.PurchaseItems.Select(pi => new
                    {
                        pi.ItemId,
                        Batch = pi.Batch ?? "",
                        p.BillNo,
                        p.BillDate
                    }))
                    .GroupBy(x => $"{x.ItemId}_{x.Batch}")
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.BillDate).First());

                int sr = 1;
                decimal total = 0;

                foreach (var g in batchStock)
                {
                    decimal qty = g.Sum(x => x.Qty);
                    if (qty <= 0) continue;

                    decimal avg = qty > 0 ? g.Sum(x => x.Qty * x.PurchaseRate) / qty : 0;

                    var item = itemDict[g.Key.ItemId];
                    var key = $"{g.Key.ItemId}_{g.Key.Batch}";
                    var p = purchaseBatchInfo.ContainsKey(key) ? purchaseBatchInfo[key] : null;

                    table.AddCell(sr++.ToString());
                    table.AddCell(p?.BillDate?.ToString("dd-MM-yyyy") ?? "-");
                    table.AddCell(p?.BillNo ?? "-");
                    table.AddCell(item.Name);
                    table.AddCell(g.Key.Batch);
                    table.AddCell(qty.ToString("0.##"));
                    table.AddCell(avg.ToString("0.00"));
                    table.AddCell((qty * avg).ToString("0.00"));

                    total += qty * avg;
                }

                table.AddCell(new Cell(1, 7).Add(new Paragraph("TOTAL").SetFont(bold)));
                table.AddCell(total.ToString("0.00"));

                doc.Add(table);
            }

            else
            {
                Table table = new Table(8).UseAllAvailableWidth();

                string[] headers = { "S.No", "Company", "Item", "Packing", "Unit", "Qty", "Avg Rate", "Amount" };

                foreach (var h in headers)
                    table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold)));

                var summary = currentStocks
                    .Where(x => filteredItemIds.Contains(x.ItemId))
                    .GroupBy(x => x.ItemId);

                int sr = 1;
                decimal total = 0;

                foreach (var g in summary)
                {
                    var item = itemDict[g.Key];

                    decimal qty = g.Sum(x => x.Qty);
                    decimal avg = qty > 0 ? g.Sum(x => x.Qty * x.PurchaseRate) / qty : 0;

                    table.AddCell(sr++.ToString());
                    table.AddCell(item.Company?.Name ?? "-");
                    table.AddCell(item.Name ?? "-");
                    table.AddCell(item.Packing ?? "-");
                    table.AddCell(item.Unit1 ?? "-");
                    table.AddCell(qty.ToString("0.##"));
                    table.AddCell(avg.ToString("0.00"));
                    table.AddCell((qty * avg).ToString("0.00"));

                    total += qty * avg;
                }

                table.AddCell(new Cell(1, 7).Add(new Paragraph("TOTAL").SetFont(bold)));
                table.AddCell(total.ToString("0.00"));

                doc.Add(table);
            }

            doc.Close();
            return File(ms.ToArray(), "application/pdf", "CompanyWiseStock.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> ItemWisePurchaseReport(string fromDate, string toDate, int? supplierId)
        {
            DateTime startDate;
            DateTime endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out startDate))
            {
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Today;
            }

            endDate = endDate.AddDays(1).AddTicks(-1);

            ViewBag.FromDate = startDate.ToString("dd-MM-yyyy");
            ViewBag.ToDate = endDate.ToString("dd-MM-yyyy");
            ViewBag.SelectedSupplierId = supplierId;

            // ✅ Supplier dropdown (optional)
            ViewBag.Suppliers = (await _supplierservice.GetALL())
                .Select(s => new { s.Id, Text = s.FirstName })
                .ToList();

            var purchasesQuery = (await _purchaseservice.GetAll())
                .Where(p => p.BillDate >= startDate && p.BillDate <= endDate);

            if (supplierId.HasValue && supplierId.Value > 0)
                purchasesQuery = purchasesQuery.Where(p => p.SupplierId == supplierId.Value);

            var purchases = purchasesQuery.ToList();

            var purchaseItems = purchases
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems.Select(pi => new
                {
                    Item = pi,
                    SupplierId = p.SupplierId,
                    SupplierName = (p.Suppliers != null ? p.Suppliers.FirstName : "-")
                }))
                .ToList();

            var report = purchaseItems
                .Where(x => x.Item.ItemMasters != null)
                .GroupBy(x => new { x.Item.ItemId, ItemName = x.Item.ItemMasters.Name })
                .Select(g =>
                {
                    decimal qty = g.Sum(x => (decimal)x.Item.Qty);         // ✅ only Qty
                    decimal free = g.Sum(x => (decimal)x.Item.FreeQty);    // ✅ Free separate

                    decimal totalAmount = g.Sum(x => (decimal)x.Item.TotalAmt); // ✅ saved amount
                    decimal rate = g.Any() ? (decimal)g.First().Item.Rate : 0;  // ✅ no calculation

                    return new PurchaseItemVM
                    {
                        ItemName = g.Key.ItemName,
                        Packing = g.First().Item.ItemMasters?.Packing ?? "-",
                        Qty = qty,
                        FreeQty = free,
                        Rate = rate,
                        Amount = totalAmount
                    };
                })
                .ToList();

            ViewBag.TotalQty = report.Sum(x => x.Qty);
            ViewBag.TotalFree = report.Sum(x => x.FreeQty);
            ViewBag.TotalAmount = report.Sum(x => x.Amount);

            // ✅ Total Rate remove (do not send)
            return View(report);
        }


        //Export TO Excel
        [HttpGet]
        public async Task<IActionResult> ExportItemWisePurchaseExcel(string fromDate, string toDate, int? supplierId)
        {
            DateTime startDate;
            DateTime endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
            {
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Today;
            }

            endDate = endDate.AddDays(1).AddTicks(-1);

            var purchases = (await _purchaseservice.GetAll())
                .Where(p => p.BillDate >= startDate && p.BillDate <= endDate)
                .ToList();

            if (supplierId.HasValue && supplierId > 0)
                purchases = purchases.Where(x => x.SupplierId == supplierId).ToList();

            var items = purchases
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems.Select((pi, index) => new
                {
                    pi,
                    SupplierName = p.Suppliers?.FirstName ?? "-"
                }))
                .ToList();

            var report = items
                .Where(x => x.pi.ItemMasters != null)
                .GroupBy(x => new { x.pi.ItemId, x.pi.ItemMasters.Name })
                .Select((g, index) => new
                {
                    SNo = index + 1,
                    ItemName = g.Key.Name,
                    Packing = g.First().pi.ItemMasters?.Packing ?? "-",
                    Qty = g.Sum(x => x.pi.Qty),
                    Free = g.Sum(x => x.pi.FreeQty),
                    Rate = g.First().pi.Rate,
                    Amount = g.Sum(x => x.pi.TotalAmt)
                }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Item Wise Purchase");

            var user = await _userManager.GetUserAsync(User);
            string companyName = (await _tenantService.GetById(user.TenantId))?.Name ?? "Company";
            // ================= COMPANY =================
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 7).Merge();
            ws.Range(1, 1, 1, 7).Style.Font.SetBold().Font.SetFontSize(14);
            ws.Range(1, 1, 1, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // ================= TITLE =================
            ws.Cell(2, 1).Value = "Item Wise Purchase Report";
            ws.Range(2, 1, 2, 7).Merge();   // ✅ FIXED ROW
            ws.Range(2, 1, 2, 7).Style.Font.SetBold().Font.SetFontSize(12);
            ws.Range(2, 1, 2, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // ================= DATE =================
            ws.Cell(3, 1).Value = $"From {startDate:dd-MM-yyyy} To {endDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 7).Merge();   // ✅ FIXED ROW
            ws.Range(3, 1, 3, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            int row = 5;

            string[] headers = { "S.No", "Item Name", "Packing", "Qty", "Free", "Rate", "Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 7).Style.Font.SetBold();
            row++;

            foreach (var i in report)
            {
                ws.Cell(row, 1).Value = i.SNo;
                ws.Cell(row, 2).Value = i.ItemName;
                ws.Cell(row, 3).Value = i.Packing;
                ws.Cell(row, 4).Value = i.Qty;
                ws.Cell(row, 5).Value = i.Free;
                ws.Cell(row, 6).Value = i.Rate;
                ws.Cell(row, 7).Value = i.Amount;
                row++;
            }

            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 4).Value = report.Sum(x => x.Qty);
            ws.Cell(row, 5).Value = report.Sum(x => x.Free);
            ws.Cell(row, 7).Value = report.Sum(x => x.Amount);

            ws.Range(row, 1, row, 7).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ItemWisePurchase.xlsx");
        }
        //Export To Pdf
        [HttpGet]
        public async Task<IActionResult> ExportItemWisePurchasePdf(string fromDate, string toDate, int? supplierId)
        {
            DateTime startDate;
            DateTime endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
            {
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Today;
            }

            endDate = endDate.AddDays(1).AddTicks(-1);

            var purchases = (await _purchaseservice.GetAll())
                .Where(p => p.BillDate >= startDate && p.BillDate <= endDate)
                .ToList();

            if (supplierId.HasValue && supplierId > 0)
                purchases = purchases.Where(x => x.SupplierId == supplierId).ToList();

            var items = purchases
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems)
                .ToList();

            var report = items
                .Where(x => x.ItemMasters != null)
                .GroupBy(x => new { x.ItemId, x.ItemMasters.Name })
                .Select((g, index) => new
                {
                    SNo = index + 1,
                    ItemName = g.Key.Name,
                    Packing = g.First().ItemMasters?.Packing ?? "-",
                    Qty = g.Sum(x => x.Qty),
                    Free = g.Sum(x => x.FreeQty),
                    Rate = g.First().Rate,
                    Amount = g.Sum(x => x.TotalAmt)
                }).ToList();

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _userManager.GetUserAsync(User);
            string companyName = (await _tenantService.GetById(user.TenantId))?.Name ?? "Company";

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // COMPANY
            document.Add(new Paragraph(companyName)
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER));

            // TITLE
            document.Add(new Paragraph("Item Wise Purchase Report")
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER));

            // DATE
            document.Add(new Paragraph($"From {startDate:dd-MM-yyyy} To {endDate:dd-MM-yyyy}")
                .SetFont(normal)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(5));

            Table table = new Table(new float[] { 2, 6, 4, 2, 2, 2, 3 }).UseAllAvailableWidth();

            string[] headers = { "S.No", "Item Name", "Packing", "Qty", "Free", "Rate", "Amount" };

            foreach (var h in headers)
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(9)));

            foreach (var i in report)
            {
                table.AddCell(new Paragraph(i.SNo.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(i.ItemName).SetFontSize(9));
                table.AddCell(new Paragraph(i.Packing).SetFontSize(9));
                table.AddCell(new Paragraph(i.Qty.ToString()).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(i.Free.ToString()).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(i.Rate.ToString("0.00")).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(i.Amount.ToString("0.00")).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 3).Add(new Paragraph("TOTAL").SetFont(bold)).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph(report.Sum(x => x.Qty).ToString()).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph(report.Sum(x => x.Free).ToString()).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph(" "));
            table.AddCell(new Paragraph(report.Sum(x => x.Amount).ToString("0.00")).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "ItemWisePurchase.pdf");
        }


        //[HttpGet]
        //public async Task<IActionResult> OverStockReport(string fromDate, string toDate)
        //{
        //    DateTime startDate;
        //    DateTime endDate;

        //    if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy",
        //        System.Globalization.CultureInfo.InvariantCulture,
        //        System.Globalization.DateTimeStyles.None, out startDate))
        //        startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        //    if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
        //        System.Globalization.CultureInfo.InvariantCulture,
        //        System.Globalization.DateTimeStyles.None, out endDate))
        //        endDate = DateTime.Today;

        //    endDate = endDate.AddDays(1).AddTicks(-1);

        //    ViewBag.FromDate = startDate.ToString("dd-MM-yyyy");
        //    ViewBag.ToDate = endDate.ToString("dd-MM-yyyy");

        //    var items = await _itemmasterservice.GetAll();
        //    var purchases = (await _purchaseservice.GetAll())
        //        .Where(p => p.BillDate <= endDate && p.PurchaseItems != null)
        //        .ToList();

        //    var report = items.Select(item =>
        //    {
        //        var purchaseQty = purchases
        //            .SelectMany(p => p.PurchaseItems)
        //            .Where(x => x.ItemId == item.Id)
        //            .Sum(x => x.Qty + x.FreeQty);

        //        var availableQty = purchaseQty;

        //        var totalAmount = purchases
        //            .SelectMany(p => p.PurchaseItems)
        //            .Where(x => x.ItemId == item.Id)
        //            .Sum(x => (x.Qty + x.FreeQty) *
        //                     (x.Rate != 0 ? x.Rate :
        //                      (x.salserateA != 0 ? x.salserateB : x.Mrp)));

        //        var avgRate = purchaseQty != 0 ? totalAmount / purchaseQty : 0;

        //        var extraQty = availableQty > item.MaximumQty ? availableQty - item.MaximumQty : 0;
        //        var extraAmount = extraQty > 0 ? extraQty * avgRate : 0;


        //        return new PurchaseItemVM
        //        {
        //            ItemName = item.Name,
        //            AvgSales = purchaseQty,
        //            MaximumQty = item.MaximumQty,
        //            Qty = (int)availableQty,
        //            AvgRate = Math.Round(avgRate, 2),
        //            AvgAmount = extraAmount
        //        };
        //    })
        //    .OrderBy(x => x.ItemName)
        //    .ToList();


        //    var overStockedItems = report.Where(r => r.Qty > r.MaximumQty).ToList();

        //    ViewBag.OverStockedItems = overStockedItems;

        //    return View(report);
        //}

        [HttpGet]
        public async Task<IActionResult> OverStockReport(string toDate)
        {
            DateTime endDate;

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Today;
            }

            ViewBag.AsOnDate = endDate.ToString("dd-MM-yyyy");

            // ✅ 1. Item Masters
            var itemMasters = await _itemmasterservice.GetAll();

            // ✅ 2. CurrentStock Service - NO DATE FILTER (Always latest)
            var currentStocks = await _currenstockService.GetAll();

            // ✅ 3. Group by ItemId
            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => new
                {
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Qty * x.PurchaseRate),
                    AvgRate = g.Sum(x => x.Qty) > 0
                        ? g.Sum(x => x.Qty * x.PurchaseRate) / g.Sum(x => x.Qty)
                        : 0
                });

            // ✅ 4. TabletWise Setting
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            // ✅ 5. Build Report
            var report = itemMasters.Select(item =>
            {
                var id = item.Id;

                decimal currentStock = stockDict.ContainsKey(id) ? stockDict[id].TotalQty : 0;
                decimal avgRate = stockDict.ContainsKey(id) ? stockDict[id].AvgRate : 0;

                decimal maxQty = item.MaximumQty;
                decimal extraQty = currentStock > maxQty ? currentStock - maxQty : 0;
                decimal extraAmount = extraQty * avgRate;

                int conversion = item.Conversion > 0 ? item.Conversion : 1;
                string qtyDisplay;
                string excessQtyDisplay;

                if (isTabletWise && conversion > 0)
                {
                    int totalTablets = (int)Math.Round(currentStock * conversion, MidpointRounding.AwayFromZero);
                    int strips = totalTablets / conversion;
                    int tablets = totalTablets % conversion;
                    qtyDisplay = $"{strips}:{tablets}";

                    int excessTablets = (int)Math.Round(extraQty * conversion, MidpointRounding.AwayFromZero);
                    int excessStrips = excessTablets / conversion;
                    int excessTab = excessTablets % conversion;
                    excessQtyDisplay = $"{excessStrips}:{excessTab}";
                }
                else
                {
                    qtyDisplay = currentStock.ToString("0.##");
                    excessQtyDisplay = extraQty.ToString("0.##");
                }

                return new PurchaseItemVM
                {
                    ItemName = item.Name,
                    Qty = currentStock,
                    CurrentQtyDisplay = qtyDisplay,
                    ExcessQtyDisplay = excessQtyDisplay,
                    MaximumQty = (int)maxQty,
                    AvgRate = Math.Round(avgRate, 2),
                    AvgSales = extraQty,
                    AvgAmount = Math.Round(extraAmount, 2)
                };
            })
            .Where(x => x.Qty > x.MaximumQty && x.MaximumQty > 0 && x.Qty > 0)
            .OrderBy(x => x.ItemName)
            .ToList();

            return View(report);
        }
        //Export to excel
        [HttpGet]
        public async Task<IActionResult> ExportOverStockReportExcel(string toDate)
        {
            DateTime endDate;

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Today;
            }

            ViewBag.AsOnDate = endDate.ToString("dd-MM-yyyy");

            // ✅ 1. Item Masters
            var itemMasters = await _itemmasterservice.GetAll();

            // ✅ 2. CurrentStock Service - NO DATE FILTER (Always latest)
            var currentStocks = await _currenstockService.GetAll();

            // ✅ 3. Group by ItemId
            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => new
                {
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Qty * x.PurchaseRate),
                    AvgRate = g.Sum(x => x.Qty) > 0
                        ? g.Sum(x => x.Qty * x.PurchaseRate) / g.Sum(x => x.Qty)
                        : 0
                });

            // ✅ 4. TabletWise Setting
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            // ✅ 5. Build Report
            var report = itemMasters.Select(item =>
            {
                var id = item.Id;

                decimal currentStock = stockDict.ContainsKey(id) ? stockDict[id].TotalQty : 0;
                decimal avgRate = stockDict.ContainsKey(id) ? stockDict[id].AvgRate : 0;

                decimal maxQty = item.MaximumQty;
                decimal extraQty = currentStock > maxQty ? currentStock - maxQty : 0;
                decimal extraAmount = extraQty * avgRate;

                int conversion = item.Conversion > 0 ? item.Conversion : 1;
                string qtyDisplay;
                string excessQtyDisplay;

                if (isTabletWise && conversion > 0)
                {
                    int totalTablets = (int)Math.Round(currentStock * conversion, MidpointRounding.AwayFromZero);
                    int strips = totalTablets / conversion;
                    int tablets = totalTablets % conversion;
                    qtyDisplay = $"{strips}:{tablets}";

                    int excessTablets = (int)Math.Round(extraQty * conversion, MidpointRounding.AwayFromZero);
                    int excessStrips = excessTablets / conversion;
                    int excessTab = excessTablets % conversion;
                    excessQtyDisplay = $"{excessStrips}:{excessTab}";
                }
                else
                {
                    qtyDisplay = currentStock.ToString("0.##");
                    excessQtyDisplay = extraQty.ToString("0.##");
                }

                return new PurchaseItemVM
                {
                    ItemName = item.Name,
                    Qty = currentStock,
                    CurrentQtyDisplay = qtyDisplay,
                    ExcessQtyDisplay = excessQtyDisplay,
                    MaximumQty = (int)maxQty,
                    AvgRate = Math.Round(avgRate, 2),
                    AvgSales = extraQty,
                    AvgAmount = Math.Round(extraAmount, 2)
                };
            })
            .Where(x => x.Qty > x.MaximumQty && x.MaximumQty > 0 && x.Qty > 0)
            .OrderBy(x => x.ItemName)
            .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Over Stock Report");
            // ✅ COMPANY NAME
            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null && user.TenantId != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            // Row Start
            int row = 1;

            // COMPANY NAME
            ws.Cell(row, 1).Value = companyName;
            ws.Range(row, 1, row, 7).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            row++;

            // REPORT TITLE
            ws.Cell(row, 1).Value = "Over Stock Report";
            ws.Range(row, 1, row, 7).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            row++;

            // DATE
            ws.Cell(row, 1).Value = $"As On : {endDate:dd-MM-yyyy}";
            ws.Range(row, 1, row, 7).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            row += 2; // spacing

            ws.Cell(row, 1).Value = "S.No";
            ws.Cell(row, 2).Value = "Item Name";
            ws.Cell(row, 3).Value = "Maximum Qty";
            ws.Cell(row, 4).Value = "Available Qty";
            ws.Cell(row, 5).Value = "Avg Rate";
            ws.Cell(row, 6).Value = "Excess Stock";
            ws.Cell(row, 7).Value = "Extra Amount";

            ws.Range(row, 1, row, 7).Style.Font.SetBold();
            row++;

            int sno = 1;

            foreach (var i in report)
            {
                ws.Cell(row, 1).Value = sno++;
                ws.Cell(row, 2).Value = i.ItemName;
                ws.Cell(row, 3).Value = i.MaximumQty;
                ws.Cell(row, 4).Value = i.CurrentQtyDisplay;
                ws.Cell(row, 5).Value = i.AvgRate;
                ws.Cell(row, 6).Value = i.ExcessQtyDisplay;
                ws.Cell(row, 7).Value = i.AvgAmount;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "OverStockReport.xlsx");
        }

        //EXPORT TO PDF
        [HttpGet]
        public async Task<IActionResult> ExportOverStockReportPdf(string toDate)
        {
            DateTime endDate;

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Today;
            }

            ViewBag.AsOnDate = endDate.ToString("dd-MM-yyyy");

            // ✅ 1. Item Masters
            var itemMasters = await _itemmasterservice.GetAll();

            // ✅ 2. CurrentStock Service - NO DATE FILTER (Always latest)
            var currentStocks = await _currenstockService.GetAll();

            // ✅ 3. Group by ItemId
            var stockDict = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => new
                {
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Qty * x.PurchaseRate),
                    AvgRate = g.Sum(x => x.Qty) > 0
                        ? g.Sum(x => x.Qty * x.PurchaseRate) / g.Sum(x => x.Qty)
                        : 0
                });

            // ✅ 4. TabletWise Setting
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            // ✅ 5. Build Report
            var report = itemMasters.Select(item =>
            {
                var id = item.Id;

                decimal currentStock = stockDict.ContainsKey(id) ? stockDict[id].TotalQty : 0;
                decimal avgRate = stockDict.ContainsKey(id) ? stockDict[id].AvgRate : 0;

                decimal maxQty = item.MaximumQty;
                decimal extraQty = currentStock > maxQty ? currentStock - maxQty : 0;
                decimal extraAmount = extraQty * avgRate;

                int conversion = item.Conversion > 0 ? item.Conversion : 1;
                string qtyDisplay;
                string excessQtyDisplay;

                if (isTabletWise && conversion > 0)
                {
                    int totalTablets = (int)Math.Round(currentStock * conversion, MidpointRounding.AwayFromZero);
                    int strips = totalTablets / conversion;
                    int tablets = totalTablets % conversion;
                    qtyDisplay = $"{strips}:{tablets}";

                    int excessTablets = (int)Math.Round(extraQty * conversion, MidpointRounding.AwayFromZero);
                    int excessStrips = excessTablets / conversion;
                    int excessTab = excessTablets % conversion;
                    excessQtyDisplay = $"{excessStrips}:{excessTab}";
                }
                else
                {
                    qtyDisplay = currentStock.ToString("0.##");
                    excessQtyDisplay = extraQty.ToString("0.##");
                }

                return new PurchaseItemVM
                {
                    ItemName = item.Name,
                    Qty = currentStock,
                    CurrentQtyDisplay = qtyDisplay,
                    ExcessQtyDisplay = excessQtyDisplay,
                    MaximumQty = (int)maxQty,
                    AvgRate = Math.Round(avgRate, 2),
                    AvgSales = extraQty,
                    AvgAmount = Math.Round(extraAmount, 2)
                };
            })
            .Where(x => x.Qty > x.MaximumQty && x.MaximumQty > 0 && x.Qty > 0)
            .OrderBy(x => x.ItemName)
            .ToList();
            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null && user.TenantId != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // ✅ COMPANY NAME
            doc.Add(new Paragraph(companyName)
                .SetFont(bold)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            // ✅ REPORT TITLE
            doc.Add(new Paragraph("Over Stock Report")
                .SetFont(bold)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            // ✅ DATE
            doc.Add(new Paragraph($"As On : {endDate:dd-MM-yyyy}")
                .SetFont(normal)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));
           

            Table table = new Table(new float[] { 1, 4, 2, 3, 2, 2, 3 }).UseAllAvailableWidth();

            string[] headers =
            {
        "S.No","Item Name","Maximum Qty","Available Qty","Avg Rate","Excess Stock","Extra Amount"
    };

            foreach (var h in headers)
            {
                table.AddHeaderCell(
                    new Cell().Add(new Paragraph(h)
                    .SetFont(bold)
                    .SetFontSize(9))
                );
            }

            int sno = 1;
            foreach (var i in report)
            {
                table.AddCell(new Paragraph(sno++.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(i.ItemName).SetFontSize(9));
                table.AddCell(new Paragraph(i.MaximumQty.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(i.CurrentQtyDisplay).SetFontSize(9));
                table.AddCell(new Paragraph(i.AvgRate.ToString("0.00")).SetFontSize(9));
                table.AddCell(new Paragraph(i.ExcessQtyDisplay).SetFontSize(9));
                table.AddCell(new Paragraph(i.AvgAmount.ToString("0.00")).SetFontSize(9));
            }
            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "OverStockReport.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> ItemWisePurchaseDetailsReport(string fromDate, string toDate)
        {
            // 1. Date Logic
            DateTime startDate = string.IsNullOrEmpty(fromDate) ? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) : DateTime.ParseExact(fromDate, "dd-MM-yyyy", null);
            DateTime endDate = string.IsNullOrEmpty(toDate) ? DateTime.Today : DateTime.ParseExact(toDate, "dd-MM-yyyy", null);
            endDate = endDate.AddDays(1).AddTicks(-1);

            ViewBag.FromDate = startDate.ToString("dd-MM-yyyy");
            ViewBag.ToDate = endDate.ToString("dd-MM-yyyy");

            // 2. Data Fetching
            var purchases = await _purchaseservice.GetAll();

            // Filtering and Flattening
            var purchaseItemsList = purchases
                .Where(p => p.BillDate >= startDate && p.BillDate <= endDate && p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems)
                .ToList();

            // 3. Report Data Calculation
            var reportData = purchaseItemsList.Select(x =>
            {
                // Basic Calculation
                decimal qty = x.Qty;
                decimal freeQty = x.FreeQty;
                decimal rate = x.Rate;
                decimal basicAmount = qty * rate;
                decimal discountAmt = x.DiscountAmt;
                decimal taxableAmount = basicAmount - discountAmt;

                // GST Calculation - Sabse pehle CGST+SGST check karo, nahi toh GstAmount, nahi toh % se calculate karo
                decimal gstAmt = 0;
                if (x.CGstAmount > 0 || x.SGstAmount > 0)
                {
                    gstAmt = x.CGstAmount + x.SGstAmount;
                }
                else if (x.GstAmount > 0)
                {
                    gstAmt = x.GstAmount;
                }
                else if (x.Gst > 0)
                {
                    gstAmt = (taxableAmount * x.Gst) / 100;
                }

                // Cess Calculation - Percentage se calculate karo
                decimal cessPercent = x.Cess;
                decimal cessAmt = cessPercent > 0 ? (taxableAmount * cessPercent) / 100 : 0;

                // Combined Tax (GST + Cess)
                decimal combinedTax = gstAmt + cessAmt;

                // Final Amount = Taxable Amount + Combined Tax
                decimal finalAmount = taxableAmount + combinedTax;

                return new PurchaseItemVM
                {
                    BillNo = x.Purchases?.BillNo,
                    BillDate = x.Purchases?.BillDate,
                    SupplierName = x.Purchases?.Suppliers?.FirstName,
                    ItemName = x.ItemMasters?.Name,
                    Qty = (int)(qty),
                    Rate = rate,
                    FreeQty = (int)freeQty,
                    GstAmount = combinedTax,    // GST + Cess dono yahan add hokar jayenge
                    Amount = finalAmount        // Final Total Amount
                };
            }).ToList();

            // 4. Footer Totals for View
            ViewBag.TotalQty = reportData.Sum(x => x.Qty);
            ViewBag.TotalGst = reportData.Sum(x => x.GstAmount);
            ViewBag.TotalAmount = reportData.Sum(x => x.Amount);

            return View(reportData);
        }
        //EXPORT TO EXCEL
        [HttpGet]
        public async Task<IActionResult> ExportItemWisePurchaseDetailsExcel(string fromDate, string toDate)
        {
            DateTime startDate, endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                endDate = DateTime.Today;

            endDate = endDate.AddDays(1).AddTicks(-1);

            var purchases = (await _purchaseservice.GetAll())
                .Where(p => p.BillDate >= startDate && p.BillDate <= endDate)
                .ToList();

            var items = purchases
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems)
                .Where(x => x.ItemMasters != null)
                .ToList();

            var report = items.Select((x, index) =>
            {
                decimal qty = x.Qty;
                decimal free = x.FreeQty;
                decimal rate = x.Rate;

                decimal taxable = (qty * rate) - x.DiscountAmt;

                decimal gst = (x.CGstAmount + x.SGstAmount) > 0
                    ? x.CGstAmount + x.SGstAmount
                    : (x.GstAmount > 0 ? x.GstAmount : (taxable * x.Gst) / 100);

                decimal cess = x.Cess > 0 ? (taxable * x.Cess) / 100 : 0;

                decimal totalTax = gst + cess;
                decimal finalAmount = taxable + totalTax;

                return new
                {
                    SNo = index + 1,
                    BillNo = x.Purchases?.BillNo,
                    BillDate = x.Purchases?.BillDate,
                    Supplier = x.Purchases?.Suppliers?.FirstName,
                    Item = x.ItemMasters?.Name,
                    Qty = qty,
                    Free = free,
                    Rate = rate,
                    Gst = totalTax,
                    Amount = finalAmount
                };
            }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Item Wise Purchase Details");

            var user = await _userManager.GetUserAsync(User);
            string companyName = (await _tenantService.GetById(user.TenantId))?.Name ?? "Company";

            // COMPANY
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 10).Merge().Style.Font.SetBold().Font.SetFontSize(14);
            ws.Range(1, 1, 1, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TITLE
            ws.Cell(2, 1).Value = "Item Wise Purchase Details Report";
            ws.Range(2, 1, 2, 10).Merge().Style.Font.SetBold();
            ws.Range(2, 1, 2, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // DATE
            ws.Cell(3, 1).Value = $"From {startDate:dd-MM-yyyy} To {endDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 10).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            string[] headers = { "S.No", "Bill No", "Bill Date", "Supplier", "Item", "Qty", "Free", "Rate", "GST", "Total Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 10).Style.Font.SetBold();
            row++;

            foreach (var i in report)
            {
                ws.Cell(row, 1).Value = i.SNo;
                ws.Cell(row, 2).Value = i.BillNo;
                ws.Cell(row, 3).Value = i.BillDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 4).Value = i.Supplier;
                ws.Cell(row, 5).Value = i.Item;
                ws.Cell(row, 6).Value = i.Qty;
                ws.Cell(row, 7).Value = i.Free;
                ws.Cell(row, 8).Value = i.Rate;
                ws.Cell(row, 9).Value = i.Gst;
                ws.Cell(row, 10).Value = i.Amount;
                row++;
            }

            // TOTAL
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 6).Value = report.Sum(x => x.Qty);
            ws.Cell(row, 9).Value = report.Sum(x => x.Gst);
            ws.Cell(row, 10).Value = report.Sum(x => x.Amount);

            ws.Range(row, 1, row, 10).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ItemWisePurchaseDetails.xlsx");
        }
        //EXPORT TO PDF
        [HttpGet]
        public async Task<IActionResult> ExportItemWisePurchaseDetailsPdf(string fromDate, string toDate)
        {
            DateTime startDate, endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                endDate = DateTime.Today;

            endDate = endDate.AddDays(1).AddTicks(-1);

            var purchases = (await _purchaseservice.GetAll())
                .Where(p => p.BillDate >= startDate && p.BillDate <= endDate)
                .ToList();

            var items = purchases
                .Where(p => p.PurchaseItems != null)
                .SelectMany(p => p.PurchaseItems)
                .Where(x => x.ItemMasters != null)
                .ToList();

            var report = items.Select((x, index) =>
            {
                decimal qty = x.Qty;
                decimal free = x.FreeQty;
                decimal rate = x.Rate;

                decimal taxable = (qty * rate) - x.DiscountAmt;

                decimal gst = (x.CGstAmount + x.SGstAmount) > 0
                    ? x.CGstAmount + x.SGstAmount
                    : (x.GstAmount > 0 ? x.GstAmount : (taxable * x.Gst) / 100);

                decimal cess = x.Cess > 0 ? (taxable * x.Cess) / 100 : 0;

                decimal totalTax = gst + cess;
                decimal finalAmount = taxable + totalTax;

                return new
                {
                    SNo = index + 1,
                    BillNo = x.Purchases?.BillNo,
                    BillDate = x.Purchases?.BillDate,
                    Supplier = x.Purchases?.Suppliers?.FirstName,
                    Item = x.ItemMasters?.Name,
                    Qty = qty,
                    Free = free,
                    Rate = rate,
                    Gst = totalTax,
                    Amount = finalAmount
                };
            }).ToList();

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var user = await _userManager.GetUserAsync(User);
            string companyName = (await _tenantService.GetById(user.TenantId))?.Name ?? "Company";

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // COMPANY
            document.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            // TITLE
            document.Add(new Paragraph("Item Wise Purchase Details Report")
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            // DATE
            document.Add(new Paragraph($"From {startDate:dd-MM-yyyy} To {endDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(5));

            Table table = new Table(new float[] { 2, 3, 3, 5, 6, 2, 2, 2, 3, 3 }).UseAllAvailableWidth();

            string[] headers = { "S.No", "Bill No", "Bill Date", "Supplier", "Item", "Qty", "Free", "Rate", "GST", "Total Amount" };

            foreach (var h in headers)
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(9)));

            foreach (var i in report)
            {
                table.AddCell(new Paragraph(i.SNo.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(i.BillNo).SetFontSize(9));
                table.AddCell(new Paragraph(i.BillDate?.ToString("dd-MM-yyyy")).SetFontSize(9));
                table.AddCell(new Paragraph(i.Supplier).SetFontSize(9));
                table.AddCell(new Paragraph(i.Item).SetFontSize(9));
                table.AddCell(new Paragraph(i.Qty.ToString()).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(i.Free.ToString()).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(i.Rate.ToString("0.00")).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(i.Gst.ToString("0.00")).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(i.Amount.ToString("0.00")).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL
            // ✅ TOTAL ROW FIXED PROPERLY

            // TOTAL label (colspan 5)
            table.AddCell(new Cell(1, 5)
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(9))
                .SetTextAlignment(TextAlignment.RIGHT));

            // Qty (col 6)
            table.AddCell(new Paragraph(report.Sum(x => x.Qty).ToString())
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT));

            // Free (col 7) → blank or 0
            table.AddCell(new Paragraph("") // or "0"
                .SetFontSize(9));

            // Rate (col 8) → blank
            table.AddCell(new Paragraph("")
                .SetFontSize(9));

            // GST (col 9)
            table.AddCell(new Paragraph(report.Sum(x => x.Gst).ToString("0.00"))
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT));

            // Total Amount (col 10)
            table.AddCell(new Paragraph(report.Sum(x => x.Amount).ToString("0.00"))
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT));
            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "ItemWisePurchaseDetails.pdf");
        }


        //[HttpGet]
        //public async Task<IActionResult> BatchWiseStock(DateTime? fromDate, DateTime? toDate, int? companyId, int? supplierId, int? itemId, string stockType)
        //{
        //    fromDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        //    toDate ??= DateTime.Now;

        //    // Load item masters and filters
        //    var items = (await _itemmasterservice.GetAll()).Where(i => i.IsActive).ToList();
        //    if (companyId.HasValue)
        //        items = items.Where(i => i.CompanyId == companyId.Value).ToList();
        //    if (itemId.HasValue)
        //        items = items.Where(i => i.Id == itemId.Value).ToList();

        //    // Load all transactions up to today
        //    var today = DateTime.Today;
        //    var purchases = (await _purchaseservice.GetAll()).Where(x => x.BillDate <= today).ToList();
        //    var purchaseReturns = (await _purchasereturnservice.GetAll()).Where(x => x.BillDate <= today).ToList();
        //    var sales = (await _salesService.GetAll()).Where(x => x.BillDate <= today).ToList();
        //    var stockIssues = (await _stockissueservice.GetAll()).Where(x => x.ChallanDate <= today).ToList();
        //    var stockReturns = (await _stockrturnservice.GetAll()).Where(x => x.ChallanDate <= today).ToList();
        //    var stockReceives = (await _stockreceiveservice.GetAll()).Where(x => x.ChallanDate <= today).ToList();

        //    // Flatten items
        //    var purchaseItems = purchases.SelectMany(x => x.PurchaseItems ?? new List<PurchaseItem>());
        //    var purchaseReturnItems = purchaseReturns.SelectMany(x => x.PurchaseReturnItems ?? new List<PurchaseReturnItem>());
        //    var salesItems = sales.SelectMany(x => x.SalesItems ?? new List<SalesItem>());
        //    var stockIssueItems = stockIssues.SelectMany(x => x.StockIssuesItems ?? new List<StockIssueItem>());
        //    var stockReturnItems = stockReturns.SelectMany(x => x.StockReturnItems ?? new List<StockReturnItem>());
        //    var stockReceiveItems = stockReceives.SelectMany(x => x.StockReceiveItems ?? new List<StockReceiveItem>());
        //    var suppliers = await _supplierservice.GetALL();
        //    var companies = await _companyServices.GetAll();

        //    // Prepare dropdowns
        //    ViewBag.SupplierList = suppliers.Select(s => new SelectListItem
        //    {
        //        Value = s.Id.ToString(),
        //        Text = s.FirstName
        //    }).ToList();

        //    ViewBag.ItemList = items.Select(i => new SelectListItem
        //    {
        //        Value = i.Id.ToString(),
        //        Text = i.Name
        //    }).ToList();

        //    ViewBag.companydata = companies;
        //    ViewBag.SelectedCompanyId = companyId;
        //    ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
        //    ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
        //    ViewBag.StockType = stockType;

        //    // Filter by supplier if needed
        //    if (supplierId.HasValue)
        //        purchaseItems = purchaseItems.Where(x => x.Purchases?.SupplierId == supplierId.Value).ToList();
        //    var batchWiseReport = purchaseItems
        //        .GroupBy(x => new { x.ItemId, x.Batch })
        //        .Select(g =>
        //        {
        //            var itemId = g.Key.ItemId;
        //            var batch = g.Key.Batch;

        //            decimal totalPurchased = g.Sum(x => x.Qty + x.FreeQty);
        //            decimal totalPurchaseReturn = purchaseReturnItems
        //                .Where(x => x.ItemId == itemId && x.Batch == batch)
        //                .Sum(x => x.Qty + x.FreeQty);

        //            decimal totalSales = salesItems
        //                .Where(x => x.ItemMasterId == itemId && x.Batch == batch)
        //                .Sum(x => x.Qty);

        //            decimal totalIssue = stockIssueItems
        //                .Where(x => x.ItemMasterId == itemId && x.Batch == batch)
        //                .Sum(x => x.Qty);

        //            decimal totalReturn = stockReturnItems
        //                .Where(x => x.ItemMasterId == itemId && x.Batch == batch)
        //                .Sum(x => x.Qty);

        //            decimal totalReceive = stockReceiveItems
        //                .Where(x => x.ItemMasterId == itemId && x.Batch == batch)
        //                .Sum(x => x.Qty);

        //            decimal currentStock = totalPurchased - totalPurchaseReturn - totalSales - totalIssue + totalReturn + totalReceive;

        //            // Always get the item master for Packing/Unit
        //            var itemMaster = items.FirstOrDefault(i => i.Id == itemId);

        //            return new PurchaseItemVM
        //            {
        //                ItemName = itemMaster?.Name ?? "Unknown",
        //                Batch = batch,
        //                ExpiryDate = g.FirstOrDefault()?.ExpiryDate,
        //                Mrp = g.FirstOrDefault()?.Mrp ?? 0,       
        //                Qty = (int)currentStock,
        //                Packing = itemMaster?.Packing ?? "-",          
        //                Unit = itemMaster?.Unit1 ?? "-"           
        //            };
        //        })

        //        .OrderBy(x => x.ItemName)
        //        .ThenBy(x => x.Batch)
        //        .ToList();
        //    // Filter by stock type if needed
        //    if (!string.IsNullOrEmpty(stockType))
        //    {
        //        if (stockType == "Available")
        //            batchWiseReport = batchWiseReport.Where(x => x.Qty > 0).ToList();
        //        else if (stockType == "Whole")
        //            batchWiseReport = batchWiseReport.Where(x => x.Qty <= 0).ToList();
        //    }

        //    return View(batchWiseReport);
        //}
        [HttpGet]
        public async Task<IActionResult> BatchWiseStock(string fromDate, string toDate, int? companyId, int? supplierId, int? itemId, string stockType)
        {
            DateTime startDate;
            DateTime endDate;

            // ✅ Parse From Date
            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out startDate))
            {
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            // ✅ Parse To Date
            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Today;
            }

            endDate = endDate.AddDays(1).AddTicks(-1);

            // ✅ 1. Load Items - Dictionary for fast lookup
            var allItems = (await _itemmasterservice.GetAll()).Where(i => i.IsActive).ToList();
            var itemDict = allItems.ToDictionary(i => i.Id);

            // ✅ 2. Get Pre-calculated Stock from CurrentStock Service
            var currentStocks = await _currenstockService.GetAll();

            // ✅ 3. Filter Items
            var filteredItemIds = allItems.Select(i => i.Id).ToHashSet();

            if (companyId.HasValue)
                filteredItemIds = filteredItemIds.Where(id =>
                    itemDict.ContainsKey(id) && itemDict[id].CompanyId == companyId).ToHashSet();

            if (itemId.HasValue)
                filteredItemIds = filteredItemIds.Where(id => id == itemId.Value).ToHashSet();

            // ✅ 4. Supplier Filter (Lightweight - only for filtering, not calculation)
            if (supplierId.HasValue)
            {
                var supplierItemIds = (await _purchaseservice.GetAll())
                    .Where(p => p.SupplierId == supplierId.Value)
                    .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                    .Select(pi => pi.ItemId)
                    .Distinct()
                    .ToHashSet();

                filteredItemIds = filteredItemIds.Intersect(supplierItemIds).ToHashSet();
            }

            // ✅ 5. Group by ItemId + Batch (Single Pass - FAST)
            var batchStockDict = currentStocks
                .Where(x => filteredItemIds.Contains(x.ItemId))
                .GroupBy(x => new { x.ItemId, x.Batch })
                .ToDictionary(g => g.Key, g => new
                {
                    Qty = g.Sum(x => x.Qty),
                    ExpiryDate = g.FirstOrDefault()?.ExpiryDate,
                    Mrp = g.FirstOrDefault()?.Mrp ?? 0,
                    PurchaseRate = g.FirstOrDefault()?.PurchaseRate ?? 0,
                    SalesRateA = g.FirstOrDefault()?.SalesRateA ?? 0,
                    SalesRateB = g.FirstOrDefault()?.SalesRateB ?? 0,
                    Barcode = g.FirstOrDefault()?.Barcode
                });

            // ✅ 6. Item Dropdown - Items with transactions
            var itemsWithTransactions = currentStocks
                .Where(x => filteredItemIds.Contains(x.ItemId))
                .Select(x => x.ItemId)
                .Distinct()
                .Where(id => itemDict.ContainsKey(id))
                .Select(id => itemDict[id])
                .OrderBy(i => i.Name)
                .ToList();

            // ✅ 7. ViewBag Data
            var suppliers = await _supplierservice.GetALL();
            var companies = await _companyServices.GetAll();

            ViewBag.SupplierList = suppliers.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.FirstName
            }).ToList();

            ViewBag.companydata = companies;
            ViewBag.SelectedCompanyId = companyId;

            ViewBag.FromDate = startDate.ToString("dd-MM-yy");
            ViewBag.ToDate = endDate.ToString("dd-MM-yy");

            ViewBag.StockType = string.IsNullOrEmpty(stockType) ? "Whole" : stockType;

            ViewBag.ItemList = itemsWithTransactions.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name
            }).ToList();

            // ✅ 8. TabletWise Setting
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            // ✅ 9. Build Final Report (Using Pre-calculated Stock)
            var batchWiseReport = batchStockDict
                .Select(x =>
                {
                    var key = x.Key;

                    if (!itemDict.ContainsKey(key.ItemId))
                        return null;

                    var itemMaster = itemDict[key.ItemId];
                    decimal currentStock = x.Value.Qty;

                    // TabletWise Display Format
                    int conversion = itemMaster.Conversion > 0 ? itemMaster.Conversion : 1;
                    string qtyDisplay;

                    if (isTabletWise && conversion > 0)
                    {
                        int totalTablets = (int)Math.Round(currentStock * conversion, MidpointRounding.AwayFromZero);
                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;
                        qtyDisplay = $"{strips}:{tablets}";
                    }
                    else
                    {
                        qtyDisplay = currentStock.ToString("0.##");
                    }

                    return new PurchaseItemVM
                    {
                        ItemName = itemMaster.Name,
                        Batch = key.Batch,
                        ExpiryDate = x.Value.ExpiryDate,
                        Mrp = x.Value.Mrp,
                        Qty = currentStock,
                        CurrentQtyDisplay = qtyDisplay,
                        Packing = itemMaster.Packing ?? "-",
                        Unit = itemMaster.Unit1 ?? "-"
                    };
                })
                .Where(x => x != null)
                .ToList();

            // ✅ 10. Apply Stock Type Filter
            if (stockType == "Available")
                batchWiseReport = batchWiseReport.Where(x => x.Qty > 0).ToList();
            else if (stockType == "Zero")
                batchWiseReport = batchWiseReport.Where(x => x.Qty <= 0).ToList();

            // ✅ 11. Sort
            batchWiseReport = batchWiseReport
                .OrderBy(x => x.ItemName)
                .ThenBy(x => x.Batch)
                .ToList();

            ViewBag.IsTabletWise = isTabletWise;

            return View(batchWiseReport);
        }

        [HttpGet]
        public async Task<IActionResult> BatchWiseStockExcel(string fromDate, string toDate, int? companyId, int? supplierId, int? itemId, string stockType)
        {
            DateTime startDate, endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                endDate = DateTime.Today;

            endDate = endDate.AddDays(1).AddTicks(-1);

            // ✅ SAME LOGIC AS MAIN REPORT
            var allItems = (await _itemmasterservice.GetAll()).Where(i => i.IsActive).ToList();
            var itemDict = allItems.ToDictionary(i => i.Id);
            var currentStocks = await _currenstockService.GetAll();

            var filteredItemIds = allItems.Select(i => i.Id).ToHashSet();

            if (companyId.HasValue)
                filteredItemIds = filteredItemIds.Where(id => itemDict[id].CompanyId == companyId).ToHashSet();

            if (itemId.HasValue)
                filteredItemIds = filteredItemIds.Where(id => id == itemId).ToHashSet();

            if (supplierId.HasValue)
            {
                var supplierItemIds = (await _purchaseservice.GetAll())
                    .Where(p => p.SupplierId == supplierId)
                    .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                    .Select(x => x.ItemId)
                    .Distinct()
                    .ToHashSet();

                filteredItemIds = filteredItemIds.Intersect(supplierItemIds).ToHashSet();
            }

            var batchStock = currentStocks
                .Where(x => filteredItemIds.Contains(x.ItemId))
                .GroupBy(x => new { x.ItemId, x.Batch })
                .ToList();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            var report = batchStock.Select(g =>
            {
                var item = itemDict[g.Key.ItemId];
                decimal qty = g.Sum(x => x.Qty);

                int conversion = item.Conversion > 0 ? item.Conversion : 1;
                string stockDisplay;

                if (isTabletWise)
                {
                    int total = (int)Math.Round(qty * conversion);
                    stockDisplay = $"{total / conversion}:{total % conversion}";
                }
                else
                {
                    stockDisplay = qty.ToString("0.##");
                }

                var first = g.First();

                return new
                {
                    ItemName = item.Name,
                    Packing = item.Packing,
                    Batch = g.Key.Batch,
                    Expiry = first.ExpiryDate,
                    Mrp = first.Mrp,
                    Stock = stockDisplay,
                    Unit = item.Unit1
                };
            })
            .OrderBy(x => x.ItemName)
            .ThenBy(x => x.Batch)
            .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Batch Wise Stock");

            // ✅ COMPANY
            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user?.TenantId != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            int row = 1;

            ws.Cell(row, 1).Value = companyName;
            ws.Range(row, 1, row, 8).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row++;

            ws.Cell(row, 1).Value = "Batch Wise Stock Report";
            ws.Range(row, 1, row, 8).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row++;

            ws.Cell(row, 1).Value = $"From : {startDate:dd-MM-yyyy}  To : {endDate:dd-MM-yyyy}";
            ws.Range(row, 1, row, 8).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row += 2;

            // ✅ HEADERS (EXACT)
            string[] headers = { "Sr", "Item Name", "Packing", "Batch", "Expiry Date", "MRP", "Stock", "Unit" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 8).Style.Font.SetBold();
            row++;

            int sr = 1;

            foreach (var x in report)
            {
                ws.Cell(row, 1).Value = sr++;
                ws.Cell(row, 2).Value = x.ItemName;
                ws.Cell(row, 3).Value = x.Packing;
                ws.Cell(row, 4).Value = x.Batch;
                ws.Cell(row, 5).Value = x.Expiry?.ToString("dd-MM-yyyy");
                ws.Cell(row, 6).Value = x.Mrp;
                ws.Cell(row, 7).Value = x.Stock;
                ws.Cell(row, 8).Value = x.Unit;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BatchWiseStock.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> BatchWiseStockPdf(string fromDate, string toDate, int? companyId, int? supplierId, int? itemId, string stockType)
        {
            DateTime startDate, endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                endDate = DateTime.Today;

            endDate = endDate.AddDays(1).AddTicks(-1);

            // ✅ SAME LOGIC AS MAIN REPORT
            var allItems = (await _itemmasterservice.GetAll()).Where(i => i.IsActive).ToList();
            var itemDict = allItems.ToDictionary(i => i.Id);
            var currentStocks = await _currenstockService.GetAll();

            var filteredItemIds = allItems.Select(i => i.Id).ToHashSet();

            if (companyId.HasValue)
                filteredItemIds = filteredItemIds.Where(id => itemDict[id].CompanyId == companyId).ToHashSet();

            if (itemId.HasValue)
                filteredItemIds = filteredItemIds.Where(id => id == itemId).ToHashSet();

            if (supplierId.HasValue)
            {
                var supplierItemIds = (await _purchaseservice.GetAll())
                    .Where(p => p.SupplierId == supplierId)
                    .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                    .Select(x => x.ItemId)
                    .Distinct()
                    .ToHashSet();

                filteredItemIds = filteredItemIds.Intersect(supplierItemIds).ToHashSet();
            }

            var batchStock = currentStocks
                .Where(x => filteredItemIds.Contains(x.ItemId))
                .GroupBy(x => new { x.ItemId, x.Batch })
                .ToList();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            var report = batchStock.Select(g =>
            {
                var item = itemDict[g.Key.ItemId];
                decimal qty = g.Sum(x => x.Qty);

                int conversion = item.Conversion > 0 ? item.Conversion : 1;
                string stockDisplay;

                if (isTabletWise)
                {
                    int total = (int)Math.Round(qty * conversion);
                    stockDisplay = $"{total / conversion}:{total % conversion}";
                }
                else
                {
                    stockDisplay = qty.ToString("0.##");
                }

                var first = g.First();

                return new
                {
                    ItemName = item.Name,
                    Packing = item.Packing,
                    Batch = g.Key.Batch,
                    Expiry = first.ExpiryDate,
                    Mrp = first.Mrp,
                    Stock = stockDisplay,
                    Unit = item.Unit1
                };
            })
            .OrderBy(x => x.ItemName)
            .ThenBy(x => x.Batch)
            .ToList();


            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var user = await _userManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user?.TenantId != null)
            {
                var tenant = await _tenantService.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }
            // COMPANY
            doc.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            // TITLE
            doc.Add(new Paragraph("Batch Wise Stock Report")
                .SetFont(bold).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            // DATE
            doc.Add(new Paragraph($"From : {startDate:dd-MM-yyyy}  To : {endDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            Table table = new Table(new float[] { 1, 5, 3, 3, 3, 2, 3, 2 }).UseAllAvailableWidth();

            string[] headers = { "Sr", "Item Name", "Packing", "Batch", "Expiry Date", "MRP", "Stock", "Unit" };

            foreach (var h in headers)
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(9)));

            int sr = 1;

            foreach (var x in report)
            {
                    table.AddCell(new Paragraph(sr++.ToString()).SetFontSize(9));
                    table.AddCell(new Paragraph(x.ItemName ?? "").SetFontSize(9));
                    table.AddCell(new Paragraph(x.Packing ?? "").SetFontSize(9));
                    table.AddCell(new Paragraph(x.Batch ?? "").SetFontSize(9));
                    table.AddCell(new Paragraph(x.Expiry?.ToString("dd-MM-yyyy") ?? "").SetFontSize(9));
                    table.AddCell(new Paragraph(x.Mrp.ToString("0.00")).SetFontSize(9));
                    table.AddCell(new Paragraph(x.Stock ?? "").SetFontSize(9));
                    table.AddCell(new Paragraph(x.Unit ?? "").SetFontSize(9));
            }

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "BatchWiseStock.pdf");
        }


        [HttpGet]
        public async Task<IActionResult> GetSupplierGst(int supplierId)
        {
            var supplier = await _supplierservice.GetBySupplierId(supplierId);

            if (supplier == null)
                return Json(null);

            return Json(new { gst = supplier.GstNO });
        }
        //[HttpGet]
        //public async Task<IActionResult> GetPendingPurchaseOrders(int supplierId)
        //{
        //    var data = (await _purchaseOrderRepo.GetBySupplierId(supplierId))
        //        .Select(x => new {
        //            id = x.Id,
        //            poNumber = x.BillNo,
        //            date = x.BillDate?.ToString("dd-MM-yyyy"),
        //            totalAmount = x.TotalPayable
        //        }).ToList();

        //    return Json(data);
        //}
        [HttpGet]
        public async Task<IActionResult> GetPendingPurchaseChallans(int supplierId)
        {
            var data = (await _purchaseChallanRepository.GetAll())
                .Where(x =>
                    x.SupplierId == supplierId &&
                    !x.ConvertedPurchaseId.HasValue &&
                    !string.Equals(x.Status, "Converted", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.BillDate)
                .ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    id = x.Id,
                    challanNo = x.BillNo,
                    date = x.BillDate?.ToString("dd-MM-yyyy"),
                    supplierBillNo = x.PartyBillNo,
                    totalAmount = x.TotalPayable
                })
                .ToList();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseChallanDetails(int challanId)
        {
            var challan = await _purchaseChallanRepository.GetById(challanId);
            if (challan == null ||
                challan.ConvertedPurchaseId.HasValue ||
                string.Equals(challan.Status, "Converted", StringComparison.OrdinalIgnoreCase))
            {
                return Json(null);
            }

            var result = new
            {
                partyBillNo = challan.PartyBillNo,
                partyBillDate = challan.PartyBillDate?.ToString("yyyy-MM-dd"),
                purchaseType = string.IsNullOrWhiteSpace(challan.PurchaseType) ? "Local" : challan.PurchaseType,
                billingType = string.IsNullOrWhiteSpace(challan.billingType) ? "registered" : challan.billingType,
                items = challan.PurchaseChallanItems?.Select(i => new
                {
                    itemId = i.ItemId,
                    itemName = i.ItemMasters?.Name,
                    sourcePurchaseChallanId = challan.Id,
                    qty = i.Qty,
                    freeQty = i.FreeQty,
                    rate = i.Rate,
                    mrp = i.Mrp,
                    batch = i.Batch,
                    expiryDate = i.ExpiryDate?.ToString("yyyy-MM-dd"),
                    discount = i.Discount,
                    gst = i.Gst,
                    cgst = i.CGst,
                    sgst = i.SGst,
                    scgst = i.CGst + i.SGst,
                    hsnId = i.HsnId,
                    unit = i.Unit,
                    batchWiseCose = i.BatchWiseCose,
                    salserateA = i.salserateA,
                    salserateB = i.salserateB,
                    barcode = i.Barcode
                }).ToList()
            };

            return Json(result);
        }

        // MERGED FROM TL: Helper validations for decimal qty, index gaps, and expiry
        private static bool TryValidateDecimalQtyFreeQtyRule(IEnumerable<PurchaseItemVM>? items, out string message)
        {
            message = string.Empty;

            if (items == null)
            {
                return true;
            }

            foreach (var item in items)
            {
                if (item == null || item.ItemId <= 0 || item.Qty <= 0)
                {
                    continue;
                }

                var requiredFreeQty = Math.Ceiling(item.Qty) - item.Qty;
                if (requiredFreeQty <= 0)
                {
                    continue;
                }

                var requiredRounded = Math.Round(requiredFreeQty, 6, MidpointRounding.AwayFromZero);
                var freeQtyRounded = Math.Round(item.FreeQty, 6, MidpointRounding.AwayFromZero);

                if (freeQtyRounded != requiredRounded)
                {
                    message = $"For decimal qty {item.Qty:0.######}, Free Qty must be {requiredRounded:0.######} to complete the next whole quantity.";
                    return false;
                }
            }

            return true;
        }

        private static void ApplyDefaultExpiryIfMissing(IEnumerable<PurchaseItemVM>? items, DateTime defaultExpiryDate)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }

                var hasRowData = item.ItemId > 0
                                 || !string.IsNullOrWhiteSpace(item.Batch)
                                 || item.Qty > 0
                                 || item.FreeQty > 0
                                 || item.Rate > 0
                                 || item.Mrp > 0;
                if (!hasRowData)
                {
                    continue;
                }

                item.ExpiryDate ??= defaultExpiryDate;
            }
        }

        private async Task<bool> IsCurrentTenantPharmacyAsync()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return false;
            }

            var tenant = await _tenantService.GetById(tenantId);
            return tenant?.BusinessType == BusinessType.Pharmacy;
        }

        private static bool TryValidateRequiredExpiry(IEnumerable<PurchaseItemVM>? items, out string message)
        {
            message = string.Empty;

            if (items == null)
            {
                return true;
            }

            var rowNumber = 1;
            foreach (var item in items)
            {
                if (item == null || item.ItemId <= 0)
                {
                    rowNumber++;
                    continue;
                }

                if (!item.ExpiryDate.HasValue)
                {
                    message = $"Expiry is required for pharmacy purchase item row {rowNumber}.";
                    return false;
                }

                rowNumber++;
            }

            return true;
        }

        private bool HasCollectionIndexGap(string collectionName)
        {
            if (!Request.HasFormContentType)
            {
                return false;
            }

            var pattern = $"^{Regex.Escape(collectionName)}\\[(\\d+)\\]\\.";
            var indexes = Request.Form.Keys
                .Select(key => Regex.Match(key, pattern))
                .Where(match => match.Success)
                .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
                .Distinct()
                .OrderBy(index => index)
                .ToList();

            for (var expectedIndex = 0; expectedIndex < indexes.Count; expectedIndex++)
            {
                if (indexes[expectedIndex] != expectedIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameStockLayer(PurchaseItem existingItem, PurchaseItemVM updatedItem)
        {
            var existingBatch = string.IsNullOrWhiteSpace(existingItem.Batch) ? string.Empty : existingItem.Batch.Trim();
            var updatedBatch = string.IsNullOrWhiteSpace(updatedItem.Batch) ? string.Empty : updatedItem.Batch.Trim();

            return existingItem.ItemId == updatedItem.ItemId
                   && string.Equals(existingBatch, updatedBatch, StringComparison.OrdinalIgnoreCase)
                   && existingItem.Mrp == updatedItem.Mrp
                   && existingItem.ExpiryDate == updatedItem.ExpiryDate;
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingPurchaseOrders(int supplierId)
        {
            var allPOs = await _purchaseOrderRepo.GetBySupplierId(supplierId);

            var usedPOIds = (await _purchaseservice.GetAll()).Where(p => p.PurchaseOrderId != null).Select(p => p.PurchaseOrderId).ToList();

            var data = allPOs
                .Where(po => !usedPOIds.Contains(po.Id))
                .Select(x => new {
                    id = x.Id,
                    poNumber = x.BillNo,
                    date = x.BillDate?.ToString("dd-MM-yyyy"),
                    totalAmount = x.TotalPayable
                }).ToList();

            return Json(data);
        }
        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderDetails(int poId)
        {
            var po = await _purchaseOrderRepo.GetById(poId);

            if (po == null) return Json(null);

            var result = new
            {
                date = po.BillDate?.ToString("yyyy-MM-dd"),

                items = po.PurchaseOrderItems?.Select(i => new {
                    itemId = i.ItemId,
                    itemName = i.ItemMasters?.Name,
                    qty = i.Qty,
                    // freeQty = i.FreeQty,
                    rate = i.Rate,
                    mrp = i.Mrp,
                    //  batch = i.Batch,
                    //  expiryDate = i.ExpiryDate?.ToString("MM/yyyy"),
                    discount = i.Discount,
                    gst = i.Gst,
                    cgst = i.CGst,
                    sgst = i.SGst,
                    scgst = (i.SGst + i.CGst),
                    hsnId = i.HsnId,
                    amount = i.Amount
                })
            };

            return Json(result);
        }
    }
}
