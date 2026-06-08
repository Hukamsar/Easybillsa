using AOne.DataAccess.ProfileService;
using AOne.Models.Entity;
using ClosedXML.Excel;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Service.Whatsapp;
using iText.Html2pdf;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Reporting.NETCore;
using Microsoft.ReportingServices.Interfaces;
using NPOI.POIFS.Properties;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

using EasyBill.UI.Service.Loyalty;

namespace EasyBill.UI.Controllers
{
    public class SalesController : Controller
    {
        private readonly IDivisionRepository _divisionRepo;
        private readonly ISalesRepository _salesservice;
        private readonly IProfileService _profileService;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IPurchaseItemRepository _purchaseitemservice;
        private readonly ISalseSettingRepository _salsesettingservice;
        private readonly IPurchaseRepository _purchaseservice;
        private readonly IPurchaseReturnRepository _purchasereturnservice;
        private readonly ISalesRepository _salesService;
        private readonly IStockIssueRepository _stockissueservice;
        private readonly IStockReturnRepository _stockrturnservice;
        private readonly IStockReceiveRepository _stockreceiveservice;
        private readonly ICompanyRepository _companyService;
        private readonly IModeOfPaymentRepository _modeofpaymentservice;
        private readonly IOfferRepository _offerrepo;
        private readonly ISalesOrderRepository _salesOrderRepo;
        private readonly IUnitOfWork _unitofwork;
        private readonly UserManager<ApplicationUsers> _usersManager;
        private readonly ITenantRegistrationRepository _tenantRepository;
        private readonly IInvoiceThemeSettingRepository _themeRepo;
        private readonly ISupplierRepository _supplierRepo;
        private readonly IStockService _currenstockService;
        private readonly ICustomerAdvanceRepository _customerAdvanceRepo;
        private readonly ISupplierAdvanceRepository _supplierAdvanceRepo;
        private readonly WhatsAppService _whatsappservice;
        private readonly IRazorViewEngine _viewEngine;
        private readonly CustomerLoyaltyService _loyaltyService;

        public SalesController(
            ISalesRepository salesservice,
            IProfileService profileService,
            ICustomerRepository customerservice,
            IItemMasterRepository itemmasterservice,
            IPurchaseItemRepository purchaseitemservice,
            ISalseSettingRepository salsesettingservice,
            IPurchaseRepository purchaseservice,
            IPurchaseReturnRepository purchasereturnservice,
            ISalesRepository salesService,
            IStockIssueRepository stockissueservice,
            IStockReturnRepository stockrturnservice,
            IStockReceiveRepository stockreceiveservice,
            ICompanyRepository companyService,
            IModeOfPaymentRepository modeofpaymentservice,
            IOfferRepository offerrepo,
            ISalesOrderRepository salesOrderRepo,
            IUnitOfWork unitofwork,
            IDivisionRepository divisionRepo,
            UserManager<ApplicationUsers> userManager,
            ITenantRegistrationRepository tenantRegistration,
            IInvoiceThemeSettingRepository themeRepo,
            ISupplierRepository supplierRepo,
            IStockService currenstockService,
            ICustomerAdvanceRepository customerAdvanceRepo,
            ISupplierAdvanceRepository supplierAdvanceRepo,
            WhatsAppService whatsappservice,
            IRazorViewEngine viewEngine,
            CustomerLoyaltyService loyaltyService)
        {
            _salesservice = salesservice;
            _profileService = profileService;
            _customerservice = customerservice;
            _itemmasterservice = itemmasterservice;
            _purchaseitemservice = purchaseitemservice;
            _salsesettingservice = salsesettingservice;
            _purchaseservice = purchaseservice;
            _purchasereturnservice = purchasereturnservice;
            _salesService = salesservice;
            _stockissueservice = stockissueservice;
            _stockrturnservice = stockrturnservice;
            _stockreceiveservice = stockreceiveservice;
            _companyService = companyService;
            _modeofpaymentservice = modeofpaymentservice;
            _offerrepo = offerrepo;
            _salesOrderRepo = salesOrderRepo;
            _unitofwork = unitofwork;
            _divisionRepo = divisionRepo;
            _usersManager = userManager;
            _tenantRepository = tenantRegistration;
            _themeRepo = themeRepo;
            _supplierRepo = supplierRepo;
            _currenstockService = currenstockService;
            _customerAdvanceRepo = customerAdvanceRepo;
            _supplierAdvanceRepo = supplierAdvanceRepo;
            _whatsappservice = whatsappservice;
            _viewEngine = viewEngine;
            _loyaltyService = loyaltyService;
        }

        public async Task<IActionResult> Index()
        {
            await _profileService.Set(User);
            var data = await _salesservice.GetAll();
            var customers = await _customerservice.GetAll();

            ViewBag.CustomerList = customers
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = await InitializeSalesVM();
            await SetupViewBagData();

            var accountGroups = _unitofwork.GetRepository<AccountGroup>().GetAll().ToList();
            ViewBag.AccountGroupList = new SelectList(accountGroups, "Id", "Name");
            ViewBag.ParentAccountGroupList = new SelectList(accountGroups, "Id", "Name");
            ViewBag.GSTTypes = Enum.GetValues(typeof(GSTType))
                .Cast<GSTType>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = x.ToString()
                })
                .ToList();
            ViewBag.CustomerCategories = Enum.GetValues(typeof(CustomerCategory))
                 .Cast<CustomerCategory>()
                 .Select(x => new SelectListItem
                 {
                     Value = ((int)x).ToString(),
                     Text = x.ToString()
                 })
                 .ToList();

            // Check bussiness type
            var tenantId = User?.FindFirst("TenantId")?.Value;
            var user = (await _tenantRepository.GetAll())
           .FirstOrDefault(x => x.Id == tenantId);
            var businessType = user?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SalesVM Vm, bool isShare = false)
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

            var data = await _salesservice.GetAll();
            var billNo = Vm.BillNo?.Trim();

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

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";


            var itemMasters = (await _itemmasterservice.GetAll()).Where(x => x.IsActive).OrderBy(x => x.Name);

            if (Vm != null)
            {
                if (Vm.billingType == "registered")
                {
                    var customerList = await _customerservice.GetAll();

                    // Mobile required for registered customer
                    if (string.IsNullOrWhiteSpace(Vm.MobileNo))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Mobile number is required for registered customer."
                        });
                    }

                    var isExist = customerList.Any(x =>
                        !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                        x.PhoneNo.Trim() == Vm.MobileNo.Trim()
                    );

                    // Customer NOT found → ERROR
                    if (!isExist)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Customer does not exist. Please create customer first."
                        });
                    }
                }

                var model = new Models.Entity.Sales
                {
                    CustomerId = Vm.billingType == "registered"
                    ? Vm.CustomerId
                    : null,

                    MobileNo = Vm.billingType == "registered"
                    ? Vm.MobileNo
                    : null, // walk-in ka mobile optional

                    Address = Vm.billingType == "registered"
                    ? Vm.Address
                    : null, // walk-in ka mobile optional

                    billingType = Vm.billingType == "registered"
                    ? "registered"
                    : "Cash", // walk-in ka mobile optional

                    PaymentType = Vm.PaymentType,
                    BillDate = Vm.BillDate.HasValue
                   ? Vm.BillDate.Value.Date.Add(DateTime.Now.TimeOfDay)
                   : DateTime.Now,
                    BillNo = Vm.BillNo,
                    PharmacyDoctorId = Vm.PharmacyDoctorId,
                    DoctorMobileNumber = Vm.DoctorMobileNumber,
                    DoctorRegNumber = Vm.DoctorRegNumber,
                    Total = Vm.Total,
                    TotalGstAmt = Vm.TotalGstAmt,
                    discountPercent = Vm.discountPercent,
                    discountAmount = Vm.discountAmount,
                    Totaldiscount = Vm.Totaldiscount,
                    TotalPayable = Vm.TotalPayable,
                    RoundOffAmount = Vm.RoundOffAmount,
                    PaidAmount = Vm.PaidAmount,
                    ReturnAmount = Math.Max(0, Vm.PaidAmount - Vm.TotalPayable),
                    NetCollection = Vm.PaidAmount - (Math.Max(0, Vm.PaidAmount - Vm.TotalPayable)),
                    SalesOrderId = Vm.SalesOrderId,
                    OfferId = Vm.OfferId,
                    Balance = Vm.Balance,
                    TotalCessAmount = Vm.TotalCessAmount,
                    SalesItems = Vm.SalesItemVMs?.Select(x =>
                    {
                        var item = itemMasters.FirstOrDefault(i => i.Id == x.ItemMasterId);

                        decimal finalQty = x.Qty;
                        if (isTabletWise && item != null && item.Conversion > 0)
                        {
                            //finalQty = (x.Qty * item.Conversion) + x.TabletQty;
                            finalQty = x.Qty + ((decimal)x.TabletQty / item.Conversion);
                        }

                        return new SalesItem()
                        {
                            ItemMasterId = x.ItemMasterId,
                            PurchaseItemId = x.PurchaseItemId,
                            Batch = x.Batch,
                            Expirydate = x.Expirydate,
                            Mrp = x.Mrp,
                            Qty = finalQty,   // Converted Qty
                            Rate = x.Rate,
                            StripRate = x.StripRate,
                            Gst = x.Gst,
                            Cess = x.Cess ?? 0m,
                            Discount = x.Discount,
                            Amount = x.Amount,
                            IsSoldInTablets = isTabletWise,
                        };
                    }).ToList() ?? new List<SalesItem>(),
                    SalsePaymentDetails = Vm.SalsePaymentDetails?.Select(pd => new SalsePaymentDetails
                    {
                        PaymentModeId = pd.PaymentModeId,
                        Amount = pd.Amount,
                        ReferenceNo = pd.ReferenceNo,
                        Description = pd.Description,
                        //CustomerId = Vm.CustomerId,
                        CustomerId = Vm.billingType == "registered" ? Vm.CustomerId : null,
                    }).ToList() ?? new List<SalsePaymentDetails>()
                };
                try
                {
                    string? walletMessage = null;

                    foreach (var item in model.SalesItems)
                    {
                        await _currenstockService.UpdateStock(
                            item.ItemMasterId,
                            item.Batch?.Trim() ?? "",
                            -(item.Qty),
                            item.Expirydate,
                            item.Mrp,
                            item.StripRate
                        );
                    }

                    var createdSale = await _salesservice.Create(model);

                    if (createdSale != null)
                    {
                        await _loyaltyService.ApplyPointsForSaleAsync(createdSale, Vm.billingType == "registered" ? Vm.RedeemPoints : 0);
                    }

                    if (isShare)
                    {
                        var tenantId = User?.FindFirst("TenantId")?.Value;
                        var tenant = !string.IsNullOrWhiteSpace(tenantId)
                            ? await _tenantRepository.GetById(tenantId)
                            : null;

                        var whatsappCharge = tenant == null
                            ? 0m
                            : decimal.Round(Math.Max(tenant.WhatsAppMessageCharge, 0m), 2, MidpointRounding.AwayFromZero);

                        var destinationMobileNo = await ResolveWhatsAppMobileNoAsync(model.MobileNo, model.CustomerId);

                        if (string.IsNullOrWhiteSpace(destinationMobileNo))
                        {
                            walletMessage = "WhatsApp not sent. Customer mobile number not available.";
                        }
                        else if (tenant != null && !tenant.IsWalletActive)
                        {
                            walletMessage = "WhatsApp not sent because wallet is inactive.";
                        }
                        else if (tenant != null && !tenant.IsWhatsAppChargeActive)
                        {
                            walletMessage = "WhatsApp sending is disabled.";
                        }
                        else if (tenant != null && whatsappCharge > 0m && tenant.WalletBalance < whatsappCharge)
                        {
                            walletMessage = "WhatsApp not sent due to low wallet balance.";
                        }
                        else
                        {
                            string pdfFileName = await GenerateInvoicePdf(createdSale.Id);
                            string baseUrl = $"{Request.Scheme}://{Request.Host}";
                            string pdfUrl = $"{baseUrl}/Invoices/{pdfFileName}";
                            //string message = "Dear";
                            string customerName = "Customer";

                            if (model.CustomerId.HasValue)
                            {
                                var customer = await _customerservice.GetById(model.CustomerId.Value);

                                if (customer != null && !string.IsNullOrWhiteSpace(customer.Name))
                                {
                                    customerName = customer.Name;
                                }
                            }


                            string message = $"Dear {customerName}," +
                                             $"Your invoice {createdSale.BillNo} is attached." +
                                             $"Thank you!";
                            var sendResult = await _whatsappservice.SendWhatsAppMessageWithStatusAsync(
                                destinationMobileNo,
                                message,
                                pdfUrl);

                            if (!sendResult.IsSuccess)
                            {
                                walletMessage = sendResult.Message;
                            }
                            else
                            {
                                ScheduleGeneratedInvoiceDeletion(pdfFileName);
                                walletMessage = "WhatsApp sent successfully.";

                                if (tenant != null && whatsappCharge > 0m)
                                {
                                    try
                                    {
                                        tenant.WalletBalance = decimal.Round(
                                            Math.Max(tenant.WalletBalance - whatsappCharge, 0m),
                                            2,
                                            MidpointRounding.AwayFromZero);

                                        await _tenantRepository.Update(tenant);
                                        await LogTenantWalletHistoryAsync(
                                            tenant.Id,
                                            whatsappCharge,
                                            paymentMode: "Auto",
                                            referenceNo: createdSale.BillNo,
                                            remarks: "WhatsApp share charge deducted.",
                                            closingBalance: tenant.WalletBalance,
                                            referenceSaleId: createdSale.Id);
                                    }
                                    catch (Exception ex)
                                    {
                                        walletMessage =
                                            $"WhatsApp sent successfully, but wallet update/history failed. Error: {ex.Message}"; 
                                    }
                                }
                            }
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        saleId = createdSale.Id,
                        walletMessage
                    });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false });
                }
            }

            return Json(new { success = false });
        }

        private async Task<string?> ResolveWhatsAppMobileNoAsync(string? saleMobileNo, int? customerId)
        {
            if (!string.IsNullOrWhiteSpace(saleMobileNo))
            {
                return saleMobileNo.Trim();
            }

            if (!customerId.HasValue || customerId.Value <= 0)
            {
                return null;
            }

            var customer = await _customerservice.GetById(customerId.Value);
            if (customer == null || string.IsNullOrWhiteSpace(customer.PhoneNo))
            {
                return null;
            }

            return customer.PhoneNo.Trim();
        }

        private async Task LogTenantWalletHistoryAsync(
            string tenantId,
            decimal amount,
            string paymentMode,
            string? referenceNo,
            string? remarks,
            decimal closingBalance,
            int? referenceSaleId)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || amount <= 0)
            {
                return;
            }

            var historyRepo = _unitofwork.GetRepository<TenantWalletHistory>();
            historyRepo.Add(new TenantWalletHistory
            {
                TenantId = tenantId,
                TransactionDateTime = DateTime.Now,
                Credit = 0m,
                Debit = amount,
                PaymentMode = paymentMode,
                ReferenceNo = string.IsNullOrWhiteSpace(referenceNo) ? null : referenceNo.Trim(),
                ReferenceSaleId = referenceSaleId,
                ServiceType = "WhatsApp",
                ServiceCharge = decimal.Round(Math.Max(amount, 0m), 2, MidpointRounding.AwayFromZero),
                Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
                ClosingBalance = decimal.Round(Math.Max(closingBalance, 0m), 2, MidpointRounding.AwayFromZero)
            });

            using var transaction = historyRepo.BeginTransaction();
            await historyRepo.SaveChangesAsync();
            transaction.Commit();
        }


        [HttpGet]
        public async Task<IActionResult> GetCustomerBalance(int customerId)
        {
            // Total Sales (udhaar + total payable)
            var totalSales = (await _salesservice.GetAll())
                .Where(x => x.CustomerId == customerId)
                .Sum(x => (decimal?)x.TotalPayable) ?? 0;

            // Total Payment received
            var totalPayments = (await _salesservice.GetAll())
                .Where(x => x.CustomerId == customerId)
                .Sum(x => (decimal?)x.PaidAmount) ?? 0;

            var balance = totalSales - totalPayments;

            return Json(new
            {
                success = true,
                balance = balance
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetByMobile(string mobile)
        {
            var customer = (await _customerservice.GetAll())
                .FirstOrDefault(x => x.PhoneNo == mobile);

            if (customer == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                customerId = customer.Id,
                name = customer.Name,
                address = customer.Address
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id,string? returnUrl)
        {
            ViewBag.ReturnUrl=returnUrl;
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Set Loyalty Program Activation Date for historical sale exclusion
            var pointSettings = await _unitofwork.GetRepository<PointSetting>().Query().ToListAsync();
            var earliestSetting = pointSettings.OrderBy(x => x.Created).FirstOrDefault();
            ViewBag.LoyaltyActivationDate = earliestSetting?.Created?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "";

            var setting = await _salsesettingservice.GetByUserId(userId);
            Models.Entity.Sales model = await _salesservice.GetById(Id);
            ViewBag.CreatedDate = model?.Created?.ToString("yyyy-MM-ddTHH:mm:ss") ?? model?.BillDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "";
            SalesVM VM = new SalesVM();

            if (model != null)
            {
                VM.billingType = model.billingType == "Cash" ? "walkin" : "registered";
                VM.PaymentType = model.PaymentType;
                VM.Id = model.Id;
                VM.CustomerId = model.CustomerId;
                VM.CustomerName = model.Customers?.Name;
                VM.BillNo = model.BillNo;
                VM.BillDate = model.BillDate;
                VM.MobileNo = model.MobileNo;

                #region Doctor Required
                //var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                //var setting = await _salsesettingservice.GetByUserId(userId);
                VM.DoctorRequired = setting?.DoctorRequired ?? false;
                #endregion

                VM.PharmacyDoctorId = model.PharmacyDoctorId;
                VM.DoctorName = model.PharmacyDoctor?.Name;
                VM.DoctorMobileNumber = model.DoctorMobileNumber;
                VM.DoctorRegNumber = model.DoctorRegNumber;
                VM.Address = model.Address;
                VM.Total = model.Total;
                VM.TotalGstAmt = model.TotalGstAmt;
                VM.TotalCessAmount = model.TotalCessAmount;
                VM.discountPercent = model.discountPercent;
                VM.discountAmount = model.discountAmount;
                VM.Totaldiscount = model.Totaldiscount;
                VM.TotalPayable = model.TotalPayable;
                VM.RoundOffAmount = model.RoundOffAmount;
                VM.PaidAmount = model.PaidAmount;
                VM.ReturnAmount = model.ReturnAmount;
                VM.OfferId = model.OfferId;
                VM.Balance = model.Balance;

                // GET ALL ITEMS FOR CONVERSION LOOKUP
                var itemMasters = (await _itemmasterservice.GetAll()).ToDictionary(x => x.Id, x => x);

                // CHECK IF TABLET-WISE MODE
                bool isTabletWiseMode = setting != null && setting.ItemConversion == "TabletWise";

                VM.SalesItemVMs = model.SalesItems != null
                ? model.SalesItems.Select(x =>
                {
                    //decimal displayQty = x.Qty;
                    //decimal displayTabletQty = 0;

                    //// ✅ SPLIT QTY IF TABLET-WISE MODE
                    //if (isTabletWiseMode && itemMasters.ContainsKey(x.ItemMasterId))
                    //{
                    //    var item = itemMasters[x.ItemMasterId];
                    //    int conversion = item.Conversion > 0 ? item.Conversion : 1;

                    //    // Database me x.Qty = TOTAL TABLETS
                    //    // Split: Strips = Floor(Qty / Conversion)
                    //    //        Remaining = Qty % Conversion

                    //    displayQty = Math.Floor(x.Qty / conversion);
                    //    displayTabletQty = x.Qty % conversion;
                    //}

                    decimal displayQty = x.Qty;          // strips
                    decimal displayTabletQty = 0;

                    if (isTabletWiseMode && itemMasters.ContainsKey(x.ItemMasterId))
                    {
                        var item = itemMasters[x.ItemMasterId];
                        int conversion = item.Conversion > 0 ? item.Conversion : 1;

                        // Step 1: convert strip qty to total tablets
                        int totalTablets = (int)Math.Round(x.Qty * conversion, MidpointRounding.AwayFromZero);

                        // Step 2: split tablets into strip + tablets
                        displayQty = totalTablets / conversion;          // strips
                        displayTabletQty = totalTablets % conversion;    // tablets
                    }


                    return new SalesItemVM
                    {
                        Id = x.Id,
                        ItemMasterId = x.ItemMasterId,
                        ItemBarcodeNumber = x.ItemMaster?.Name,
                        PurchaseItemId = x.PurchaseItemId,
                        Batch = x.Batch,
                        Expirydate = x.Expirydate,
                        Mrp = x.Mrp,
                        Qty = displayQty,              // STRIPS (or original qty if StripWise)
                        TabletQty = displayTabletQty,  // REMAINING TABLETS (0 if StripWise)
                        Rate = x.Rate,
                        StripRate = x.StripRate,
                        Gst = x.Gst,
                        Cess = x.Cess,
                        Discount = x.Discount,
                        Amount = x.Amount,
                    };
                }).ToList()
                : new List<SalesItemVM>();

                VM.SalsePaymentDetails = model.SalsePaymentDetails?
                    .Where(pd => pd.Description == null || !pd.Description.Contains("Adjusted from loyalty points", StringComparison.OrdinalIgnoreCase))
                    .Select(pd => new SalsePaymentDetailsVM
                    {
                        Id = pd.Id,
                        PaymentModeId = pd.PaymentModeId,
                        Amount = pd.Amount,
                        ReferenceNo = pd.ReferenceNo,
                        Description = pd.Description,
                        CustomerId = pd.CustomerId,
                    }).ToList() ?? new List<SalsePaymentDetailsVM>();

                if (model.CustomerId.HasValue)
                {
                    var pointTx = await _unitofwork.GetRepository<PointTransaction>().Query()
                        .FirstOrDefaultAsync(x => x.SaleId == model.Id && x.CustomerId == model.CustomerId.Value);
                    if (pointTx != null)
                    {
                        VM.RedeemPoints = pointTx.RedeemedPoints;
                    }
                }
            }
            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            var accountGroups = _unitofwork.GetRepository<AccountGroup>().GetAll().ToList();
            ViewBag.AccountGroupList = new SelectList(accountGroups, "Id", "Name");
            ViewBag.ParentAccountGroupList = new SelectList(accountGroups, "Id", "Name");
            ViewBag.GSTTypes = Enum.GetValues(typeof(GSTType))
                .Cast<GSTType>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = x.ToString()
                })
                .ToList();
            ViewBag.CustomerCategories = Enum.GetValues(typeof(CustomerCategory))
                 .Cast<CustomerCategory>()
                 .Select(x => new SelectListItem
                 {
                     Value = ((int)x).ToString(),
                     Text = x.ToString()
                 })
                 .ToList();

            // Check bussiness type
            var tenantId = User?.FindFirst("TenantId")?.Value;
            var user = (await _tenantRepository.GetAll())
           .FirstOrDefault(x => x.Id == tenantId);
            var businessType = user?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;

            return View(VM);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SalesVM VM, bool isShare = false)
        {
            //if (!ModelState.IsValid)
            //{
            //    ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            //    ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
            //    ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
            //    return View(VM);
            //}

            if (!ModelState.IsValid)
            {
                ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
                ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
                ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
                return Json(new { success = false, message = "Validation failed. Please check all fields." });
            }

            if (VM.billingType == "registered")
            {
                var customerList = await _customerservice.GetAll();

                // Mobile required for registered customer
                if (string.IsNullOrWhiteSpace(VM.MobileNo))
                {
                    ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
                    ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
                    ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
                    TempData["error"] = "Mobile number is required for registered customer.";
                    return View(VM);
                }

                var isExist = customerList.Any(x =>
                    !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                    x.PhoneNo.Trim() == VM.MobileNo.Trim()
                );

                // Customer NOT found → ERROR
                if (!isExist)
                {
                    ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
                    ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
                    ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
                    TempData["error"] = "Customer does not exist. Please create customer first.";
                    return View(VM);
                }
            }

            // GET SETTING & ITEM MASTERS (SAME AS CREATE)
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name);

            Models.Entity.Sales model = await _salesservice.GetById(VM.Id);
            if (model != null)
            {
                string? walletMessage = null;

                model.CustomerId = VM.billingType == "registered" ? VM.CustomerId : null;
                model.MobileNo = VM.billingType == "registered" ? VM.MobileNo : null;
                model.Address = VM.billingType == "registered" ? VM.Address : null;
                model.billingType = VM.billingType == "registered" ? VM.billingType : "Cash";
                model.PaymentType = VM.PaymentType;
                model.BillDate = VM.BillDate;
                model.BillNo = VM.BillNo;
                model.PharmacyDoctorId = VM.PharmacyDoctorId;
                model.DoctorMobileNumber = VM.DoctorMobileNumber;
                model.DoctorRegNumber = VM.DoctorRegNumber;
                model.Total = VM.Total;
                model.TotalGstAmt = VM.TotalGstAmt;
                model.TotalCessAmount = VM.TotalCessAmount;
                model.TotalPayable = VM.TotalPayable;
                model.RoundOffAmount = VM.RoundOffAmount;
                model.discountPercent = VM.discountPercent;
                model.discountAmount = VM.discountAmount;
                model.Totaldiscount = VM.Totaldiscount;
                model.PaidAmount = VM.PaidAmount;
                model.ReturnAmount = Math.Max(0, VM.PaidAmount - VM.TotalPayable);
                model.OfferId = VM.OfferId;
                model.Balance = VM.Balance;
                model.NetCollection = VM.PaidAmount - model.ReturnAmount;

                if (VM.billingType == "registered" && VM.CustomerId.HasValue)
                {
                    var customer = await _customerservice.GetById(VM.CustomerId);
                    if (customer != null)
                    {
                        customer.Name = VM.CustomerName;
                        customer.PhoneNo = VM.MobileNo;
                        customer.Address = VM.Address;
                        await _customerservice.Update(customer);
                    }
                }

                if (model.SalesItems == null)
                    model.SalesItems = new List<SalesItem>();

                if (VM.SalesItemVMs == null)
                    VM.SalesItemVMs = new List<SalesItemVM>();

                // ❌ Remove Deleted Items
                var removedItems = model.SalesItems
                    .Where(dbItem => !VM.SalesItemVMs.Any(vmItem => vmItem.Id == dbItem.Id))
                    .ToList();

                foreach (var item in removedItems)
                {
                    await _currenstockService.UpdateStock(
                            item.ItemMasterId,
                            item.Batch?.Trim() ?? "",
                            item.Qty, // 🔥 reverse (add back)
                            item.Expirydate,
                            item.Mrp,
                            item.Rate
                        );
                    model.SalesItems.Remove(item);
                }

                // UPDATE/ADD ITEMS (WITH TABLET-WISE CONVERSION)
                foreach (var item in VM.SalesItemVMs)
                {
                    // GET ITEM MASTER FOR CONVERSION
                    var itemMaster = itemMasters.FirstOrDefault(i => i.Id == item.ItemMasterId);

                    //// CALCULATE FINAL QTY (SAME LOGIC AS CREATE)
                    //decimal finalQty = item.Qty;
                    //if (isTabletWise && itemMaster != null && itemMaster.Conversion > 0)
                    //{
                    //    // Convert: (Strips × Conversion) + Tablets = Total Tablets
                    //    finalQty = (item.Qty * itemMaster.Conversion) + item.TabletQty;
                    //}


                    // CALCULATE FINAL QTY (STRIP-BASED, SAME AS CREATE)
                    decimal finalQty = item.Qty;

                    if (isTabletWise && itemMaster != null && itemMaster.Conversion > 0)
                    {
                        finalQty = item.Qty + ((decimal)item.TabletQty / itemMaster.Conversion);
                    }

                    if (item.Id > 0) // UPDATE EXISTING ITEMS
                    {
                        var existingItem = model.SalesItems.FirstOrDefault(x => x.Id == item.Id);
                        if (existingItem != null)
                        {
                            decimal oldQty = existingItem.Qty;
                            decimal newQty = finalQty;

                            // 🔴 Step 1: old stock wapas add karo
                            await _currenstockService.UpdateStock(
                                existingItem.ItemMasterId,
                                existingItem.Batch?.Trim() ?? "",
                                oldQty,
                                existingItem.Expirydate,
                                existingItem.Mrp,
                                existingItem.Rate
                            );

                            // 🟢 Step 2: new stock minus karo
                            await _currenstockService.UpdateStock(
                                item.ItemMasterId,
                                item.Batch?.Trim() ?? "",
                                -newQty,
                                item.Expirydate,
                                item.Mrp,
                                item.Rate
                            );

                            existingItem.ItemMasterId = item.ItemMasterId;
                            existingItem.PurchaseItemId = item.PurchaseItemId;
                            existingItem.Batch = item.Batch;
                            existingItem.Expirydate = item.Expirydate;
                            existingItem.Mrp = item.Mrp;
                            existingItem.Qty = finalQty;  // CONVERTED QTY
                            existingItem.Rate = item.Rate;
                            existingItem.StripRate = item.StripRate;
                            existingItem.Gst = item.Gst;
                            existingItem.Cess = item.Cess ?? 0m;
                            existingItem.Discount = item.Discount;
                            existingItem.Amount = item.Amount;
                            existingItem.IsSoldInTablets = isTabletWise; // SET FLAG
                        }
                    }
                    else // ADD NEW ITEMS
                    {
                        await _currenstockService.UpdateStock(
                            item.ItemMasterId,
                            item.Batch?.Trim() ?? "",
                            -finalQty, // 🔥 minus
                            item.Expirydate,
                            item.Mrp,
                            item.Rate
                        );
                        model.SalesItems.Add(new SalesItem
                        {
                            ItemMasterId = item.ItemMasterId,
                            PurchaseItemId = item.PurchaseItemId,
                            Batch = item.Batch,
                            Expirydate = item.Expirydate,
                            Mrp = item.Mrp,
                            Qty = finalQty,  // CONVERTED QTY
                            Rate = item.Rate,
                            StripRate = item.StripRate,
                            Gst = item.Gst,
                            Cess = item.Cess ?? 0m,
                            Discount = item.Discount,
                            Amount = item.Amount,
                            IsSoldInTablets = isTabletWise, // SET FLAG
                        });
                    }
                }

                // PAYMENT DETAILS (NO CHANGES)
                if (model.SalsePaymentDetails == null)
                    model.SalsePaymentDetails = new List<SalsePaymentDetails>();

                if (VM.SalsePaymentDetails == null)
                    VM.SalsePaymentDetails = new List<SalsePaymentDetailsVM>();

                var removedPaymentDetails = model.SalsePaymentDetails
                   .Where(dbPayment => 
                       !VM.SalsePaymentDetails.Any(vmItem => vmItem.Id == dbPayment.Id) &&
                       (dbPayment.Description == null || !dbPayment.Description.Contains("Adjusted from loyalty points", StringComparison.OrdinalIgnoreCase)))
                   .ToList();

                foreach (var payment in removedPaymentDetails)
                {
                    model.SalsePaymentDetails.Remove(payment);
                }

                foreach (var data in VM.SalsePaymentDetails)
                {
                    if (data.Id > 0)
                    {
                        var existingPayment = model.SalsePaymentDetails.FirstOrDefault(x => x.Id == data.Id);
                        if (existingPayment != null)
                        {
                            existingPayment.PaymentModeId = data.PaymentModeId;
                            existingPayment.Amount = data.Amount;
                            existingPayment.ReferenceNo = data.ReferenceNo;
                            existingPayment.Description = data.Description;
                            existingPayment.CustomerId = VM.billingType == "registered" ? VM.CustomerId : null;
                        }
                    }
                    else
                    {
                        model.SalsePaymentDetails.Add(new SalsePaymentDetails
                        {
                            PaymentModeId = data.PaymentModeId,
                            Amount = data.Amount,
                            ReferenceNo = data.ReferenceNo,
                            Description = data.Description,
                            CustomerId = VM.billingType == "registered" ? VM.CustomerId : null,
                        });
                    }
                }

                //    await _salesservice.Update(model);
                //}

                //return RedirectToAction("Index");

                await _salesservice.Update(model);

                if (model != null)
                {
                    await _loyaltyService.ApplyPointsForSaleAsync(model, VM.billingType == "registered" ? VM.RedeemPoints : 0);
                }

                if (isShare)
                {
                    var tenantId = User?.FindFirst("TenantId")?.Value;
                    var tenant = !string.IsNullOrWhiteSpace(tenantId)
                        ? await _tenantRepository.GetById(tenantId)
                        : null;

                    var whatsappCharge = tenant == null
                        ? 0m
                        : decimal.Round(Math.Max(tenant.WhatsAppMessageCharge, 0m), 2, MidpointRounding.AwayFromZero);

                    var destinationMobileNo = await ResolveWhatsAppMobileNoAsync(model.MobileNo, model.CustomerId);

                    if (string.IsNullOrWhiteSpace(destinationMobileNo))
                    {
                        walletMessage = "WhatsApp not sent. Customer mobile number not available.";
                    }
                    else if (tenant != null && !tenant.IsWalletActive)
                    {
                        walletMessage = "WhatsApp not sent because wallet is inactive.";
                    }
                    else if (tenant != null && !tenant.IsWhatsAppChargeActive)
                    {
                        walletMessage = "WhatsApp sending is disabled.";
                    }
                    else if (tenant != null && whatsappCharge > 0m && tenant.WalletBalance < whatsappCharge)
                    {
                        walletMessage = "WhatsApp not sent due to low wallet balance.";
                    }
                    else
                    {
                        string pdfFileName = await GenerateInvoicePdf(model.Id);
                        string baseUrl = $"{Request.Scheme}://{Request.Host}";
                        string pdfUrl = $"{baseUrl}/Invoices/{pdfFileName}";

                        string customerName = "Customer";
                        if (model.CustomerId.HasValue)
                        {
                            var customer = await _customerservice.GetById(model.CustomerId.Value);

                            if (customer != null && !string.IsNullOrWhiteSpace(customer.Name))
                            {
                                customerName = customer.Name;
                            }
                        }

                        string message = $"Dear {customerName}," +
                                         $"Your invoice {model.BillNo} is attached." +
                                         $"Thank you!";
                        var sendResult = await _whatsappservice.SendWhatsAppMessageWithStatusAsync(
                            destinationMobileNo,
                            message,
                            pdfUrl);

                        if (!sendResult.IsSuccess)
                        {
                            walletMessage = sendResult.Message;
                        }
                        else
                        {
                            ScheduleGeneratedInvoiceDeletion(pdfFileName);
                            walletMessage = "WhatsApp sent successfully.";

                            if (tenant != null && whatsappCharge > 0m)
                            {
                                try
                                {
                                    tenant.WalletBalance = decimal.Round(
                                        Math.Max(tenant.WalletBalance - whatsappCharge, 0m),
                                        2,
                                        MidpointRounding.AwayFromZero);

                                    await _tenantRepository.Update(tenant);
                                    await LogTenantWalletHistoryAsync(
                                        tenant.Id,
                                        whatsappCharge,
                                        paymentMode: "Auto",
                                        referenceNo: model.BillNo,
                                        remarks: "WhatsApp share charge deducted.",
                                        closingBalance: tenant.WalletBalance,
                                        referenceSaleId: model.Id);
                                }
                                catch (Exception ex)
                                {
                                    walletMessage =
                                        $"WhatsApp sent successfully, but wallet update/history failed. Error: {ex.Message}";
                                }
                            }
                        }
                    }
                }

                return Json(new { success = true, saleId = model.Id, walletMessage });
            }

            return Json(new { success = false, message = "Sale not found." });
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

                var model = await _salesservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                foreach (var item in model.SalesItems)
                {
                    await _currenstockService.UpdateStock(
                        item.ItemMasterId,
                        item.Batch ?? "",
                        item.Qty, // 🔺 SALE delete = add back
                        item.Expirydate,
                        item.Mrp,
                        item.Rate
                    );
                }

                // Clean up loyalty points transaction if any
                var pointRepo = _unitofwork.GetRepository<PointTransaction>();
                var existingPointTx = await pointRepo.Query()
                    .FirstOrDefaultAsync(x => x.SaleId == model.Id);
                if (existingPointTx != null)
                {
                    pointRepo.Delete(existingPointTx);
                    await _unitofwork.SaveAsync();
                }

                await _salesservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        public async Task<IActionResult> GetDataByFilter(string BillNo, DateTime? StartBillDate, DateTime? EndBillDate, int? CustomerId, string MobileNo)
        {
            var query = (await _salesservice.GetAll()).AsQueryable();

            if (!string.IsNullOrWhiteSpace(BillNo))
                query = query.Where(x => x.BillNo.Contains(BillNo));

            if (StartBillDate.HasValue)
                query = query.Where(x => x.BillDate >= StartBillDate.Value);

            if (EndBillDate.HasValue)
                query = query.Where(x => x.BillDate <= EndBillDate.Value);

            if (CustomerId.HasValue && CustomerId.Value > 0)
            {
                query = query.Where(x => x.CustomerId == CustomerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(MobileNo))
                query = query.Where(x => x.MobileNo.Contains(MobileNo));

            var result = query.ToList();

            return PartialView("_SalesTable", result);
        }

        [HttpGet]
        public async Task<IActionResult> GetNewBillNo()
        {
            var lastBillNo = await GenerateNxtNumber();
            return Json(new { success = true, billNo = lastBillNo });
        }

        // Helper method
        private async Task SetupViewBagData()
        {
            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
        }

        private async Task<SalesVM> InitializeSalesVM(SalesVM vm = null)
        {
            var viewModel = vm ?? new SalesVM();
            viewModel.BillDate = DateTime.Now;
            viewModel.BillNo = await GenerateNxtNumber();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            ViewBag.HasSetting = setting != null;
            viewModel.DoctorRequired = setting?.DoctorRequired ?? false;

            return viewModel;
        }

        public async Task<string> GenerateNxtNumber()
        {
            var lastCode = (await _salesservice.GetAll())
                .Where(x => !string.IsNullOrWhiteSpace(x.BillNo))
                .OrderByDescending(x => x.Created)
                .Select(x => x.BillNo)
                .FirstOrDefault();

            return GenerateNextProductCode(lastCode);
        }

        private string GenerateNextProductCode(string lastCode)
        {
            if (string.IsNullOrWhiteSpace(lastCode))
                return "SA0001";

            // Last numeric sequence only
            var match = Regex.Match(lastCode, @"(\d+)(?!.*\d)");

            if (!match.Success)
                return "SA0001";

            int number = int.Parse(match.Value);
            string prefix = lastCode[..match.Index];
            string suffix = lastCode[(match.Index + match.Length)..];

            string nextNumber = (number + 1).ToString($"D{match.Length}");

            return $"{prefix}{nextNumber}{suffix}";
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesOrdersByCustomer(int customerId)
        {
            // Jinki sale ho chuki hai
            var usedSalesOrderIds = (await _salesservice.GetAll())
                .Where(s => s.SalesOrderId != null)
                .Select(s => s.SalesOrderId.Value)
                .ToList();

            // Sirf wahi SalesOrder lao jinki sale nahi hui WITH payment details
            var allOrders = await _salesOrderRepo.GetByCustomerIdWithPayments(customerId);

            var orders = allOrders
                .Where(o => !usedSalesOrderIds.Contains(o.Id))
                .Select(o => new
                {
                    id = o.Id,
                    billNo = o.BillNo,
                    billDate = o.BillDate,
                    discountPercent = o.discountPercent,
                    discountAmount = o.discountAmount,
                    total = o.Total,
                    Totaldiscount = o.Totaldiscount,
                    totalGstAmt = o.TotalGstAmt,
                    totalPayable = o.TotalPayable,
                    paidAmount = o.PaidAmount,
                    balance = o.Balance,
                    returnAmount = o.ReturnAmount,
                    totalcessamt = o.TotalCessAmt,
                    items = o.salesOrderItems.Select(i => new
                    {
                        itemId = i.ItemMasterId,
                        itemName = i.ItemMaster != null ? i.ItemMaster.Name : "",
                        batchNo = i.Batch,
                        expiryDate = i.Expirydate.HasValue ? i.Expirydate.Value.ToString("MM/yyyy") : "",
                        qty = i.Qty,
                        mrp = i.Mrp,
                        rate = i.Rate,
                        gstPercent = i.Gst,
                        cess = i.Cess,
                        discountPercent = i.Discount,
                        amount = i.Amount
                    }).ToList(),
                    paymentDetails = (o.SalsePaymentDetails ?? new List<SalsePaymentDetails>())
                        .Select(pd => new
                        {
                            id = pd.Id,
                            paymentModeId = pd.PaymentModeId,
                            amount = pd.Amount,
                            referenceNo = pd.ReferenceNo,
                            description = pd.Description
                        }).ToList()
                })
                .ToList();

            return Json(orders);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetBatchesByItemId(int id)
        //{
        //    var item = await _itemmasterservice.GetByItemMasterId(id);
        //    if (item == null)
        //        return Json(new { error = "Item not found." });

        //    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    var setting = await _salsesettingservice.GetByUserId(userId);

        //    // --- Fetch all related transactions ---
        //    var purchaseItems = await _purchaseitemservice.GetByItemMasterId(id);
        //    var purchaseReturnItems = (await _purchasereturnservice.GetAll())
        //        .Where(r => r.PurchaseReturnItems != null)
        //        .SelectMany(r => r.PurchaseReturnItems)
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    var salesItems = (await _salesService.GetAll())
        //        .Where(s => s.SalesItems != null)
        //        .SelectMany(s => s.SalesItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockIssueItems = (await _stockissueservice.GetAll())
        //        .Where(s => s.StockIssuesItems != null)
        //        .SelectMany(s => s.StockIssuesItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockReturnItems = (await _stockrturnservice.GetAll())
        //        .Where(s => s.StockReturnItems != null)
        //        .SelectMany(s => s.StockReturnItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockReceiveItems = (await _stockreceiveservice.GetAll())
        //        .Where(s => s.StockReceiveItems != null)
        //        .SelectMany(s => s.StockReceiveItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    // --- Calculate remaining qty per batch ---
        //    var groupedResult = purchaseItems
        //        .GroupBy(b => new { b.Batch, b.ExpiryDate, b.Mrp, b.Rate })
        //        .Select(g =>
        //        {
        //            var batchNo = g.Key.Batch;
        //            var expiry = g.Key.ExpiryDate;
        //            var mrp = g.Key.Mrp;
        //            decimal conversion = g.First().ItemMasters?.Conversion ?? 1;

        //            // Priority-based rate selection from first item in group
        //            var firstItem = g.First();
        //            var rate = firstItem.salserateA > 0 ? firstItem.salserateA : 0;

        //            // Purchased Qty
        //            var totalPurchased = g.Sum(x => x.Qty + x.FreeQty);

        //            // Returned to supplier
        //            var purchaseReturned = purchaseReturnItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty + x.FreeQty);

        //            // Sold Qty
        //            var soldQty = salesItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // Stock issued Qty
        //            var issuedQty = stockIssueItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // Stock returned Qty
        //            var returnedQty = stockReturnItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // Stock received Qty
        //            var receivedQty = stockReceiveItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // ✅ Final available quantity
        //            var currentQty = totalPurchased + returnedQty + receivedQty
        //                           - purchaseReturned - soldQty - issuedQty;

        //            decimal ratePerUnit = rate; // default strip rate

        //            if (setting != null && setting.ItemConversion == "TabletWise")
        //            {
        //                ratePerUnit = conversion != 0 ? rate / conversion : rate;
        //            }


        //            string stripTabsQty;

        //            if (setting != null && setting.ItemConversion == "TabletWise" && conversion > 0)
        //            {
        //                int conversionInt = (int)conversion;

        //                int totalTablets = (int)Math.Round(
        //                    currentQty * conversionInt,
        //                    MidpointRounding.AwayFromZero
        //                );

        //                int strips = totalTablets / conversionInt;
        //                int tablets = totalTablets % conversionInt;

        //                stripTabsQty = $"{strips}:{tablets}";
        //            }
        //            else
        //            {
        //                stripTabsQty = currentQty.ToString("0.##");
        //            }


        //            return new
        //            {
        //                batch = batchNo,
        //                expiry = expiry,
        //                rate = ratePerUnit,
        //                striprate = rate,
        //                mrp = mrp,
        //                qty = currentQty,
        //                StripTabsQty = stripTabsQty,
        //                purchaseItemId = g.Select(x => x.Id).FirstOrDefault(),
        //                conversion = conversion
        //            };
        //        });

        //    // Filter based on AllowNegative setting
        //    if (setting != null && !setting.AllowNegative)
        //    {
        //        // ❌ qty = 0 hide
        //        // ✅ qty > 0 OR qty < 0 show
        //        groupedResult = groupedResult.Where(x => x.qty != 0);
        //    }

        //    // YAHAN ADD KARO - Expiry date filtering
        //    if (setting != null && setting.ExpiryAllowedDays > 0)
        //    {
        //        var allowedExpiryDate = DateTime.Now.AddDays(setting.ExpiryAllowedDays);
        //        groupedResult = groupedResult.Where(x => x.expiry == null || x.expiry >= allowedExpiryDate);
        //    }

        //    // Convert to list AFTER filtering
        //    var finalResult = groupedResult
        //        .OrderBy(x => x.expiry)
        //        .ToList();

        //    var result = new
        //    {
        //        gst = item.Hsn?.IGST ?? 0,
        //        groupedResult = finalResult
        //    };

        //    return Json(result);
        //}
        [HttpGet]
        public async Task<IActionResult> GetBatchesByItemId(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
                return Json(new { error = "Item not found." });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            var currentStocks = await _currenstockService.GetAll();

            var itemStocks = currentStocks
                .Where(x => x.ItemId == id)
                .ToList();

            var groupedResult = itemStocks
                .GroupBy(x => new { x.Batch, x.ExpiryDate, x.Mrp, x.PurchaseRate })
                .Select(g =>
                {
                    var batch = g.Key.Batch;
                    var expiry = g.Key.ExpiryDate;
                    var mrp = g.Key.Mrp;
                    var purchaseRate = g.FirstOrDefault()?.SalesRateA ?? 0;
                    decimal qty = g.Sum(x => x.Qty);

                    decimal conversion = item.Conversion > 0 ? item.Conversion : 1;

                    decimal ratePerUnit = purchaseRate;
                    if (setting != null && setting.ItemConversion == "TabletWise")
                    {
                        ratePerUnit = conversion != 0 ? purchaseRate / conversion : purchaseRate;
                    }

                    string stripTabsQty;

                    if (setting != null && setting.ItemConversion == "TabletWise" && conversion > 0)
                    {
                        int conv = (int)conversion;

                        int totalTablets = (int)Math.Round(qty * conv, MidpointRounding.AwayFromZero);

                        int strips = totalTablets / conv;
                        int tablets = totalTablets % conv;

                        stripTabsQty = $"{strips}:{tablets}";
                    }
                    else
                    {
                        stripTabsQty = qty.ToString("0.##");
                    }

                    return new
                    {
                        batch = batch,
                        expiry = expiry,
                        rate = ratePerUnit,
                        striprate = purchaseRate,
                        mrp = mrp,
                        qty = qty,
                        StripTabsQty = stripTabsQty,
                        conversion = conversion
                    };
                });

            if (setting != null && !setting.AllowNegative)
            {
                groupedResult = groupedResult.Where(x => x.qty > 0);
            }

            if (setting != null && setting.ExpiryAllowedDays > 0)
            {
                var allowedDate = DateTime.Now.AddDays(setting.ExpiryAllowedDays);

                groupedResult = groupedResult
                    .Where(x => x.expiry == null || x.expiry >= allowedDate);
            }

            var finalResult = groupedResult
                .OrderBy(x => x.expiry)
                .ToList();

            var result = new
            {
                gst = item.Hsn?.IGST ?? 0,
                cess = item.Hsn?.Cess ?? 0,
                groupedResult = finalResult
            };

            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetBatchesByItemIdForEdit(int id, int saleId = 0)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
                return Json(new { error = "Item not found." });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            var currentStocks = await _currenstockService.GetAll();

            var itemStocks = currentStocks
                .Where(x => x.ItemId == id)
                .ToList();

            var currentSaleItems = new List<SalesItem>();
            if (saleId > 0)
            {
                var sale = await _salesService.GetById(saleId);
                if (sale?.SalesItems != null)
                {
                    currentSaleItems = sale.SalesItems.ToList();
                }
            }

            var groupedResult = itemStocks
                .GroupBy(x => new { x.Batch, x.ExpiryDate, x.Mrp, x.PurchaseRate })
                .Select(g =>
                {
                    var batch = g.Key.Batch;
                    var expiry = g.Key.ExpiryDate;
                    var mrp = g.Key.Mrp;
                    var purchaseRate = g.FirstOrDefault()?.SalesRateA ?? 0;

                    decimal qty = g.Sum(x => x.Qty);

                    var saleQty = currentSaleItems
                        .Where(x => x.ItemMasterId == id &&
                                    x.Batch == batch &&
                                    x.Mrp == mrp &&
                                    x.Expirydate == expiry)
                        .Sum(x => x.Qty);

                    qty += saleQty;

                    decimal conversion = item.Conversion > 0 ? item.Conversion : 1;

                    decimal ratePerUnit = purchaseRate;
                    if (setting != null && setting.ItemConversion == "TabletWise")
                    {
                        ratePerUnit = conversion != 0 ? purchaseRate / conversion : purchaseRate;
                    }

                    string stripTabsQty;

                    if (setting != null && setting.ItemConversion == "TabletWise" && conversion > 0)
                    {
                        int conv = (int)conversion;

                        int totalTablets = (int)Math.Round(qty * conv, MidpointRounding.AwayFromZero);

                        int strips = totalTablets / conv;
                        int tablets = totalTablets % conv;

                        stripTabsQty = $"{strips}:{tablets}";
                    }
                    else
                    {
                        stripTabsQty = qty.ToString("0.##");
                    }

                    return new
                    {
                        batch = batch,
                        expiry = expiry,
                        rate = ratePerUnit,
                        striprate = purchaseRate,
                        mrp = mrp,
                        qty = qty,
                        StripTabsQty = stripTabsQty,
                        conversion = conversion
                    };
                });

            if (setting != null && !setting.AllowNegative)
            {
                groupedResult = groupedResult.Where(x => x.qty > 0);
            }

            if (setting != null && setting.ExpiryAllowedDays > 0)
            {
                var allowedDate = DateTime.Now.AddDays(setting.ExpiryAllowedDays);

                groupedResult = groupedResult
                    .Where(x => x.expiry == null || x.expiry >= allowedDate);
            }

            var finalResult = groupedResult
                .OrderBy(x => x.expiry)
                .ToList();

            var result = new
            {
                gst = item.Hsn?.IGST ?? 0,
                cess = item.Hsn?.Cess ?? 0,
                groupedResult = finalResult
            };

            return Json(result);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetBatchesByItemIdForEdit(int id, int saleId = 0)
        //{
        //    var item = await _itemmasterservice.GetByItemMasterId(id);
        //    if (item == null)
        //        return Json(new { error = "Item not found." });

        //    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    var setting = await _salsesettingservice.GetByUserId(userId);

        //    // --- Fetch all related transactions ---
        //    var purchaseItems = await _purchaseitemservice.GetByItemMasterId(id);
        //    var purchaseReturnItems = (await _purchasereturnservice.GetAll())
        //        .Where(r => r.PurchaseReturnItems != null)
        //        .SelectMany(r => r.PurchaseReturnItems)
        //        .Where(x => x.ItemId == id)
        //        .ToList();

        //    var salesItems = (await _salesService.GetAll())
        //        .Where(s => s.SalesItems != null)
        //        .SelectMany(s => s.SalesItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockIssueItems = (await _stockissueservice.GetAll())
        //        .Where(s => s.StockIssuesItems != null)
        //        .SelectMany(s => s.StockIssuesItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockReturnItems = (await _stockrturnservice.GetAll())
        //        .Where(s => s.StockReturnItems != null)
        //        .SelectMany(s => s.StockReturnItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    var stockReceiveItems = (await _stockreceiveservice.GetAll())
        //        .Where(s => s.StockReceiveItems != null)
        //        .SelectMany(s => s.StockReceiveItems)
        //        .Where(x => x.ItemMasterId == id)
        //        .ToList();

        //    // --- Calculate remaining qty per batch ---
        //    var groupedResult = purchaseItems
        //        .GroupBy(b => new { b.Batch, b.ExpiryDate, b.Mrp, b.Rate })
        //        .Select(g =>
        //        {
        //            var batchNo = g.Key.Batch;
        //            var expiry = g.Key.ExpiryDate;
        //            var mrp = g.Key.Mrp;
        //            decimal conversion = g.First().ItemMasters?.Conversion ?? 1;

        //            // Priority-based rate selection from first item in group
        //            var firstItem = g.First();
        //            var rate = firstItem.salserateA > 0 ? firstItem.salserateA : 0;

        //            // Purchased Qty
        //            var totalPurchased = g.Sum(x => x.Qty + x.FreeQty);

        //            // Returned to supplier
        //            var purchaseReturned = purchaseReturnItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty + x.FreeQty);

        //            // Sold Qty
        //            //var soldQty = salesItems
        //            //    .Where(x => x.Batch == batchNo)
        //            //    .Sum(x => x.Qty);

        //            // Sold Qty (current sale exclude karo)
        //            var soldQty = salesItems
        //                .Where(x => x.Batch == batchNo && x.SalesId != saleId)
        //                .Sum(x => x.Qty);

        //            // Stock issued Qty
        //            var issuedQty = stockIssueItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // Stock returned Qty
        //            var returnedQty = stockReturnItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // Stock received Qty
        //            var receivedQty = stockReceiveItems
        //                .Where(x => x.Batch == batchNo)
        //                .Sum(x => x.Qty);

        //            // Final available quantity
        //            var currentQty = totalPurchased + returnedQty + receivedQty
        //                           - purchaseReturned - soldQty - issuedQty;

        //            //currentQty += soldQty;
        //            decimal ratePerUnit = rate; // default strip rate
        //            if (setting != null && setting.ItemConversion == "TabletWise")
        //            {
        //                ratePerUnit = conversion != 0 ? rate / conversion : rate;
        //            }


        //            string stripTabsQty;
        //            if (setting != null && setting.ItemConversion == "TabletWise" && conversion > 0)
        //            {
        //                int conversionInt = (int)conversion;

        //                int totalTablets = (int)Math.Round(
        //                    currentQty * conversionInt,
        //                    MidpointRounding.AwayFromZero
        //                );

        //                int strips = totalTablets / conversionInt;
        //                int tablets = totalTablets % conversionInt;

        //                stripTabsQty = $"{strips}:{tablets}";
        //            }
        //            else
        //            {
        //                stripTabsQty = currentQty.ToString("0.##");
        //            }

        //            return new
        //            {
        //                batch = batchNo,
        //                expiry = expiry,
        //                rate = ratePerUnit,
        //                striprate = rate,
        //                mrp = mrp,
        //                qty = currentQty,
        //                StripTabsQty = stripTabsQty,
        //                purchaseItemId = g.Select(x => x.Id).FirstOrDefault(),
        //                conversion = conversion
        //            };
        //        });

        //    // Filter based on AllowNegative setting
        //    if (setting != null && !setting.AllowNegative)
        //    {
        //        // qty = 0 hide
        //        // qty > 0 OR qty < 0 show
        //        groupedResult = groupedResult.Where(x => x.qty != 0);
        //    }

        //    // YAHAN ADD KARO - Expiry date filtering
        //    if (setting != null && setting.ExpiryAllowedDays > 0)
        //    {
        //        var allowedExpiryDate = DateTime.Now.AddDays(setting.ExpiryAllowedDays);
        //        groupedResult = groupedResult.Where(x => x.expiry == null || x.expiry >= allowedExpiryDate);
        //    }

        //    // Convert to list AFTER filtering
        //    var finalResult = groupedResult
        //        .OrderBy(x => x.expiry)
        //        .ToList();

        //    var result = new
        //    {
        //        gst = item.Hsn?.IGST ?? 0,
        //        groupedResult = finalResult
        //    };


        //    return Json(result);
        //}

        [HttpGet]
        public async Task<IActionResult> GetItemDetails(int itemId)
        {
            if (itemId > 0)
            {
                var hsndata = await _itemmasterservice.GetByItemMasterId(itemId);
                if (hsndata != null)
                {
                    var model = new
                    {
                        gst = hsndata.Hsn?.IGST,
                        purchaseItems = (await _purchaseitemservice.GetByItemMasterId(itemId)).Select(x => new
                        {
                            x.Id,
                            Text = x.Batch
                        }).ToList(),
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



        //[HttpPost]
        //public async Task<IActionResult> Edit(SalesVM VM)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
        //        ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
        //        ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
        //        return View(VM);
        //    }


        //    if (VM.billingType == "registered")
        //    {
        //        var customerList = await _customerservice.GetAll();

        //        // 🔴 Mobile required for registered customer
        //        if (string.IsNullOrWhiteSpace(VM.MobileNo))
        //        {
        //            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
        //            ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
        //            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
        //            TempData["error"] = "Mobile number is required for registered customer.";
        //            return View(VM);
        //        }

        //        var isExist = customerList.Any(x =>
        //            !string.IsNullOrWhiteSpace(x.PhoneNo) &&
        //            x.PhoneNo.Trim() == VM.MobileNo.Trim()
        //        );

        //        // 🔴 Customer NOT found → ERROR
        //        if (!isExist)
        //        {
        //            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
        //            ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
        //            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");
        //            TempData["error"] = "Customer does not exist.Please create customer first.";
        //            return View(VM);
        //        }
        //    }


        //    Sales model = await _salesservice.GetById(VM.Id);
        //    if (model != null)
        //    {
        //        model.CustomerId = VM.billingType == "registered" ? VM.CustomerId : null;
        //        model.MobileNo = VM.billingType == "registered" ? VM.MobileNo : null; // walk-in ka mobile optional
        //        model.Address = VM.billingType == "registered" ? VM.Address : null; // walk-in ka mobile optional
        //        model.billingType = VM.billingType == "registered" ? null : "Cash"; // walk-in ka mobile optional
        //        model.PaymentType = VM.PaymentType;

        //        //model.CustomerId = VM.CustomerId;
        //        model.BillDate = VM.BillDate;
        //        model.BillNo = VM.BillNo;
        //        //model.MobileNo = VM.MobileNo;
        //        //model.Address = VM.Address;
        //        model.PharmacyDoctorId = VM.PharmacyDoctorId;
        //        model.DoctorMobileNumber = VM.DoctorMobileNumber;
        //        model.DoctorRegNumber = VM.DoctorRegNumber;
        //        model.Total = VM.Total;
        //        model.TotalGstAmt = VM.TotalGstAmt;
        //        model.TotalPayable = VM.TotalPayable;
        //        model.discountPercent = VM.discountPercent;
        //        model.discountAmount = VM.discountAmount;
        //        model.Totaldiscount = VM.Totaldiscount;
        //        model.PaidAmount = VM.PaidAmount;
        //        model.ReturnAmount = VM.ReturnAmount;
        //        model.OfferId = VM.OfferId;
        //        model.Balance = VM.Balance;
        //        if (model.SalesItems == null)
        //            model.SalesItems = new List<SalesItem>();

        //        if (VM.SalesItemVMs == null)
        //            VM.SalesItemVMs = new List<SalesItemVM>();
        //        var removedItems = model.SalesItems.Where(dbItem => !VM.SalesItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();

        //        // ❌ Remove Deleted Items
        //        foreach (var item in removedItems)
        //        {
        //            model.SalesItems.Remove(item);
        //        }
        //        foreach (var item in VM.SalesItemVMs)
        //        {
        //            if (item.Id > 0) // Update existing items
        //            {
        //                var existingItem = model.SalesItems.FirstOrDefault(x => x.Id == item.Id);
        //                if (existingItem != null)
        //                {
        //                    existingItem.ItemMasterId = item.ItemMasterId;
        //                    existingItem.PurchaseItemId = item.PurchaseItemId;
        //                    existingItem.Batch = item.Batch;
        //                    existingItem.Expirydate = item.Expirydate;
        //                    existingItem.Mrp = item.Mrp;
        //                    existingItem.Qty = item.Qty;
        //                    existingItem.Rate = item.Rate;
        //                    existingItem.Gst = item.Gst;
        //                    existingItem.Discount = item.Discount;
        //                    existingItem.Amount = item.Amount;
        //                }
        //            }
        //            else // Add new items
        //            {
        //                model.SalesItems.Add(new SalesItem
        //                {
        //                    ItemMasterId = item.ItemMasterId,
        //                    PurchaseItemId = item.PurchaseItemId,
        //                    Batch = item.Batch,
        //                    Expirydate = item.Expirydate,
        //                    Mrp = item.Mrp,
        //                    Qty = item.Qty,
        //                    Rate = item.Rate,
        //                    Gst = item.Gst,
        //                    Discount = item.Discount,
        //                    Amount = item.Amount,
        //                });
        //            }
        //        }
        //        if (model.SalsePaymentDetails == null)
        //            model.SalsePaymentDetails = new List<SalsePaymentDetails>();

        //        if (VM.SalsePaymentDetails == null)
        //            VM.SalsePaymentDetails = new List<SalsePaymentDetailsVM>();
        //        var removedPaymentDetails = model.SalsePaymentDetails
        //           .Where(dbPayment => !VM.SalsePaymentDetails.Any(vmItem => vmItem.Id == dbPayment.Id)).ToList();
        //        foreach (var payment in removedPaymentDetails)
        //        {
        //            model.SalsePaymentDetails.Remove(payment);
        //        }
        //        foreach (var data in VM.SalsePaymentDetails)
        //        {
        //            if (data.Id > 0)
        //            {
        //                var existingPayment = model.SalsePaymentDetails.FirstOrDefault(x => x.Id == data.Id);
        //                if (existingPayment != null)
        //                {
        //                    existingPayment.PaymentModeId = data.PaymentModeId;
        //                    existingPayment.Amount = data.Amount;
        //                    existingPayment.ReferenceNo = data.ReferenceNo;
        //                    existingPayment.Description = data.Description;
        //                    //existingPayment.CustomerId = (int)VM.CustomerId;
        //                    existingPayment.CustomerId = VM.billingType == "registered" ? VM.CustomerId : null;
        //                }
        //            }
        //            else
        //            {
        //                model.SalsePaymentDetails.Add(new SalsePaymentDetails
        //                {
        //                    PaymentModeId = data.PaymentModeId,
        //                    Amount = data.Amount,
        //                    ReferenceNo = data.ReferenceNo,
        //                    Description = data.Description,
        //                    //CustomerId = (int)VM.CustomerId,
        //                    CustomerId = VM.billingType == "registered" ? VM.CustomerId : null,
        //                });
        //            }
        //        }

        //        await _salesservice.Update(model);
        //    }

        //    return RedirectToAction("Index");
        //}

        //REPORT
        [HttpGet]
        public async Task<IActionResult> SalesBook(DateTime? fromDate, DateTime? toDate, int? customerId)
        {
            var allSales = await _salesservice.GetAll();
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            allSales = allSales
                .Where(s => s.BillDate >= fromDate.Value && s.BillDate <= toDate.Value)
                .ToList();

            if (customerId.HasValue && customerId.Value > 0)
                allSales = allSales.Where(s => s.CustomerId == customerId.Value).ToList();

            allSales = allSales
                .Where(s =>
                    s.SalesItems != null &&
                    s.SalesItems.Any(si =>
                        si.ItemMaster != null &&
                        si.ItemMaster.IsActive == true))
                .ToList();

            var Data = allSales.Select(s => new SalesBookVM
            {
                Date = s.BillDate,
                InvNo = s.BillNo,
                Customer = s.Customers?.Name ?? s.MobileNo,
                TaxableAmt = s.Total,
                TaxValue = s.TotalGstAmt,
                TotalDiscount = s.Totaldiscount,
                RoundOff = s.RoundOffAmount,
                TotalAmt = s.TotalPayable
            }).ToList();

            var customers = await _customerservice.GetAll();
            ViewBag.CustomerList = new SelectList(customers, "Id", "Name", customerId);
            ViewBag.TaxableAmt = Data.Sum(x => x.TaxableAmt);
            ViewBag.TotalGst = Data.Sum(x => x.TaxValue);
            ViewBag.TotalAmt = Data.Sum(x => x.TotalAmt);
            return View(Data);
        }

        //EXPORT TO EXCEL
        [HttpGet]
        public async Task<IActionResult> ExportSalesBookExcel(DateTime? fromDate, DateTime? toDate, int? customerId)
        {
            var allSales = await _salesservice.GetAll();

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            allSales = allSales
                .Where(s => s.BillDate >= fromDate.Value && s.BillDate <= toDate.Value)
                .ToList();

            if (customerId.HasValue && customerId.Value > 0)
                allSales = allSales.Where(s => s.CustomerId == customerId.Value).ToList();

            allSales = allSales
                .Where(s => s.SalesItems != null &&
                            s.SalesItems.Any(si => si.ItemMaster != null && si.ItemMaster.IsActive))
                .ToList();

            var data = allSales.Select(s => new SalesBookVM
            {
                Date = s.BillDate,
                InvNo = s.BillNo,
                Customer = s.Customers?.Name ?? s.MobileNo,
                TaxableAmt = s.Total,
                TaxValue = s.TotalGstAmt,
                TotalAmt = s.TotalPayable
            }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Sales Book");

            ws.Cell(1, 1).Value = "Sales Book Report";
            ws.Range(1, 1, 1, 6).Merge()
                .Style.Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"From: {fromDate:dd-MM-yyyy}   To: {toDate:dd-MM-yyyy}";
            ws.Range(2, 1, 2, 6).Merge();

            int row = 4;

            string[] headers =
            {
        "Date", "Invoice No", "Customer",
        "Taxable Amount", "GST Amount", "Total Amount"
    };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(row, i + 1).Value = headers[i];

            ws.Range(row, 1, row, 6).Style.Font.SetBold();
            row++;

            foreach (var s in data)
            {
                ws.Cell(row, 1).Value = s.Date?.ToString("dd-MM-yyyy");
                ws.Cell(row, 2).Value = s.InvNo;
                ws.Cell(row, 3).Value = s.Customer;
                ws.Cell(row, 4).Value = s.TaxableAmt;
                ws.Cell(row, 5).Value = s.TaxValue;
                ws.Cell(row, 6).Value = s.TotalAmt;
                row++;
            }

            // ✅ TOTAL ROW
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 4).Value = data.Sum(x => x.TaxableAmt);
            ws.Cell(row, 5).Value = data.Sum(x => x.TaxValue);
            ws.Cell(row, 6).Value = data.Sum(x => x.TotalAmt);

            ws.Range(row, 1, row, 6).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.LightGray);

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SalesBook.xlsx"
            );
        }

        // EXPORT TO PDF
        [HttpGet]
        public async Task<IActionResult> ExportSalesBookPdf(DateTime? fromDate, DateTime? toDate, int? customerId)
        {
            var allSales = await _salesservice.GetAll();

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            allSales = allSales
                .Where(s => s.BillDate >= fromDate.Value && s.BillDate <= toDate.Value)
                .ToList();

            if (customerId.HasValue && customerId.Value > 0)
                allSales = allSales.Where(s => s.CustomerId == customerId.Value).ToList();

            allSales = allSales
                .Where(s => s.SalesItems != null &&
                            s.SalesItems.Any(si => si.ItemMaster != null && si.ItemMaster.IsActive))
                .ToList();

            var data = allSales.Select(s => new SalesBookVM
            {
                Date = s.BillDate,
                InvNo = s.BillNo,
                Customer = s.Customers?.Name ?? s.MobileNo,
                TaxableAmt = s.Total,
                TaxValue = s.TotalGstAmt,
                TotalAmt = s.TotalPayable
            }).ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            document.Add(new Paragraph("Sales Book Report")
                .SetFont(bold)
                .SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"From: {fromDate:dd-MM-yyyy}   To: {toDate:dd-MM-yyyy}")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            Table table = new Table(new float[] { 3, 3, 6, 4, 4, 4 })
                .UseAllAvailableWidth();

            string[] headers =
            {
        "Date", "Invoice No", "Customer",
        "Taxable Amt", "GST Amt", "Total Amt"
    };

            foreach (var h in headers)
            {
                table.AddHeaderCell(
                    new Cell().Add(new Paragraph(h).SetFont(bold))
                              .SetTextAlignment(TextAlignment.CENTER)
                );
            }

            foreach (var s in data)
            {
                table.AddCell(new Paragraph(s.Date?.ToString("dd-MM-yyyy") ?? "")
                    .SetFont(normal));

                table.AddCell(new Paragraph(s.InvNo ?? "")
                    .SetFont(normal));

                table.AddCell(new Paragraph(s.Customer ?? "")
                    .SetFont(normal));

                table.AddCell(new Paragraph($"{s.TaxableAmt:0.00}")
                    .SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Paragraph($"{s.TaxValue:0.00}")
                    .SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Paragraph($"{s.TotalAmt:0.00}")
                    .SetTextAlignment(TextAlignment.RIGHT));
            }

            // ✅ TOTAL ROW (FIXED)
            table.AddCell(new Cell(1, 3)
     .Add(new Paragraph("TOTAL").SetFont(bold)));

            table.AddCell(new Paragraph($"{data.Sum(x => x.TaxableAmt):0.00}")
                .SetFont(bold)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph($"{data.Sum(x => x.TaxValue):0.00}")
                .SetFont(bold)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph($"{data.Sum(x => x.TotalAmt):0.00}")
                .SetFont(bold)
                .SetTextAlignment(TextAlignment.RIGHT));


            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "SalesBook.pdf");
        }



        [HttpGet]
        public async Task<IActionResult> GetCustomer()
        {
            var customersList = await _customerservice.GetAll();

            if (customersList == null || !customersList.Any())
                return Json(new { found = false });

            var result = customersList.Select(x => new
            {
                x.Id,
                x.Name,
                x.PhoneNo,
                x.Address
            }).ToList();

            return Json(new
            {
                found = true,
                data = result
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerByMobile(string mobileNo)
        {
            var customer = await _customerservice.GetCustomerByMobileno(mobileNo);

            if (customer == null)
                return Json(new { found = false });

            return Json(new
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

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerVM Vm)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new
                {
                    success = false,
                    errors = errors
                });
            }

            var customerList = await _customerservice.GetAll();

            if (!string.IsNullOrEmpty(Vm.PhoneNo))
            {
                var isExist = customerList.Any(x =>
                    !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                    x.PhoneNo.Trim() == Vm.PhoneNo.Trim()
                );

                if (isExist)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Customer's phone number already exists."
                    });
                }
            }

            Vm.GSTType = string.IsNullOrWhiteSpace(Vm.GSTNo)
                ? GSTType.UnRegistered
                : GSTType.Registered;

            var customer = new Models.Entity.Customer
            {
                Name = Vm.Name,
                PhoneNo = Vm.PhoneNo,
                Address = Vm.Address,
                Email = Vm.Email,
                AccountGroupId = Vm.AccountGroupId,
                GSTNo = Vm.GSTNo?.ToUpper(),
                StateCode = Vm.StateCode,
                GSTType = Vm.GSTType,
                Category = Vm.Category,
                Status = Vm.Status,
                PaymentDays = Vm.PaymentDays
            };

            var result = await _customerservice.Create(customer);

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

        [HttpGet]
        public async Task<IActionResult> SalseReportItemwise(DateTime? fromDate, DateTime? toDate, int? companyId, int? divisionId, string reportType = "Summary")
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();
            var companys = await _companyService.GetAll();
            var divisions = await _divisionRepo.GetAll();

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            var details = sales.Where(s => s.SalesItems != null)
                        .SelectMany(s => s.SalesItems.Select(i =>
                        {
                            decimal qty = i.Qty;
                            int conversion = i.ItemMaster?.Conversion ?? 0;

                            string formattedQty = qty.ToString("0.##");

                            if (conversion > 0)
                            {
                                int totalTablets = (int)Math.Round(qty * conversion, MidpointRounding.AwayFromZero);
                                int strips = totalTablets / conversion;
                                int tablets = totalTablets % conversion;

                                formattedQty = $"{strips}:{tablets}";
                            }

                            return new StockVM
                            {
                                BillNo = s.BillNo,
                                BillDate = s.BillDate,
                                ItemName = i.ItemMaster?.Name,
                                Qty = qty,
                                Packing = i.ItemMaster?.Packing,
                                Conversion = conversion,
                                StockDisplay = formattedQty,
                                Mrp = i.Mrp,
                                Rate = i.Rate,
                                Amount = i.Amount
                            };
                        }))
                        .OrderBy(x => x.BillDate)
                        .ToList();


            var summary = details.GroupBy(x => x.ItemName)
                          .Select(g =>
                          {
                              decimal totalQty = g.Sum(x => x.Qty);

                              int conversion = g.First().Conversion;
                              string formattedQty = totalQty.ToString("0.##");

                              if (conversion > 0)
                              {
                                  int totalTablets = (int)Math.Round(totalQty * conversion);
                                  int strips = totalTablets / conversion;
                                  int tablets = totalTablets % conversion;

                                  formattedQty = $"{strips}:{tablets}";
                              }

                              return new StockVM
                              {
                                  ItemName = g.Key,
                                  Qty = totalQty,
                                  Packing = g.First().Packing,
                                  Conversion = conversion,
                                  StockDisplay = formattedQty,
                                  Amount = g.Sum(x => x.Amount ?? 0)
                              };
                          })
                          .ToList();


            ViewBag.ReportType = reportType;
            ViewBag.Summary = summary;
            ViewBag.CompanyList = new SelectList(companys, "Id", "Name", companyId);
            ViewBag.DivisionList = new SelectList(divisions, "Id", "Name", divisionId);
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalQty = details.Sum(x => x.Qty);
            ViewBag.TotalAmount = details.Sum(x => x.Amount ?? 0);
            return View(details);
        }

        //Export To Excel 
        [HttpGet]
        public async Task<IActionResult> ExportItemWiseSalesExcel(DateTime? fromDate, DateTime? toDate, int? companyId, int? divisionId, string reportType = "Summary")
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();
            sales = sales.Where(x => x.BillDate >= fromDate && x.BillDate <= toDate).ToList();

            var details = sales
                .Where(s => s.SalesItems != null)
                .SelectMany(s => s.SalesItems.Select(i =>
                {
                    decimal qty = i.Qty;
                    int conversion = i.ItemMaster?.Conversion ?? 0;

                    string formattedQty = qty.ToString("0.##");

                    if (conversion > 0)
                    {
                        int totalTablets = (int)Math.Round(qty * conversion, MidpointRounding.AwayFromZero);
                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;
                        formattedQty = $"{strips}:{tablets}";
                    }

                    return new StockVM
                    {
                        BillNo = s.BillNo,
                        BillDate = s.BillDate,
                        ItemName = i.ItemMaster?.Name,
                        Packing = i.ItemMaster?.Packing,
                        Qty = qty,
                        Conversion = conversion,
                        StockDisplay = formattedQty,
                        Mrp = i.Mrp,
                        Rate = i.Rate,
                        Amount = i.Amount
                    };
                }))
                .OrderBy(x => x.BillDate)
                .ToList();

            var summary = details
                .GroupBy(x => x.ItemName)
                .Select(g =>
                {
                    decimal totalQty = g.Sum(x => x.Qty);
                    int conversion = g.First().Conversion;

                    string formattedQty = totalQty.ToString("0.##");

                    if (conversion > 0)
                    {
                        int totalTablets = (int)Math.Round(totalQty * conversion);
                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;
                        formattedQty = $"{strips}:{tablets}";
                    }

                    return new StockVM
                    {
                        ItemName = g.Key,
                        Packing = g.First().Packing,
                        Qty = totalQty,
                        Conversion = conversion,
                        StockDisplay = formattedQty,
                        Amount = g.Sum(x => x.Amount ?? 0)
                    };
                }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Item Wise Sales");

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 9).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Item Wise Sales Report";
            ws.Range(2, 1, 2, 9).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 9).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;
            int srNo = 1;

            if (reportType == "Summary")
            {
                string[] headers = { "S.No", "Item Name", "Total Qty", "Packing", "Total Amount" };

                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(row, i + 1).Value = headers[i];

                ws.Range(row, 1, row, headers.Length).Style.Font.SetBold();
                row++;

                foreach (var i in summary)
                {
                    ws.Cell(row, 1).Value = srNo++;
                    ws.Cell(row, 2).Value = i.ItemName;
                    ws.Cell(row, 3).Value = i.StockDisplay;
                    ws.Cell(row, 4).Value = i.Packing;
                    ws.Cell(row, 5).Value = i.Amount;
                    row++;
                }

                ws.Cell(row, 2).Value = "TOTAL";
                ws.Cell(row, 3).Value = summary.Sum(x => x.Qty);
                ws.Cell(row, 4).Value = "";
                ws.Cell(row, 5).Value = summary.Sum(x => x.Amount ?? 0);

                ws.Range(row, 1, row, 5).Style
                    .Font.SetBold()
                    .Fill.SetBackgroundColor(XLColor.LightGray);
            }
            else
            {
                string[] headers = { "S.No", "Bill No", "Bill Date", "Item Name", "Qty", "Packing", "MRP", "Rate", "Amount" };

                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(row, i + 1).Value = headers[i];

                ws.Range(row, 1, row, headers.Length).Style.Font.SetBold();
                row++;

                int sno = 1;

                foreach (var d in details)
                {
                    ws.Cell(row, 1).Value = sno++;
                    ws.Cell(row, 2).Value = d.BillNo;
                    ws.Cell(row, 3).Value = d.BillDate?.ToString("dd-MM-yyyy");
                    ws.Cell(row, 4).Value = d.ItemName;
                    ws.Cell(row, 5).Value = d.StockDisplay;
                    ws.Cell(row, 6).Value = d.Packing;
                    ws.Cell(row, 7).Value = d.Mrp;
                    ws.Cell(row, 8).Value = d.Rate;
                    ws.Cell(row, 9).Value = d.Amount;
                    row++;
                }

                // ✅ TOTAL ROW (exact जैसा आपने दिया)
                ws.Cell(row, 2).Value = "TOTAL";
                ws.Cell(row, 5).Value = details.Sum(x => x.Qty); // 24.00
                ws.Cell(row, 9).Value = details.Sum(x => x.Amount ?? 0);

                ws.Range(row, 1, row, 9).Style
                    .Font.SetBold()
                    .Fill.SetBackgroundColor(XLColor.LightGray);
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ItemWiseSales_{reportType}.xlsx");
        }
        //Export To PDF
        [HttpGet]
        public async Task<IActionResult> ExportItemWiseSalesPdf(DateTime? fromDate, DateTime? toDate, int? companyId, int? divisionId, string reportType = "Summary")
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .OrderBy(x => x.BillDate)
                .ToList();

            var details = sales
                .Where(s => s.SalesItems != null)
                .SelectMany(s => s.SalesItems.Select(i =>
                {
                    decimal qty = i.Qty;
                    int conversion = i.ItemMaster?.Conversion ?? 0;

                    string formattedQty = qty.ToString("0.##");

                    if (conversion > 0)
                    {
                        int totalTablets = (int)Math.Round(qty * conversion, MidpointRounding.AwayFromZero);
                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;
                        formattedQty = $"{strips}:{tablets}";
                    }

                    return new StockVM
                    {
                        BillNo = s.BillNo,
                        BillDate = s.BillDate,
                        ItemName = i.ItemMaster?.Name,
                        Packing = i.ItemMaster?.Packing,
                        Qty = qty,
                        StockDisplay = formattedQty,
                        Mrp = i.Mrp,
                        Rate = i.Rate,
                        Amount = i.Amount
                    };
                }))
                .OrderBy(x => x.BillDate)
                .ToList();

            var summary = details
                .GroupBy(x => x.ItemName)
                .Select(g =>
                {
                    decimal totalQty = g.Sum(x => x.Qty);
                    int conversion = g.First().Conversion;

                    string formattedQty = totalQty.ToString("0.##");

                    if (conversion > 0)
                    {
                        int totalTablets = (int)Math.Round(totalQty * conversion);
                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;
                        formattedQty = $"{strips}:{tablets}";
                    }

                    return new StockVM
                    {
                        ItemName = g.Key,
                        Packing = g.First().Packing,
                        Qty = totalQty,
                        StockDisplay = formattedQty,
                        Amount = g.Sum(x => x.Amount ?? 0)
                    };
                })
                .OrderBy(x => x.ItemName)
                .ToList();

            using var stream = new MemoryStream();

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            var pdf = new PdfDocument(new PdfWriter(stream));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fontSize = 9;

            doc.Add(new Paragraph(companyName).SetFont(bold).SetFontSize(14).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph("Item Wise Sales Report").SetFont(bold).SetFontSize(11).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(10));

            int srNo = 1;

            // ================= SUMMARY =================
            if (reportType == "Summary")
            {
                var table = new Table(new float[] { 2, 6, 3, 3, 3 }).UseAllAvailableWidth();

                string[] headers = { "S.No", "Item Name", "Total Qty", "Packing", "Total Amount" };

                foreach (var h in headers)
                    table.AddHeaderCell(new Paragraph(h).SetFont(bold).SetFontSize(fontSize));

                foreach (var s in summary)
                {
                    table.AddCell(new Paragraph((srNo++).ToString()).SetFontSize(fontSize));
                    table.AddCell(new Paragraph(s.ItemName ?? "").SetFontSize(fontSize));
                    table.AddCell(new Paragraph(s.StockDisplay ?? "").SetFontSize(fontSize));
                    table.AddCell(new Paragraph(s.Packing ?? "").SetFontSize(fontSize));
                    table.AddCell(new Paragraph((s.Amount ?? 0).ToString("0.00")).SetFontSize(fontSize));
                }

                // TOTAL ROW
                table.AddCell("");
                table.AddCell(new Paragraph("TOTAL").SetFont(bold).SetFontSize(fontSize));
                table.AddCell(new Paragraph(summary.Sum(x => x.Qty).ToString("0.00")).SetFont(bold).SetFontSize(fontSize));
                table.AddCell("");
                table.AddCell(new Paragraph(summary.Sum(x => x.Amount ?? 0).ToString("0.00")).SetFont(bold).SetFontSize(fontSize));

                doc.Add(table);
            }
            else
            {
                // ================= DETAILS =================
                var table = new Table(new float[] { 2, 3, 3, 6, 3, 3, 2, 2, 3 }).UseAllAvailableWidth();

                string[] headers = { "S.No", "Bill No", "Bill Date", "Item Name", "Qty", "Packing", "MRP", "Rate", "Amount" };

                foreach (var h in headers)
                    table.AddHeaderCell(new Paragraph(h).SetFont(bold).SetFontSize(fontSize));

                foreach (var d in details)
                {
                    table.AddCell(new Paragraph((srNo++).ToString()).SetFontSize(fontSize));
                    table.AddCell(new Paragraph(d.BillNo ?? "").SetFontSize(fontSize));
                    table.AddCell(new Paragraph(d.BillDate?.ToString("dd-MM-yyyy") ?? "").SetFontSize(fontSize));
                    table.AddCell(new Paragraph(d.ItemName ?? "").SetFontSize(fontSize));
                    table.AddCell(new Paragraph(d.StockDisplay ?? "").SetFontSize(fontSize));
                    table.AddCell(new Paragraph(d.Packing ?? "").SetFontSize(fontSize));
                    table.AddCell(new Paragraph(d.Mrp.ToString("0.00")).SetFontSize(fontSize));
                    table.AddCell(new Paragraph(d.Rate.ToString("0.00")).SetFontSize(fontSize));
                    table.AddCell(new Paragraph((d.Amount ?? 0).ToString("0.00")).SetFontSize(fontSize));
                }

                // TOTAL ROW (EXACT FORMAT)
                table.AddCell("");
                table.AddCell(new Paragraph("TOTAL").SetFont(bold).SetFontSize(fontSize));
                table.AddCell("");
                table.AddCell("");
                table.AddCell(new Paragraph(details.Sum(x => x.Qty).ToString("0.00")).SetFont(bold).SetFontSize(fontSize));
                table.AddCell("");
                table.AddCell("");
                table.AddCell("");
                table.AddCell(new Paragraph(details.Sum(x => x.Amount ?? 0).ToString("0.00")).SetFont(bold).SetFontSize(fontSize));

                doc.Add(table);
            }

            doc.Close();

            return File(stream.ToArray(), "application/pdf", $"ItemWiseSales_{reportType}.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> ItemWiseBatchWiseSales(DateTime? fromDate, DateTime? toDate, int? companyId, int? divisionId)
        {
            var sales = await _salesservice.GetAll();
            var companies = await _companyService.GetAll();
            var divisions = await _divisionRepo.GetAll();

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            var report = sales
                .Where(s => s.SalesItems != null)
                .SelectMany(s => s.SalesItems.Select(i =>
                {
                    decimal qty = i.Qty;
                    int conversion = i.ItemMaster?.Conversion ?? 0;

                    string formattedQty = qty.ToString("0.##");

                    if (conversion > 0)
                    {
                        int totalTablets = (int)Math.Round(qty * conversion, MidpointRounding.AwayFromZero);
                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;

                        formattedQty = $"{strips}:{tablets}";
                    }

                    return new ItemBatchwiseSalesVM
                    {
                        BillNo = s.BillNo,
                        BillDate = s.BillDate,

                        ItemMasterId = i.ItemMasterId,
                        ItemName = i.ItemMaster?.Name,

                        BatchNo = i.Batch,

                        Qty = qty,
                        // Conversion = conversion,
                        StockDisplay = formattedQty,

                        Mrp = i.Mrp,
                        Rate = i.Rate,
                        Amount = i.Amount
                    };
                }))
                .AsQueryable();

            if (companyId.HasValue && companyId > 0)
                report = report.Where(x =>
                    sales.Any(s =>
                        s.BillNo == x.BillNo &&
                        s.SalesItems.Any(si =>
                            si.ItemMaster.CompanyId == companyId)));

            if (divisionId.HasValue && divisionId > 0)
                report = report.Where(x =>
                    sales.Any(s =>
                        s.BillNo == x.BillNo &&
                        s.SalesItems.Any(si =>
                            si.ItemMaster.DivisionId == divisionId)));

            var finalReport = report
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ThenBy(x => x.ItemName)
                .ThenBy(x => x.BatchNo)
                .ToList();

            ViewBag.CompanyList = new SelectList(companies, "Id", "Name", companyId);
            ViewBag.DivisionList = new SelectList(divisions, "Id", "Name", divisionId);

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            ViewBag.TotalQty = finalReport.Sum(x => x.Qty);
            ViewBag.TotalAmount = finalReport.Sum(x => x.Amount);

            return View(finalReport);
        }

        //EXPORT TO EXCEL
        [HttpGet]
        public async Task<IActionResult> ExportItemWiseBatchWiseSales(DateTime? fromDate, DateTime? toDate, int? companyId, int? divisionId)
        {
            var sales = await _salesservice.GetAll();

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            var finalReport = sales
                .Where(s => s.SalesItems != null)
                .SelectMany(s => s.SalesItems.Select(i =>
                {
                    decimal qty = i.Qty;
                    int conversion = i.ItemMaster?.Conversion ?? 0;

                    string stockDisplay = qty.ToString("0.##");

                    if (conversion > 0)
                    {
                        int totalTablets = (int)Math.Round(qty * conversion, MidpointRounding.AwayFromZero);
                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;
                        stockDisplay = $"{strips}:{tablets}";
                    }

                    return new
                    {
                        s.BillNo,
                        s.BillDate,
                        ItemName = i.ItemMaster?.Name,
                        BatchNo = i.Batch,
                        Qty = stockDisplay,
                        RawQty = qty,
                        Amount = i.Amount
                    };
                }))
                .OrderBy(x => x.BillDate)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Batchwise Sales");

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // ===== HEADER =====
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 6).Merge().Style
                .Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Item Wise Batch Wise Sales Report";
            ws.Range(2, 1, 2, 6).Merge().Style
                .Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 6).Merge().Style
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // ===== TABLE HEADER =====
            int row = 5;

            ws.Cell(row, 1).Value = "Bill No";
            ws.Cell(row, 2).Value = "Bill Date";
            ws.Cell(row, 3).Value = "Item Name";
            ws.Cell(row, 4).Value = "Batch No";
            ws.Cell(row, 5).Value = "Qty";
            ws.Cell(row, 6).Value = "Amount";

            ws.Range(row, 1, row, 6).Style.Font.SetBold();

            row++;

            foreach (var item in finalReport)
            {
                ws.Cell(row, 1).Value = item.BillNo;
                ws.Cell(row, 2).Value = item.BillDate?.ToString("dd-MMM-yy hh:mm:ss tt");
                ws.Cell(row, 3).Value = item.ItemName;
                ws.Cell(row, 4).Value = item.BatchNo;
                ws.Cell(row, 5).Value = item.Qty;
                ws.Cell(row, 6).Value = item.Amount;
                row++;
            }

            // TOTAL
            ws.Cell(row, 3).Value = "TOTAL";
            ws.Cell(row, 5).Value = finalReport.Sum(x => x.RawQty);
            ws.Cell(row, 6).Value = finalReport.Sum(x => x.Amount);

            ws.Range(row, 1, row, 6).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ItemWiseBatchWiseSales.xlsx");
        }

        // Export Item Wise Batch Wise Sales to PDF
        [HttpGet]
        public async Task<IActionResult> ExportItemWiseBatchWiseSalesPdf(DateTime? fromDate, DateTime? toDate, int? companyId, int? divisionId)
        {
            var sales = await _salesservice.GetAll();

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            var finalReport = sales
                .Where(s => s.SalesItems != null)
                .SelectMany(s => s.SalesItems.Select(i =>
                {
                    decimal qty = i.Qty;
                    int conversion = i.ItemMaster?.Conversion ?? 0;

                    string stockDisplay = qty.ToString("0.##");

                    if (conversion > 0)
                    {
                        int totalTablets = (int)Math.Round(qty * conversion, MidpointRounding.AwayFromZero);
                        int strips = totalTablets / conversion;
                        int tablets = totalTablets % conversion;
                        stockDisplay = $"{strips}:{tablets}";
                    }

                    return new
                    {
                        s.BillNo,
                        s.BillDate,
                        ItemName = i.ItemMaster?.Name,
                        BatchNo = i.Batch,
                        Qty = stockDisplay,
                        RawQty = qty,
                        Amount = i.Amount
                    };
                }))
                .OrderBy(x => x.BillDate)
                .ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fontSize = 9;

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            doc.Add(new Paragraph(companyName).SetFont(bold).SetFontSize(14).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph("Item Wise Batch Wise Sales Report").SetFont(bold).SetFontSize(11).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFontSize(9).SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(10));

            Table table = new Table(new float[] { 3, 4, 6, 3, 2, 3 }).UseAllAvailableWidth();

            string[] headers = { "Bill No", "Bill Date", "Item Name", "Batch No", "Qty", "Amount" };

            foreach (var h in headers)
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(bold).SetFontSize(fontSize)));

            foreach (var item in finalReport)
            {
                table.AddCell(new Paragraph(item.BillNo).SetFontSize(fontSize));
                table.AddCell(new Paragraph(item.BillDate?.ToString("dd-MMM-yy hh:mm:ss tt")).SetFontSize(fontSize));
                table.AddCell(new Paragraph(item.ItemName).SetFontSize(fontSize));
                table.AddCell(new Paragraph(item.BatchNo).SetFontSize(fontSize));
                table.AddCell(new Paragraph(item.Qty).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(item.Amount.ToString("0.00")).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL
            table.AddCell(new Cell(1, 4).Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(fontSize)));
            table.AddCell(new Paragraph(finalReport.Sum(x => x.RawQty).ToString("0.00")).SetFont(bold).SetFontSize(fontSize));
            table.AddCell(new Paragraph(finalReport.Sum(x => x.Amount).ToString("0.00")).SetFont(bold).SetFontSize(fontSize));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "ItemWiseBatchWiseSales.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> CustomerWiseSales(DateTime? fromDate, DateTime? toDate, int? customerId, string reportType = "Summary")
        {
            ViewBag.CustomerList = new SelectList(
                await _customerservice.GetAll(),
                "Id",
                "Name",
                customerId
            );

            var sales = await _salesservice.GetAll();

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }
            if (fromDate.HasValue)
                sales = sales.Where(x => x.BillDate >= fromDate.Value).ToList();

            if (toDate.HasValue)
                sales = sales.Where(x => x.BillDate <= toDate.Value).ToList();

            if (customerId.HasValue && customerId > 0)
                sales = sales.Where(x => x.CustomerId == customerId).ToList();

            var vm = new CustomerWiseSalesReportVM
            {
                ReportType = reportType,
                Summary = new List<CustomerWiseSalesSummaryVM>(),
                Details = new List<CustomerWiseSalesDetailsVM>()
            };

            if (reportType == "Summary")
            {
                vm.Summary = sales
                    .GroupBy(x => new
                    {
                        x.CustomerId,
                        CustomerName = x.Customers != null ? x.Customers.Name : "Walk-in"
                    })
                    .Select(g => new CustomerWiseSalesSummaryVM
                    {
                        CustomerId = g.Key.CustomerId ?? 0,
                        CustomerName = g.Key.CustomerName,
                        TotalSales = g.Sum(x => x.TotalPayable)
                    })
                    .OrderBy(x => x.CustomerName)
                    .ToList();
            }
            if (reportType == "Details")
            {
                vm.Details = sales
                    .Select(x => new CustomerWiseSalesDetailsVM
                    {
                        BillNo = x.BillNo,
                        BillDate = x.BillDate,
                        CustomerName = x.Customers != null ? x.Customers.Name : "Walk-in",
                        BillValue = x.TotalPayable
                    })
                    .OrderBy(x => x.BillDate)
                    .ToList();
            }

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(vm);
        }

        //Export to Excel
        [HttpGet]
        public async Task<IActionResult> ExportCustomerWiseSalesExcel(DateTime? fromDate, DateTime? toDate, int? customerId, string reportType = "Summary")
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            if (customerId.HasValue && customerId > 0)
                sales = sales.Where(x => x.CustomerId == customerId).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Customer Wise Sales");

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 4).Merge().Style
                .Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Customer Wise Sales Report";
            ws.Range(2, 1, 2, 4).Merge().Style
                .Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 4).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            if (reportType == "Summary")
            {
                ws.Cell(row, 1).Value = "Customer";
                ws.Cell(row, 2).Value = "Total Sales";

                ws.Range(row, 1, row, 2).Style.Font.SetBold();
                row++;

                var summary = sales
                    .GroupBy(x => new
                    {
                        CustomerName = x.Customers != null ? x.Customers.Name : "Walk-in"
                    })
                    .Select(g => new
                    {
                        g.Key.CustomerName,
                        TotalSales = g.Sum(x => x.TotalPayable)
                    })
                    .OrderBy(x => x.CustomerName)
                    .ToList();

                decimal total = 0;

                foreach (var item in summary)
                {
                    ws.Cell(row, 1).Value = item.CustomerName;
                    ws.Cell(row, 2).Value = item.TotalSales;
                    ws.Cell(row, 2).Style.NumberFormat.Format = "0.00";
                    total += item.TotalSales;
                    row++;
                }

                // TOTAL
                ws.Cell(row, 1).Value = "TOTAL";
                ws.Cell(row, 1).Style.Font.Bold = true;

                ws.Cell(row, 2).Value = total;
                ws.Cell(row, 2).Style.NumberFormat.Format = "0.00";
                ws.Cell(row, 2).Style.Font.Bold = true;
            }
            else
            {
                ws.Cell(row, 1).Value = "Bill No";
                ws.Cell(row, 2).Value = "Bill Date";
                ws.Cell(row, 3).Value = "Customer";
                ws.Cell(row, 4).Value = "Amount";

                ws.Range(row, 1, row, 4).Style.Font.SetBold();
                row++;

                decimal total = 0;

                foreach (var s in sales.OrderBy(x => x.BillDate))
                {
                    ws.Cell(row, 1).Value = s.BillNo;
                    ws.Cell(row, 2).Value = s.BillDate?.ToString("dd-MM-yyyy");
                    ws.Cell(row, 3).Value = s.Customers != null ? s.Customers.Name : "Walk-in";
                    ws.Cell(row, 4).Value = s.TotalPayable;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "0.00";

                    total += s.TotalPayable;
                    row++;
                }

                ws.Cell(row, 3).Value = "TOTAL";
                ws.Cell(row, 3).Style.Font.Bold = true;

                ws.Cell(row, 4).Value = total;
                ws.Cell(row, 4).Style.NumberFormat.Format = "0.00";
                ws.Cell(row, 4).Style.Font.Bold = true;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CustomerWiseSales_{reportType}.xlsx");
        }
        //Export to PDF 
        [HttpGet]
        public async Task<IActionResult> ExportCustomerWiseSalesPdf(DateTime? fromDate, DateTime? toDate, int? customerId, string reportType = "Summary")
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            if (customerId.HasValue && customerId > 0)
                sales = sales.Where(x => x.CustomerId == customerId).ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fontSize = 9;

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            document.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Customer Wise Sales Report")
                .SetFont(bold).SetFontSize(13)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(fontSize)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            if (reportType == "Summary")
            {
                Table table = new Table(new float[] { 6, 4 }).UseAllAvailableWidth();

                table.AddHeaderCell(new Cell().Add(new Paragraph("Customer").SetFont(bold).SetFontSize(fontSize)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Total Sales").SetFont(bold).SetFontSize(fontSize))
                    .SetTextAlignment(TextAlignment.RIGHT));

                decimal total = 0;

                var summary = sales
                    .GroupBy(x => new
                    {
                        CustomerName = x.Customers != null ? x.Customers.Name : "Walk-in"
                    })
                    .Select(g => new
                    {
                        g.Key.CustomerName,
                        TotalSales = g.Sum(x => x.TotalPayable)
                    })
                    .OrderBy(x => x.CustomerName)
                    .ToList();

                foreach (var item in summary)
                {
                    table.AddCell(new Cell().Add(new Paragraph(item.CustomerName).SetFont(normal).SetFontSize(fontSize)));
                    table.AddCell(new Cell().Add(new Paragraph(item.TotalSales.ToString("0.00")).SetFont(normal).SetFontSize(fontSize))
                        .SetTextAlignment(TextAlignment.RIGHT));

                    total += item.TotalSales;
                }

                // TOTAL
                table.AddCell(new Cell().Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(fontSize))
                    .SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Cell().Add(new Paragraph(total.ToString("0.00")).SetFont(bold).SetFontSize(fontSize))
                    .SetTextAlignment(TextAlignment.RIGHT));

                document.Add(table);
            }
            else
            {
                Table table = new Table(new float[] { 3, 3, 6, 3 }).UseAllAvailableWidth();

                table.AddHeaderCell(new Cell().Add(new Paragraph("Bill No").SetFont(bold).SetFontSize(fontSize)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Bill Date").SetFont(bold).SetFontSize(fontSize)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Customer").SetFont(bold).SetFontSize(fontSize)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Amount").SetFont(bold).SetFontSize(fontSize))
                    .SetTextAlignment(TextAlignment.RIGHT));

                decimal total = 0;

                foreach (var s in sales.OrderBy(x => x.BillDate))
                {
                    table.AddCell(new Cell().Add(new Paragraph(s.BillNo).SetFont(normal).SetFontSize(fontSize)));
                    table.AddCell(new Cell().Add(new Paragraph(s.BillDate?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fontSize)));
                    table.AddCell(new Cell().Add(new Paragraph(s.Customers != null ? s.Customers.Name : "Walk-in").SetFont(normal).SetFontSize(fontSize)));

                    table.AddCell(new Cell().Add(new Paragraph(s.TotalPayable.ToString("0.00")).SetFont(normal).SetFontSize(fontSize))
                        .SetTextAlignment(TextAlignment.RIGHT));

                    total += s.TotalPayable;
                }

                // TOTAL
                table.AddCell(new Cell(1, 3).Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(fontSize))
                    .SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Cell().Add(new Paragraph(total.ToString("0.00")).SetFont(bold).SetFontSize(fontSize))
                    .SetTextAlignment(TextAlignment.RIGHT));

                document.Add(table);
            }

            document.Close();

            return File(stream.ToArray(),
                "application/pdf",
                $"CustomerWiseSales_{reportType}.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> DateWiseSalesReport(DateTime? fromDate, DateTime? toDate)
        {
            var sales = await _salesservice.GetAll();

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            sales = sales
                .Where(x => x.BillDate.HasValue &&
                            x.BillDate.Value.Date >= fromDate.Value.Date &&
                            x.BillDate.Value.Date <= toDate.Value.Date)
                .ToList();

            var report = sales
                .Where(x => x.BillDate.HasValue)
                .GroupBy(x => x.BillDate.Value.Date)
                .Select(g => new DateWiseSalesVM
                {
                    Date = g.Key,
                    NoOfBills = g.Count(),
                    TaxableAmount = g.Sum(x => x.Total),

                    // ✅ GST ke sath Cess bhi add ho kar aayega
                    GstAmount = g.Sum(x => x.TotalGstAmt + (x.TotalCessAmount)),

                    TotalDiscount = g.Sum(x => x.Totaldiscount),
                    RoundOff = g.Sum(x => x.RoundOffAmount),
                    TotalValue = g.Sum(x => x.TotalPayable)
                })
                .OrderBy(x => x.Date)
                .ToList();

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(report);
        }
        //Export to Excel 
        [HttpGet]
        public async Task<IActionResult> ExportDateWiseSalesExcel(DateTime? fromDate, DateTime? toDate)
        {
            var sales = await _salesservice.GetAll();

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            sales = sales
                .Where(x => x.BillDate.HasValue &&
                            x.BillDate.Value.Date >= fromDate.Value.Date &&
                            x.BillDate.Value.Date <= toDate.Value.Date)
                .ToList();

            var report = sales
                .GroupBy(x => x.BillDate.Value.Date)
                .Select(g => new DateWiseSalesVM
                {
                    Date = g.Key,
                    NoOfBills = g.Count(),
                    TaxableAmount = g.Sum(x => x.Total),
                    GstAmount = g.Sum(x => x.TotalGstAmt + x.TotalCessAmount),
                    TotalDiscount = g.Sum(x => x.Totaldiscount),
                    RoundOff = g.Sum(x => x.RoundOffAmount),
                    TotalValue = g.Sum(x => x.TotalPayable)
                })
                .OrderBy(x => x.Date)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("DateWise Sales");

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 8).Merge().Style
                .Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Date Wise Sales Report";
            ws.Range(2, 1, 2, 8).Merge().Style
                .Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 8).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TABLE HEADER
            ws.Cell(5, 1).Value = "S.No";
            ws.Cell(5, 2).Value = "Date";
            ws.Cell(5, 3).Value = "No Of Bills";
            ws.Cell(5, 4).Value = "Taxable Amount";
            ws.Cell(5, 5).Value = "GST Amount";
            ws.Cell(5, 6).Value = "Discount";
            ws.Cell(5, 7).Value = "Round Off";
            ws.Cell(5, 8).Value = "Total Value";

            ws.Range("A5:H5").Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.LightGray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 6;
            int sno = 1;

            foreach (var item in report)
            {
                ws.Cell(row, 1).Value = sno++;
                ws.Cell(row, 2).Value = item.Date.ToString("dd-MM-yyyy");
                ws.Cell(row, 3).Value = item.NoOfBills;

                ws.Cell(row, 4).Value = item.TaxableAmount;
                ws.Cell(row, 5).Value = item.GstAmount;
                ws.Cell(row, 6).Value = item.TotalDiscount;
                ws.Cell(row, 7).Value = item.RoundOff;
                ws.Cell(row, 8).Value = item.TotalValue;

                ws.Range(row, 4, row, 8).Style.NumberFormat.Format = "0.00";

                row++;
            }

            // TOTAL ROW
            ws.Cell(row, 3).Value = "TOTAL";
            ws.Cell(row, 3).Style.Font.Bold = true;

            ws.Cell(row, 4).Value = report.Sum(x => x.TaxableAmount);
            ws.Cell(row, 5).Value = report.Sum(x => x.GstAmount);
            ws.Cell(row, 6).Value = report.Sum(x => x.TotalDiscount);
            ws.Cell(row, 7).Value = report.Sum(x => x.RoundOff);
            ws.Cell(row, 8).Value = report.Sum(x => x.TotalValue);

            ws.Range(row, 4, row, 8).Style
                .NumberFormat.Format = "0.00";
            ws.Range(row, 4, row, 8).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "DateWiseSalesReport.xlsx");
        }

        //Export TO PDF
        [HttpGet]
        public async Task<IActionResult> ExportDateWiseSalesPdf(DateTime? fromDate, DateTime? toDate)
        {
            var sales = await _salesservice.GetAll();

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            sales = sales
                .Where(x => x.BillDate.HasValue &&
                            x.BillDate.Value.Date >= fromDate.Value.Date &&
                            x.BillDate.Value.Date <= toDate.Value.Date)
                .ToList();

            var report = sales
                .GroupBy(x => x.BillDate.Value.Date)
                .Select(g => new DateWiseSalesVM
                {
                    Date = g.Key,
                    NoOfBills = g.Count(),
                    TaxableAmount = g.Sum(x => x.Total),
                    GstAmount = g.Sum(x => x.TotalGstAmt + x.TotalCessAmount),
                    TotalDiscount = g.Sum(x => x.Totaldiscount),
                    RoundOff = g.Sum(x => x.RoundOffAmount),
                    TotalValue = g.Sum(x => x.TotalPayable)
                })
                .OrderBy(x => x.Date)
                .ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fontSize = 9;

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            document.Add(new Paragraph(companyName)
                .SetFont(boldFont).SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Date Wise Sales Report")
                .SetFont(boldFont).SetFontSize(13)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normalFont).SetFontSize(fontSize)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            Table table = new Table(new float[] { 1, 3, 2, 3, 3, 3, 3, 3 })
                .UseAllAvailableWidth();

            // HEADER
            string[] headers = { "S.No", "Date", "Bills", "Taxable", "GST", "Discount", "RoundOff", "Total" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(boldFont).SetFontSize(fontSize))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            int sno = 1;

            foreach (var item in report)
            {
                table.AddCell(new Cell().Add(new Paragraph(sno++.ToString()).SetFont(normalFont).SetFontSize(fontSize)));
                table.AddCell(new Cell().Add(new Paragraph(item.Date.ToString("dd-MM-yyyy")).SetFont(normalFont).SetFontSize(fontSize)));
                table.AddCell(new Cell().Add(new Paragraph(item.NoOfBills.ToString()).SetFont(normalFont).SetFontSize(fontSize)));

                table.AddCell(new Cell().Add(new Paragraph(item.TaxableAmount.ToString("0.00")).SetFont(normalFont).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Cell().Add(new Paragraph(item.GstAmount.ToString("0.00")).SetFont(normalFont).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Cell().Add(new Paragraph(item.TotalDiscount.ToString("0.00")).SetFont(normalFont).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Cell().Add(new Paragraph(item.RoundOff.ToString("0.00")).SetFont(normalFont).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Cell().Add(new Paragraph(item.TotalValue.ToString("0.00")).SetFont(normalFont).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL
            table.AddCell(new Cell(1, 3)
                .Add(new Paragraph("TOTAL").SetFont(boldFont).SetFontSize(fontSize))
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Cell().Add(new Paragraph(report.Sum(x => x.TaxableAmount).ToString("0.00")).SetFont(boldFont).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Cell().Add(new Paragraph(report.Sum(x => x.GstAmount).ToString("0.00")).SetFont(boldFont).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Cell().Add(new Paragraph(report.Sum(x => x.TotalDiscount).ToString("0.00")).SetFont(boldFont).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Cell().Add(new Paragraph(report.Sum(x => x.RoundOff).ToString("0.00")).SetFont(boldFont).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Cell().Add(new Paragraph(report.Sum(x => x.TotalValue).ToString("0.00")).SetFont(boldFont).SetFontSize(fontSize)).SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(),
                "application/pdf",
                "DateWiseSalesReport.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> SalseReportBillwise(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();

            var billwiseList = sales
                .Where(s => s.BillDate.HasValue &&
                            s.BillDate.Value.Date >= fromDate.Value.Date &&
                            s.BillDate.Value.Date <= toDate.Value.Date)
                .Select(sale => new SalesVM
                {
                    Id = sale.Id,
                    BillNo = sale.BillNo,
                    BillDate = sale.BillDate,
                    CustomerName = sale.billingType == "Cash" ? "Cash" : sale.Customers?.Name ?? "",
                    MobileNo = sale.MobileNo,
                    TotalGstAmt = sale.TotalGstAmt + (sale.TotalCessAmount),
                    TotalAmount = sale.TotalPayable,
                    HasNarcoticItem = sale.SalesItems
            .Any(d => d.ItemMaster != null && d.ItemMaster.Narcotics)
                })
                .Where(x => x.TotalAmount > 0)
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            // Calculate totals
            ViewBag.TotalGst = billwiseList.Sum(x => x.TotalGstAmt);
            ViewBag.TotalAmount = billwiseList.Sum(x => x.TotalAmount);

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            return View(billwiseList);
        }
        //Export Billwise to Excel
        [HttpGet]
        public async Task<IActionResult> ExportBillwiseExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();

            var billwiseList = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .Select(sale => new SalesVM
                {
                    BillNo = sale.BillNo,
                    BillDate = sale.BillDate,
                    CustomerName = sale.billingType == "Cash" ? "Cash" : sale.Customers?.Name ?? "Cash",
                    TotalGstAmt = sale.TotalGstAmt,
                    TotalAmount = sale.TotalPayable
                })
                .Where(x => x.TotalAmount > 0)
                .OrderBy(x => x.BillDate)
                .ToList();

            using var workbook = new XLWorkbook();
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }
            var ws = workbook.Worksheets.Add("Billwise Sales");

            // ===== REPORT HEADER =====
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 5).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Billwise Sales Report";
            ws.Range(2, 1, 2, 5).Merge().Style
                .Font.SetBold()
                .Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 5).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // ===== TABLE HEADER =====
            ws.Cell(5, 1).Value = "Bill No";
            ws.Cell(5, 2).Value = "Bill Date";
            ws.Cell(5, 3).Value = "Customer";
            ws.Cell(5, 4).Value = "GST Amount";
            ws.Cell(5, 5).Value = "Total Amount";

            ws.Range("A5:E5").Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.LightGray)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 6;
            foreach (var item in billwiseList)
            {
                ws.Cell(row, 1).Value = item.BillNo;
                ws.Cell(row, 2).Value = item.BillDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 3).Value = item.CustomerName;
                ws.Cell(row, 4).Value = item.TotalGstAmt;
                ws.Cell(row, 4).Style.NumberFormat.Format = "0.00"; // ✅ 2 decimal places
                ws.Cell(row, 5).Value = item.TotalAmount;
                ws.Cell(row, 5).Style.NumberFormat.Format = "0.00"; // ✅ 2 decimal places
                row++;
            }

            // Grand Total row
            ws.Cell(row, 3).Value = "TOTAL";
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 4).Value = billwiseList.Sum(x => x.TotalGstAmt);
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Cell(row, 5).Value = billwiseList.Sum(x => x.TotalAmount);
            ws.Cell(row, 5).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 5).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BillwiseSalesReport.xlsx"
            );
        }

        // Export Billwise to PDF
        [HttpGet]
        public async Task<IActionResult> ExportBillwisePdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();

            var billwiseList = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .Select(sale => new SalesVM
                {
                    BillNo = sale.BillNo,
                    BillDate = sale.BillDate,

                    // ✅ FIXED CUSTOMER
                    CustomerName = sale.billingType == "Cash"
                        ? "Cash"
                        : sale.Customers?.Name ?? "Cash",

                    TotalGstAmt = sale.TotalGstAmt,
                    TotalAmount = sale.TotalPayable
                })
                .Where(x => x.TotalAmount > 0)
                .OrderBy(x => x.BillDate)
                .ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // ✅ SMALL FONT SAME AS ITEMWISE
            float fontSize = 9;

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            document.Add(new Paragraph(companyName)
                .SetFont(boldFont).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Billwise Sales Report")
                .SetFont(boldFont).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normalFont).SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // TABLE
            Table table = new Table(new float[] { 4, 3, 6, 3, 3 }).UseAllAvailableWidth();

            // HEADER
            string[] headers = { "Bill No", "Bill Date", "Customer", "GST Amount", "Total Amount" };

            foreach (var h in headers)
                table.AddHeaderCell(new Cell().Add(new Paragraph(h).SetFont(boldFont).SetFontSize(fontSize)));

            // ROWS
            foreach (var item in billwiseList)
            {
                table.AddCell(new Cell().Add(new Paragraph(item.BillNo).SetFontSize(fontSize)));
                table.AddCell(new Cell().Add(new Paragraph(item.BillDate?.ToString("dd-MM-yyyy") ?? "").SetFontSize(fontSize)));
                table.AddCell(new Cell().Add(new Paragraph(item.CustomerName).SetFontSize(fontSize)));

                table.AddCell(new Cell().Add(new Paragraph(item.TotalGstAmt.ToString("0.00")).SetFontSize(fontSize))
                    .SetTextAlignment(TextAlignment.RIGHT));

                table.AddCell(new Cell().Add(new Paragraph((item.TotalAmount ?? 0).ToString("0.00")).SetFontSize(fontSize))
                    .SetTextAlignment(TextAlignment.RIGHT));
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 3)
                .Add(new Paragraph("TOTAL").SetFont(boldFont).SetFontSize(fontSize))
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Cell()
                .Add(new Paragraph(billwiseList.Sum(x => x.TotalGstAmt).ToString("0.00"))
                .SetFont(boldFont).SetFontSize(fontSize))
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Cell()
                .Add(new Paragraph(billwiseList.Sum(x => x.TotalAmount ?? 0).ToString("0.00"))
                .SetFont(boldFont).SetFontSize(fontSize))
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "BillwiseSalesReport.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> BillwiseProfitReport(DateTime? fromDate, DateTime? toDate, string gstMode = "with")
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();
            var purchase = await _purchaseservice.GetAll();

            // Latest purchase cost per item
            var latestPurchaseCost = purchase
                .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                .GroupBy(pi => pi.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    CostRate = g.OrderByDescending(x => x.Id).First().BatchWiseCose
                })
                .ToDictionary(x => x.ItemId, x => x.CostRate);

            // Date filter
            var filteredSales = sales
                .Where(s => s.Deleted == null
                         && s.BillDate.HasValue
                         && s.BillDate.Value.Date >= fromDate.Value.Date
                         && s.BillDate.Value.Date <= toDate.Value.Date)
                .ToList();

            // Bill wise profit calculate karo
            //var billwiseList = filteredSales
            //    .Select(sale => {
            //        var items = sale.SalesItems ?? new List<SalesItem>();

            //        var totalSalesValue = items.Sum(si => si.Qty * si.Rate);
            //        var totalCostValue = items.Sum(si =>
            //            si.Qty * (latestPurchaseCost.ContainsKey(si.ItemMasterId)
            //                ? latestPurchaseCost[si.ItemMasterId] : 0));

            //        var grossProfit = totalSalesValue - totalCostValue;
            //        var profitPct = totalSalesValue > 0
            //            ? Math.Round((grossProfit / totalSalesValue) * 100, 2) : 0;

            //        // GST Mode apply karo
            //        decimal displaySales;
            //        decimal displayCost = totalCostValue;
            //        decimal displayProfit;

            //        if (gstMode == "without")
            //        {
            //            // Without GST — GST amount minus kar do sales se
            //            displaySales = totalSalesValue - sale.TotalGstAmt;
            //            displayProfit = displaySales - displayCost;
            //        }
            //        else
            //        {
            //            // With GST — as it is
            //            displaySales = totalSalesValue;
            //            displayProfit = grossProfit;
            //        }

            //        var displayProfitPct = displaySales > 0
            //            ? Math.Round((displayProfit / displaySales) * 100, 2) : 0;

            //        return new BillwiseProfitVM
            //        {
            //            Id = sale.Id,
            //            BillNo = sale.BillNo,
            //            BillDate = sale.BillDate,
            //            CustomerName = sale.Customers?.Name ?? "Walk-in Customer",
            //            MobileNo = sale.MobileNo,
            //            TotalSalesValue = displaySales,
            //            TotalCostValue = displayCost,
            //            TotalGstAmt = sale.TotalGstAmt,
            //            BillAmount = sale.TotalPayable,
            //            Discount = sale.Totaldiscount,
            //            GrossProfit = displayProfit,
            //            ProfitPct = displayProfitPct,
            //            TotalItems = items.Count(),
            //            TotalQty = items.Sum(x => x.Qty)
            //        };
            //    })
            //    .Where(x => x.BillAmount > 0)
            //    .OrderByDescending(x => x.BillDate)
            //    .ToList();



            //var billwiseList = filteredSales
            //.Select(sale => {
            //    var items = sale.SalesItems ?? new List<SalesItem>();

            //    // Base sales value (without GST)
            //    var totalSalesValue = items.Sum(si => (si.Qty * si.ItemMaster?.Conversion ?? 0) * si.Rate );

            //    var totalCostValue = items.Sum(si =>
            //        si.Qty * (latestPurchaseCost.ContainsKey(si.ItemMasterId)
            //            ? latestPurchaseCost[si.ItemMasterId] : 0));

            //    decimal displaySales;
            //    decimal displayCost = totalCostValue;
            //    decimal displayProfit;

            //    if (gstMode == "without")
            //    {
            //        // Without GST — sirf item value
            //        displaySales = totalSalesValue;
            //        displayProfit = displaySales - displayCost;
            //    }
            //    else
            //    {
            //        // With GST — item value + bill GST
            //        displaySales = totalSalesValue + sale.TotalGstAmt; // ✅
            //        displayProfit = displaySales - displayCost;
            //    }

            //    var displayProfitPct = displaySales > 0
            //        ? Math.Round((displayProfit / displaySales) * 100, 2) : 0;

            //    return new BillwiseProfitVM
            //    {
            //        Id = sale.Id,
            //        BillNo = sale.BillNo,
            //        BillDate = sale.BillDate,
            //        CustomerName = sale.Customers?.Name ?? "Walk-in Customer",
            //        MobileNo = sale.MobileNo,
            //        TotalSalesValue = displaySales,
            //        TotalCostValue = displayCost,
            //        TotalGstAmt = sale.TotalGstAmt,
            //        BillAmount = sale.TotalPayable,
            //        Discount = sale.Totaldiscount,
            //        GrossProfit = displayProfit,
            //        ProfitPct = displayProfitPct,
            //        TotalItems = items.Count(),
            //        TotalQty = items.Sum(x => x.Qty)
            //    };
            //})

            var billwiseList = filteredSales
            .Select(sale =>
            {
                var items = sale.SalesItems ?? new List<SalesItem>();

                decimal totalSalesValue = 0;
                decimal totalCostValue = 0;

                foreach (var si in items)
                {
                    var conversion = si.ItemMaster.Conversion == 0 ? 1 : si.ItemMaster.Conversion;

                    // 🔹 Sales value (sale unit me hi hota hai)
                    var salesQtyBase = si.Qty * conversion;
                    var salesAmount = salesQtyBase * si.Rate;

                    totalSalesValue += salesAmount;

                    // 🔹 Cost calculate karo
                    if (latestPurchaseCost.ContainsKey(si.ItemMasterId))
                    {
                        var purchaseRate = latestPurchaseCost[si.ItemMasterId];

                        // Purchase rate per base unit nikalo
                        var costPerBaseUnit = purchaseRate / conversion;

                        var costAmount = salesQtyBase * costPerBaseUnit;

                        totalCostValue += costAmount;
                    }
                }

                decimal displaySales;
                decimal displayCost = totalCostValue;
                decimal displayProfit;

                if (gstMode == "without")
                {
                    displaySales = totalSalesValue;
                }
                else
                {
                    displaySales = totalSalesValue + sale.TotalGstAmt;
                }

                displayProfit = displaySales - displayCost;

                var displayProfitPct = displaySales > 0
                    ? Math.Round((displayProfit / displaySales) * 100, 2)
                    : 0;

                return new BillwiseProfitVM
                {
                    Id = sale.Id,
                    BillNo = sale.BillNo,
                    BillDate = sale.BillDate,
                    CustomerName = sale.Customers?.Name ?? "Walk-in Customer",
                    MobileNo = sale.MobileNo,
                    TotalSalesValue = displaySales,
                    TotalCostValue = displayCost,
                    TotalGstAmt = sale.TotalGstAmt,
                    BillAmount = sale.TotalPayable,
                    Discount = sale.Totaldiscount,
                    GrossProfit = displayProfit,
                    ProfitPct = displayProfitPct,
                    TotalItems = items.Count(),
                    TotalQty = items.Sum(x => x.Qty)
                };
            })
            .Where(x => x.BillAmount > 0)
            .OrderBy(x => x.BillDate)
            .ToList();

            // ViewBag totals
            ViewBag.TotalSalesValue = billwiseList.Sum(x => x.TotalSalesValue);
            ViewBag.TotalCostValue = billwiseList.Sum(x => x.TotalCostValue);
            ViewBag.TotalProfit = billwiseList.Sum(x => x.GrossProfit);
            ViewBag.TotalGst = billwiseList.Sum(x => x.TotalGstAmt);
            ViewBag.TotalAmount = billwiseList.Sum(x => x.BillAmount);
            ViewBag.TotalDiscount = billwiseList.Sum(x => x.Discount);
            ViewBag.AvgProfitPct = billwiseList.Count() > 0
                ? Math.Round(billwiseList.Average(x => x.ProfitPct), 2) : 0;

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            ViewBag.GstMode = gstMode;

            return View(billwiseList);
        }

        [HttpGet]
        public async Task<IActionResult> BillwiseProfitReportExcel(DateTime? fromDate, DateTime? toDate, string gstMode = "with")
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();
            var purchase = await _purchaseservice.GetAll();

            var latestPurchaseCost = purchase
                .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                .GroupBy(pi => pi.ItemId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First().BatchWiseCose);

            var data = sales.Where(s => s.Deleted == null && s.BillDate >= fromDate && s.BillDate <= toDate).ToList();

            var list = data.Select(sale =>
            {
                var items = sale.SalesItems ?? new List<SalesItem>();

                decimal salesVal = 0, costVal = 0;

                foreach (var si in items)
                {
                    var conv = si.ItemMaster.Conversion == 0 ? 1 : si.ItemMaster.Conversion;

                    var qtyBase = si.Qty * conv;
                    salesVal += qtyBase * si.Rate;

                    if (latestPurchaseCost.ContainsKey(si.ItemMasterId))
                    {
                        var cost = latestPurchaseCost[si.ItemMasterId] / conv;
                        costVal += qtyBase * cost;
                    }
                }

                var displaySales = gstMode == "without" ? salesVal : salesVal + sale.TotalGstAmt;
                var profit = displaySales - costVal;

                return new
                {
                    sale.BillNo,
                    sale.BillDate,
                    Customer = sale.Customers?.Name ?? "Walk-in Customer",
                    Mobile = sale.MobileNo,
                    Cost = costVal,
                    Bill = sale.TotalPayable,
                    Profit = profit,
                    ProfitPct = displaySales > 0 ? Math.Round((profit / displaySales) * 100, 2) : 0,
                    Items = items.Count(),
                    Qty = items.Sum(x => x.Qty)
                };
            }).ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Profit Report");

            // Header
            ws.Cell(1, 1).Value = "Billwise Profit Report";
            ws.Range(1, 1, 1, 11).Merge().Style.Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(2, 1, 2, 11).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Table Header
            string[] headers = { "SNo", "Bill No", "Bill Date", "Customer", "Mobile", "Cost Value", "Bill Amount", "Gross Profit", "Profit %", "Items", "Qty" };

            int row = 4;
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
                ws.Cell(row, i + 1).Style.Font.Bold = true;
            }

            row++;
            int sno = 1;

            decimal tCost = 0, tBill = 0, tProfit = 0;

            foreach (var x in list)
            {
                ws.Cell(row, 1).Value = sno++;
                ws.Cell(row, 2).Value = x.BillNo;
                ws.Cell(row, 3).Value = x.BillDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 4).Value = x.Customer;
                ws.Cell(row, 5).Value = x.Mobile;

                ws.Cell(row, 6).Value = x.Cost;
                ws.Cell(row, 7).Value = x.Bill;
                ws.Cell(row, 8).Value = x.Profit;
                ws.Cell(row, 9).Value = x.ProfitPct;
                ws.Cell(row, 10).Value = x.Items;
                ws.Cell(row, 11).Value = x.Qty;

                ws.Range(row, 6, row, 8).Style.NumberFormat.Format = "₹ #,##0.00";
                ws.Cell(row, 9).Style.NumberFormat.Format = "0.00";

                tCost += x.Cost;
                tBill += x.Bill;
                tProfit += x.Profit;

                row++;
            }

            // TOTAL
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 1).Style.Font.Bold = true;

            ws.Cell(row, 6).Value = tCost;
            ws.Cell(row, 7).Value = tBill;
            ws.Cell(row, 8).Value = tProfit;
            ws.Cell(row, 9).Value = list.Count > 0 ? Math.Round(list.Average(x => x.ProfitPct), 2) : 0;

            ws.Range(row, 6, row, 8).Style.NumberFormat.Format = "₹ #,##0.00";
            ws.Range(row, 1, row, 11).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BillwiseProfitReport.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> BillwiseProfitReportPdf(DateTime? fromDate, DateTime? toDate, string gstMode = "with")
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();
            var purchase = await _purchaseservice.GetAll();

            var latestPurchaseCost = purchase
                .SelectMany(p => p.PurchaseItems ?? new List<PurchaseItem>())
                .GroupBy(pi => pi.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    CostRate = g.OrderByDescending(x => x.Id).First().BatchWiseCose
                })
                .ToDictionary(x => x.ItemId, x => x.CostRate);

            var filteredSales = sales
                .Where(s => s.Deleted == null
                         && s.BillDate.HasValue
                         && s.BillDate.Value.Date >= fromDate.Value.Date
                         && s.BillDate.Value.Date <= toDate.Value.Date)
                .ToList();

            int sr = 1;

            var list = filteredSales.Select(sale =>
            {
                var items = sale.SalesItems ?? new List<SalesItem>();

                decimal totalSales = 0;
                decimal totalCost = 0;

                foreach (var si in items)
                {
                    var conversion = si.ItemMaster?.Conversion == 0 ? 1 : si.ItemMaster?.Conversion ?? 1;

                    var qtyBase = si.Qty * conversion;
                    var salesAmt = qtyBase * si.Rate;

                    totalSales += salesAmt;

                    if (latestPurchaseCost.ContainsKey(si.ItemMasterId))
                    {
                        var purchaseRate = latestPurchaseCost[si.ItemMasterId];
                        var costPerBase = purchaseRate / conversion;
                        totalCost += qtyBase * costPerBase;
                    }
                }

                decimal displaySales = gstMode == "without"
                    ? totalSales
                    : totalSales + sale.TotalGstAmt;

                decimal profit = displaySales - totalCost;

                decimal profitPct = displaySales > 0
                    ? Math.Round((profit / displaySales) * 100, 2)
                    : 0;

                return new
                {
                    Sr = sr++,
                    sale.BillNo,
                    sale.BillDate,
                    Customer = sale.Customers?.Name ?? "Walk-in Customer",
                    Mobile = sale.MobileNo ?? "",
                    Cost = totalCost,
                    BillAmount = sale.TotalPayable,
                    Profit = profit,
                    ProfitPct = profitPct,
                    Items = items.Count(),
                    Qty = items.Sum(x => x.Qty)
                };
            })
            .OrderBy(x => x.BillDate)
            .ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fs = 9;

            // Company
            var user = await _usersManager.GetUserAsync(User);
            string companyName = user != null
                ? (await _tenantRepository.GetById(user.TenantId))?.Name ?? "Company Name"
                : "Company Name";

            doc.Add(new Paragraph(companyName).SetFont(bold).SetFontSize(14).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph("Billwise Profit Report").SetFont(bold).SetFontSize(11).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(10));

            // ✅ FIX: EXACT 11 COLUMNS
            Table table = new Table(new float[] { 2, 3, 3, 5, 4, 3, 3, 3, 3, 2, 2 })
                .UseAllAvailableWidth();

            string[] headers = {
        "Sr","Bill No","Bill Date","Customer","Mobile",
        "Cost Value","Bill Amount","Gross Profit","Profit %","Items","Qty"
    };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(fs))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            foreach (var x in list)
            {
                table.AddCell(new Paragraph(x.Sr.ToString()).SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.BillNo ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.BillDate?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Customer).SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Mobile).SetFont(normal).SetFontSize(fs));

                table.AddCell(new Paragraph($"₹ {x.Cost:0.00}").SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph($"₹ {x.BillAmount:0.00}").SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph($"₹ {x.Profit:0.00}").SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph($"{x.ProfitPct:0.00} %").SetFont(normal).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(x.Items.ToString()).SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Qty.ToString()).SetFont(normal).SetFontSize(fs));
            }

            // ✅ FIXED TOTAL ROW (ALIGN PERFECT)
            table.AddCell(new Cell(1, 5)
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(fs)));

            table.AddCell(new Paragraph($"₹ {list.Sum(x => x.Cost):0.00}").SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph($"₹ {list.Sum(x => x.BillAmount):0.00}").SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph($"₹ {list.Sum(x => x.Profit):0.00}").SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph($"{(list.Count > 0 ? list.Average(x => x.ProfitPct) : 0):0.00} %").SetFont(bold).SetFontSize(fs).SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(""));
            table.AddCell(new Paragraph(""));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "BillwiseProfitReport.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> SalesBillReturnAndCollectionReport(DateTime? fromDate, DateTime? toDate)
        {
            // 🔹 Default date = Current Month
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now.Date;
            }

            var sales = await _salesservice.GetAll();

            var reportList = sales
                .Where(s => s.BillDate.HasValue &&
                            s.BillDate.Value.Date >= fromDate.Value.Date &&
                            s.BillDate.Value.Date <= toDate.Value.Date)
                .Select(s => new SalesVM
                {
                    Id = s.Id,
                    BillNo = s.BillNo,
                    BillDate = s.BillDate,
                    TotalAmount = s.TotalPayable,
                    PaidAmount = s.PaidAmount,
                    ReturnAmount = s.ReturnAmount,
                    NetCollection = s.NetCollection
                })
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            // 🔹 Footer totals (optional but useful)
            ViewBag.TotalBillAmount = reportList.Sum(x => x.TotalAmount);
            ViewBag.TotalPaidAmount = reportList.Sum(x => x.PaidAmount);
            ViewBag.TotalReturnAmount = reportList.Sum(x => x.ReturnAmount);
            ViewBag.TotalNetCollection = reportList.Sum(x => x.NetCollection);

            // 🔹 Date binding for UI
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            return View(reportList);
        }

        //public async Task<IActionResult> SalseReportCompanywise(int? companyId)
        //{
        //    var itemMasters = await _itemmasterservice.GetAll();
        //    var companies = await _companyService.GetAll();
        //    var division = await _divisionRepo.GetAll();

        //    ViewBag.companydata = companies;
        //    ViewBag.divisionData = division;
        //    ViewBag.SelectedCompanyId = companyId;
        //    var sales = await _salesservice.GetAll();

        //    // Flatten all SalesItems
        //    var salesItems = sales.Where(s => s.SalesItems != null).SelectMany(s => s.SalesItems);
        //    if (companyId.HasValue && companyId.Value > 0)
        //    {
        //        // Filter salesItems where item's company matches selected company
        //        salesItems = salesItems
        //            .Where(x => x.ItemMaster?.CompanyId == companyId.Value);
        //    }

        //    // Group by ItemMasterId to calculate total sales
        //    var groupedSales = salesItems
        //                      .GroupBy(x => x.ItemMasterId)
        //                      .Select(g => new
        //                      {
        //                          ItemMasterId = g.Key,
        //                          TotalQty = g.Sum(x => x.Qty),
        //                          TotalAmount = g.Sum(x => x.Amount), // Assuming already discounted
        //                          TotalGst = g.Sum(x =>
        //                          {
        //                              decimal rate = x.Rate;
        //                              decimal qty = x.Qty;
        //                              decimal discountPercent = x.Discount;
        //                              decimal gstPercent = x.Gst;

        //                              decimal gross = rate * qty;
        //                              decimal discountAmt = gross * (discountPercent / 100);
        //                              decimal taxable = gross - discountAmt;
        //                              decimal gstAmt = taxable * (gstPercent / 100);

        //                              return gstAmt;
        //                          })
        //                      })
        //                      .ToList();


        //    // Join grouped results with item masters
        //    var stockList = (from item in itemMasters
        //                     join g in groupedSales on item.Id equals g.ItemMasterId
        //                     where g.TotalQty > 0
        //                     select new StockVM
        //                     {
        //                         ItemMasterId = item.Id,
        //                         ItemCode = item.Code,
        //                         CategoryName = item.Category?.CategoryName ?? "Unknown",
        //                         ItemName = item.Name,
        //                         Unit1 = item.Unit1 ?? "",
        //                         Unit2 = item.Unit2 ?? "",
        //                         Stocks = g.TotalQty,
        //                         Amount = g.TotalAmount,
        //                         GstAmount = g.TotalGst
        //                     }).ToList();

        //    return View(stockList);
        //}

        //EXPORT TO EXCEL

        [HttpGet]
        public async Task<IActionResult> ExportSalesBillReturnCollectionExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now.Date;
            }

            var sales = await _salesservice.GetAll();

            var report = sales
                .Where(s => s.BillDate.HasValue &&
                            s.BillDate.Value.Date >= fromDate.Value.Date &&
                            s.BillDate.Value.Date <= toDate.Value.Date)
                .Select(s => new
                {
                    s.BillNo,
                    s.BillDate,
                    TotalAmount = s.TotalPayable,
                    s.PaidAmount,
                    s.ReturnAmount,
                    s.NetCollection
                })
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Sales Collection");

            // Company Name
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";
            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Sales Bill Return & Collection Report";
            ws.Range(2, 1, 2, 6).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 6).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TABLE HEADER
            int row = 5;

            string[] headers = { "Bill No", "Bill Date", "Bill Amount", "Paid", "Return", "Net Collection" };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
                ws.Cell(row, i + 1).Style.Font.SetBold();
            }

            row++;

            decimal totalBill = 0, totalPaid = 0, totalReturn = 0, totalNet = 0;

            foreach (var item in report)
            {
                ws.Cell(row, 1).Value = item.BillNo;
                ws.Cell(row, 2).Value = item.BillDate?.ToString("dd-MM-yyyy");

                ws.Cell(row, 3).Value = item.TotalAmount;
                ws.Cell(row, 4).Value = item.PaidAmount;
                ws.Cell(row, 5).Value = item.ReturnAmount;
                ws.Cell(row, 6).Value = item.NetCollection;

                ws.Range(row, 3, row, 6).Style.NumberFormat.Format = "0.00";

                totalBill += item.TotalAmount;
                totalPaid += item.PaidAmount;
                totalReturn += item.ReturnAmount;
                totalNet += item.NetCollection;

                row++;
            }

            // TOTAL ROW (same UI)
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 1).Style.Font.SetBold();

            ws.Cell(row, 3).Value = totalBill;
            ws.Cell(row, 4).Value = totalPaid;
            ws.Cell(row, 5).Value = totalReturn;
            ws.Cell(row, 6).Value = totalNet;

            ws.Range(row, 3, row, 6).Style.NumberFormat.Format = "0.00";
            ws.Range(row, 1, row, 6).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SalesBillReturnCollection.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportSalesBillReturnCollectionPdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now.Date;
            }

            var sales = await _salesservice.GetAll();

            var report = sales
                .Where(s => s.BillDate.HasValue &&
                            s.BillDate.Value.Date >= fromDate.Value.Date &&
                            s.BillDate.Value.Date <= toDate.Value.Date)
                .Select(s => new
                {
                    s.BillNo,
                    s.BillDate,
                    TotalAmount = s.TotalPayable,
                    s.PaidAmount,
                    s.ReturnAmount,
                    s.NetCollection
                })
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fontSize = 9;

            // Company Name
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";
            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            document.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Sales Bill Return & Collection Report")
                .SetFont(bold).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(fontSize)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // TABLE
            Table table = new Table(new float[] { 3, 3, 3, 3, 3, 3 }).UseAllAvailableWidth();

            string[] headers = { "Bill No", "Bill Date", "Bill Amount", "Paid", "Return", "Net Collection" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell().Add(
                    new Paragraph(h).SetFont(bold).SetFontSize(fontSize)
                ));
            }

            decimal totalBill = 0, totalPaid = 0, totalReturn = 0, totalNet = 0;

            foreach (var item in report)
            {
                table.AddCell(new Paragraph(item.BillNo).SetFont(normal).SetFontSize(fontSize));
                table.AddCell(new Paragraph(item.BillDate?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fontSize));

                table.AddCell(new Paragraph(item.TotalAmount.ToString("0.00")).SetFont(normal).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(item.PaidAmount.ToString("0.00")).SetFont(normal).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(item.ReturnAmount.ToString("0.00")).SetFont(normal).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(item.NetCollection.ToString("0.00")).SetFont(normal).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));

                totalBill += item.TotalAmount;
                totalPaid += item.PaidAmount;
                totalReturn += item.ReturnAmount;
                totalNet += item.NetCollection;
            }

            // TOTAL ROW (same as UI)
            table.AddCell(new Cell(1, 2)
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(fontSize))
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(totalBill.ToString("0.00")).SetFont(bold).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph(totalPaid.ToString("0.00")).SetFont(bold).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph(totalReturn.ToString("0.00")).SetFont(bold).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph(totalNet.ToString("0.00")).SetFont(bold).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(),
                "application/pdf",
                "SalesBillReturnCollection.pdf");
        }
        public async Task<IActionResult> SpeechtoText()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetItemByBarcode(string? barcode)
        {
            var item = await _itemmasterservice.GetByBarcode(barcode);
            if (item == null || item.ItemType == "Bulk")
                return Json(new { error = "Item not found" });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            return Json(new
            {
                itemId = item.Id,
                itemname = item.Name,
                maximumDiscount = item.MaximumDiscount,
                allowNegative = setting?.AllowNegative ?? false,
                minimumQty = item.MinimumQty,
                conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m,
                itemConversion = setting?.ItemConversion ?? "StripWise",
                gst = item.Hsn?.IGST ?? 0,
                cess = item.Hsn?.Cess ?? 0
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetItemById(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null || item.ItemType == "Bulk")
                return Json(new { error = "Item not found" });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);

            return Json(new
            {
                itemId = item.Id,
                itemname = item.Name,
                maximumDiscount = item.MaximumDiscount,
                allowNegative = setting?.AllowNegative ?? false,
                minimumQty = item.MinimumQty,
                conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m,
                itemConversion = setting?.ItemConversion ?? "StripWise",
                gst = item.Hsn?.IGST ?? 0,
                cess = item.Hsn?.Cess ?? 0
            });
        }

        //[HttpGet]
        //public async Task<IActionResult> GetAllItems()
        //{
        //    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized();

        //    var setting = await _salsesettingservice.GetByUserId(userId);

        //    //var itemMasters = (await _itemmasterservice.GetAll()).OrderBy(x => x.Name);

        //    var itemMasters = (await _itemmasterservice.GetAll())
        //            .Where(x => x.IsActive)
        //            .OrderBy(x => x.Name);

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

        //    var stockList = itemMasters.Select(item =>
        //    {
        //        var itemId = item.Id;

        //        // YAHAN FILTERING ADD KARO - Get valid batches only
        //        IEnumerable<dynamic> validPurchaseItems = purchaseItems.Where(x => x.ItemId == itemId);

        //        // Apply expiry filtering if ExpiryAllowedDays is set
        //        if (setting != null && setting.ExpiryAllowedDays > 0)
        //        {
        //            var allowedExpiryDate = DateTime.Now.AddDays(setting.ExpiryAllowedDays);
        //            validPurchaseItems = validPurchaseItems.Where(x => x.ExpiryDate == null || x.ExpiryDate >= allowedExpiryDate);
        //        }

        //        var totalPurchasedQty = validPurchaseItems
        //            .Sum(x => x.Qty + x.FreeQty);

        //        var totalsalseQty = salesItems
        //            .Where(x => x.ItemMasterId == itemId)
        //            .Sum(x => x.Qty);

        //        var totalpurchaseReturnedQty = purchaseReturnItems
        //            .Where(x => x.ItemId == itemId)
        //            .Sum(x => x.Qty + x.FreeQty);

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

        //        string availableQty;
        //        if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
        //        {
        //            int conversion = item.Conversion;

        //            int totalTablets = (int)Math.Round(currentStock * conversion, MidpointRounding.AwayFromZero);

        //            int strips = totalTablets / conversion;
        //            int tablets = totalTablets % conversion;

        //            availableQty = $"{strips}:{tablets}";
        //        }
        //        else
        //        {
        //            availableQty = currentStock.ToString("0.##");
        //        }

        //        return new
        //        {
        //            id = itemId,
        //            barcode = item.Barcode,
        //            code = item.Code,
        //            name = item.Name,
        //            gst = item.Hsn?.IGST ?? 0,
        //            qty = currentStock,
        //            availableQty = availableQty,
        //            maximumdiscount = item.MaximumDiscount,
        //            allowNegative = setting?.AllowNegative ?? false,
        //            minimumQty = item.MinimumQty,
        //            conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m,
        //            ItemConversion = setting.ItemConversion == null ? "StripWise" : setting.ItemConversion
        //        };
        //    });

        //    // NEW: Filter based on AllowNegative setting
        //    if (setting != null && !setting.AllowNegative)
        //    {
        //        // Filter out items with 0 or negative stock
        //        stockList = stockList.Where(x => x.qty > 0);
        //    }

        //    // Convert to list AFTER filtering
        //    var result = stockList.ToList();
        //    ViewBag.ItemConversion = setting.ItemConversion == null ? "StripWise" : setting.ItemConversion;
        //    return Json(result);
        //}
        [HttpGet]
        public async Task<IActionResult> GetAllItems()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var setting = await _salsesettingservice.GetByUserId(userId);
            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => x.IsActive && x.ItemType != "Bulk")
                .OrderBy(x => x.Name)
                .ToList();
            var currentStocks = await _currenstockService.GetAll();

            var stockByItem = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var itemConversion = setting?.ItemConversion ?? "StripWise";

            var stockList = itemMasters.Select(item =>
            {
                stockByItem.TryGetValue(item.Id, out var currentStock);

                string availableQty;
                if (itemConversion == "TabletWise" && item.Conversion > 0)
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
                    id = item.Id,
                    barcode = item.Barcode,
                    code = item.Code,
                    name = item.Name,
                    salt = item.Salt,
                    gst = item.Hsn?.IGST ?? 0,
                    cess = item.Hsn?.Cess ?? 0,
                    qty = currentStock,
                    availableQty = availableQty,
                    maximumdiscount = item.MaximumDiscount,
                    allowNegative = setting?.AllowNegative ?? false,
                    minimumQty = item.MinimumQty,
                    conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m,
                    ItemConversion = itemConversion
                };
            });

            if (setting != null && !setting.AllowNegative)
            {
                stockList = stockList.Where(x => x.qty > 0);
            }

            var result = stockList.ToList();
            ViewBag.ItemConversion = itemConversion;
            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllItemsForEdit(int saleId = 0)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var setting = await _salsesettingservice.GetByUserId(userId);
            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => x.IsActive && x.ItemType != "Bulk")
                .OrderBy(x => x.Name)
                .ToList();
            var currentStocks = await _currenstockService.GetAll();

            var stockByItem = currentStocks
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var sale = saleId > 0 ? await _salesService.GetById(saleId) : null;
            var currentSaleQtyByItem = sale?.SalesItems?
                .GroupBy(x => x.ItemMasterId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty))
                ?? new Dictionary<int, decimal>();

            var itemConversion = setting?.ItemConversion ?? "StripWise";

            var stockList = itemMasters.Select(item =>
            {
                stockByItem.TryGetValue(item.Id, out var currentStock);
                if (currentSaleQtyByItem.TryGetValue(item.Id, out var saleQty))
                {
                    currentStock += saleQty;
                }

                string availableQty;
                if (itemConversion == "TabletWise" && item.Conversion > 0)
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
                    id = item.Id,
                    barcode = item.Barcode,
                    code = item.Code,
                    name = item.Name,
                    salt = item.Salt,
                    gst = item.Hsn?.IGST ?? 0,
                    cess = item.Hsn?.Cess ?? 0,
                    qty = currentStock,
                    availableQty = availableQty,
                    maximumdiscount = item.MaximumDiscount,
                    allowNegative = setting?.AllowNegative ?? false,
                    minimumQty = item.MinimumQty,
                    conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m,
                    ItemConversion = itemConversion
                };
            });

            if (setting != null && !setting.AllowNegative)
            {
                stockList = stockList.Where(x => x.qty > 0);
            }

            var result = stockList.ToList();
            ViewBag.ItemConversion = itemConversion;
            return Json(result);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetAllItemsForEdit(int saleId = 0)
        //{
        //    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized();

        //    var setting = await _salsesettingservice.GetByUserId(userId);

        //    //var itemMasters = (await _itemmasterservice.GetAll()).OrderBy(x => x.Name);

        //    var itemMasters = (await _itemmasterservice.GetAll())
        //            .Where(x => x.IsActive)
        //            .OrderBy(x => x.Name);

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

        //    var stockList = itemMasters.Select(item =>
        //    {
        //        var itemId = item.Id;

        //        // YAHAN FILTERING ADD KARO - Get valid batches only
        //        IEnumerable<dynamic> validPurchaseItems = purchaseItems.Where(x => x.ItemId == itemId);

        //        // Apply expiry filtering if ExpiryAllowedDays is set
        //        if (setting != null && setting.ExpiryAllowedDays > 0)
        //        {
        //            var allowedExpiryDate = DateTime.Now.AddDays(setting.ExpiryAllowedDays);
        //            validPurchaseItems = validPurchaseItems.Where(x => x.ExpiryDate == null || x.ExpiryDate >= allowedExpiryDate);
        //        }

        //        var totalPurchasedQty = validPurchaseItems
        //               .Sum(x => x.Qty + x.FreeQty);

        //        //var totalsalseQty = salesItems
        //        //       .Where(x => x.ItemMasterId == itemId)
        //        //       .Sum(x => x.Qty);

        //        var totalsalseQty = salesItems
        //            .Where(x => x.ItemMasterId == itemId && x.SalesId != saleId)
        //            .Sum(x => x.Qty);

        //        var totalpurchaseReturnedQty = purchaseReturnItems
        //            .Where(x => x.ItemId == itemId)
        //            .Sum(x => x.Qty + x.FreeQty);

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


        //        string availableQty;
        //        if (setting != null && setting.ItemConversion == "TabletWise" && item.Conversion > 0)
        //        {
        //            int conversion = item.Conversion;

        //            int totalTablets = (int)Math.Round(currentStock * conversion, MidpointRounding.AwayFromZero);

        //            int strips = totalTablets / conversion;
        //            int tablets = totalTablets % conversion;

        //            availableQty = $"{strips}:{tablets}";
        //        }
        //        else
        //        {
        //            availableQty = currentStock.ToString("0.##");
        //        }

        //        return new
        //        {
        //            id = itemId,
        //            barcode = item.Barcode,
        //            code = item.Code,
        //            name = item.Name,
        //            gst = item.Hsn?.IGST ?? 0,
        //            qty = currentStock,
        //            availableQty = availableQty, // Only for display purpase
        //            maximumdiscount = item.MaximumDiscount,
        //            allowNegative = setting?.AllowNegative ?? false,
        //            minimumQty = item.MinimumQty,
        //            conversion = item.Conversion > 0 ? (decimal)item.Conversion : 1m,
        //            ItemConversion = setting.ItemConversion == null ? "StripWise" : setting.ItemConversion
        //        };
        //    });

        //    // NEW: Filter based on AllowNegative setting
        //    if (setting != null && !setting.AllowNegative)
        //    {
        //        // Filter out items with 0 or negative stock
        //        stockList = stockList.Where(x => x.qty > 0);
        //    }

        //    // Convert to list AFTER filtering
        //    var result = stockList.ToList();
        //    ViewBag.ItemConversion = setting.ItemConversion == null ? "StripWise" : setting.ItemConversion;
        //    return Json(result);
        //}

        public async Task<IActionResult> SalseModeOfPaymentReport(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now.Date; // aaj ki date
            }

            var salsedata = await _salesService.GetAll();
            var modeofpaymentdata = await _modeofpaymentservice.GetAll();

            // Date-wise filter yahin lagana hoga
            salsedata = salsedata
                .Where(x => x.BillDate >= fromDate.Value.Date
                         && x.BillDate <= toDate.Value.Date)
                .ToList();

            // Flatten all sales payments
            var salsepaymentdata = salsedata
                .Where(x => x.SalsePaymentDetails != null)
                .SelectMany(x => x.SalsePaymentDetails);

            // Group by payment mode
            var report = salsepaymentdata
                .GroupBy(x => x.PaymentModeId)
                .Select(group => new SalsePaymentDetailsVM
                {
                    PaymentModeId = group.Key,
                    PaymentModeName = modeofpaymentdata
                                        .FirstOrDefault(m => m.Id == group.Key)?.Name ?? "Unknown",
                    Amount = group.Sum(x => x.Amount)
                })
                .ToList();

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            return View(report);
        }

        //export to excel
        [HttpGet]
        public async Task<IActionResult> ExportSalseModeOfPaymentExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now.Date;
            }

            var salsedata = await _salesService.GetAll();
            var modeofpaymentdata = await _modeofpaymentservice.GetAll();

            salsedata = salsedata
                .Where(x => x.BillDate >= fromDate.Value.Date &&
                            x.BillDate <= toDate.Value.Date)
                .ToList();

            var salsepaymentdata = salsedata
                .Where(x => x.SalsePaymentDetails != null)
                .SelectMany(x => x.SalsePaymentDetails);

            var report = salsepaymentdata
                .GroupBy(x => x.PaymentModeId)
                .Select(group => new
                {
                    PaymentMode = modeofpaymentdata
                        .FirstOrDefault(m => m.Id == group.Key)?.Name ?? "Unknown",
                    Amount = group.Sum(x => x.Amount)
                })
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Sales Mode Of Payment");
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }
            // COMPANY NAME
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 2).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // REPORT NAME
            ws.Cell(2, 1).Value = "Sales Mode Of Payment Report";
            ws.Range(2, 1, 2, 2).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // DATE RANGE
            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 2).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            ws.Cell(row, 1).Value = "Payment Mode";
            ws.Cell(row, 2).Value = "Amount";

            ws.Range(row, 1, row, 2).Style.Font.SetBold();
            row++;

            foreach (var item in report)
            {
                ws.Cell(row, 1).Value = item.PaymentMode;
                ws.Cell(row, 2).Value = item.Amount;
                row++;
            }

            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 2).Value = report.Sum(x => x.Amount);

            ws.Range(row, 1, row, 2).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.LightGray);

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SalesModeOfPaymentReport.xlsx"
            );
        }

        //Export to pdf
        [HttpGet]
        public async Task<IActionResult> ExportSalseModeOfPaymentPdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Now;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now.Date;
            }

            var salsedata = await _salesService.GetAll();
            var modeofpaymentdata = await _modeofpaymentservice.GetAll();

            salsedata = salsedata
                .Where(x => x.BillDate >= fromDate.Value.Date &&
                            x.BillDate <= toDate.Value.Date)
                .ToList();

            var salsepaymentdata = salsedata
                .Where(x => x.SalsePaymentDetails != null)
                .SelectMany(x => x.SalsePaymentDetails);

            var report = salsepaymentdata
                .GroupBy(x => x.PaymentModeId)
                .Select(group => new
                {
                    PaymentMode = modeofpaymentdata
                        .FirstOrDefault(m => m.Id == group.Key)?.Name ?? "Unknown",
                    Amount = group.Sum(x => x.Amount)
                })
                .ToList();

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var doc = new Document(pdf);
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }
            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Company Name
            doc.Add(new Paragraph(companyName)
                .SetFont(bold)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            // Report Name
            doc.Add(new Paragraph("Sales Mode Of Payment Report")
                .SetFont(bold)
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER));

            // Date Range
            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            Table table = new Table(2).UseAllAvailableWidth();

            // Headers
            table.AddHeaderCell(new Cell()
                .Add(new Paragraph("Payment Mode").SetFont(bold).SetFontSize(10)));

            table.AddHeaderCell(new Cell()
                .Add(new Paragraph("Amount").SetFont(bold).SetFontSize(10)));

            // Rows
            foreach (var item in report)
            {
                table.AddCell(new Cell()
                    .Add(new Paragraph(item.PaymentMode).SetFont(normal).SetFontSize(9)));

                table.AddCell(new Cell()
                    .Add(new Paragraph($"{item.Amount:0.00}").SetFont(normal).SetFontSize(9)));
            }

            // TOTAL
            table.AddCell(new Cell()
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(10)));

            table.AddCell(new Cell()
                .Add(new Paragraph($"{report.Sum(x => x.Amount):0.00}")
                .SetFont(bold).SetFontSize(10)));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "SalesModeOfPaymentReport.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> GetBuyXGetXFreeItems(int offerId)
        {
            var offer = await _offerrepo.GetById(offerId);

            var cartFreeItems = new List<object>();

            if (offer != null && offer.OfferType == OfferType.BuyXGetX)
            {
                foreach (var offerItem in offer.OfferItems)
                {
                    cartFreeItems.Add(new
                    {
                        itemId = offerItem.FreeItemId,
                        itemName = offerItem.FreeItem?.Name ?? "Free Item",
                        qty = offerItem.FreeQty
                    });
                }
            }

            return Json(cartFreeItems);
        }

        [HttpGet]
        public async Task<IActionResult> ApplyComboOffer(int offerId)
        {
            var offer = await _offerrepo.GetById(offerId);
            var comboItemsList = new List<object>();

            if (offer != null && offer.OfferType == OfferType.Combo)
            {
                foreach (var offerItem in offer.OfferItems)
                {
                    comboItemsList.Add(new
                    {
                        itemId = offerItem.ItemId,
                        itemName = offerItem.ItemMaster?.Name ?? "Combo Item",
                        qty = offerItem.BuyQty,
                        rate = offerItem.fixedPrice
                    });
                }
            }

            return Json(comboItemsList);
        }

        //[HttpPost]
        //public async Task<IActionResult> EvaluateOfferss([FromBody] SalesVM data)
        //{
        //    // Logic to evaluate offers based on list
        //    // For example, add discount to qualifying items
        //    var result = await EvaluateOffer(data.SalesItemVMs, data.TotalPayable);
        //    return Json(result);
        //    //  return Json(new { success = true, message = "Offer applied successfully" });
        //}

        [HttpPost]
        public async Task<IActionResult> EvaluateOfferss([FromBody] SalesVM data)
        {
            // ✅ Null check
            if (data == null)
            {
                return Json(new { success = false, message = "No data received." });
            }

            if (data.SalesItemVMs == null || !data.SalesItemVMs.Any())
            {
                return Json(new { success = false, message = "Sales items not found." });
            }

            // Offer evaluation
            var result = await EvaluateOffer(data.SalesItemVMs, data.TotalPayable);

            return Json(result);
        }

        public async Task<AppliedOfferResult> EvaluateOffer(IList<SalesItemVM> salesItems, decimal totalAmount)
        {
            var result = new AppliedOfferResult();
            var today = DateTime.Today;

            var allOffers = await _offerrepo.GetAll();
            var offers = allOffers
                .Where(x => x.IsActive && x.StartDate <= today && x.EndDate > today)
                .ToList();

            foreach (var offer in offers)
            {
                switch (offer.OfferType)
                {
                    case OfferType.DisCountAmount:
                        if (totalAmount >= (offer.MinAmount ?? 0))
                        {
                            result.TotalDiscount += offer.DiscountValue;
                            result.Offers.Add(new OfferVM
                            {
                                Id = offer.Id,
                                OfferName = offer.OfferName,
                                OfferType = offer.OfferType,
                                DiscountValue = offer.DiscountValue
                            });
                        }
                        break;

                    case OfferType.DiscountPercent:
                        if (totalAmount >= (offer.MinAmount ?? 0))
                        {
                            var percentDiscount = (totalAmount * offer.DiscountValue / 100);
                            result.TotalDiscount += percentDiscount;
                            result.Offers.Add(new OfferVM
                            {
                                Id = offer.Id,
                                OfferName = offer.OfferName,
                                OfferType = offer.OfferType,
                                DiscountValue = percentDiscount
                            });
                        }
                        break;

                    case OfferType.BuyXGetX:
                        foreach (var offerItem in offer.OfferItems)
                        {
                            var match = salesItems.FirstOrDefault(x => x.ItemMasterId == offerItem.ItemId);
                            if (match != null && match.Qty >= offerItem.BuyQty)
                            {
                                result.FreeItems.Add(new SalesItemVM
                                {
                                    ItemMasterId = offerItem.FreeItemId ?? 0,
                                    Qty = offerItem.FreeQty,
                                    Rate = 0
                                });

                                if (!result.Offers.Any(o => o.OfferName == offer.OfferName))
                                {
                                    result.Offers.Add(new OfferVM
                                    {
                                        Id = offer.Id,
                                        OfferName = offer.OfferName,
                                        OfferType = offer.OfferType,
                                        DiscountValue = 0
                                    });

                                }
                            }
                        }
                        break;

                    case OfferType.Combo:
                        var comboGroup = offer.OfferItems.FirstOrDefault()?.ComboGroupId;
                        if (!string.IsNullOrEmpty(comboGroup))
                        {
                            var comboItems = offer.OfferItems;
                            bool isValidCombo = comboItems.Any(ci =>
                                salesItems.Any(c => c.ItemMasterId == ci.ItemId && c.Qty >= ci.BuyQty));

                            if (isValidCombo)
                            {
                                var totalComboPrice = comboItems.Sum(x => x.fixedPrice);
                                //var actualComboPrice = comboItems.Sum(ci =>
                                //    salesItems.First(c => c.ItemMasterId == ci.ItemId).Rate * (decimal)ci.BuyQty);
                                var actualComboPrice = comboItems.Sum(ci =>
                                {
                                    var cartItem = salesItems.FirstOrDefault(c => c.ItemMasterId == ci.ItemId);
                                    return cartItem != null ? cartItem.Rate * (decimal)ci.BuyQty : 0;
                                });


                                var comboDiscount = actualComboPrice - totalComboPrice;
                                result.TotalDiscount += comboDiscount;

                                result.Offers.Add(new OfferVM
                                {
                                    Id = offer.Id,
                                    OfferName = offer.OfferName,
                                    OfferType = offer.OfferType,
                                    DiscountValue = comboDiscount
                                });
                            }
                        }
                        break;

                    case OfferType.CashbackAmount:
                        if (totalAmount >= (offer.MinAmount ?? 0))
                        {
                            result.Offers.Add(new OfferVM
                            {
                                Id = offer.Id,
                                OfferName = offer.OfferName,
                                OfferType = offer.OfferType,
                                DiscountValue = offer.DiscountValue
                            });
                        }
                        break;

                    case OfferType.CashbackPercent:
                        if (totalAmount >= (offer.MinAmount ?? 0))
                        {
                            var cashbackAmount = (totalAmount * offer.DiscountValue / 100);
                            result.Offers.Add(new OfferVM
                            {
                                Id = offer.Id,
                                OfferName = offer.OfferName,
                                OfferType = offer.OfferType,
                                DiscountValue = cashbackAmount
                            });
                        }
                        break;
                }
            }

            return result;
        }

        #region Hold & Unhold logics..
        public async Task<IActionResult> HoldList()
        {
            await _profileService.Set(User);
            var data = await _salesservice.GetAllHoldSales();
            return View(data);
        }
        [HttpPost]
        public async Task<IActionResult> Hold(SalesVM vm)
        {
            // ❌ Basic validation
            if (vm == null || vm.SalesItemVMs == null || !vm.SalesItemVMs.Any())
                return Json(new { success = false, message = "No items to hold" });

            try
            {
                if (vm.billingType == "registered")
                {
                    var customerList = await _customerservice.GetAll();

                    // Mobile required for registered customer
                    if (string.IsNullOrWhiteSpace(vm.MobileNo))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Mobile number is required for registered customer."
                        });
                    }

                    var isExist = customerList.Any(x =>
                        !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                        x.PhoneNo.Trim() == vm.MobileNo.Trim()
                    );

                    // Customer NOT found → ERROR
                    if (!isExist)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Customer does not exist. Please create customer first."
                        });
                    }
                }


                // ResumeHold → Re-Hold (HoldId mandatory here)
                if (vm.HoldId > 0)
                {
                    // 🔥 delete old hold completely
                    await _salesservice.DeleteHoldSales(vm.HoldId);
                }

                // 🔥 STEP 2: CREATE fresh HOLD (replacement)
                var newHold = new HoldSales
                {
                    HoldToken = "H" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    HoldDate = DateTime.Now,

                    billingType = vm.billingType,
                    CustomerId = vm.CustomerId > 0 ? vm.CustomerId : null,
                    CustomerName = vm.CustomerName,
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
                    OfferId = vm.OfferId,

                    HoldSalesItems = vm.SalesItemVMs.Select(x => new HoldSalesItem
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
                    }).ToList()
                };

                await _salesservice.CreateHold(newHold);

                return Json(new
                {
                    success = true,
                    message = "Hold updated successfully",
                    holdId = newHold.Id
                });
            }
            catch
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to update hold"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResumeHold(int id)
        {
            // HOLD data load karo
            var hold = await _salesservice.GetHoldSalesById(id);

            SalesVM VM = new SalesVM();

            if (hold != null)
            {
                VM.HoldId = hold.Id;              // 🔑 important (delete later)
                VM.IsHoldMode = true;             // UI ke liye optional
                VM.billingType = hold.billingType;
                VM.CustomerId = hold.CustomerId ?? 0;
                VM.CustomerName = hold.CustomerName;
                VM.MobileNo = hold.MobileNo;
                VM.Address = hold.Address;

                VM.BillNo = await GenerateNxtNumber();
                VM.BillDate = DateTime.Now;

                VM.PharmacyDoctorId = hold.PharmacyDoctorId;
                VM.DoctorMobileNumber = hold.DoctorMobileNumber;
                VM.DoctorRegNumber = hold.DoctorRegNumber;

                VM.Total = hold.Total;
                VM.TotalGstAmt = hold.TotalGstAmt;
                VM.discountPercent = hold.discountPercent;
                VM.discountAmount = hold.discountAmount;
                VM.Totaldiscount = hold.Totaldiscount;
                VM.TotalPayable = hold.TotalPayable;
                VM.OfferId = hold.OfferId;

                // ❌ HOLD me payment nahi hota
                VM.PaidAmount = 0;
                VM.ReturnAmount = 0;
                VM.Balance = VM.TotalPayable;

                // ✅ HOLD ITEMS → SalesItemVM
                VM.SalesItemVMs = hold.HoldSalesItems != null
                    ? hold.HoldSalesItems.Select(x => new SalesItemVM
                    {
                        ItemMasterId = x.ItemMasterId,
                        ItemBarcodeNumber = x.ItemMaster?.Name,
                        PurchaseItemId = x.PurchaseItemId,
                        Batch = x.Batch,
                        Expirydate = x.Expirydate,
                        Mrp = x.Mrp,
                        Qty = x.Qty,
                        Rate = x.Rate,
                        Gst = x.Gst,
                        Discount = x.Discount,
                        Amount = x.Amount
                    }).ToList()
                    : new List<SalesItemVM>();

                // ❌ Payment details empty
                VM.SalsePaymentDetails = new List<SalsePaymentDetailsVM>();
            }

            // Dropdowns (same as Edit)
            ViewBag.Customer = new SelectList(await _customerservice.GetAll(), "Id", "Name");
            ViewBag.Items = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name");
            ViewBag.PaymentMode = new SelectList(await _modeofpaymentservice.GetAll(), "Id", "Name");

            // 🔁 SAME Create page reuse
            return View(VM);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFromHold(SalesVM Vm)
        {
            if (Vm == null || Vm.HoldId <= 0)
                return Json(new { success = false, message = "Invalid hold data" });

            try
            {
                var model = new Models.Entity.Sales
                {
                    billingType = Vm.billingType,
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
                    Balance = Vm.Balance,

                    OfferId = Vm.OfferId,

                    SalesItems = Vm.SalesItemVMs.Select(x => new SalesItem
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
                    }).ToList(),

                    SalsePaymentDetails = Vm.SalsePaymentDetails.Select(pd => new SalsePaymentDetails
                    {
                        PaymentModeId = pd.PaymentModeId,
                        Amount = pd.Amount,
                        ReferenceNo = pd.ReferenceNo,
                        Description = pd.Description,
                        CustomerId = Vm.CustomerId
                    }).ToList()
                };

                var createdSale = await _salesservice.Create(model);

                // 🔥 Hold delete
                await _salesservice.DeleteHoldSales(Vm.HoldId);

                return Json(new
                {
                    success = true,
                    saleId = createdSale.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteHold(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("HoldList");
            }

            try
            {
                // HARD DELETE: HoldSales + HoldSalesItems
                await _salesservice.DeleteHoldSales(id);

                TempData["SuccessMessage"] = "Hold entry deleted successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Unable to delete hold entry. Please try again.";
            }

            // 🔁 Back to Hold List
            return RedirectToAction("HoldList");
        }

        [HttpGet]
        public async Task<IActionResult> HoldListModal()
        {
            await _profileService.Set(User);
            var data = await _salesservice.GetAllHoldSales();
            return PartialView("_HoldListModal", data);
        }
        #endregion

        #region GSTR1Report
        // Main Action Method
        //public async Task<IActionResult> GSTR1Report(DateTime? StartBillDate, DateTime? EndBillDate, string ViewType = "Summary")
        //{
        //    // ✅ Set default dates (Current Month)
        //    if (!StartBillDate.HasValue)
        //        StartBillDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        //    if (!EndBillDate.HasValue)
        //        EndBillDate = DateTime.Today;

        //    ViewBag.ViewType = ViewType;
        //    ViewBag.StartBillDate = StartBillDate.Value.ToString("yyyy-MM-dd");
        //    ViewBag.EndBillDate = EndBillDate.Value.ToString("yyyy-MM-dd");

        //    if (ViewType == "Summary")
        //    {
        //        var summaryData = await GetGstr1SummaryData(StartBillDate, EndBillDate);
        //        return View(summaryData);
        //    }

        //    var details = await GetGSTR1Data(StartBillDate, EndBillDate);
        //    return View(details);
        //}

        [HttpGet]
        public async Task<IActionResult> GSTR1Report(DateTime? StartBillDate, DateTime? EndBillDate, string ViewType = "Summary")
        {
            // ✅ Default: Current Month
            if (!StartBillDate.HasValue)
                StartBillDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            if (!EndBillDate.HasValue)
                EndBillDate = DateTime.Today;

            ViewBag.ViewType = ViewType;
            ViewBag.StartBillDate = StartBillDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndBillDate = EndBillDate.Value.ToString("yyyy-MM-dd");

            if (ViewType == "Summary")
            {
                var summaryData = await GetGstr1SummaryData(StartBillDate, EndBillDate);
                return View(summaryData);
            }
            //else if (ViewType == "Details")
            //{
            //    var sales = await _salesservice.GetAllGSTR1Hsn();

            //    // ✅ Date Filter
            //    sales = sales
            //        .Where(x => x.BillDate >= StartBillDate && x.BillDate <= EndBillDate)
            //        .ToList();

            //    return View("GSTR1DetailsView", sales);
            //}
            else if (ViewType == "Details")
            {
                var sales = await _salesservice.GetAllGSTR1Hsn();

                sales = sales
                    .Where(x => x.BillDate >= StartBillDate && x.BillDate <= EndBillDate)
                    .ToList();

                string companyStateCode = "07"; // ya DB se lo

                var data = CalculateGstr1Details(sales, companyStateCode);

                return View("GSTR1DetailsView", data);
            }

            return View(new List<Gstr1SummaryVM>());
        }
        private List<Gstr1DetailVM> CalculateGstr1Details(IList<Sales> sales, string companyStateCode)
        {
            var result = new List<Gstr1DetailVM>();
            int srNo = 1;

            foreach (var sale in sales)
            {
                var validItems = sale.SalesItems?
                    .Where(x => x.Deleted == null && x.ItemMaster != null && x.Qty > 0)
                    .ToList();

                if (validItems == null || !validItems.Any())
                    continue;

                // ✅ IMPORTANT: Bill Discount %
                decimal billDiscountPercent = sale.discountPercent;

                string customerGST = sale.Customers?.GSTNo ?? "";
                string partyStateCode = (!string.IsNullOrEmpty(customerGST) && customerGST.Length >= 2)
                    ? customerGST.Substring(0, 2)
                    : null;

                bool isLocal = string.IsNullOrEmpty(customerGST) ||
                               partyStateCode == companyStateCode;

                string groupName =
                    !string.IsNullOrEmpty(customerGST) ? "B2B" :
                    (sale.Total > 250000 ? "B2C (Large) Invoice" : "B2C (Small) Invoice");
                decimal invoiceTotal = Math.Round(sale.Total + sale.TotalGstAmt, 2);
                foreach (var item in validItems)
                {
                    int conversion = item.ItemMaster?.Conversion ?? 1;

                    decimal effectiveRate;

                    // 👉 अगर strip rate available है तो use करो
                    if (item.StripRate > 0 && conversion > 1)
                    {
                        effectiveRate = item.StripRate / conversion; // strip → per tab
                    }
                    else
                    {
                        effectiveRate = item.Rate; // already per tab
                    }
                    // ✅ STEP 1: BASIC
                    decimal basicAmount = item.Qty * item.StripRate;

                    // ITEM DISCOUNT %
                    decimal itemDiscountAmount = basicAmount * item.Discount / 100;

                    // AFTER ITEM DISCOUNT
                    decimal netItemAmount = basicAmount - itemDiscountAmount;

                    // BILL DISCOUNT %
                    decimal billDiscount = netItemAmount * billDiscountPercent / 100;

                    // FINAL TAXABLE
                    decimal taxable = netItemAmount - billDiscount;

                    // ✅ GST
                    decimal gstPercent = item.Gst;
                    decimal cessPer = item.ItemMaster?.Hsn?.Cess ?? 0;

                    decimal sgst = 0, cgst = 0, igst = 0;
                    decimal sgstPer = 0, cgstPer = 0, igstPer = 0;

                    if (gstPercent > 0)
                    {
                        if (isLocal)
                        {
                            sgstPer = gstPercent / 2;
                            cgstPer = gstPercent / 2;

                            sgst = Math.Round(taxable * sgstPer / 100, 2);
                            cgst = Math.Round(taxable * cgstPer / 100, 2);
                        }
                        else
                        {
                            igstPer = gstPercent;
                            igst = Math.Round(taxable * igstPer / 100, 2);
                        }
                    }

                    // ✅ CESS
                    decimal cessAmt = Math.Round(taxable * (cessPer / 100), 2);

                    decimal totalGST = sgst + cgst + igst + cessAmt;

                    // ✅ FINAL AMOUNT
                    decimal grossAmount = Math.Round(taxable + totalGST, 2);

                    // ✅ Qty Format
                    string qtyDisplay;
                    // int conversion = item.ItemMaster?.Conversion ?? 1;

                    if (conversion > 1)
                    {
                        int totalTabs = (int)Math.Round(item.Qty * conversion);
                        int strips = totalTabs / conversion;
                        int tabs = totalTabs % conversion;

                        qtyDisplay = strips > 0
                            ? $"{strips} Strip + {tabs} Tab"
                            : $"{tabs} Tab";
                    }
                    else
                    {
                        qtyDisplay = item.Qty.ToString();
                    }

                    result.Add(new Gstr1DetailVM
                    {
                        SrNo = srNo++,
                        GroupName = groupName,

                        CustomerName = sale.Customers?.Name,
                        GSTIN = customerGST,
                        BillDate = sale.BillDate,
                        BillNo = sale.BillNo,

                        InvoiceValue = invoiceTotal,
                        SupplyType = isLocal ? "Local" : "Central",

                        HSN = item.ItemMaster?.Hsn?.HsnCode,
                        QtyDisplay = qtyDisplay,

                        Amount = grossAmount,
                        Taxable = taxable,

                        SGSTPer = sgstPer,
                        SGST = sgst,

                        CGSTPer = cgstPer,
                        CGST = cgst,

                        IGSTPer = igstPer,
                        IGST = igst,

                        Cess = cessAmt,
                        TotalGST = totalGST
                    });
                }
            }

            return result;
        }
        //public async Task<IActionResult> GSTR1Report(DateTime? StartBillDate, DateTime? EndBillDate, string ViewType = "Summary")
        //{
        //    ViewBag.ViewType = ViewType;

        //    if (ViewType == "Summary")
        //    {
        //        var summaryData = await GetGstr1SummaryData(StartBillDate, EndBillDate);
        //        return View(summaryData);
        //    }

        //    var details = await GetGSTR1Data(StartBillDate, EndBillDate);
        //    return View(details);
        //}

        //Excel GSTR1Report

        public async Task<IActionResult> ExportGSTR1Excel(DateTime? StartBillDate, DateTime? EndBillDate, string ViewType = "Summary")
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("GSTR1 Report");

            var user = await _usersManager.GetUserAsync(User);
            var tenant = await _tenantRepository.GetById(user.TenantId);

            string companyName = tenant?.Name ?? "";
            string address = tenant?.Address1 ?? "";
            string gstin = tenant?.GstNo ?? "";

            string companyStateCode = !string.IsNullOrEmpty(gstin) && gstin.Length >= 2
                ? gstin.Substring(0, 2)
                : null;

            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 20).Merge().Style.Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = address;
            ws.Range(2, 1, 2, 20).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"GSTIN : {gstin}";
            ws.Range(3, 1, 3, 20).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(4, 1).Value = $"GSTR1 {ViewType.ToUpper()} FOR THE PERIOD {StartBillDate:dd/MM/yyyy} TO {EndBillDate:dd/MM/yyyy}";
            ws.Range(4, 1, 4, 20).Merge().Style.Font.SetBold()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int headerRow = 6;

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

                int row = headerRow + 1;

                var data = await GetGstr1SummaryData(StartBillDate, EndBillDate);

                foreach (var d in data)
                {
                    ws.Cell(row, 1).Value = d.Key.Replace("_", " ");
                    ws.Cell(row, 2).Value = d.Count;
                    ws.Cell(row, 3).Value = d.Taxable;
                    ws.Cell(row, 4).Value = d.SGST;
                    ws.Cell(row, 5).Value = d.CGST;
                    ws.Cell(row, 6).Value = d.IGST;
                    ws.Cell(row, 7).Value = d.Cess;
                    ws.Cell(row, 8).Value = d.TotalGST;
                    ws.Cell(row, 9).Value = d.InvoiceAmount;

                    if (d.Key == "TOTAL")
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
                ws.Cell(headerRow, 2).Value = "Desc";
                ws.Cell(headerRow, 3).Value = "GSTIN";
                ws.Cell(headerRow, 4).Value = "Invoice Date";
                ws.Cell(headerRow, 5).Value = "Invoice No.";
                ws.Cell(headerRow, 6).Value = "Invoice Value";
                ws.Cell(headerRow, 7).Value = "Local/Central";
                ws.Cell(headerRow, 8).Value = "Invoice Type";
                ws.Cell(headerRow, 9).Value = "HSN Code";
                ws.Cell(headerRow, 10).Value = "Quantity";
                ws.Cell(headerRow, 11).Value = "Amount";
                ws.Cell(headerRow, 12).Value = "Taxable Amount";

                ws.Range(headerRow, 13, headerRow, 14).Merge().Value = "SGST";
                ws.Range(headerRow, 15, headerRow, 16).Merge().Value = "CGST";
                ws.Range(headerRow, 17, headerRow, 18).Merge().Value = "IGST";

                ws.Cell(headerRow, 19).Value = "Cess";
                ws.Cell(headerRow, 20).Value = "Total GST";

                ws.Cell(headerRow + 1, 13).Value = "%age";
                ws.Cell(headerRow + 1, 14).Value = "Amount";
                ws.Cell(headerRow + 1, 15).Value = "%age";
                ws.Cell(headerRow + 1, 16).Value = "Amount";
                ws.Cell(headerRow + 1, 17).Value = "%age";
                ws.Cell(headerRow + 1, 18).Value = "Amount";

                for (int i = 1; i <= 12; i++)
                    ws.Range(headerRow, i, headerRow + 1, i).Merge();

                ws.Range(headerRow, 19, headerRow + 1, 19).Merge();
                ws.Range(headerRow, 20, headerRow + 1, 20).Merge();

                ws.Range(headerRow, 1, headerRow + 1, 20).Style.Font.SetBold()
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                int row = headerRow + 2;

                var sales = await _salesservice.GetAllGSTR1Hsn();

                if (StartBillDate.HasValue)
                    sales = sales.Where(x => x.BillDate >= StartBillDate.Value).ToList();

                if (EndBillDate.HasValue)
                    sales = sales.Where(x => x.BillDate <= EndBillDate.Value).ToList();

                var groupNames = new List<string>
    {
        "B2B",
        "B2C (Large) Invoice",
        "B2C (Small) Invoice",
        "Nil Rated/Exempted",
        "Export Invoices",
        "Tax Liability on Advance",
        "Set/off Tax on Advance of prior period"
    };

                var groupedData = sales.GroupBy(s =>
                    !string.IsNullOrEmpty(s.Customers?.GSTNo) ? "B2B" :
                    (s.Total > 250000 ? "B2C (Large) Invoice" : "B2C (Small) Invoice")
                ).ToDictionary(g => g.Key, g => g.ToList());

                int srNo = 1;

                decimal grandTaxable = 0, grandSGST = 0, grandCGST = 0,
                        grandIGST = 0, grandCess = 0, grandGST = 0, grandInvoice = 0;

                foreach (var groupName in groupNames)
                {
                    ws.Cell(row, 2).Value = groupName;
                    ws.Cell(row, 2).Style.Font.SetBold();
                    row++;

                    if (!groupedData.ContainsKey(groupName))
                        continue;

                    foreach (var sale in groupedData[groupName])
                    {

                        var validItems = sale.SalesItems
                            .Where(x => x.Deleted == null && x.ItemMaster != null && x.Amount > 0)
                            .ToList();
                        decimal billDiscount = sale.Totaldiscount; // ya discountAmount jo use kar rahi ho
                        decimal totalItemAmount = validItems.Sum(x => x.Amount);
                        if (!validItems.Any())
                            continue;

                        decimal invoiceTotal = sale.Total + sale.TotalGstAmt;
                        grandInvoice += invoiceTotal;

                        bool isFirstItem = true;

                        string customerGST = sale.Customers?.GSTNo ?? "";
                        string partyStateCode = (!string.IsNullOrEmpty(customerGST) && customerGST.Length >= 2)
                            ? customerGST.Substring(0, 2)
                            : null;

                        bool isLocal = string.IsNullOrEmpty(customerGST) ||
                            (!string.IsNullOrEmpty(companyStateCode) && partyStateCode == companyStateCode);

                        foreach (var item in validItems)
                        {
                            decimal itemAmount = item.Amount;

                            decimal gstPercent = item.Gst;
                            decimal cessPer = item.ItemMaster?.Hsn?.Cess ?? 0;
                            decimal totalTaxPercent = gstPercent + cessPer;

                            // ✅ STEP 1: ORIGINAL TAXABLE
                            decimal originalTaxable = totalTaxPercent > 0
                                ? itemAmount / (1 + totalTaxPercent / 100)
                                : itemAmount;

                            // ✅ STEP 2: DISTRIBUTE DISCOUNT
                            decimal ratio = totalItemAmount > 0 ? itemAmount / totalItemAmount : 0;
                            decimal itemDiscount = billDiscount * ratio;

                            // ✅ STEP 3: DISCOUNT ON TAXABLE
                            decimal discountTaxable = totalTaxPercent > 0
                                ? itemDiscount / (1 + totalTaxPercent / 100)
                                : itemDiscount;

                            decimal taxable = Math.Round(originalTaxable - discountTaxable, 2);

                            // ✅ STEP 4: GST
                            decimal sgst = 0, cgst = 0, igst = 0;
                            decimal sgstPer = 0, cgstPer = 0, igstPer = 0;

                            if (gstPercent > 0)
                            {
                                if (isLocal)
                                {
                                    sgstPer = gstPercent / 2;
                                    cgstPer = gstPercent / 2;

                                    sgst = Math.Round(taxable * sgstPer / 100, 2);
                                    cgst = Math.Round(taxable * cgstPer / 100, 2);
                                }
                                else
                                {
                                    igstPer = gstPercent;
                                    igst = Math.Round(taxable * igstPer / 100, 2);
                                }
                            }

                            decimal cessAmt = Math.Round(taxable * (cessPer / 100), 2);

                            decimal totalItemGst = sgst + cgst + igst + cessAmt;

                            // ✅ FINAL AMOUNT
                            decimal grossAmount = Math.Round(taxable + totalItemGst, 2);
                            int col = 1;

                            ws.Cell(row, col++).Value = srNo;

                            if (isFirstItem)
                            {
                                ws.Cell(row, col++).Value = sale.Customers?.Name;
                                ws.Cell(row, col++).Value = customerGST;
                                ws.Cell(row, col++).Value = sale.BillDate?.ToString("dd-MM-yyyy");
                                ws.Cell(row, col++).Value = sale.BillNo;
                                ws.Cell(row, col++).Value = Math.Round(invoiceTotal, 2);
                                ws.Cell(row, col++).Value = isLocal ? "Local" : "Central";
                                ws.Cell(row, col++).Value = "Inventory";

                                isFirstItem = false;
                            }
                            else
                            {
                                for (int i = 2; i <= 8; i++)
                                    ws.Cell(row, i).Value = "";

                                col = 9;
                            }

                            srNo++;

                            // ✅ HSN
                            ws.Cell(row, col++).Value = item.ItemMaster?.Hsn?.HsnCode ?? "";

                            // ✅ QTY FORMAT (SAME AS VIEW)
                            decimal qty = item.Qty;
                            int conversion = item.ItemMaster?.Conversion ?? 1;

                            string qtyDisplay;
                            if (conversion > 1)
                            {
                                int totalTabs = (int)Math.Round(qty * conversion);
                                int strips = totalTabs / conversion;
                                int tabs = totalTabs % conversion;

                                qtyDisplay = strips > 0
                                    ? $"{strips} Strip + {tabs} Tab"
                                    : $"{tabs} Tab";
                            }
                            else
                            {
                                qtyDisplay = qty.ToString();
                            }

                            ws.Cell(row, col++).Value = qtyDisplay;

                            ws.Cell(row, col++).Value = Math.Round(grossAmount, 2);
                            ws.Cell(row, col++).Value = Math.Round(taxable, 2);

                            ws.Cell(row, col++).Value = sgstPer;
                            ws.Cell(row, col++).Value = Math.Round(sgst, 2);

                            ws.Cell(row, col++).Value = cgstPer;
                            ws.Cell(row, col++).Value = Math.Round(cgst, 2);

                            ws.Cell(row, col++).Value = igstPer;
                            ws.Cell(row, col++).Value = Math.Round(igst, 2);

                            ws.Cell(row, col++).Value = Math.Round(cessAmt, 2);
                            ws.Cell(row, col++).Value = Math.Round(totalItemGst, 2);

                            // ✅ TOTALS
                            grandTaxable += taxable;
                            grandSGST += sgst;
                            grandCGST += cgst;
                            grandIGST += igst;
                            grandCess += cessAmt;
                            grandGST += totalItemGst;

                            row++;
                        }
                    }
                }

                // ✅ GRAND TOTAL
                ws.Cell(row, 2).Value = "Gross Total";
                ws.Cell(row, 6).Value = Math.Round(grandInvoice, 2);
                ws.Cell(row, 12).Value = Math.Round(grandTaxable, 2);
                ws.Cell(row, 14).Value = Math.Round(grandSGST, 2);
                ws.Cell(row, 16).Value = Math.Round(grandCGST, 2);
                ws.Cell(row, 18).Value = Math.Round(grandIGST, 2);
                ws.Cell(row, 19).Value = Math.Round(grandCess, 2);
                ws.Cell(row, 20).Value = Math.Round(grandGST, 2);

                ws.Range(row, 1, row, 20).Style.Font.SetBold()
                    .Fill.SetBackgroundColor(XLColor.LightYellow);

                ws.SheetView.FreezeRows(7);
                ws.Columns().AdjustToContents();
            }

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"GSTR1_{ViewType}.xlsx");
        }

        private async Task<List<Gstr1SummaryVM>> GetGstr1SummaryData(DateTime? startDate, DateTime? endDate)
        {
            var tenantId = User.FindFirstValue("TenantId");

            var result = new List<Gstr1SummaryVM>();

            // Get B2C Large data
            var b2cLargeData = await GetB2CLargeData(startDate, endDate, tenantId);
            if (b2cLargeData != null)
                result.Add(b2cLargeData);

            // Get B2C Small data
            var b2cSmallData = await GetB2CSmallData(startDate, endDate, tenantId);
            if (b2cSmallData != null)
                result.Add(b2cSmallData);

            //// Get Nil Rated / Exempted data
            //var nilData = await GetNilRatedExemptedData(startDate, endDate, tenantId);
            //if (nilData != null)
            //    result.Add(nilData);

            var nilRated = await GetNilRatedData(startDate, endDate);
            var exempted = await GetExemptedData(startDate, endDate);

            if (nilRated != null)
                result.Add(nilRated);

            if (exempted != null)
                result.Add(exempted);

            var nilTotal = await GetNilCombinedData(nilRated, exempted);
            if (nilTotal != null)
                result.Add(nilTotal);

            // Calculate Total
            //if (result.Any())
            //{
            //    result.Add(new Gstr1SummaryVM
            //    {
            //        Key = Gstr1SummaryKeys.TOTAL,
            //        Count = result.Sum(x => x.Count),
            //        Taxable = result.Sum(x => x.Taxable),
            //        SGST = result.Sum(x => x.SGST),
            //        CGST = result.Sum(x => x.CGST),
            //        IGST = result.Sum(x => x.IGST),
            //        Cess = result.Sum(x => x.Cess),
            //        InvoiceAmount = result.Sum(x => x.InvoiceAmount)
            //    });
            //}
            if (result.Any())
            {
                var totalRows = result.Where(x => x.Key != Gstr1SummaryKeys.NIL_RATED && x.Key != Gstr1SummaryKeys.EXEMPTED).ToList();

                result.Add(new Gstr1SummaryVM
                {
                    Key = Gstr1SummaryKeys.TOTAL,
                    Count = totalRows.Sum(x => x.Count),
                    Taxable = totalRows.Sum(x => x.Taxable),
                    SGST = totalRows.Sum(x => x.SGST),
                    CGST = totalRows.Sum(x => x.CGST),
                    IGST = totalRows.Sum(x => x.IGST),
                    Cess = totalRows.Sum(x => x.Cess),
                    InvoiceAmount = totalRows.Sum(x => x.InvoiceAmount)
                });
            }
            return result;
        }

        // Get B2C Large Data
        private async Task<Gstr1SummaryVM> GetB2CLargeData(DateTime? startDate, DateTime? endDate, string tenantId)
        {
            var user = (await _tenantRepository.GetAll())
                       .FirstOrDefault(x => x.Id == tenantId);

            if (user == null)
                return null;

            // Tenant ka state code (assuming it's stored in user table or separate tenant table)
            // Adjust this according to your actual tenant structure
            int? tenantStateCode = (int?)user.StateCode; // or however you get tenant state

            // Get B2C Large invoices from repository
            var sales = await _salesservice.GetB2CLargeInvoices(tenantId, startDate, endDate);

            if (!sales.Any())
                return null;

            decimal totalTaxable = 0;
            decimal totalSGST = 0;
            decimal totalCGST = 0;
            decimal totalIGST = 0;
            decimal totalCess = 0;
            decimal totalInvoiceValue = 0;
            int count = 0;
            foreach (var sale in sales)
            {
                // Get customer state
                //var customerStateCode = sale.Customers?.StateCode;
                //var isSameState = (int?)customerStateCode == tenantStateCode;

                //foreach (var item in sale.SalesItems.Where(si => si.Deleted == null && si.ItemMaster != null))
                //{
                //    // Check if item is taxable (not exempted)
                //    bool isTaxable = (int?)(item.ItemMaster?.Local ?? 0) == 1 && (int?)(item.ItemMaster?.Central ?? 0) == 1;

                //    // Calculate taxable amount (Amount without GST)
                //    decimal taxableAmount = item.Gst > 0
                //        ? item.Amount / (1 + (item.Gst / 100))
                //        : item.Amount;

                //    totalTaxable += taxableAmount;

                //    // Calculate GST amount
                //    decimal gstAmount = item.Amount - taxableAmount;

                //    // Distribute GST based on state and taxability
                //    if (isTaxable && item.Gst > 0)
                //    {
                //        if (isSameState)
                //        {
                //            // Intra-state: SGST + CGST (50-50)
                //            totalSGST += gstAmount / 2;
                //            totalCGST += gstAmount / 2;
                //        }
                //        else
                //        {
                //            // Inter-state: IGST
                //            totalIGST += gstAmount;
                //        }
                //    }

                //    totalInvoiceValue += item.Amount;
                //}
                var items = sale.SalesItems?.Where(x => x.Deleted == null && x.ItemMaster != null).ToList();

                if (items == null || !items.Any())
                    continue;

                bool hasTaxableItem = items.Any(i => i.Gst > 0 && i.ItemMaster.Local == TaxStatus.Taxable && i.ItemMaster.Central == TaxStatus.Taxable);

                if (!hasTaxableItem)
                    continue;
                var customerStateCode = sale.Customers?.StateCode;
                bool isSameState = (int?)customerStateCode == tenantStateCode;

                decimal saleTaxable = sale.Total;
                decimal saleGst = sale.TotalGstAmt;

                totalTaxable += saleTaxable;
                // totalInvoiceValue += sale.TotalPayable;
                totalInvoiceValue += saleTaxable + saleGst;

                if (saleGst > 0)
                {
                    if (isSameState || sale.billingType == "Cash")
                    {
                        totalCGST += saleGst / 2;
                        totalSGST += saleGst / 2;
                    }
                    else
                    {
                        totalIGST += saleGst;
                    }
                }
                count++;
            }

            return new Gstr1SummaryVM
            {
                Key = Gstr1SummaryKeys.B2C_LARGE,
                Count = count, //sales.Count,
                Taxable = totalTaxable,
                SGST = totalSGST,
                CGST = totalCGST,
                IGST = totalIGST,
                Cess = totalCess,
                InvoiceAmount = totalInvoiceValue
            };
        }

        // Get B2C Small Data
        private async Task<Gstr1SummaryVM> GetB2CSmallData(DateTime? startDate, DateTime? endDate, string tenantId)
        {

            var user = (await _tenantRepository.GetAll()).FirstOrDefault(x => x.Id == tenantId);


            if (user == null)
                return null;

            int? tenantStateCode = (int?)user.StateCode;

            var sales = await _salesservice.GetB2CSmallInvoices(tenantId, startDate, endDate);

            if (!sales.Any())
                return null;

            decimal totalTaxable = 0;
            decimal totalSGST = 0;
            decimal totalCGST = 0;
            decimal totalIGST = 0;
            decimal totalCess = 0;
            decimal totalInvoiceValue = 0;
            int count = 0;

            foreach (var sale in sales)
            {
                //var customerStateCode = sale.Customers?.StateCode;
                //var isSameState = (int?)customerStateCode == tenantStateCode;

                //foreach (var item in sale.SalesItems.Where(si => si.Deleted == null && si.ItemMaster != null))
                //{
                //    //bool isTaxable = item.ItemMaster.Local == 0 && item.ItemMaster.Central == 0;
                //    bool isTaxable = (int?)(item.ItemMaster?.Local ?? 0) == 1 && (int?)(item.ItemMaster?.Central ?? 0) == 1;

                //    decimal taxableAmount = item.Gst > 0
                //        ? item.Amount / (1 + (item.Gst / 100))
                //        : item.Amount;

                //   // totalTaxable += taxableAmount;
                //    decimal gstAmount = item.Amount - taxableAmount;

                //    if (isTaxable && item.Gst > 0)
                //    {
                //        if (isSameState || sale.billingType == "Cash")
                //        {
                //            totalSGST += gstAmount / 2;
                //            totalCGST += gstAmount / 2;
                //        }
                //        else
                //        {
                //            totalIGST += gstAmount;
                //        }
                //    }

                //   // totalInvoiceValue += item.Amount;
                //}
                //totalTaxable += sale.Total;
                //totalInvoiceValue += (sale.Total + sale.TotalGstAmt);
                var items = sale.SalesItems?.Where(x => x.Deleted == null && x.ItemMaster != null).ToList();

                if (items == null || !items.Any())
                    continue;

                bool hasTaxableItem = items.Any(i => i.Gst > 0 && i.ItemMaster.Local == TaxStatus.Taxable && i.ItemMaster.Central == TaxStatus.Taxable);

                if (!hasTaxableItem)
                    continue;

                var customerStateCode = sale.Customers?.StateCode;
                bool isSameState = customerStateCode == null || (int?)customerStateCode == tenantStateCode;

                decimal saleTaxable = sale.Total;
                decimal saleGst = sale.TotalGstAmt;
                decimal saleCess = sale.TotalCessAmount;

                totalTaxable += saleTaxable;
                totalCess += saleCess;
                // totalInvoiceValue += sale.TotalPayable;
                totalInvoiceValue += saleTaxable + saleGst + saleCess;

                if (saleGst > 0)
                {
                    if (isSameState || sale.billingType == "Cash")
                    {
                        totalCGST += saleGst / 2;
                        totalSGST += saleGst / 2;
                    }
                    else
                    {
                        totalIGST += saleGst;
                    }
                }
                count++;
            }

            return new Gstr1SummaryVM
            {
                Key = Gstr1SummaryKeys.B2C_SMALL,
                Count = count,  //sales.Count,
                Taxable = totalTaxable,
                SGST = totalSGST,
                CGST = totalCGST,
                IGST = totalIGST,
                Cess = totalCess,
                InvoiceAmount = totalInvoiceValue
            };
        }

        //private async Task<Gstr1SummaryVM> GetNilRatedExemptedData(DateTime? startDate, DateTime? endDate, string tenantId)
        //{
        //    var sales = await _salesservice.GetAll();

        //    if (startDate.HasValue)
        //        sales = sales.Where(x => x.BillDate >= startDate.Value).ToList();

        //    if (endDate.HasValue)
        //        sales = sales.Where(x => x.BillDate <= endDate.Value).ToList();

        //    decimal totalTaxable = 0;
        //    decimal totalInvoiceValue = 0;
        //    int count = 0;

        //    foreach (var sale in sales)
        //    {
        //        var items = sale.SalesItems?
        //            .Where(x => x.Deleted == null && x.ItemMaster != null)
        //            .ToList();

        //        if (items == null || !items.Any())
        //            continue;

        //        bool allExempted = items.All(i =>
        //            i.ItemMaster.Local == TaxStatus.Exempted &&
        //            i.ItemMaster.Central == TaxStatus.Exempted
        //        );

        //        bool allNilRated = items.All(i =>
        //            i.Gst == 0 &&
        //            i.ItemMaster.Local == TaxStatus.Taxable &&
        //            i.ItemMaster.Central == TaxStatus.Taxable
        //        );

        //        if (allExempted || allNilRated)
        //        {
        //            decimal saleTaxable = sale.Total;
        //            decimal saleGst = sale.TotalGstAmt;

        //            totalTaxable += saleTaxable;
        //            totalInvoiceValue += saleTaxable + saleGst;

        //            count++;
        //        }
        //    }

        //    if (count == 0)
        //        return null;

        //    return new Gstr1SummaryVM
        //    {
        //        Key = Gstr1SummaryKeys.NIL,
        //        Count = count,
        //        Taxable = totalTaxable,
        //        SGST = 0,
        //        CGST = 0,
        //        IGST = 0,
        //        Cess = 0,
        //        InvoiceAmount = totalInvoiceValue
        //    };
        //}

        private async Task<Gstr1SummaryVM> GetNilRatedData(DateTime? startDate, DateTime? endDate)
        {
            var sales = await _salesservice.GetAll();

            if (startDate.HasValue)
                sales = sales.Where(x => x.BillDate >= startDate.Value).ToList();

            if (endDate.HasValue)
                sales = sales.Where(x => x.BillDate <= endDate.Value).ToList();

            decimal totalTaxable = 0;
            decimal totalInvoiceValue = 0;
            int count = 0;

            foreach (var sale in sales)
            {
                var items = sale.SalesItems?.Where(x => x.Deleted == null && x.ItemMaster != null).ToList();
                if (items == null || !items.Any()) continue;

                bool allNilRated = items.All(i =>
                    i.Gst == 0 &&
                    i.ItemMaster.Local == TaxStatus.Taxable &&
                    i.ItemMaster.Central == TaxStatus.Taxable
                );

                if (allNilRated)
                {
                    totalTaxable += sale.Total;
                    totalInvoiceValue += sale.Total + sale.TotalGstAmt;
                    count++;
                }
            }

            if (count == 0) return null;

            return new Gstr1SummaryVM
            {
                Key = Gstr1SummaryKeys.NIL_RATED,
                Count = count,
                Taxable = totalTaxable,
                InvoiceAmount = totalInvoiceValue
            };
        }

        private async Task<Gstr1SummaryVM> GetExemptedData(DateTime? startDate, DateTime? endDate)
        {
            var sales = await _salesservice.GetAll();

            if (startDate.HasValue)
                sales = sales.Where(x => x.BillDate >= startDate.Value).ToList();

            if (endDate.HasValue)
                sales = sales.Where(x => x.BillDate <= endDate.Value).ToList();

            decimal totalTaxable = 0;
            decimal totalInvoiceValue = 0;
            int count = 0;

            foreach (var sale in sales)
            {
                var items = sale.SalesItems?.Where(x => x.Deleted == null && x.ItemMaster != null).ToList();
                if (items == null || !items.Any()) continue;

                bool allExempted = items.All(i =>
                    i.ItemMaster.Local == TaxStatus.Exempted &&
                    i.ItemMaster.Central == TaxStatus.Exempted
                );

                if (allExempted)
                {
                    totalTaxable += sale.Total;
                    totalInvoiceValue += sale.Total + sale.TotalGstAmt;
                    count++;
                }
            }

            if (count == 0) return null;

            return new Gstr1SummaryVM
            {
                Key = Gstr1SummaryKeys.EXEMPTED,
                Count = count,
                Taxable = totalTaxable,
                InvoiceAmount = totalInvoiceValue
            };
        }

        private async Task<Gstr1SummaryVM> GetNilCombinedData(Gstr1SummaryVM nilRated, Gstr1SummaryVM exempted)
        {
            if (nilRated == null && exempted == null)
                return null;

            return new Gstr1SummaryVM
            {
                Key = Gstr1SummaryKeys.NIL,
                Count = (nilRated?.Count ?? 0) + (exempted?.Count ?? 0),
                Taxable = (nilRated?.Taxable ?? 0) + (exempted?.Taxable ?? 0),
                InvoiceAmount = (nilRated?.InvoiceAmount ?? 0) + (exempted?.InvoiceAmount ?? 0)
            };
        }

        // GSTR1 Keys Constants
        public static class Gstr1SummaryKeys
        {
            public const string B2B = "B2B";
            public const string B2C_LARGE = "B2C_LARGE";
            public const string B2C_SMALL = "B2C_SMALL";
            public const string NIL = "NIL";
            public const string NIL_RATED = "NIL_RATED";
            public const string EXEMPTED = "EXEMPTED";
            public const string EXPORT = "EXPORT";
            public const string ADVANCE = "ADVANCE";
            public const string TOTAL = "TOTAL";
        }

        // Get Item-wise Details (for Details view)
        private async Task<List<StockVM>> GetGSTR1Data(DateTime? startDate, DateTime? endDate)
        {
            var itemMasters = await _itemmasterservice.GetAll();
            var sales = await _salesservice.GetAll();

            if (startDate.HasValue)
                sales = sales.Where(x => x.BillDate >= startDate.Value).ToList();

            if (endDate.HasValue)
                sales = sales.Where(x => x.BillDate <= endDate.Value).ToList();

            var salesItems = sales.Where(s => s.SalesItems != null).SelectMany(s => s.SalesItems);

            var groupedSales = salesItems
                .GroupBy(x => x.ItemMasterId)
                .Select(g => new
                {
                    ItemMasterId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Amount),
                    TotalGst = g.Sum(x =>
                    {
                        decimal gross = x.Rate * x.Qty;
                        decimal discount = gross * (x.Discount / 100);
                        decimal taxable = gross - discount;
                        return taxable * (x.Gst / 100);
                    })
                }).ToList();

            return (from item in itemMasters
                    join g in groupedSales on item.Id equals g.ItemMasterId
                    where g.TotalQty > 0
                    select new StockVM
                    {
                        ItemCode = item.Code,
                        ItemName = item.Name,
                        CategoryName = item.Category?.CategoryName ?? "Unknown",
                        Stocks = g.TotalQty,
                        GstAmount = g.TotalGst,
                        Amount = g.TotalAmount
                    }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetGstr1Bills(string type, DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var today = DateTime.Today;

                startDate = new DateTime(today.Year, today.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
            }

            var tenantId = User.FindFirstValue("TenantId");

            var user = (await _tenantRepository.GetAll())
                .FirstOrDefault(x => x.Id == tenantId);

            if (user == null)
                return Json(new List<object>());

            int? tenantStateCode = (int?)user.StateCode;

            IEnumerable<Models.Entity.Sales> sales;

            if (type == Gstr1SummaryKeys.B2C_SMALL)
            {
                sales = await _salesservice.GetB2CSmallInvoices(tenantId, startDate, endDate);
            }
            else if (type == Gstr1SummaryKeys.B2C_LARGE)
            {
                sales = await _salesservice.GetB2CLargeInvoices(tenantId, startDate, endDate);
            }
            else
            {
                sales = await _salesservice.GetAll();
            }
            sales = sales.Where(x => x.BillDate >= startDate && x.BillDate <= endDate);
            if (startDate.HasValue)
                sales = sales.Where(x => x.BillDate >= startDate.Value);

            if (endDate.HasValue)
                sales = sales.Where(x => x.BillDate <= endDate.Value);

            var result = new List<object>();

            foreach (var sale in sales)
            {
                var items = sale.SalesItems?
                    .Where(x => x.Deleted == null && x.ItemMaster != null)
                    .ToList();

                if (items == null || !items.Any())
                    continue;

                bool include = false;
                string invoiceCategory = type;
                // B2C SMALL / LARGE
                if (type == Gstr1SummaryKeys.B2C_SMALL || type == Gstr1SummaryKeys.B2C_LARGE)
                {
                    include = items.Any(i =>
                        i.Gst > 0 &&
                        i.ItemMaster.Local == TaxStatus.Taxable &&
                        i.ItemMaster.Central == TaxStatus.Taxable);
                }

                // NIL RATED
                else if (type == Gstr1SummaryKeys.NIL_RATED)
                {
                    include = items.All(i =>
                        i.Gst == 0 &&
                        i.ItemMaster.Local == TaxStatus.Taxable &&
                        i.ItemMaster.Central == TaxStatus.Taxable);
                }

                // EXEMPTED
                else if (type == Gstr1SummaryKeys.EXEMPTED)
                {
                    include = items.All(i =>
                        i.ItemMaster.Local == TaxStatus.Exempted &&
                        i.ItemMaster.Central == TaxStatus.Exempted);
                }

                // NIL (COMBINED)
                else if (type == Gstr1SummaryKeys.NIL)
                {
                    bool isNilRated = items.All(i =>
                        i.Gst == 0 &&
                        i.ItemMaster.Local == TaxStatus.Taxable &&
                        i.ItemMaster.Central == TaxStatus.Taxable);

                    bool isExempted = items.All(i =>
                        i.ItemMaster.Local == TaxStatus.Exempted &&
                        i.ItemMaster.Central == TaxStatus.Exempted);

                    if (isNilRated)
                    {
                        include = true;
                        invoiceCategory = Gstr1SummaryKeys.NIL_RATED;
                    }
                    else if (isExempted)
                    {
                        include = true;
                        invoiceCategory = Gstr1SummaryKeys.EXEMPTED;
                    }
                }

                if (!include)
                    continue;

                result.Add(new
                {
                    id = sale.Id,
                    billNo = sale.BillNo,
                    billDate = sale.BillDate?.ToString("dd-MM-yyyy"),
                    customer = sale.Customers?.Name,
                    taxable = sale.Total,
                    gst = sale.TotalGstAmt + sale.TotalCessAmount,
                    invoiceAmount = sale.Total + sale.TotalGstAmt + sale.TotalCessAmount,
                    invoicecategory = invoiceCategory
                });
            }

            return Json(result);
        }
        public async Task<IActionResult> GetBillItems(int id)
        {
            var tenantId = User.FindFirstValue("TenantId");

            var tenant = (await _tenantRepository.GetAll())
                .FirstOrDefault(x => x.Id == tenantId);

            var sale = await _salesservice.GetById(id);

            int? companyState = (int?)tenant?.StateCode;
            int? customerState = (int?)sale.Customers?.StateCode;

            bool isLocal = companyState == customerState || sale.billingType == "Cash";

            var items = sale.SalesItems
                .Where(x => x.Deleted == null)
                .Select(x =>
                {
                    // 🔥 In DB, x.Amount contains the GROSS amount (e.g., 33.52)
                    decimal grossAmount = x.Amount;
                    decimal gstPercent = x.Gst;
                    decimal cessPer = x.ItemMaster.Hsn.Cess;

                    decimal totalTaxPercent = gstPercent + cessPer;

                    // 🔥 THE FIX: REVERSE CALCULATION TO FIND ACTUAL TAXABLE
                    // Formula: Taxable = Gross / (1 + (Tax% / 100))
                    decimal taxable = 0;
                    if (totalTaxPercent > 0)
                    {
                        taxable = Math.Round(grossAmount / (1 + (totalTaxPercent / 100)), 2);
                    }
                    else
                    {
                        taxable = Math.Round(grossAmount, 2);
                    }

                    // Now calculate exact taxes on the extracted Taxable amount
                    decimal sgstPer = 0, cgstPer = 0, igstPer = 0;
                    decimal sgstTax = 0, cgstTax = 0, igstTax = 0;

                    decimal cessTax = Math.Round(taxable * (cessPer / 100), 2);

                    if (isLocal || sale.billingType == "Cash")
                    {
                        sgstPer = gstPercent / 2;
                        cgstPer = gstPercent / 2;

                        sgstTax = Math.Round(taxable * (sgstPer / 100), 2);
                        cgstTax = Math.Round(taxable * (cgstPer / 100), 2);
                    }
                    else
                    {
                        igstPer = gstPercent;
                        igstTax = Math.Round(taxable * (igstPer / 100), 2);
                    }

                    decimal totalTax = sgstTax + cgstTax + igstTax + cessTax;

                    return new
                    {
                        hsn = x.ItemMaster.Hsn.HsnCode,

                        amount = Math.Round(grossAmount, 2),  // Will display 33.52
                        taxable = taxable,                    // Will display 31.92

                        sgstPer,
                        sgstTax,                              // Will display 0.80

                        cgstPer,
                        cgstTax,                              // Will display 0.80

                        igstPer,
                        igstTax,

                        cessPer,
                        cess = cessTax,

                        totalTax,                             // Will display 1.60

                        taxType = isLocal ? x.ItemMaster.Local.ToString() : x.ItemMaster.Central.ToString()
                    };
                });

            return Json(items);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetBillItems(int id)
        //{
        //    var tenantId = User.FindFirstValue("TenantId");

        //    var tenant = (await _tenantRepository.GetAll())
        //        .FirstOrDefault(x => x.Id == tenantId);

        //    var sale = await _salesservice.GetById(id);

        //    int? companyState = (int?)tenant?.StateCode;
        //    int? customerState = (int?)sale.Customers?.StateCode;

        //    bool isLocal = companyState == customerState || sale.billingType == "Cash";

        //    var items = sale.SalesItems
        //        .Where(x => x.Deleted == null)
        //        .Select(x =>
        //        {
        //            decimal taxable = x.Amount;
        //            decimal gstPercent = x.Gst;

        //            decimal sgstPer = 0;
        //            decimal cgstPer = 0;
        //            decimal igstPer = 0;

        //            decimal sgstTax = 0;
        //            decimal cgstTax = 0;
        //            decimal igstTax = 0;

        //            // 🔥 CESS ADD
        //            decimal cessPer = x.ItemMaster.Hsn.Cess; // jo tumne DB me add kiya
        //            decimal cessTax = taxable * cessPer / 100;

        //            if (isLocal || sale.billingType == "Cash")
        //            {
        //                sgstPer = gstPercent / 2;
        //                cgstPer = gstPercent / 2;

        //                sgstTax = taxable * sgstPer / 100;
        //                cgstTax = taxable * cgstPer / 100;
        //            }
        //            else
        //            {
        //                igstPer = gstPercent;
        //                igstTax = taxable * igstPer / 100;
        //            }

        //            // 🔥 TOTAL GST ME CESS INCLUDE
        //            decimal totalTax = sgstTax + cgstTax + igstTax + cessTax;

        //            return new
        //            {
        //                hsn = x.ItemMaster.Hsn.HsnCode,
        //                amount = x.Amount,
        //                taxable = taxable,

        //                sgstPer,
        //                sgstTax,

        //                cgstPer,
        //                cgstTax,

        //                igstPer,
        //                igstTax,

        //                // 🔥 UPDATED CESS
        //                cessPer,
        //                cess = Math.Round(cessTax, 2),

        //                totalTax,

        //                taxType = isLocal ? x.ItemMaster.Local.ToString() : x.ItemMaster.Central.ToString()
        //            };
        //        });

        //    return Json(items);
        //}

        //[HttpGet]
        //public async Task<IActionResult> GetBillItems(int id)
        //{
        //    var tenantId = User.FindFirstValue("TenantId");

        //    var tenant = (await _tenantRepository.GetAll())
        //        .FirstOrDefault(x => x.Id == tenantId);

        //    var sale = await _salesservice.GetById(id);

        //    int? companyState = (int?)tenant?.StateCode;
        //    int? customerState = (int?)sale.Customers?.StateCode;

        //    bool isLocal = companyState == customerState || sale.billingType == "Cash";

        //    var items = sale.SalesItems
        //        .Where(x => x.Deleted == null)
        //        .Select(x =>
        //        {
        //            decimal taxable = x.Amount;
        //            decimal gstPercent = x.Gst;

        //            decimal sgstPer = 0;
        //            decimal cgstPer = 0;
        //            decimal igstPer = 0;

        //            decimal sgstTax = 0;
        //            decimal cgstTax = 0;
        //            decimal igstTax = 0;

        //            if (isLocal || sale.billingType == "Cash")
        //            {
        //                sgstPer = gstPercent / 2;
        //                cgstPer = gstPercent / 2;

        //                sgstTax = taxable * sgstPer / 100;
        //                cgstTax = taxable * cgstPer / 100;
        //            }
        //            else
        //            {
        //                igstPer = gstPercent;
        //                igstTax = taxable * igstPer / 100;
        //            }

        //            decimal totalTax = sgstTax + cgstTax + igstTax;

        //            return new
        //            {
        //                hsn = x.ItemMaster.Hsn.HsnCode,
        //                amount = x.Amount,
        //                taxable = taxable,

        //                sgstPer,
        //                sgstTax,

        //                cgstPer,
        //                cgstTax,

        //                igstPer,
        //                igstTax,

        //                cess = 0,
        //                totalTax,

        //                taxType = isLocal ? x.ItemMaster.Local.ToString() : x.ItemMaster.Central.ToString()
        //            };
        //        });

        //    return Json(items);
        //}
        #endregion

        #region Priti (Reports)
        //[HttpGet]
        //public async Task<IActionResult> ItemWisePartySales(DateTime? fromDate, DateTime? toDate, int? itemId, int? customerId)
        //{
        //    if (!fromDate.HasValue || !toDate.HasValue)
        //    {
        //        var today = DateTime.Today;
        //        fromDate = new DateTime(today.Year, today.Month, 1);
        //        toDate = fromDate.Value.AddMonths(1).AddDays(-1);
        //    }
        //    ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
        //    ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

        //    ViewBag.ItemList = new SelectList(
        //        await _itemmasterservice.GetAll(), "Id", "Name", itemId);

        //    ViewBag.CustomerList = new SelectList(
        //        await _customerservice.GetAll(), "Id", "Name", customerId);

        //    if (!itemId.HasValue || itemId == 0)
        //        return View(new List<ItemWisePartySalesVM>());

        //    var sales = await _salesservice.GetAll();
        //    sales = sales
        //        .Where(x => x.BillDate >= fromDate.Value && x.BillDate <= toDate.Value)
        //        .ToList();

        //    if (customerId.HasValue && customerId > 0)
        //        sales = sales.Where(x => x.CustomerId == customerId).ToList();

        //    var data = sales
        //        .Where(s => s.SalesItems != null)
        //        .SelectMany(s => s.SalesItems
        //            .Where(i => i.ItemMasterId == itemId)
        //            .Select(i => new
        //            {
        //                s.CustomerId,
        //                CustomerName = s.Customers != null ? s.Customers.Name : "Walk-in",
        //                Month = s.BillDate.Value.Month,
        //                Qty = i.Qty
        //            }))
        //        .ToList();

        //    var report = data
        //        .GroupBy(x => new { x.CustomerId, x.CustomerName })
        //        .Select(g => new ItemWisePartySalesVM
        //        {
        //            CustomerId = g.Key.CustomerId ?? 0,
        //            CustomerName = g.Key.CustomerName,

        //            Jan = g.Where(x => x.Month == 1).Sum(x => x.Qty),
        //            Feb = g.Where(x => x.Month == 2).Sum(x => x.Qty),
        //            Mar = g.Where(x => x.Month == 3).Sum(x => x.Qty),
        //            Apr = g.Where(x => x.Month == 4).Sum(x => x.Qty),
        //            May = g.Where(x => x.Month == 5).Sum(x => x.Qty),
        //            Jun = g.Where(x => x.Month == 6).Sum(x => x.Qty),
        //            Jul = g.Where(x => x.Month == 7).Sum(x => x.Qty),
        //            Aug = g.Where(x => x.Month == 8).Sum(x => x.Qty),
        //            Sep = g.Where(x => x.Month == 9).Sum(x => x.Qty),
        //            Oct = g.Where(x => x.Month == 10).Sum(x => x.Qty),
        //            Nov = g.Where(x => x.Month == 11).Sum(x => x.Qty),
        //            Dec = g.Where(x => x.Month == 12).Sum(x => x.Qty),
        //        })
        //        .OrderBy(x => x.CustomerName)
        //        .ToList();

        //    return View(report);
        //}
        [HttpGet]
        public async Task<IActionResult> ItemWisePartySales(DateTime? fromDate, DateTime? toDate, int? itemId, int? customerId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            ViewBag.ItemList = new SelectList(await _itemmasterservice.GetAll(), "Id", "Name", itemId);
            ViewBag.CustomerList = new SelectList(await _customerservice.GetAll(), "Id", "Name", customerId);

            if (!itemId.HasValue || itemId == 0)
                return View(new List<ItemWisePartySalesVM>());

            var sales = await _salesservice.GetAll();

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            if (customerId.HasValue && customerId > 0)
                sales = sales.Where(x => x.CustomerId == customerId).ToList();

            var data = sales
                .Where(s => s.SalesItems != null)
                .SelectMany(s => s.SalesItems
                    .Where(i => i.ItemMasterId == itemId)
                    .Select(i => new
                    {
                        s.CustomerId,
                        CustomerName = s.Customers != null ? s.Customers.Name : "Walk-in",
                        MonthYear = s.BillDate.Value.ToString("MMM-yyyy"),
                        Qty = i.Qty,
                        Conversion = i.ItemMaster?.Conversion ?? 0
                    }))
                .ToList();

            var report = data
                .GroupBy(x => new { x.CustomerId, x.CustomerName })
                .Select(g =>
                {
                    int conversion = g.First().Conversion;

                    var monthData = g
                        .GroupBy(x => x.MonthYear)
                        .ToDictionary(
                            m => m.Key,
                            m =>
                            {
                                decimal qty = m.Sum(x => x.Qty);

                                if (conversion > 0)
                                {
                                    int totalTabs = (int)Math.Round(qty * conversion);
                                    int strip = totalTabs / conversion;
                                    int tab = totalTabs % conversion;

                                    return $"{strip}:{tab}";
                                }

                                return qty.ToString("0.##");
                            });

                    return new ItemWisePartySalesVM
                    {
                        CustomerId = g.Key.CustomerId ?? 0,
                        CustomerName = g.Key.CustomerName,
                        MonthData = monthData
                    };
                })
                .OrderBy(x => x.CustomerName)
                .ToList();

            ViewBag.MonthColumns = data
     .Select(x => x.MonthYear)
     .Distinct()
     .OrderBy(x => DateTime.ParseExact(x, "MMM-yyyy", null))
     .ToList();

            if (ViewBag.MonthColumns == null)
                ViewBag.MonthColumns = new List<string>();

            return View(report);
        }

        //Export To Excel 
        [HttpGet]
        public async Task<IActionResult> ExportItemWisePartySalesExcel(DateTime? fromDate, DateTime? toDate, int? itemId, int? customerId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            if (!itemId.HasValue || itemId == 0)
                return RedirectToAction(nameof(ItemWisePartySales));

            var sales = await _salesservice.GetAll();

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            if (customerId.HasValue && customerId > 0)
                sales = sales.Where(x => x.CustomerId == customerId).ToList();

            var data = sales
                .Where(s => s.SalesItems != null)
                .SelectMany(s => s.SalesItems
                    .Where(i => i.ItemMasterId == itemId)
                    .Select(i => new
                    {
                        CustomerName = s.Customers != null ? s.Customers.Name : "Walk-in",
                        MonthYear = s.BillDate.Value.ToString("MMM-yyyy"),
                        Qty = i.Qty,
                        Conversion = i.ItemMaster?.Conversion ?? 0
                    }))
                .ToList();

            var months = data
                .Select(x => x.MonthYear)
                .Distinct()
                .OrderBy(x => DateTime.ParseExact(x, "MMM-yyyy", null))
                .ToList();

            var report = data
                .GroupBy(x => x.CustomerName)
                .Select(g =>
                {
                    int conversion = g.First().Conversion;

                    var monthData = g.GroupBy(x => x.MonthYear)
                        .ToDictionary(
                            m => m.Key,
                            m =>
                            {
                                decimal qty = m.Sum(x => x.Qty);

                                if (conversion > 0)
                                {
                                    int total = (int)Math.Round(qty * conversion);
                                    int strip = total / conversion;
                                    int tab = total % conversion;
                                    return $"{strip}:{tab}";
                                }

                                return qty.ToString("0.##");
                            });

                    return new
                    {
                        Customer = g.Key,
                        MonthData = monthData
                    };
                }).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Item Wise Party");

            // Company Name
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, months.Count + 1).Merge().Style
                .Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Item Wise Party Sales Report";
            ws.Range(2, 1, 2, months.Count + 1).Merge().Style
                .Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, months.Count + 1).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int headerRow = 5;

            ws.Cell(headerRow, 1).Value = "Customer";

            for (int i = 0; i < months.Count; i++)
            {
                ws.Cell(headerRow, i + 2).Value = months[i];
            }

            ws.Range(headerRow, 1, headerRow, months.Count + 1).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.LightBlue)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = headerRow + 1;

            foreach (var r in report)
            {
                ws.Cell(row, 1).Value = r.Customer;

                for (int i = 0; i < months.Count; i++)
                {
                    string month = months[i];
                    ws.Cell(row, i + 2).Value =
                        r.MonthData.ContainsKey(month) ? r.MonthData[month] : "-";
                }

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ItemWisePartySales.xlsx");
        }

        //Export To PDF
        [HttpGet]
        public async Task<IActionResult> ExportItemWisePartySalesPdf(DateTime? fromDate, DateTime? toDate, int? itemId, int? customerId)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            var sales = await _salesservice.GetAll();

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            if (customerId.HasValue && customerId > 0)
                sales = sales.Where(x => x.CustomerId == customerId).ToList();

            var data = sales
                .Where(s => s.SalesItems != null)
                .SelectMany(s => s.SalesItems
                    .Where(i => i.ItemMasterId == itemId)
                    .Select(i => new
                    {
                        CustomerName = s.Customers != null ? s.Customers.Name : "Walk-in",
                        MonthYear = s.BillDate.Value.ToString("MMM-yyyy"),
                        Qty = i.Qty,
                        Conversion = i.ItemMaster?.Conversion ?? 0
                    }))
                .ToList();

            var months = data
                .Select(x => x.MonthYear)
                .Distinct()
                .OrderBy(x => DateTime.ParseExact(x, "MMM-yyyy", null))
                .ToList();

            var report = data
                .GroupBy(x => x.CustomerName)
                .Select(g =>
                {
                    int conversion = g.First().Conversion;

                    var monthData = g.GroupBy(x => x.MonthYear)
                        .ToDictionary(
                            m => m.Key,
                            m =>
                            {
                                decimal qty = m.Sum(x => x.Qty);

                                if (conversion > 0)
                                {
                                    int total = (int)Math.Round(qty * conversion);
                                    int strip = total / conversion;
                                    int tab = total % conversion;
                                    return $"{strip}:{tab}";
                                }

                                return qty.ToString("0.##");
                            });

                    return new
                    {
                        Customer = g.Key,
                        MonthData = monthData
                    };
                }).ToList();

            // ===== PDF =====
            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fontSize = 9;

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // Header
            document.Add(new Paragraph(companyName)
                .SetFont(boldFont).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Item Wise Party Sales Report")
                .SetFont(boldFont).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normalFont).SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // Table
            Table table = new Table(months.Count + 1).UseAllAvailableWidth();

            table.AddHeaderCell(new Cell().Add(new Paragraph("Customer").SetFont(boldFont).SetFontSize(fontSize)));

            foreach (var m in months)
            {
                table.AddHeaderCell(
                    new Cell().Add(new Paragraph(m).SetFont(boldFont).SetFontSize(fontSize))
                    .SetTextAlignment(TextAlignment.RIGHT));
            }

            foreach (var r in report)
            {
                table.AddCell(new Paragraph(r.Customer).SetFont(normalFont).SetFontSize(fontSize));

                foreach (var m in months)
                {
                    string val = r.MonthData.ContainsKey(m) ? r.MonthData[m] : "-";

                    table.AddCell(
                        new Paragraph(val).SetFont(normalFont).SetFontSize(fontSize)
                        .SetTextAlignment(TextAlignment.RIGHT));
                }
            }

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "ItemWisePartySales.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> SalseReportCompanywise(DateTime? fromDate, DateTime? toDate, int? companyId, int? divisionId)
        {
            var sales = await _salesservice.GetAll();
            var companies = await _companyService.GetAll();
            var divisions = await _divisionRepo.GetAll();

            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var today = DateTime.Today;
                fromDate = new DateTime(today.Year, today.Month, 1);
                toDate = fromDate.Value.AddMonths(1).AddDays(-1);
            }

            ViewBag.CompanyList = new SelectList(companies, "Id", "Name", companyId);
            ViewBag.DivisionList = new SelectList(divisions, "Id", "Name", divisionId);
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            sales = sales
                .Where(x => x.BillDate >= fromDate.Value && x.BillDate <= toDate.Value)
                .ToList();

            sales = sales.Where(s =>
                s.SalesItems != null &&
                s.SalesItems.Any(i =>
                    (!companyId.HasValue || i.ItemMaster.CompanyId == companyId.Value) &&
                    (!divisionId.HasValue || i.ItemMaster.DivisionId == divisionId.Value)
                )).ToList();

            var data = sales
                .GroupBy(s => new
                {
                    CustomerName = s.Customers != null ? s.Customers.Name : "Walk-in",
                    Month = s.BillDate.Value.Month
                })
                .Select(g => new
                {
                    g.Key.CustomerName,
                    g.Key.Month,
                    Amount = g.Sum(x => x.TotalPayable)
                })
                .ToList();

            var result = data
                .GroupBy(x => x.CustomerName)
                .Select(g => new CompanyWiseCustomerMonthVM
                {
                    CustomerName = g.Key,
                    MonthAmounts = g.ToDictionary(x => x.Month, x => x.Amount)
                })
                .ToList();

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> FastMovingItems(DateTime? fromDate, DateTime? toDate)
        {
            // Agar date null hai, to default current month ki 1st date se aaj tak ka data set karein
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now; // Ya mahine ki aakhri date: fromDate.Value.AddMonths(1).AddDays(-1);
            }

            // View mein wapas date bhejne ke liye (taki input fields mein dikhe)
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            var sales = await _salesservice.GetAll();

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            var result = sales
                .Where(s => s.SalesItems != null)
                .SelectMany(s => s.SalesItems)
                .GroupBy(x => new
                {
                    x.ItemMasterId,
                    x.ItemMaster.Name,
                    x.ItemMaster.Conversion
                })
                .Select(g =>
                {
                    decimal totalQty = g.Sum(x => x.Qty);
                    int conversion = g.Key.Conversion;

                    return new SalesVM
                    {
                        ItemName = g.Key.Name,
                        TotalQty = totalQty,
                        QtyDisplay = GetConvertedQty(totalQty, conversion),
                        // ✅ FIX: Qty × Rate se calculate karein
                        TotalAmount = g.Sum(x => x.Qty * x.Rate)
                    };
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(10)
                .ToList();

            ViewBag.FromDate = fromDate.Value.ToString("dd-MM-yyyy");
            ViewBag.ToDate = toDate.Value.ToString("dd-MM-yyyy");

            ViewBag.TotalQty = result.Sum(x => x.TotalQty);
            ViewBag.TotalAmt = result.Sum(x => x.TotalAmount);

            return View(result);
        }
        private string GetConvertedQty(decimal qty, int conversion)
        {
            if (conversion <= 0)
                return qty.ToString("0.##");

            int totalTablets = (int)Math.Round(qty * conversion, MidpointRounding.AwayFromZero);

            int strip = totalTablets / conversion;
            int tablet = totalTablets % conversion;

            return $"{strip}:{tablet}";
        }

        //EXPORT TO EXCEL FAST MOVING ITEMS
        [HttpGet]
        public async Task<IActionResult> FastMovingItemsExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now; // Ya mahine ki aakhri date: fromDate.Value.AddMonths(1).AddDays(-1);
            }

            // View mein wapas date bhejne ke liye (taki input fields mein dikhe)
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            var sales = await _salesservice.GetAll();

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            var result = sales
                .Where(s => s.SalesItems != null)
                .SelectMany(s => s.SalesItems)
                .GroupBy(x => new
                {
                    x.ItemMasterId,
                    x.ItemMaster.Name,
                    x.ItemMaster.Conversion
                })
                .Select(g =>
                {
                    decimal totalQty = g.Sum(x => x.Qty);
                    int conversion = g.Key.Conversion;

                    return new SalesVM
                    {
                        ItemName = g.Key.Name,
                        TotalQty = totalQty,
                        QtyDisplay = GetConvertedQty(totalQty, conversion),
                        // ✅ FIX: Qty × Rate se calculate karein
                        TotalAmount = g.Sum(x => x.Qty * x.Rate)
                    };
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(10)
                .ToList();
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Fast Moving");

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user?.TenantId != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            // HEADER
            ws.Cell("A1").Value = companyName;
            ws.Range("A1:D1").Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell("A2").Value = "Fast Moving Items Report";
            ws.Range("A2:D2").Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell("A3").Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range("A3:D3").Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            string[] headers = { "S.No", "Item Name", "Total Qty", "Total Amount" };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(5, i + 1).Value = headers[i];

            ws.Range("A5:D5").Style.Font.SetBold();

            int row = 6;
            int sr = 1;

            foreach (var x in result)
            {
                ws.Cell(row, 1).Value = sr++;
                ws.Cell(row, 2).Value = x.ItemName;
                ws.Cell(row, 3).Value = x.TotalQty;
                ws.Cell(row, 4).Value = x.TotalAmount;

                ws.Cell(row, 3).Style.NumberFormat.Format = "0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "0.00";

                row++;
            }

            ws.Cell(row, 2).Value = "TOTAL";
            ws.Cell(row, 3).Value = result.Sum(x => x.TotalQty);
            ws.Cell(row, 4).Value = result.Sum(x => x.TotalAmount);

            ws.Range(row, 2, row, 4).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "FastMovingItems.xlsx");
        }

        //EXPORT TO PDF FAST MOVING ITEMS
        [HttpGet]
        public async Task<IActionResult> FastMovingItemsPdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now; // Ya mahine ki aakhri date: fromDate.Value.AddMonths(1).AddDays(-1);
            }

            // View mein wapas date bhejne ke liye (taki input fields mein dikhe)
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            var sales = await _salesservice.GetAll();

            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            var result = sales
                .Where(s => s.SalesItems != null)
                .SelectMany(s => s.SalesItems)
                .GroupBy(x => new
                {
                    x.ItemMasterId,
                    x.ItemMaster.Name,
                    x.ItemMaster.Conversion
                })
                .Select(g =>
                {
                    decimal totalQty = g.Sum(x => x.Qty);
                    int conversion = g.Key.Conversion;

                    return new SalesVM
                    {
                        ItemName = g.Key.Name,
                        TotalQty = totalQty,
                        QtyDisplay = GetConvertedQty(totalQty, conversion),
                        // ✅ FIX: Qty × Rate se calculate karein
                        TotalAmount = g.Sum(x => x.Qty * x.Rate)
                    };
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(10)
                .ToList();

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user?.TenantId != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            using var ms = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(ms));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4);

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            doc.Add(new Paragraph(companyName).SetFont(bold).SetFontSize(12).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph("Fast Moving Items Report").SetFont(bold).SetFontSize(10).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph(" "));

            Table table = new Table(4).UseAllAvailableWidth();

            string[] headers = { "S.No", "Item Name", "Total Qty", "Total Amount" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell().Add(new Paragraph(h)
                    .SetFont(bold).SetFontSize(9)));
            }

            int sr = 1;

            foreach (var x in result)
            {
                table.AddCell(new Paragraph(sr++.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.ItemName).SetFontSize(9));
                table.AddCell(new Paragraph(x.TotalQty.ToString("0.00")).SetFontSize(9));
                table.AddCell(new Paragraph(x.TotalAmount.ToString()).SetFontSize(9));
            }

            table.AddCell(new Cell(1, 2).Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(9)));

            table.AddCell(new Paragraph(result.Sum(x => x.TotalQty).ToString("0.00"))
                .SetFont(bold).SetFontSize(9));

            table.AddCell(new Paragraph(result.Sum(x => x.TotalAmount).ToString())
                .SetFont(bold).SetFontSize(9));

            doc.Add(table);
            doc.Close();

            return File(ms.ToArray(), "application/pdf", "FastMovingItems.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> SlowMovingItems(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now; // Ya mahine ki aakhri date: fromDate.Value.AddMonths(1).AddDays(-1);
            }

            // View mein wapas date bhejne ke liye (taki input fields mein dikhe)
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            var sales = await _salesservice.GetAll();

            // ✅ Apply filter using calculated dates
            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            var result = sales
                .SelectMany(s => s.SalesItems)
                .GroupBy(x => new { x.ItemMasterId, x.ItemMaster.Name })
                .Select(g => new SalesVM
                {
                    ItemName = g.Key.Name,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderBy(x => x.TotalQty)
                .Take(15)
                .ToList();

            ViewBag.TotalQty = result.Sum(x => x.TotalQty);
            ViewBag.TotalAmt = result.Sum(x => x.TotalAmount);
            return View(result);
        }

        //EXPORT TO EXCEL SLOW MOVING ITEMS
        [HttpGet]
        public async Task<IActionResult> SlowMovingItemsExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                var now = DateTime.Today;
                fromDate = new DateTime(now.Year, now.Month, 1);
                toDate = now; // Ya mahine ki aakhri date: fromDate.Value.AddMonths(1).AddDays(-1);
            }

            // View mein wapas date bhejne ke liye (taki input fields mein dikhe)
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            var sales = await _salesservice.GetAll();

            // ✅ Apply filter using calculated dates
            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            var result = sales
                .SelectMany(s => s.SalesItems)
                .GroupBy(x => new { x.ItemMasterId, x.ItemMaster.Name })
                .Select(g => new SalesVM
                {
                    ItemName = g.Key.Name,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderBy(x => x.TotalQty)
                .Take(15)
                .ToList();


            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Slow Moving Items");
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null && user.TenantId != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }
            // COMPANY NAME
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 3).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // REPORT TITLE
            ws.Cell(2, 1).Value = "Slow Moving Items Report";
            ws.Range(2, 1, 2, 3).Merge()
                .Style.Font.SetBold()
                .Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // DATE RANGE
            ws.Cell(3, 1).Value = $"From : {fromDate:dd-MM-yyyy}   To : {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 3).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // 🔹 Headers
            ws.Cell(5, 1).Value = "Item Name";
            ws.Cell(5, 2).Value = "Total Qty";
            ws.Cell(5, 3).Value = "Total Amount";
            ws.Range(5, 1, 4, 3).Style.Font.SetBold();

            int row = 6;
            foreach (var x in result)
            {
                ws.Cell(row, 1).Value = x.ItemName;
                ws.Cell(row, 2).Value = x.TotalQty;
                ws.Cell(row, 3).Value = x.TotalAmount;
                row++;
            }

            // 🔹 Totals
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 2).Value = result.Sum(x => x.TotalQty);
            ws.Cell(row, 3).Value = result.Sum(x => x.TotalAmount);
            ws.Range(row, 1, row, 3).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SlowMovingItems.xlsx");
        }

        //EXPORT TO PDF SLOW MOVING ITEMS
        [HttpGet]
        public async Task<IActionResult> SlowMovingItemsPdf(DateTime? fromDate, DateTime? toDate)
        {
            DateTime startDate = fromDate?.Date
                ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            DateTime endDate = toDate?.Date.AddDays(1).AddTicks(-1)
                ?? DateTime.Today.Date.AddDays(1).AddTicks(-1);

            var sales = await _salesservice.GetAll();

            // ✅ Apply filter using calculated dates
            sales = sales
                .Where(x => x.BillDate >= fromDate && x.BillDate <= toDate)
                .ToList();

            var result = sales
                .SelectMany(s => s.SalesItems)
                .GroupBy(x => new { x.ItemMasterId, x.ItemMaster.Name })
                .Select(g => new SalesVM
                {
                    ItemName = g.Key.Name,
                    TotalQty = g.Sum(x => x.Qty),
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderBy(x => x.TotalQty)
                .Take(15)
                .ToList();


            using var ms = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(ms));
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null && user.TenantId != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }
            doc.SetFontSize(8);

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // COMPANY NAME
            doc.Add(new Paragraph(companyName)
                .SetFont(bold)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            // REPORT TITLE
            doc.Add(new Paragraph("Slow Moving Items Report")
                .SetFont(bold)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            // DATE RANGE
            doc.Add(new Paragraph($"From : {startDate:dd-MM-yyyy}   To : {endDate:dd-MM-yyyy}")
                .SetFont(normal)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph(" "));

            Table table = new Table(3).UseAllAvailableWidth();

            // 🔹 Headers
            table.AddHeaderCell(new Cell().Add(new Paragraph("Item Name").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Total Qty").SetFont(bold).SetFontSize(9))
                .SetTextAlignment(TextAlignment.RIGHT));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Total Amount").SetFont(bold).SetFontSize(9))
                .SetTextAlignment(TextAlignment.RIGHT));

            // 🔹 Rows
            foreach (var x in result)
            {
                table.AddCell(new Paragraph(x.ItemName).SetFontSize(8));
                table.AddCell(new Paragraph(x.TotalQty.ToString())
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(x.TotalAmount.ToString())
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.RIGHT));
            }

            // 🔹 Totals
            table.AddCell(new Cell(1, 1)
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(9)));

            table.AddCell(new Paragraph(result.Sum(x => x.TotalQty).ToString())
                .SetFont(bold).SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(result.Sum(x => x.TotalAmount).ToString())
                .SetFont(bold).SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT));

            doc.Add(table);
            doc.Close();

            return File(ms.ToArray(), "application/pdf", "SlowMovingItems.pdf");
        }

        #endregion

        [HttpGet]
        public async Task<IActionResult> Billwisecollectionreport(DateTime? fromDate, DateTime? toDate, int? CustomerId, string billNo)
        {
            var sales = await _salesservice.GetAll();
            var customers = await _customerservice.GetAll();

            // ✅ Default Date = Month Start to Today
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            // ✅ Customer Dropdown
            ViewBag.CustomerList = customers
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

            // ✅ Bill No Dropdown
            ViewBag.BillList = sales
                .Where(x => !string.IsNullOrEmpty(x.BillNo))
                .Select(x => x.BillNo)
                .Distinct()
                .Select(b => new SelectListItem
                {
                    Value = b,
                    Text = b
                }).ToList();

            // ✅ Date Filter
            sales = sales.Where(x =>
                x.BillDate >= fromDate.Value &&
                x.BillDate <= toDate.Value).ToList();

            // ✅ Customer Filter
            if (CustomerId.HasValue)
                sales = sales.Where(x => x.CustomerId == CustomerId.Value).ToList();

            // ✅ Bill Filter
            if (!string.IsNullOrWhiteSpace(billNo))
                sales = sales.Where(x => x.BillNo == billNo).ToList();

            // ✅ FIX: query variable ki jagah sales use kiya
            var result = sales
                .OrderBy(x => x.BillDate)
                .Select(x => new SalesVM
                {
                    Id = x.Id,
                    BillNo = x.BillNo,
                    BillDate = x.BillDate,
                    CustomerName = x.Customers != null ? x.Customers.Name : "",
                    TotalPayable = x.TotalPayable,
                    PaidAmount = x.PaidAmount,
                    ReturnAmount = x.ReturnAmount,
                    Balance = x.Balance,
                    NetCollection = x.NetCollection,
                })
                .ToList();

            ViewBag.FromDate = fromDate?.ToString("dd-MM-yyyy");
            ViewBag.ToDate = toDate?.ToString("dd-MM-yyyy");

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> ExportBillWiseCollectionExcel(DateTime? fromDate, DateTime? toDate, int? CustomerId, string billNo)
        {
            var sales = await _salesservice.GetAll();

            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            // Filters
            sales = sales.Where(x =>
                x.BillDate.HasValue &&
                x.BillDate.Value.Date >= fromDate.Value.Date &&
                x.BillDate.Value.Date <= toDate.Value.Date
            ).ToList();

            if (CustomerId.HasValue)
                sales = sales.Where(x => x.CustomerId == CustomerId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(billNo))
                sales = sales.Where(x => x.BillNo == billNo).ToList();

            var report = sales
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Bill Wise Collection");

            // HEADER
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";
            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 8).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Bill Wise Collection Report";
            ws.Range(2, 1, 2, 8).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 8).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            int row = 5;

            // EXACT HEADER
            string[] headers = { "Sr", "Bill No", "Date", "Customer", "Total", "Paid", "Net Collection", "Return Amt" };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
                ws.Cell(row, i + 1).Style.Font.SetBold();
            }

            row++;

            int sr = 1;
            decimal total = 0, paid = 0, net = 0, ret = 0;

            foreach (var item in report)
            {
                ws.Cell(row, 1).Value = sr++;
                ws.Cell(row, 2).Value = item.BillNo;
                ws.Cell(row, 3).Value = item.BillDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 4).Value = item.Customers != null ? item.Customers.Name : "";

                ws.Cell(row, 5).Value = item.TotalPayable;
                ws.Cell(row, 6).Value = item.PaidAmount;
                ws.Cell(row, 7).Value = item.NetCollection;
                ws.Cell(row, 8).Value = item.ReturnAmount;

                ws.Range(row, 5, row, 8).Style.NumberFormat.Format = "0.00";

                total += item.TotalPayable;
                paid += item.PaidAmount;
                net += item.NetCollection;
                ret += item.ReturnAmount;

                row++;
            }

            // TOTAL ROW (EXACT LIKE YOUR FORMAT)
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 1).Style.Font.SetBold();

            ws.Cell(row, 5).Value = total;
            ws.Cell(row, 6).Value = paid;
            ws.Cell(row, 7).Value = net;
            ws.Cell(row, 8).Value = ret;

            ws.Range(row, 5, row, 8).Style.NumberFormat.Format = "0.00";
            ws.Range(row, 1, row, 8).Style.Font.SetBold();

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BillWiseCollection.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportBillWiseCollectionPdf(DateTime? fromDate, DateTime? toDate, int? CustomerId, string billNo)
        {
            var sales = await _salesservice.GetAll();

            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            sales = sales.Where(x =>
                x.BillDate >= fromDate.Value &&
                x.BillDate <= toDate.Value).ToList();

            if (CustomerId.HasValue)
                sales = sales.Where(x => x.CustomerId == CustomerId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(billNo))
                sales = sales.Where(x => x.BillNo == billNo).ToList();

            var report = sales
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fontSize = 9;

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            document.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Bill Wise Collection Report")
                .SetFont(bold).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(fontSize)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // TABLE
            Table table = new Table(new float[] { 2, 3, 3, 4, 3, 3, 3, 3 })
                .UseAllAvailableWidth();

            string[] headers = { "Sr", "Bill No", "Date", "Customer", "Total", "Paid", "Net Collection", "Return Amt" };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(fontSize)));
            }

            int sr = 1;
            decimal total = 0, paid = 0, net = 0, ret = 0;

            foreach (var item in report)
            {
                table.AddCell(new Paragraph(sr++.ToString()).SetFont(normal).SetFontSize(fontSize));
                table.AddCell(new Paragraph(item.BillNo).SetFont(normal).SetFontSize(fontSize));
                table.AddCell(new Paragraph(item.BillDate?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fontSize));
                table.AddCell(new Paragraph(item.Customers != null ? item.Customers.Name : "").SetFont(normal).SetFontSize(fontSize));

                table.AddCell(new Paragraph(item.TotalPayable.ToString("0.00")).SetFont(normal).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(item.PaidAmount.ToString("0.00")).SetFont(normal).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(item.NetCollection.ToString("0.00")).SetFont(normal).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(item.ReturnAmount.ToString("0.00")).SetFont(normal).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));

                total += item.TotalPayable;
                paid += item.PaidAmount;
                net += item.NetCollection;
                ret += item.ReturnAmount;
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 4)
                .Add(new Paragraph("TOTAL").SetFont(bold).SetFontSize(fontSize))
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(total.ToString("0.00")).SetFont(bold).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph(paid.ToString("0.00")).SetFont(bold).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph(net.ToString("0.00")).SetFont(bold).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));
            table.AddCell(new Paragraph(ret.ToString("0.00")).SetFont(bold).SetFontSize(fontSize).SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(),
                "application/pdf",
                "BillWiseCollection.pdf");
        }
        public async Task<IActionResult> Print(int id)
        {
            // 1. Fetch Actual Sales Data
            var salesData = await _salesService.GetById(id);

            if (salesData != null && salesData.CustomerId.HasValue)
            {
                var pointSettings = await _unitofwork.GetRepository<PointSetting>().Query()
                    .Where(x => x.TenantId == salesData.TenantId)
                    .ToListAsync();
                var earliestSetting = pointSettings.OrderBy(x => x.Created).FirstOrDefault();
                var earliestCreated = earliestSetting?.Created ?? DateTime.MaxValue;
                var saleCompareDate = salesData.Created ?? salesData.BillDate ?? DateTime.Now;

                if (saleCompareDate >= earliestCreated)
                {
                    var pointRepo = _unitofwork.GetRepository<PointTransaction>();
                    var currentPointsBalance = await pointRepo.Query()
                        .Where(x => x.CustomerId == salesData.CustomerId.Value)
                        .SumAsync(x => x.EarnedPoints - x.RedeemedPoints);
                    ViewBag.CustomerPointsBalance = currentPointsBalance;

                    var pointTxForSale = await pointRepo.Query()
                        .FirstOrDefaultAsync(x => x.SaleId == salesData.Id);
                    if (pointTxForSale != null)
                    {
                        ViewBag.PointsEarned = pointTxForSale.EarnedPoints;
                        ViewBag.PointsRedeemed = pointTxForSale.RedeemedPoints;
                    }
                    else
                    {
                        ViewBag.PointsEarned = 0;
                        ViewBag.PointsRedeemed = 0;
                    }
                }
                else
                {
                    ViewBag.CustomerPointsBalance = null;
                    ViewBag.PointsEarned = null;
                    ViewBag.PointsRedeemed = null;
                }
            }

            if (salesData == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            EasyBill.Models.Entity.InvoiceThemeSetting? themeSetting = null;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                themeSetting = await _themeRepo.GetDefaultThemeAsync(userId);
            }

            var rawPaper = (themeSetting?.PaperSize ?? "A4").Trim();
            var effectivePaper = string.Equals(rawPaper, "A5", StringComparison.OrdinalIgnoreCase)
                ? "A5"
                : string.Equals(rawPaper, "Thermal", StringComparison.OrdinalIgnoreCase)
                    ? "Thermal"
                    : "A4";

            if (string.Equals(effectivePaper, "Thermal", StringComparison.OrdinalIgnoreCase))
            {
                var tSize = themeSetting?.ThermalPaperSize == 58 ? "58mm" : "80mm";
                ViewBag.PaperSize = tSize;
            }

            // Business profile data for all invoice templates (A4/A5/Thermal)
            var tenantId = User.FindFirstValue("TenantId");
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                var tenant = (await _tenantRepository.GetAll()).FirstOrDefault(x => x.Id == tenantId);
                if (tenant != null)
                {
                    var addressParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(tenant.Address1)) addressParts.Add(tenant.Address1);
                    if (!string.IsNullOrWhiteSpace(tenant.Address2)) addressParts.Add(tenant.Address2);
                    if (!string.IsNullOrWhiteSpace(tenant.Location)) addressParts.Add(tenant.Location);
                    if (tenant.City != null && !string.IsNullOrWhiteSpace(tenant.City.Name)) addressParts.Add(tenant.City.Name);
                    if (tenant.State != null && !string.IsNullOrWhiteSpace(tenant.State.Name)) addressParts.Add(tenant.State.Name);
                    if (!string.IsNullOrWhiteSpace(tenant.PinCode)) addressParts.Add($"Pin: {tenant.PinCode}");

                    var businessPhone = !string.IsNullOrWhiteSpace(tenant.MobileNo) ? tenant.MobileNo : tenant.Phone;

                    ViewBag.BusinessName = tenant.Name ?? "Your Business Name";
                    ViewBag.BusinessAddress = string.Join(", ", addressParts);
                    ViewBag.BusinessPhone = businessPhone ?? string.Empty;
                    ViewBag.BusinessEmail = tenant.Email ?? string.Empty;
                    ViewBag.BusinessGSTIN = tenant.GstNo ?? string.Empty;
                }
            }

            ViewBag.ThemeSettings = themeSetting;
            return View($"~/Views/Shared/InvoiceTemplates/{effectivePaper}_Print.cshtml", salesData);
        }

        [HttpGet]
        public async Task<IActionResult> StockInSalesStatmentReport(DateTime? fromDate, DateTime? toDate, int? companyId)
        {
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            // Get companies for dropdown
            var companies = await _companyService.GetAll();
            ViewBag.CompanyList = companies
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

            ViewBag.SelectedCompanyId = companyId;
            ViewBag.FromDate = fromDate?.ToString("dd-MM-yyyy");
            ViewBag.ToDate = toDate?.ToString("dd-MM-yyyy");

            // Get all data
            var items = await _itemmasterservice.GetAll();
            var purchases = await _purchaseservice.GetAll();
            var purchaseReturns = await _purchasereturnservice.GetAll();
            var sales = await _salesservice.GetAll();
            var salesReturns = await _stockrturnservice.GetAll();
            var currentStock = await _purchaseitemservice.GetAll();

            // Filter by company if selected
            if (companyId.HasValue)
                items = items.Where(i => i.CompanyId == companyId.Value).ToList();

            var report = new List<SalesVM>();

            foreach (var item in items)
            {
                // Average rate for this item (Base Rate - without GST)
                var avgRate = currentStock
                    .Where(x => x.ItemId == item.Id)
                    .Select(x => (decimal?)x.Rate)
                    .DefaultIfEmpty(0)
                    .Average() ?? 0;

                // Average GST % for this item from current stock
                var avgGstPercent = currentStock
                    .Where(x => x.ItemId == item.Id)
                    .Select(x => (decimal?)x.Gst)
                    .DefaultIfEmpty(0)
                    .Average() ?? 0;

                // Average Cess % for this item from current stock
                var avgCessPercent = currentStock
                    .Where(x => x.ItemId == item.Id)
                    .Select(x => (decimal?)x.Cess)
                    .DefaultIfEmpty(0)
                    .Average() ?? 0;

                // Helper function to calculate amount with GST + Cess
                decimal CalculateAmountWithTax(decimal qty, decimal rate, decimal gstPercent, decimal cessPercent)
                {
                    var baseAmount = qty * rate;
                    var gstAmount = baseAmount * (gstPercent / 100);
                    var cessAmount = baseAmount * (cessPercent / 100);
                    return baseAmount + gstAmount + cessAmount;
                }

                decimal CalculateGstAmount(decimal qty, decimal rate, decimal gstPercent)
                {
                    return (qty * rate) * (gstPercent / 100);
                }

                decimal CalculateCessAmount(decimal qty, decimal rate, decimal cessPercent)
                {
                    return (qty * rate) * (cessPercent / 100);
                }

                // ===== PURCHASE during period =====
                var purchaseItemList = purchases
                    .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate)
                    .SelectMany(p => p.PurchaseItems)
                    .Where(x => x.ItemId == item.Id)
                    .ToList();

                var purchaseQty = purchaseItemList.Sum(x => (decimal?)x.Qty + x.FreeQty) ?? 0;

                // Option 1: Use Average GST/Cess from current stock
                var purchaseBaseAmount = purchaseQty * avgRate;
                var purchaseGstAmount = CalculateGstAmount(purchaseQty, avgRate, avgGstPercent);
                var purchaseCessAmount = CalculateCessAmount(purchaseQty, avgRate, avgCessPercent);
                var purchaseAmount = purchaseBaseAmount + purchaseGstAmount + purchaseCessAmount;

                // ===== PURCHASE RETURN during period =====
                var purchaseReturnItemList = purchaseReturns
                    .Where(r => r.BillDate >= fromDate && r.BillDate <= toDate)
                    .SelectMany(r => r.PurchaseReturnItems)
                    .Where(x => x.ItemId == item.Id)
                    .ToList();

                var purchaseReturnQty = purchaseReturnItemList.Sum(x => (decimal?)x.Qty) ?? 0;
                var purchaseReturnBaseAmount = purchaseReturnQty * avgRate;
                var purchaseReturnGstAmount = CalculateGstAmount(purchaseReturnQty, avgRate, avgGstPercent);
                var purchaseReturnCessAmount = CalculateCessAmount(purchaseReturnQty, avgRate, avgCessPercent);
                var purchaseReturnAmount = purchaseReturnBaseAmount + purchaseReturnGstAmount + purchaseReturnCessAmount;

                // ===== SALES during period =====
                var salesItemList = sales
                    .Where(s => s.BillDate >= fromDate && s.BillDate <= toDate)
                    .SelectMany(s => s.SalesItems)
                    .Where(x => x.ItemMasterId == item.Id)
                    .ToList();

                var salesQty = salesItemList.Sum(x => (decimal?)x.Qty) ?? 0;

                // Option 1: Use Average GST/Cess from current stock
                var salesBaseAmount = salesQty * avgRate;
                var salesGstAmount = CalculateGstAmount(salesQty, avgRate, avgGstPercent);
                var salesCessAmount = CalculateCessAmount(salesQty, avgRate, avgCessPercent);
                var salesAmount = salesBaseAmount + salesGstAmount + salesCessAmount;

                // ===== SALES RETURN during period =====
                var salesReturnItemList = salesReturns
                    .Where(r => r.ChallanDate >= fromDate && r.ChallanDate <= toDate)
                    .SelectMany(r => r.StockReturnItems)
                    .Where(x => x.ItemMasterId == item.Id)
                    .ToList();

                var salesReturnQty = salesReturnItemList.Sum(x => (decimal?)x.Qty) ?? 0;
                var salesReturnBaseAmount = salesReturnQty * avgRate;
                var salesReturnGstAmount = CalculateGstAmount(salesReturnQty, avgRate, avgGstPercent);
                var salesReturnCessAmount = CalculateCessAmount(salesReturnQty, avgRate, avgCessPercent);
                var salesReturnAmount = salesReturnBaseAmount + salesReturnGstAmount + salesReturnCessAmount;

                // ===== CLOSING STOCK = current stock =====
                var closingQty = currentStock
                    .Where(x => x.ItemId == item.Id)
                    .Sum(x => (decimal?)x.Qty) ?? 0;
                var closingBaseAmount = closingQty * avgRate;
                var closingGstAmount = CalculateGstAmount(closingQty, avgRate, avgGstPercent);
                var closingCessAmount = CalculateCessAmount(closingQty, avgRate, avgCessPercent);
                var closingAmount = closingBaseAmount + closingGstAmount + closingCessAmount;

                // ===== OPENING STOCK = Closing - Purchases + Purchase Returns + Sales - Sales Returns =====
                var openingQty = closingQty - purchaseQty + purchaseReturnQty + salesQty - salesReturnQty;
                var openingBaseAmount = openingQty * avgRate;
                var openingGstAmount = CalculateGstAmount(openingQty, avgRate, avgGstPercent);
                var openingCessAmount = CalculateCessAmount(openingQty, avgRate, avgCessPercent);
                var openingAmount = openingBaseAmount + openingGstAmount + openingCessAmount;

                report.Add(new SalesVM
                {
                    ItemName = item.Name,
                    Packing = item.Packing,

                    // Opening Stock
                    OpeningQty = openingQty,
                    OpeningAmount = openingAmount,
                    OpeningGstAmount = openingGstAmount,
                    OpeningCessAmount = openingCessAmount,

                    // Purchase
                    PurchaseQty = purchaseQty,
                    PurchaseAmount = purchaseAmount,
                    PurchaseGstAmount = purchaseGstAmount,
                    PurchaseCessAmount = purchaseCessAmount,

                    // Purchase Return
                    PurchaseReturnQty = purchaseReturnQty,
                    PurchaseReturnAmount = purchaseReturnAmount,
                    PurchaseReturnGstAmount = purchaseReturnGstAmount,
                    PurchaseReturnCessAmount = purchaseReturnCessAmount,

                    // Sales
                    SalesQty = salesQty,
                    SalesAmount = salesAmount,
                    SalesGstAmount = salesGstAmount,
                    SalesCessAmount = salesCessAmount,

                    // Sales Return
                    SalesReturnQty = salesReturnQty,
                    SalesReturnAmount = salesReturnAmount,
                    SalesReturnGstAmount = salesReturnGstAmount,
                    SalesReturnCessAmount = salesReturnCessAmount,

                    // Closing Stock
                    ClosingQty = closingQty,
                    ClosingAmount = closingAmount,
                    ClosingGstAmount = closingGstAmount,
                    ClosingCessAmount = closingCessAmount
                });
            }

            return View(report);
        }

        private async Task<List<SalesVM>> GetStockSalesReport(DateTime fromDate, DateTime toDate, int? companyId)
        {
            var items = await _itemmasterservice.GetAll();
            var purchases = await _purchaseservice.GetAll();
            var purchaseReturns = await _purchasereturnservice.GetAll();
            var sales = await _salesservice.GetAll();
            var salesReturns = await _stockrturnservice.GetAll();
            var currentStock = await _purchaseitemservice.GetAll();

            if (companyId.HasValue)
                items = items.Where(i => i.CompanyId == companyId.Value).ToList();

            var report = new List<SalesVM>();

            foreach (var item in items)
            {
                var avgRate = currentStock
                    .Where(x => x.ItemId == item.Id)
                    .Select(x => (decimal?)x.Rate)
                    .DefaultIfEmpty(0)
                    .Average() ?? 0;

                var avgGst = currentStock
                    .Where(x => x.ItemId == item.Id)
                    .Select(x => (decimal?)x.Gst)
                    .DefaultIfEmpty(0)
                    .Average() ?? 0;

                var avgCess = currentStock
                    .Where(x => x.ItemId == item.Id)
                    .Select(x => (decimal?)x.Cess)
                    .DefaultIfEmpty(0)
                    .Average() ?? 0;

                decimal GstAmt(decimal q, decimal r, decimal g) => (q * r) * g / 100;
                decimal CessAmt(decimal q, decimal r, decimal c) => (q * r) * c / 100;

                var purchaseQty = purchases
                    .Where(p => p.BillDate >= fromDate && p.BillDate <= toDate)
                    .SelectMany(p => p.PurchaseItems)
                    .Where(x => x.ItemId == item.Id)
                    .Sum(x => (decimal?)x.Qty + x.FreeQty) ?? 0;

                var purchaseReturnQty = purchaseReturns
                    .Where(r => r.BillDate >= fromDate && r.BillDate <= toDate)
                    .SelectMany(r => r.PurchaseReturnItems)
                    .Where(x => x.ItemId == item.Id)
                    .Sum(x => (decimal?)x.Qty) ?? 0;

                var salesQty = sales
                    .Where(s => s.BillDate >= fromDate && s.BillDate <= toDate)
                    .SelectMany(s => s.SalesItems)
                    .Where(x => x.ItemMasterId == item.Id)
                    .Sum(x => (decimal?)x.Qty) ?? 0;

                var salesReturnQty = salesReturns
                    .Where(r => r.ChallanDate >= fromDate && r.ChallanDate <= toDate)
                    .SelectMany(r => r.StockReturnItems)
                    .Where(x => x.ItemMasterId == item.Id)
                    .Sum(x => (decimal?)x.Qty) ?? 0;

                var closingQty = currentStock
                    .Where(x => x.ItemId == item.Id)
                    .Sum(x => (decimal?)x.Qty) ?? 0;

                var openingQty = closingQty - purchaseQty + purchaseReturnQty + salesQty - salesReturnQty;

                decimal CalcAmt(decimal q)
                {
                    var baseAmt = q * avgRate;
                    return baseAmt + GstAmt(q, avgRate, avgGst) + CessAmt(q, avgRate, avgCess);
                }

                report.Add(new SalesVM
                {
                    ItemName = item.Name,
                    Packing = item.Packing,

                    OpeningQty = openingQty,
                    OpeningAmount = CalcAmt(openingQty),

                    PurchaseQty = purchaseQty,
                    PurchaseAmount = CalcAmt(purchaseQty),

                    PurchaseReturnQty = purchaseReturnQty,
                    PurchaseReturnAmount = CalcAmt(purchaseReturnQty),

                    SalesQty = salesQty,
                    SalesAmount = CalcAmt(salesQty),

                    SalesReturnQty = salesReturnQty,
                    SalesReturnAmount = CalcAmt(salesReturnQty),

                    ClosingQty = closingQty,
                    ClosingAmount = CalcAmt(closingQty)
                });
            }

            return report;
        }

        [HttpGet]
        public async Task<IActionResult> ExportStockInSalesStatementExcel(string fromDate, string toDate, int? companyId)
        {
            DateTime startDate, endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", null,
                System.Globalization.DateTimeStyles.None, out startDate))
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", null,
                System.Globalization.DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Now;
            }

            var report = await GetStockSalesReport(startDate, endDate, companyId);
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stock Statement");

            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user?.TenantId != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            int row = 1;

            ws.Cell(row, 1).Value = companyName;
            ws.Range(row, 1, row, 15).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row++;

            ws.Cell(row, 1).Value = "Stock In Sales Statement";
            ws.Range(row, 1, row, 15).Merge().Style.Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row++;

            ws.Cell(row, 1).Value = $"From : {fromDate:dd-MM-yyyy} To : {toDate:dd-MM-yyyy}";
            ws.Range(row, 1, row, 15).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row += 2;

            // HEADER
            ws.Cell(row, 1).Value = "Sr";
            ws.Cell(row, 2).Value = "Item";
            ws.Cell(row, 3).Value = "Packing";

            string[] groups = { "Opening", "Purchase", "Purchase Return", "Sales", "Sales Return", "Closing Balance" };

            int col = 4;
            foreach (var g in groups)
            {
                ws.Range(row, col, row, col + 1).Merge().Value = g;
                col += 2;
            }

            row++;

            for (int i = 4; i <= 15; i += 2)
            {
                ws.Cell(row, i).Value = "Qty";
                ws.Cell(row, i + 1).Value = "Amount";
            }

            ws.Range(row - 1, 1, row, 15).Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            row++;

            int sr = 1;

            foreach (var x in report)
            {
                ws.Cell(row, 1).Value = sr++;
                ws.Cell(row, 2).Value = x.ItemName;
                ws.Cell(row, 3).Value = x.Packing;

                ws.Cell(row, 4).Value = Math.Round(x.OpeningQty, 2);
                ws.Cell(row, 5).Value = Math.Round(x.OpeningAmount, 2);

                ws.Cell(row, 6).Value = x.PurchaseQty;
                ws.Cell(row, 7).Value = x.PurchaseAmount;

                ws.Cell(row, 8).Value = x.PurchaseReturnQty;
                ws.Cell(row, 9).Value = x.PurchaseReturnAmount;

                ws.Cell(row, 10).Value = x.SalesQty;
                ws.Cell(row, 11).Value = x.SalesAmount;

                ws.Cell(row, 12).Value = x.SalesReturnQty;
                ws.Cell(row, 13).Value = x.SalesReturnAmount;

                ws.Cell(row, 14).Value = x.ClosingQty;
                ws.Cell(row, 15).Value = x.ClosingAmount;

                row++;
            }
            // ===== TOTAL ROW =====
            ws.Cell(row, 1).Value = "TOTAL";
            ws.Range(row, 1, row, 3).Merge().Style.Font.SetBold();

            // Qty Totals
            ws.Cell(row, 4).Value = report.Sum(x => x.OpeningQty);
            ws.Cell(row, 6).Value = report.Sum(x => x.PurchaseQty);
            ws.Cell(row, 8).Value = report.Sum(x => x.PurchaseReturnQty);
            ws.Cell(row, 10).Value = report.Sum(x => x.SalesQty);
            ws.Cell(row, 12).Value = report.Sum(x => x.SalesReturnQty);
            ws.Cell(row, 14).Value = report.Sum(x => x.ClosingQty);

            // Amount Totals
            ws.Cell(row, 5).Value = report.Sum(x => x.OpeningAmount);
            ws.Cell(row, 7).Value = report.Sum(x => x.PurchaseAmount);
            ws.Cell(row, 9).Value = report.Sum(x => x.PurchaseReturnAmount);
            ws.Cell(row, 11).Value = report.Sum(x => x.SalesAmount);
            ws.Cell(row, 13).Value = report.Sum(x => x.SalesReturnAmount);
            ws.Cell(row, 15).Value = report.Sum(x => x.ClosingAmount);

            // Styling
            ws.Range(row, 1, row, 15).Style.Font.SetBold();
            ws.Range(row, 1, row, 15).Style.Fill.SetBackgroundColor(XLColor.LightGray);

            row++;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "StockStatement.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportStockInSalesStatementPdf(string fromDate, string toDate, int? companyId)
        {

            DateTime startDate, endDate;

            if (!DateTime.TryParseExact(fromDate, "dd-MM-yyyy", null,
                System.Globalization.DateTimeStyles.None, out startDate))
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }

            if (!DateTime.TryParseExact(toDate, "dd-MM-yyyy", null,
                System.Globalization.DateTimeStyles.None, out endDate))
            {
                endDate = DateTime.Now;
            }

            var report = await GetStockSalesReport(startDate, endDate, companyId);


            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user?.TenantId != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }
            doc.Add(new Paragraph(companyName).SetFont(bold).SetFontSize(14).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph("Stock In Sales Statement").SetFont(bold).SetFontSize(11).SetTextAlignment(TextAlignment.CENTER));
            doc.Add(new Paragraph($"From : {fromDate:dd-MM-yyyy} To : {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            Table table = new Table(15).UseAllAvailableWidth();

            string[] groups = { "Opening", "Purchase", "Purchase Return", "Sales", "Sales Return", "Closing Balance" };

            table.AddHeaderCell(new Cell(2, 1).Add(new Paragraph("Sr").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell(2, 1).Add(new Paragraph("Item").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell(2, 1).Add(new Paragraph("Packing").SetFont(bold).SetFontSize(9)));

            foreach (var g in groups)
                table.AddHeaderCell(new Cell(1, 2).Add(new Paragraph(g).SetFont(bold).SetFontSize(9)));

            foreach (var g in groups)
            {
                table.AddHeaderCell(new Paragraph("Qty").SetFont(bold).SetFontSize(9));
                table.AddHeaderCell(new Paragraph("Amount").SetFont(bold).SetFontSize(9));
            }

            int sr = 1;

            foreach (var x in report)
            {
                table.AddCell(new Paragraph(sr++.ToString()).SetFont(normal).SetFontSize(9));
                table.AddCell(new Paragraph(x.ItemName ?? "").SetFont(normal).SetFontSize(9));
                table.AddCell(new Paragraph(x.Packing ?? "").SetFontSize(9));
                table.AddCell(new Paragraph(Math.Round(x.OpeningQty, 2).ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(Math.Round(x.OpeningAmount, 2).ToString("0.00")).SetFontSize(9));

                table.AddCell(new Paragraph(x.PurchaseQty.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.PurchaseAmount.ToString("0.00")).SetFontSize(9));

                table.AddCell(new Paragraph(x.PurchaseReturnQty.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.PurchaseReturnAmount.ToString("0.00")).SetFontSize(9));

                table.AddCell(new Paragraph(x.SalesQty.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.SalesAmount.ToString("0.00")).SetFontSize(9));

                table.AddCell(new Paragraph(x.SalesReturnQty.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.SalesReturnAmount.ToString("0.00")).SetFontSize(9));

                table.AddCell(new Paragraph(x.ClosingQty.ToString()).SetFontSize(9));
                table.AddCell(new Paragraph(x.ClosingAmount.ToString("0.00")).SetFontSize(9));
            }
            // ===== TOTAL ROW =====
            table.AddCell(new Paragraph("TOTAL").SetFont(bold).SetFontSize(9));
            table.AddCell(new Paragraph("").SetFont(bold).SetFontSize(9));
            table.AddCell(new Paragraph("").SetFont(bold).SetFontSize(9));

            table.AddCell(new Paragraph(report.Sum(x => x.OpeningQty).ToString("0.##")).SetFont(bold).SetFontSize(9));
            table.AddCell(new Paragraph(report.Sum(x => x.OpeningAmount).ToString("0.00")).SetFont(bold).SetFontSize(9));

            table.AddCell(new Paragraph(report.Sum(x => x.PurchaseQty).ToString("0.##")).SetFont(bold).SetFontSize(9));
            table.AddCell(new Paragraph(report.Sum(x => x.PurchaseAmount).ToString("0.00")).SetFont(bold).SetFontSize(9));

            table.AddCell(new Paragraph(report.Sum(x => x.PurchaseReturnQty).ToString("0.##")).SetFont(bold).SetFontSize(9));
            table.AddCell(new Paragraph(report.Sum(x => x.PurchaseReturnAmount).ToString("0.00")).SetFont(bold).SetFontSize(9));

            table.AddCell(new Paragraph(report.Sum(x => x.SalesQty).ToString("0.##")).SetFont(bold).SetFontSize(9));
            table.AddCell(new Paragraph(report.Sum(x => x.SalesAmount).ToString("0.00")).SetFont(bold).SetFontSize(9));

            table.AddCell(new Paragraph(report.Sum(x => x.SalesReturnQty).ToString("0.##")).SetFont(bold).SetFontSize(9));
            table.AddCell(new Paragraph(report.Sum(x => x.SalesReturnAmount).ToString("0.00")).SetFont(bold).SetFontSize(9));

            table.AddCell(new Paragraph(report.Sum(x => x.ClosingQty).ToString("0.##")).SetFont(bold).SetFontSize(9));
            table.AddCell(new Paragraph(report.Sum(x => x.ClosingAmount).ToString("0.00")).SetFont(bold).SetFontSize(9));
            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "StockStatement.pdf");
        }

        //public async Task<IActionResult> LedgerReportPage()
        //{
        //    var customers = await _customerservice.GetAll();
        //    var suppliers = await _supplierRepo.GetALL();

        //    var list = new List<LedgerVM>();

        //    // ✅ CUSTOMER
        //    foreach (var c in customers)
        //    {
        //        var sales = await _salesService.GetByCustomerId(c.Id);

        //        var debit = sales.Sum(x => x.TotalPayable - x.PaidAmount);

        //        // ✅ ONLY IF PENDING
        //        if (debit > 0)
        //        {
        //            list.Add(new LedgerVM
        //            {
        //                Id = c.Id,
        //                PartyName = c.Name,
        //                Type = "Customer",
        //                Debit = debit,
        //                Credit = 0
        //            });
        //        }
        //    }

        //    // ✅ SUPPLIER
        //    foreach (var s in suppliers)
        //    {
        //        var purchases = await _purchaseservice.GetBySupplierId(s.Id);

        //        // CHANGE: PaidAmount ki jagah PaymentAmt use karein
        //        var credit = purchases.Sum(x => x.TotalPayable - x.PaymentAmt);

        //        // ✅ ONLY IF PENDING
        //        if (credit > 0)
        //        {
        //            list.Add(new LedgerVM
        //            {
        //                Id = s.Id,
        //                PartyName = s.FirstName,
        //                Type = "Supplier",
        //                Debit = 0,
        //                Credit = credit
        //            });
        //        }
        //    }
        //        return View(list);
        //}

        public async Task<IActionResult> LedgerReportPage()
        {
            var customers = await _customerservice.GetAll();
            var suppliers = await _supplierRepo.GetALL();

            var list = new List<LedgerVM>();

            // =========================================
            // CUSTOMER LEDGER
            // =========================================

            foreach (var c in customers)
            {
                var sales = await _salesService.GetByCustomerId(c.Id);

                decimal pendingAmount =
                    sales.Sum(x => x.TotalPayable - x.PaidAmount);

                // CUSTOMER ADVANCE

                var customerAdvance =
                    (await _customerAdvanceRepo
                    .GetByCustomerId(c.Id))
                    .FirstOrDefault();

                decimal advanceAmount =
                    customerAdvance?.AdvanceAmount ?? 0;

                // =====================================
                // CASE 1 : CUSTOMER PENDING
                // =====================================

                if (pendingAmount > 0)
                {
                    list.Add(new LedgerVM
                    {
                        Id = c.Id,
                        PartyName = c.Name,
                        Type = "Customer",
                        Debit = pendingAmount,
                        Credit = 0,
                        Remarks = "Pending Amount"
                    });
                }

                // =====================================
                // CASE 2 : CUSTOMER ADVANCE
                // =====================================

                if (advanceAmount > 0)
                {
                    list.Add(new LedgerVM
                    {
                        Id = c.Id,
                        PartyName = c.Name,
                        Type = "Customer Advance",
                        Debit = 0,
                        Credit = advanceAmount,
                        Remarks = "Advance Received"
                    });
                }
            }

            // =========================================
            // SUPPLIER LEDGER
            // =========================================

            foreach (var s in suppliers)
            {
                var purchases =
                    await _purchaseservice.GetBySupplierId(s.Id);

                decimal pendingAmount =
                    purchases.Sum(x =>
                        x.TotalPayable - x.PaidAmount);

                // SUPPLIER ADVANCE

                var supplierAdvance =
                    (await _supplierAdvanceRepo
                    .GetBySupplierId(s.Id))
                    .FirstOrDefault();

                decimal advanceAmount =
                    supplierAdvance?.AdvanceAmount ?? 0;

                // =====================================
                // CASE 1 : SUPPLIER PENDING
                // =====================================

                if (pendingAmount > 0)
                {
                    list.Add(new LedgerVM
                    {
                        Id = s.Id,
                        PartyName = s.FirstName,
                        Type = "Supplier",
                        Debit = 0,
                        Credit = pendingAmount,
                        Remarks = "Pending Payment"
                    });
                }

                // =====================================
                // CASE 2 : SUPPLIER ADVANCE
                // =====================================

                if (advanceAmount > 0)
                {
                    list.Add(new LedgerVM
                    {
                        Id = s.Id,
                        PartyName = s.FirstName,
                        Type = "Supplier Advance",
                        Debit = advanceAmount,
                        Credit = 0,
                        Remarks = "Advance Paid"
                    });
                }
            }
            // ✅ SUPPLIER
            //foreach (var s in suppliers)
            //{
            //    var purchases = await _purchaseservice.GetBySupplierId(s.Id);

            //    // CHANGE: PaidAmount ki jagah PaymentAmt use karein
            //    var credit = purchases.Sum(x => x.TotalPayable - x.PaymentAmt);

            //    // ✅ ONLY IF PENDING
            //    if (credit > 0)
            //    {
            //        list.Add(new LedgerVM
            //        {
            //            Id = s.Id,
            //            PartyName = s.FirstName,
            //            Type = "Supplier",
            //            Debit = 0,
            //            Credit = credit
            //        });
            //    }
            //}
            // =========================================
            // ORDERING
            // =========================================

            list = list
                .OrderBy(x => x.PartyName)
                .ToList();

            return View(list);
        }
        private async Task<List<LedgerVM>> GetLedgerSummary()
        {
            var customers = await _customerservice.GetAll();
            var suppliers = await _supplierRepo.GetALL();

            var list = new List<LedgerVM>();

            // ✅ CUSTOMER
            foreach (var c in customers)
            {
                var sales = await _salesService.GetByCustomerId(c.Id);

                var debit = sales.Sum(x => x.TotalPayable - x.PaidAmount);

                // ✅ ONLY IF PENDING
                if (debit > 0)
                {
                    list.Add(new LedgerVM
                    {
                        Id = c.Id,
                        PartyName = c.Name,
                        Type = "Customer",
                        Debit = debit,
                        Credit = 0
                    });
                }
            }

            // ✅ SUPPLIER
            foreach (var s in suppliers)
            {
                var purchases = await _purchaseservice.GetBySupplierId(s.Id);

                // CHANGE: PaidAmount ki jagah PaymentAmt use karein
                var credit = purchases.Sum(x => x.TotalPayable - x.PaymentAmt);

                // ✅ ONLY IF PENDING
                if (credit > 0)
                {
                    list.Add(new LedgerVM
                    {
                        Id = s.Id,
                        PartyName = s.FirstName,
                        Type = "Supplier",
                        Debit = 0,
                        Credit = credit
                    });
                }
            }

            return list;
        }

        [HttpGet]
        public async Task<IActionResult> ExportLedgerSummaryExcel()
        {
            var list = await GetLedgerSummary();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Ledger Summary");

            var user = await _usersManager.GetUserAsync(User);
            var tenant = await _tenantRepository.GetById(user.TenantId);

            // 🔹 HEADER
            ws.Cell(1, 1).Value = tenant?.Name ?? "Company Name";
            ws.Range(1, 1, 1, 4).Merge().Style.Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = tenant?.Address1 ?? "Address";
            ws.Range(2, 1, 2, 4).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(5, 1).Value = "LEDGER SUMMARY REPORT";
            ws.Range(5, 1, 5, 4).Merge().Style.Font.SetBold()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // 🔹 TABLE HEADER
            ws.Cell(7, 1).Value = "Party Name";
            ws.Cell(7, 2).Value = "Type";
            ws.Cell(7, 3).Value = "Debit";
            ws.Cell(7, 4).Value = "Credit";

            ws.Range(7, 1, 7, 4).Style.Font.Bold = true;
            ws.Range(7, 1, 7, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;

            int row = 8;

            foreach (var item in list)
            {
                ws.Cell(row, 1).Value = item.PartyName;
                ws.Cell(row, 2).Value = item.Type;
                ws.Cell(row, 3).Value = item.Debit;
                ws.Cell(row, 4).Value = item.Credit;
                row++;
            }

            // 🔥 TOTAL
            //decimal totalDr = list.Sum(x => x.Debit);
            //decimal totalCr = list.Sum(x => x.Credit);

            //ws.Cell(row, 2).Value = "TOTAL";
            //ws.Cell(row, 3).Value = totalDr;
            //ws.Cell(row, 4).Value = totalCr;

            //ws.Range(row, 1, row, 4).Style.Font.Bold = true;
            //ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "LedgerSummary.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportLedgerSummaryPdf()
        {
            var list = await GetLedgerSummary();

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            document.SetMargins(10, 10, 10, 10);

            var user = await _usersManager.GetUserAsync(User);
            var tenant = await _tenantRepository.GetById(user.TenantId);

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // 🔹 HEADER
            document.Add(new Paragraph(tenant?.Name ?? "Company Name").SetFont(bold).SetFontSize(12).SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph(tenant?.Address1 ?? "Address").SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("\nLEDGER SUMMARY REPORT").SetFont(bold).SetFontSize(11).SetTextAlignment(TextAlignment.CENTER));

            // 🔹 TABLE
            float[] widths = { 6, 3, 3, 3 };
            Table table = new Table(widths).UseAllAvailableWidth();

            table.AddHeaderCell(new Paragraph("Party Name").SetFont(bold).SetFontSize(10));
            table.AddHeaderCell(new Paragraph("Type").SetFont(bold).SetFontSize(10));
            table.AddHeaderCell(new Paragraph("Debit").SetFont(bold).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));
            table.AddHeaderCell(new Paragraph("Credit").SetFont(bold).SetFontSize(10).SetTextAlignment(TextAlignment.RIGHT));

            foreach (var item in list)
            {
                table.AddCell(new Paragraph(item.PartyName).SetFontSize(9));
                table.AddCell(new Paragraph(item.Type).SetFontSize(9));
                table.AddCell(new Paragraph(item.Debit.ToString("0.00")).SetTextAlignment(TextAlignment.RIGHT));
                table.AddCell(new Paragraph(item.Credit.ToString("0.00")).SetTextAlignment(TextAlignment.RIGHT));
            }

            // 🔥 TOTAL
            //decimal totalDr = list.Sum(x => x.Debit);
            //decimal totalCr = list.Sum(x => x.Credit);

            //table.AddCell("");
            //table.AddCell(new Paragraph("TOTAL").SetFont(bold));
            //table.AddCell(new Paragraph(totalDr.ToString("0.00")).SetFont(bold).SetTextAlignment(TextAlignment.RIGHT));
            //table.AddCell(new Paragraph(totalCr.ToString("0.00")).SetFont(bold).SetTextAlignment(TextAlignment.RIGHT));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "LedgerSummary.pdf");
        }
        public async Task<IActionResult> LedgerDetail(int id, string type)
        {
            ViewBag.Id = id;

            var normalizedType = type?.Trim().ToLowerInvariant() ?? string.Empty;
            if (normalizedType.Contains("customer"))
            {
                normalizedType = "customer";
            }
            else if (normalizedType.Contains("supplier"))
            {
                normalizedType = "supplier";
            }

            ViewBag.Type = normalizedType;

            string phoneNo = "";

            if (normalizedType == "customer")
            {
                var customer = (await _customerservice.GetAll())
                               .FirstOrDefault(x => x.Id == id);
                phoneNo = customer?.PhoneNo ?? string.Empty;
            }
            else if (normalizedType == "supplier")
            {
                var supplier = (await _supplierRepo.GetALL())
                               .FirstOrDefault(x => x.Id == id);
                phoneNo = supplier?.PhoneNO ?? string.Empty;
            }

            ViewBag.Phone = phoneNo;

            var list = await GetLedgerData(id, normalizedType);
            return View(list);
        }

        private async Task<List<LedgerDetailVM>> GetLedgerData(int id, string type)
        {
            var normalizedType = type?.Trim().ToLowerInvariant() ?? string.Empty;
            if (normalizedType.Contains("customer"))
            {
                normalizedType = "customer";
            }
            else if (normalizedType.Contains("supplier"))
            {
                normalizedType = "supplier";
            }

            var list = new List<LedgerDetailVM>();

            string GetEntryType(string? description, string defaultType)
            {
                if (string.IsNullOrWhiteSpace(description))
                {
                    return defaultType;
                }

                var value = description.Trim().ToLowerInvariant();
                if (value.Contains("receive voucher"))
                {
                    return "ReceiveVoucher";
                }

                if (value.Contains("payment voucher") || value.Contains("via voucher"))
                {
                    return "PaymentVoucher";
                }

                return defaultType;
            }

            if (normalizedType == "supplier")
            {
                var purchases = (await _purchaseservice.GetAll())
                    .Where(x => x.SupplierId == id)
                    .OrderBy(x => x.BillDate)
                    .ToList();

                foreach (var purchase in purchases)
                {
                    list.Add(new LedgerDetailVM
                    {
                        Date = purchase.BillDate ?? DateTime.Now,
                        Type = "Purchase",
                        Particular = "Bill No. " + purchase.BillNo,
                        Debit = 0,
                        Credit = purchase.TotalPayable
                    });

                    var paymentDetails = (purchase.PaymentDetails ?? new List<SalsePaymentDetails>())
                        .Where(x => x.Amount > 0)
                        .OrderBy(x => x.Date ?? purchase.BillDate ?? DateTime.Now)
                        .ToList();

                    foreach (var payment in paymentDetails)
                    {
                        var description = string.IsNullOrWhiteSpace(payment.Description)
                            ? "Payment"
                            : payment.Description;

                        list.Add(new LedgerDetailVM
                        {
                            Date = payment.Date ?? purchase.BillDate ?? DateTime.Now,
                            Type = GetEntryType(payment.Description, "Purchase"),
                            Particular = "Bill No. " + purchase.BillNo + " - " + description,
                            Debit = payment.Amount,
                            Credit = 0
                        });
                    }
                }

                list = list
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Type == "Purchase" ? 0 : 1)
                    .ToList();

                decimal balance = 0;
                foreach (var item in list)
                {
                    balance += item.Credit - item.Debit;
                    item.Balance = balance;
                }
            }
            else if (normalizedType == "customer")
            {
                var sales = (await _salesService.GetByCustomerId(id))
                    .OrderBy(x => x.BillDate)
                    .ToList();

                foreach (var sale in sales)
                {
                    list.Add(new LedgerDetailVM
                    {
                        Date = sale.BillDate ?? DateTime.Now,
                        Type = "Sale",
                        Particular = "Bill No. " + sale.BillNo,
                        Debit = sale.TotalPayable,
                        Credit = 0
                    });

                    var paymentDetails = (sale.SalsePaymentDetails ?? new List<SalsePaymentDetails>())
                        .Where(x => x.Amount > 0)
                        .OrderBy(x => x.Date ?? sale.BillDate ?? DateTime.Now)
                        .ToList();

                    foreach (var payment in paymentDetails)
                    {
                        var description = string.IsNullOrWhiteSpace(payment.Description)
                            ? "Payment"
                            : payment.Description;

                        list.Add(new LedgerDetailVM
                        {
                            Date = payment.Date ?? sale.BillDate ?? DateTime.Now,
                            Type = GetEntryType(payment.Description, "Sale"),
                            Particular = "Bill No. " + sale.BillNo + " - " + description,
                            Debit = 0,
                            Credit = payment.Amount
                        });
                    }
                }

                list = list
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Type == "Sale" ? 0 : 1)
                    .ToList();

                decimal balance = 0;
                foreach (var item in list)
                {
                    balance += item.Debit - item.Credit;
                    item.Balance = balance;
                }
            }

            return list;
        }

        [HttpGet]
        public async Task<IActionResult> ExportLedgerDetailExcel(int id, string type)
        {
            var list = await GetLedgerData(id, type ?? string.Empty);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Ledger Detail");

            var user = await _usersManager.GetUserAsync(User);
            var tenant = await _tenantRepository.GetById(user.TenantId);

            // HEADER
            ws.Cell(1, 1).Value = tenant?.Name;
            ws.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = tenant?.Address1;
            ws.Range(2, 1, 2, 6).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // ✅ FIXED (Row 3 instead of 5)
            ws.Cell(3, 1).Value = "LEDGER DETAIL REPORT";
            ws.Range(3, 1, 3, 6).Merge().Style.Font.SetBold()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TABLE HEADER
            ws.Cell(5, 1).Value = "Date";
            ws.Cell(5, 2).Value = "Particulars";
            ws.Cell(5, 3).Value = "Type";
            ws.Cell(5, 4).Value = "Debit (Dr)";
            ws.Cell(5, 5).Value = "Credit (Cr)";
            ws.Cell(5, 6).Value = "Balance";

            ws.Range(5, 1, 5, 6).Style.Font.Bold = true;
            ws.Range(5, 1, 5, 6).Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 6;
            foreach (var item in list)
            {
                ws.Cell(row, 1).Value = item.Date.ToString("dd-MM-yy");
                ws.Cell(row, 2).Value = item.Particular;
                ws.Cell(row, 3).Value = item.Type;
                ws.Cell(row, 4).Value = item.Debit == 0 ? 0.00 : item.Debit;
                ws.Cell(row, 5).Value = item.Credit == 0 ? 0.00 : item.Credit;
                ws.Cell(row, 6).Value = item.Balance;
                row++;
            }

            decimal totalDr = list.Sum(x => x.Debit);
            decimal totalCr = list.Sum(x => x.Credit);
            decimal closing = list.LastOrDefault()?.Balance ?? 0;

            // Closing Balance Row
            ws.Cell(row, 1).Value = "Closing Balance";
            ws.Cell(row, 4).Value = totalDr;
            ws.Cell(row, 5).Value = totalCr;
            ws.Cell(row, 6).Value = closing;

            ws.Range(row, 1, row, 6).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "LedgerDetail.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportLedgerDetailPdf(int id, string type)
        {
            var list = await GetLedgerData(id, type ?? string.Empty);

            using var stream = new MemoryStream();
            var pdf = new PdfDocument(new PdfWriter(stream));
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            document.SetMargins(10, 10, 10, 10);

            var user = await _usersManager.GetUserAsync(User);
            var tenant = await _tenantRepository.GetById(user.TenantId);

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // ✅ HEADER (Font Size 9)
            document.Add(new Paragraph(tenant?.Name)
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph(tenant?.Address1)
                .SetFont(normal).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("LEDGER DETAIL REPORT")
                .SetFont(bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));

            float[] widths = { 2, 5, 2, 2, 2, 2 };
            Table table = new Table(widths).UseAllAvailableWidth();

            // ✅ HEADER ROW (Font Size 9)
            table.AddHeaderCell(new Cell().Add(new Paragraph("Date").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Particulars").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Type").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Debit (Dr)").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Credit (Cr)").SetFont(bold).SetFontSize(9)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Balance").SetFont(bold).SetFontSize(9)));

            // ✅ DATA ROWS (Font Size 9)
            foreach (var item in list)
            {
                table.AddCell(new Cell().Add(new Paragraph(item.Date.ToString("dd-MM-yy")).SetFont(normal).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(item.Particular).SetFont(normal).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(item.Type).SetFont(normal).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(item.Debit == 0 ? "0.00" : item.Debit.ToString("0.00")).SetFont(normal).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(item.Credit == 0 ? "0.00" : item.Credit.ToString("0.00")).SetFont(normal).SetFontSize(9)));
                table.AddCell(new Cell().Add(new Paragraph(item.Balance.ToString("0.00")).SetFont(normal).SetFontSize(9)));
            }

            decimal totalDr = list.Sum(x => x.Debit);
            decimal totalCr = list.Sum(x => x.Credit);
            decimal closing = list.LastOrDefault()?.Balance ?? 0;

            // ✅ Closing Balance Row (Font Size 9)
            table.AddCell(new Cell().Add(new Paragraph("Closing Balance").SetFont(bold).SetFontSize(9)));
            table.AddCell(new Cell().Add(new Paragraph("").SetFont(normal).SetFontSize(9)));
            table.AddCell(new Cell().Add(new Paragraph("").SetFont(normal).SetFontSize(9)));
            table.AddCell(new Cell().Add(new Paragraph(totalDr.ToString("0.00")).SetFont(normal).SetFontSize(9)));
            table.AddCell(new Cell().Add(new Paragraph(totalCr.ToString("0.00")).SetFont(normal).SetFontSize(9)));
            table.AddCell(new Cell().Add(new Paragraph(closing.ToString("0.00")).SetFont(normal).SetFontSize(9)));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "LedgerDetail.pdf");
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("Sales/DownloadPublicLedgerPdf")]
        public async Task<IActionResult> DownloadPublicLedgerPdf(string token)
        {
            try
            {
                byte[] base64EncodedBytes = Convert.FromBase64String(token);
                string decodedData = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

                string[] parts = decodedData.Split('|');
                if (parts.Length != 3) return BadRequest("Invalid Link.");

                int id = Convert.ToInt32(parts[0]);
                string type = parts[1].ToLower();

                string tenantId = parts[2];

                var tenant = await _tenantRepository.GetById(tenantId);

                var list = new List<LedgerDetailVM>();
                decimal totalPendingBalance = 0;

                if (type == "supplier")
                {
                    var purchases = await _purchaseservice.GetBySupplierId(id);
                    foreach (var p in purchases.OrderBy(x => x.BillDate))
                    {
                        var pending = p.TotalPayable - p.PaidAmount;
                        if (pending > 0)
                        {
                            totalPendingBalance -= pending;
                            list.Add(new LedgerDetailVM
                            {
                                Date = p.BillDate ?? DateTime.Now,
                                Particular = "Bill No. " + p.BillNo + " (Pending)",
                                Debit = 0,
                                Credit = pending,
                                Balance = totalPendingBalance
                            });
                        }
                    }
                }
                else if (type == "customer")
                {
                    var sales = await _salesService.GetByCustomerId(id);
                    foreach (var s in sales.OrderBy(x => x.BillDate))
                    {
                        var pending = s.TotalPayable - s.PaidAmount;
                        if (pending > 0)
                        {
                            totalPendingBalance += pending;
                            list.Add(new LedgerDetailVM
                            {
                                Date = s.BillDate ?? DateTime.Now,
                                Particular = "Bill No. " + s.BillNo + " (Pending)",
                                Debit = pending,
                                Credit = 0,
                                Balance = totalPendingBalance
                            });
                        }
                    }
                }

                using var stream = new MemoryStream();
                var pdf = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfWriter(stream));
                var document = new iText.Layout.Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());
                document.SetMargins(10, 10, 10, 10);

                iText.Kernel.Font.PdfFont bold = iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
                iText.Kernel.Font.PdfFont normal = iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                document.Add(new iText.Layout.Element.Paragraph(tenant?.Name).SetFont(bold).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new iText.Layout.Element.Paragraph("OUTSTANDING DUE BILLS").SetFont(bold).SetFontSize(11).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                float[] widths = { 2, 6, 2, 2, 2 };
                iText.Layout.Element.Table table = new iText.Layout.Element.Table(widths).UseAllAvailableWidth();

                table.AddHeaderCell("Date");
                table.AddHeaderCell("Particular (Rest Amount)");
                table.AddHeaderCell("Debit");
                table.AddHeaderCell("Credit");
                table.AddHeaderCell("Balance");

                foreach (var item in list)
                {
                    table.AddCell(item.Date.ToString("dd-MM-yyyy"));
                    table.AddCell(item.Particular);
                    table.AddCell(item.Debit.ToString("0.00"));
                    table.AddCell(item.Credit.ToString("0.00"));
                    table.AddCell(item.Balance.ToString("0.00"));
                }

                document.Add(table);
                document.Close();

                return File(stream.ToArray(), "application/pdf", $"Pending_Dues_{id}.pdf");
            }
            catch (Exception)
            {
                return BadRequest("Error processing PDF.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrintInvoice(int? id)
        {
            if (id == null) return BadRequest();

            try
            {
                var saleData = await _salesService.GetById(id);

                if (saleData != null && saleData.CustomerId.HasValue)
                {
                    var pointSettings = await _unitofwork.GetRepository<PointSetting>().Query()
                        .Where(x => x.TenantId == saleData.TenantId)
                        .ToListAsync();
                    var earliestSetting = pointSettings.OrderBy(x => x.Created).FirstOrDefault();
                    var earliestCreated = earliestSetting?.Created ?? DateTime.MaxValue;
                    var saleCompareDate = saleData.Created ?? saleData.BillDate ?? DateTime.Now;

                    if (saleCompareDate >= earliestCreated)
                    {
                        var pointRepo = _unitofwork.GetRepository<PointTransaction>();
                        var currentPointsBalance = await pointRepo.Query()
                            .Where(x => x.CustomerId == saleData.CustomerId.Value)
                            .SumAsync(x => x.EarnedPoints - x.RedeemedPoints);
                        ViewBag.CustomerPointsBalance = currentPointsBalance;

                        var pointTxForSale = await pointRepo.Query()
                            .FirstOrDefaultAsync(x => x.SaleId == saleData.Id);
                        if (pointTxForSale != null)
                        {
                            ViewBag.PointsEarned = pointTxForSale.EarnedPoints;
                            ViewBag.PointsRedeemed = pointTxForSale.RedeemedPoints;
                        }
                        else
                        {
                            ViewBag.PointsEarned = 0;
                            ViewBag.PointsRedeemed = 0;
                        }
                    }
                    else
                    {
                        ViewBag.CustomerPointsBalance = null;
                        ViewBag.PointsEarned = null;
                        ViewBag.PointsRedeemed = null;
                    }
                }

                if (saleData == null) return NotFound("Invoice data not found.");

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                var themeSetting = await _themeRepo.GetDefaultThemeAsync(userId);

                if (themeSetting == null)
                {
                    themeSetting = await _themeRepo.GetThemeSettingAsync(userId, "A4");
                }

                ViewBag.ThemeSettings = themeSetting;

                // Temporary delegation version kept commented for reference, as requested.
                // return await Print(id.Value);

                // Legacy placeholder business profile kept commented for reference.
                // ViewBag.BusinessName = "Your Business Name";

                var tenantId = User.FindFirstValue("TenantId");
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    var tenant = (await _tenantRepository.GetAll()).FirstOrDefault(x => x.Id == tenantId);
                    if (tenant != null)
                    {
                        var addressParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(tenant.Address1)) addressParts.Add(tenant.Address1);
                        if (!string.IsNullOrWhiteSpace(tenant.Address2)) addressParts.Add(tenant.Address2);
                        if (!string.IsNullOrWhiteSpace(tenant.Location)) addressParts.Add(tenant.Location);
                        if (tenant.City != null && !string.IsNullOrWhiteSpace(tenant.City.Name)) addressParts.Add(tenant.City.Name);
                        if (tenant.State != null && !string.IsNullOrWhiteSpace(tenant.State.Name)) addressParts.Add(tenant.State.Name);
                        if (!string.IsNullOrWhiteSpace(tenant.PinCode)) addressParts.Add($"Pin: {tenant.PinCode}");

                        var businessPhone = !string.IsNullOrWhiteSpace(tenant.MobileNo) ? tenant.MobileNo : tenant.Phone;

                        ViewBag.BusinessName = tenant.Name ?? "Your Business Name";
                        ViewBag.BusinessAddress = string.Join(", ", addressParts);
                        ViewBag.BusinessPhone = businessPhone ?? string.Empty;
                        ViewBag.BusinessEmail = tenant.Email ?? string.Empty;
                        ViewBag.BusinessGSTIN = tenant.GstNo ?? string.Empty;
                    }
                }

                if (ViewBag.BusinessName == null) ViewBag.BusinessName = "Your Business Name";
                if (ViewBag.BusinessAddress == null) ViewBag.BusinessAddress = string.Empty;
                if (ViewBag.BusinessPhone == null) ViewBag.BusinessPhone = string.Empty;
                if (ViewBag.BusinessEmail == null) ViewBag.BusinessEmail = string.Empty;
                if (ViewBag.BusinessGSTIN == null) ViewBag.BusinessGSTIN = string.Empty;

                string selectedPaperSize = themeSetting?.PaperSize ?? "A4";

                if (string.Equals(selectedPaperSize, "Thermal", StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.PaperSize = themeSetting?.ThermalPaperSize == 58 ? "58mm" : "80mm";
                    return View("~/Views/Shared/InvoiceTemplates/Thermal_Print.cshtml", saleData);
                }
                else if (string.Equals(selectedPaperSize, "A5", StringComparison.OrdinalIgnoreCase))
                {
                    return View("~/Views/Shared/InvoiceTemplates/A5_Print.cshtml", saleData);
                }
                else
                {
                    return View("~/Views/Shared/InvoiceTemplates/A4_Print.cshtml", saleData);
                }
            }
            catch (Exception)
            {
                // Error handling
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> NarcoticDrugsReport(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            ViewBag.FromDate = fromDate?.ToString("dd-MM-yyyy");
            ViewBag.ToDate = toDate?.ToString("dd-MM-yyyy");

            var sales = await _salesservice.GetAll();

            var reportData = sales
                .Where(s => s.BillDate >= fromDate && s.BillDate <= toDate)
                .SelectMany(s => s.SalesItems
                    .Where(x => x.ItemMaster != null && x.ItemMaster.Narcotics == true)
                    .Select(i => new SalesVM
                    {
                        BillNo = s.BillNo,
                        BillDate = s.BillDate,
                        CustomerName = s.Customers != null ? s.Customers.Name : "Walk-in",
                        Address = s.Customers != null ? s.Customers.Address : "",
                        DoctorName = s.PharmacyDoctor != null ? s.PharmacyDoctor.Name : "",
                        ItemName = i.ItemMaster?.Name ?? "",
                        Type = i.ItemMaster?.Packing ?? "",
                        Qty = (int)i.Qty,
                        Batch = i.Batch,
                        ExpiryDate = i.Expirydate,
                        Mfg = i.ItemMaster?.Company?.Name ?? ""
                    }))
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            ViewBag.TotalQty = reportData.Sum(x => x.Qty);

            // Tenant Details
            var tenantId = User?.FindFirst("TenantId")?.Value;
            string tenantName = "PHARMA ITEM MASTER";
            if (!string.IsNullOrEmpty(tenantId))
            {
                var tenants = await _tenantRepository.GetAll();
                var tenant = tenants.FirstOrDefault(x => x.Id == tenantId);
                if (tenant != null)
                    tenantName = tenant.Name;
            }
            ViewBag.TenantName = tenantName;

            return View(reportData);
        }

        [HttpGet]
        public async Task<IActionResult> ExportNarcoticDrugsExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            var sales = await _salesservice.GetAll();

            var report = sales
                .Where(s => s.BillDate >= fromDate && s.BillDate <= toDate)
                .SelectMany(s => s.SalesItems
                    .Where(x => x.ItemMaster != null && x.ItemMaster.Narcotics == true)
                    .Select(i => new
                    {
                        BillNo = s.BillNo,
                        BillDate = s.BillDate,
                        Patient = s.Customers != null ? s.Customers.Name : "Walk-in",
                        ItemName = i.ItemMaster?.Name ?? "",
                        Type = i.ItemMaster?.Packing ?? "",
                        Qty = (int)i.Qty,
                        Batch = i.Batch ?? "",
                        Mfg = i.ItemMaster?.Company?.Name ?? "",
                        Expiry = i.Expirydate
                    }))
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Narcotic Report");

            // Company Name
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // Header
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 10).Merge().Style
                .Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Narcotic Drugs Report";
            ws.Range(2, 1, 2, 10).Merge().Style
                .Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 10).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Table Header
            int row = 5;

            string[] headers = {
        "Sr","Bill No","Date","Patient","Medicine",
        "Type","Qty","Batch","Mfg","Expiry"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
                ws.Cell(row, i + 1).Style.Font.Bold = true;
            }

            row++;

            int sr = 1;
            int totalQty = 0;

            foreach (var item in report)
            {
                ws.Cell(row, 1).Value = sr++;
                ws.Cell(row, 2).Value = item.BillNo;
                ws.Cell(row, 3).Value = item.BillDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 4).Value = item.Patient;
                ws.Cell(row, 5).Value = item.ItemName;
                ws.Cell(row, 6).Value = item.Type;
                ws.Cell(row, 7).Value = item.Qty;
                ws.Cell(row, 8).Value = item.Batch;
                ws.Cell(row, 9).Value = item.Mfg;
                ws.Cell(row, 10).Value = item.Expiry?.ToString("dd-MM-yyyy");

                totalQty += item.Qty;
                row++;
            }

            // TOTAL
            ws.Cell(row, 1).Value = "TOTAL QTY";
            ws.Cell(row, 1).Style.Font.Bold = true;

            ws.Cell(row, 7).Value = totalQty;
            ws.Cell(row, 7).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "NarcoticDrugsReport.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportNarcoticDrugsPdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            var sales = await _salesservice.GetAll();

            var report = sales
                .Where(s => s.BillDate >= fromDate && s.BillDate <= toDate)
                .SelectMany(s => s.SalesItems
                    .Where(x => x.ItemMaster != null && x.ItemMaster.Narcotics == true)
                    .Select(i => new
                    {
                        BillNo = s.BillNo,
                        BillDate = s.BillDate,
                        Patient = s.Customers != null ? s.Customers.Name : "Walk-in",
                        ItemName = i.ItemMaster?.Name ?? "",
                        Type = i.ItemMaster?.Packing ?? "",
                        Qty = (int)i.Qty,
                        Batch = i.Batch ?? "",
                        Mfg = i.ItemMaster?.Company?.Name ?? "",
                        Expiry = i.Expirydate
                    }))
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fs = 9;

            // Company Name
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // Header
            doc.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph("Narcotic Drugs Report")
                .SetFont(bold).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // Table
            Table table = new Table(new float[] { 2, 4, 3, 5, 6, 4, 2, 4, 4, 4 })
                .UseAllAvailableWidth();

            string[] headers = {
        "Sr","Bill No","Date","Patient","Medicine",
        "Type","Qty","Batch","Mfg","Expiry"
    };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(fs))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            int sr = 1;
            int totalQty = 0;

            foreach (var x in report)
            {
                table.AddCell(new Paragraph(sr++.ToString()).SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.BillNo ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.BillDate?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Patient ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.ItemName ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Type ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Qty.ToString()).SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Batch ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Mfg ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Expiry?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fs));

                totalQty += x.Qty;
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 6)
                .Add(new Paragraph("TOTAL QTY").SetFont(bold).SetFontSize(fs)));

            table.AddCell(new Paragraph(totalQty.ToString())
                .SetFont(bold).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.RIGHT));

            // empty cells
            table.AddCell(new Paragraph(""));
            table.AddCell(new Paragraph(""));
            table.AddCell(new Paragraph(""));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "NarcoticDrugsReport.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> PrintNarcoticForm(string billNo)
        {
            var sales = await _salesservice.GetAll();
            var sale = sales.FirstOrDefault(s => s.BillNo == billNo);

            if (sale == null)
                return Content("Bill Not Found");

            // Tenant Details
            var tenantId = User?.FindFirst("TenantId")?.Value;
            string tenantName = "PHARMA ITEM MASTER";
            string tenantAddress = "";

            if (!string.IsNullOrEmpty(tenantId))
            {
                var tenants = await _tenantRepository.GetAll();
                var tenant = tenants.FirstOrDefault(x => x.Id == tenantId);
                if (tenant != null)
                {
                    tenantName = tenant.Name;
                    tenantAddress = tenant.Address1 ?? "";
                }
            }

            // Narcotic Items only
            var narcoticItems = sale.SalesItems
                .Where(x => x.ItemMaster != null && x.ItemMaster.Narcotics == true)
                .ToList();

            var model = new SalesVM
            {
                BillNo = sale.BillNo,
                BillDate = sale.BillDate,
                CustomerName = sale.Customers?.Name ?? "Walk-in",
                Address = sale.Customers?.Address ?? "",
                DoctorName = sale.PharmacyDoctor?.Name ?? "",
                Tenantname = tenantName,
                TenantAddress = tenantAddress,
                SalesItemVMs = narcoticItems.Select(i => new SalesItemVM
                {
                    ItemName = i.ItemMaster?.Name ?? "",
                    Packing = i.ItemMaster?.Packing ?? "",
                    Batch = i.Batch,
                    Expirydate = i.Expirydate,
                    Qty = (int)i.Qty,
                    Mfg = i.ItemMaster?.Company?.Name ?? ""
                }).ToList()
            };

            return View(model);
        }
        // ================= REPORT LIST ACTION =================
        [HttpGet]
        public async Task<IActionResult> ScheduleHDrugsReport(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            ViewBag.FromDate = fromDate?.ToString("dd-MM-yyyy");
            ViewBag.ToDate = toDate?.ToString("dd-MM-yyyy");

            var sales = await _salesservice.GetAll();

            // Filter H items and Flatten to List
            var reportData = sales
                .Where(s => s.BillDate >= fromDate && s.BillDate <= toDate)
                .SelectMany(s => s.SalesItems
                .Where(x => x.ItemMaster.ScheduleH == true)
                .Select(i => new SalesVM
                {
                    BillNo = s.BillNo,
                    BillDate = s.BillDate,
                    CustomerName = s.Customers?.Name ?? "Walk-in",
                    Address = s.Customers?.Address ?? "",
                    DoctorName = s.PharmacyDoctor?.Name ?? "",
                    ItemName = i.ItemMaster?.Name ?? "",
                    Type = i.ItemMaster?.Packing ?? "",
                    Qty = (int)i.Qty,
                    Batch = i.Batch,
                    Rate = i.Rate,
                    ExpiryDate = i.Expirydate,
                    Mfg = i.ItemMaster?.Company?.Name ?? ""
                }))
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            ViewBag.TotalQty = reportData.Sum(x => x.Qty);
            return View(reportData);
        }

        [HttpGet]
        public async Task<IActionResult> ExportScheduleHDrugsExcel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            var sales = await _salesservice.GetAll();

            var report = sales
                .Where(s => s.BillDate >= fromDate && s.BillDate <= toDate)
                .SelectMany(s => s.SalesItems
                    .Where(x => x.ItemMaster.ScheduleH == true)
                    .Select(i => new
                    {
                        BillNo = s.BillNo,
                        BillDate = s.BillDate,
                        Patient = (s.Customers?.Name ?? "Walk-in") + "\n" + (s.Customers?.Address ?? ""),
                        Doctor = s.PharmacyDoctor?.Name ?? "",
                        ItemName = i.ItemMaster?.Name ?? "",
                        Type = i.ItemMaster?.Packing ?? "",
                        Qty = (int)i.Qty,
                        Batch = i.Batch ?? "",
                        Mfg = i.ItemMaster?.Company?.Name ?? "",
                        Expiry = i.Expirydate
                    }))
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Schedule H Report");

            // Company Name
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 11).Merge().Style
                .Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Schedule H Drugs Report";
            ws.Range(2, 1, 2, 11).Merge().Style
                .Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 11).Merge()
                .Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TABLE HEADER
            int row = 5;

            string[] headers = {
        "Sr","Bill No","Date","Patient","Doctor",
        "Medicine","Type","Qty","Batch","Mfg","Expiry"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
                ws.Cell(row, i + 1).Style.Font.Bold = true;
            }

            row++;

            int sr = 1;
            int totalQty = 0;

            foreach (var x in report)
            {
                ws.Cell(row, 1).Value = sr++;
                ws.Cell(row, 2).Value = x.BillNo;
                ws.Cell(row, 3).Value = x.BillDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 4).Value = x.Patient;
                ws.Cell(row, 5).Value = x.Doctor;
                ws.Cell(row, 6).Value = x.ItemName;
                ws.Cell(row, 7).Value = x.Type;
                ws.Cell(row, 8).Value = x.Qty;
                ws.Cell(row, 9).Value = x.Batch;
                ws.Cell(row, 10).Value = x.Mfg;
                ws.Cell(row, 11).Value = x.Expiry?.ToString("dd-MM-yyyy");

                ws.Cell(row, 4).Style.Alignment.WrapText = true; // multiline fix

                totalQty += x.Qty;
                row++;
            }

            // TOTAL
            ws.Cell(row, 1).Value = "TOTAL QTY";
            ws.Cell(row, 1).Style.Font.Bold = true;

            ws.Cell(row, 8).Value = totalQty;
            ws.Cell(row, 8).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ScheduleHDrugsReport.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportScheduleHDrugsPdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            var sales = await _salesservice.GetAll();

            var report = sales
                .Where(s => s.BillDate >= fromDate && s.BillDate <= toDate)
                .SelectMany(s => s.SalesItems
                    .Where(x => x.ItemMaster.ScheduleH == true)
                    .Select(i => new
                    {
                        BillNo = s.BillNo,
                        BillDate = s.BillDate,
                        PatientName = s.Customers?.Name ?? "Walk-in",
                        Address = s.Customers?.Address ?? "",
                        Doctor = s.PharmacyDoctor?.Name ?? "",
                        ItemName = i.ItemMaster?.Name ?? "",
                        Type = i.ItemMaster?.Packing ?? "",
                        Qty = (int)i.Qty,
                        Batch = i.Batch ?? "",
                        Mfg = i.ItemMaster?.Company?.Name ?? "",
                        Expiry = i.Expirydate
                    }))
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fs = 9;

            // Company Name
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                companyName = tenant?.Name ?? "Company Name";
            }

            // HEADER
            doc.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph("Schedule H Drugs Report")
                .SetFont(bold).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // TABLE
            Table table = new Table(new float[] { 2, 4, 3, 6, 4, 6, 4, 2, 4, 4, 4 })
                .UseAllAvailableWidth();

            string[] headers = {
        "Sr","Bill No","Date","Patient","Doctor",
        "Medicine","Type","Qty","Batch","Mfg","Expiry"
    };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(fs))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            int sr = 1;
            int totalQty = 0;

            foreach (var x in report)
            {
                table.AddCell(new Paragraph(sr++.ToString()).SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.BillNo ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.BillDate?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fs));

                // ✅ MULTILINE PATIENT
                table.AddCell(new Paragraph($"{x.PatientName}\n{x.Address}")
                    .SetFont(normal).SetFontSize(fs));

                table.AddCell(new Paragraph(x.Doctor ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.ItemName ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Type ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Qty.ToString()).SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Batch ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Mfg ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(x.Expiry?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fs));

                totalQty += x.Qty;
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 7)
                .Add(new Paragraph("TOTAL QTY").SetFont(bold).SetFontSize(fs)));

            table.AddCell(new Paragraph(totalQty.ToString())
                .SetFont(bold).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(""));
            table.AddCell(new Paragraph(""));
            table.AddCell(new Paragraph(""));

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "ScheduleHDrugsReport.pdf");
        }
        // ================= PRINT FORM ACTION =================
        [HttpGet]
        public async Task<IActionResult> PrintHForm(string billNo)
        {
            var sales = await _salesservice.GetAll();
            var sale = sales.FirstOrDefault(s => s.BillNo == billNo);

            if (sale == null)
                return Content("Bill Not Found");

            // Tenant Details
            var tenantId = User?.FindFirst("TenantId")?.Value;
            string tenantName = "PHARMA ITEM MASTER";
            string tenantAddress = "";

            if (!string.IsNullOrEmpty(tenantId))
            {
                var tenants = await _tenantRepository.GetAll();
                var tenant = tenants.FirstOrDefault(x => x.Id == tenantId);
                if (tenant != null)
                {
                    tenantName = tenant.Name;
                    tenantAddress = tenant.Address1 ?? "";
                }
            }

            // H Items only
            var hItems = sale.SalesItems
                .Where(x => x.ItemMaster.ScheduleH == true)
                .ToList();

            // ViewModel
            var model = new SalesVM
            {
                BillNo = sale.BillNo,
                BillDate = sale.BillDate,
                CustomerName = sale.Customers?.Name ?? "Walk-in",
                Address = sale.Customers?.Address ?? "",
                DoctorName = sale.PharmacyDoctor?.Name ?? "",
                Tenantname = tenantName,
                TenantAddress = tenantAddress,
                SalesItemVMs = hItems.Select(i => new SalesItemVM
                {
                    ItemName = i.ItemMaster?.Name,
                    Packing = i.ItemMaster?.Packing,
                    Batch = i.Batch,
                    Expirydate = i.Expirydate,
                    Qty = (int)i.Qty,
                    Rate = i.Rate,
                    Mfg = i.ItemMaster?.Company?.Name
                }).ToList()
            };

            return View(model);
        }
        // ================= REPORT LIST ACTION =================
        [HttpGet]
        public async Task<IActionResult> ScheduleH1DrugsReport(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            ViewBag.FromDate = fromDate?.ToString("dd-MM-yyyy");
            ViewBag.ToDate = toDate?.ToString("dd-MM-yyyy");

            var sales = await _salesservice.GetAll();

            // Filter H1 items and Flatten to List
            var reportData = sales
                .Where(s => s.BillDate >= fromDate && s.BillDate <= toDate)
                .SelectMany(s => s.SalesItems.
                   Where(x => x.ItemMaster.ScheduleH1 == true)
                    .Select(i => new SalesVM
                    {
                        BillNo = s.BillNo,
                        BillDate = s.BillDate,
                        CustomerName = s.Customers?.Name ?? "Walk-in",
                        Address = s.Customers?.Address ?? "",
                        DoctorName = s.PharmacyDoctor?.Name ?? "",
                        ItemName = i.ItemMaster?.Name ?? "",
                        Type = i.ItemMaster?.Packing ?? "",
                        Qty = (int)i.Qty,
                        Batch = i.Batch,
                        ExpiryDate = i.Expirydate,
                        Mfg = i.ItemMaster?.Company?.Name ?? ""
                    }))
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            ViewBag.TotalQty = reportData.Sum(x => x.Qty);
            return View(reportData);
        }

        [HttpGet]
        public async Task<IActionResult> ExportScheduleH1Excel(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            var sales = await _salesservice.GetAll();

            var reportData = sales
                .Where(s => s.BillDate >= fromDate && s.BillDate <= toDate)
                .SelectMany(s => s.SalesItems
                    .Where(x => x.ItemMaster.ScheduleH1 == true)
                    .Select(i => new
                    {
                        s.BillNo,
                        s.BillDate,
                        Customer = (s.Customers?.Name ?? "Walk-in") + "\n" + (s.Customers?.Address ?? ""),
                        Doctor = s.PharmacyDoctor?.Name ?? "",
                        Item = i.ItemMaster?.Name ?? "",
                        Type = i.ItemMaster?.Packing ?? "",
                        Qty = (int)i.Qty,
                        Batch = i.Batch,
                        Mfg = i.ItemMaster?.Company?.Name ?? "",
                        Expiry = i.Expirydate
                    }))
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Schedule H1 Report");

            // 🔹 Company Name
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            // HEADER
            ws.Cell(1, 1).Value = companyName;
            ws.Range(1, 1, 1, 11).Merge().Style
                .Font.SetBold().Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = "Schedule H1 Drugs Report";
            ws.Range(2, 1, 2, 11).Merge().Style
                .Font.SetBold().Font.SetFontSize(13)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(3, 1).Value = $"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}";
            ws.Range(3, 1, 3, 11).Merge().Style
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // TABLE HEADER
            int row = 5;

            string[] headers = {
        "Sr","Bill No","Date","Patient","Doctor",
        "Medicine","Type","Qty","Batch","Mfg","Expiry"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
                ws.Cell(row, i + 1).Style.Font.Bold = true;
            }

            row++;

            int sr = 1;
            int totalQty = 0;

            foreach (var item in reportData)
            {
                ws.Cell(row, 1).Value = sr++;
                ws.Cell(row, 2).Value = item.BillNo;
                ws.Cell(row, 3).Value = item.BillDate?.ToString("dd-MM-yyyy");
                ws.Cell(row, 4).Value = item.Customer;
                ws.Cell(row, 5).Value = item.Doctor;
                ws.Cell(row, 6).Value = item.Item;
                ws.Cell(row, 7).Value = item.Type;
                ws.Cell(row, 8).Value = item.Qty;
                ws.Cell(row, 9).Value = item.Batch;
                ws.Cell(row, 10).Value = item.Mfg;
                ws.Cell(row, 11).Value = item.Expiry?.ToString("dd-MM-yyyy");

                ws.Cell(row, 4).Style.Alignment.WrapText = true;

                totalQty += item.Qty;
                row++;
            }

            // TOTAL ROW
            ws.Cell(row, 1).Value = "TOTAL QTY";
            ws.Cell(row, 1).Style.Font.Bold = true;

            ws.Cell(row, 8).Value = totalQty;
            ws.Cell(row, 8).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ScheduleH1Report.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportScheduleH1Pdf(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue)
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!toDate.HasValue)
                toDate = DateTime.Now;

            var sales = await _salesservice.GetAll();

            var reportData = sales
                .Where(s => s.BillDate >= fromDate && s.BillDate <= toDate)
                .SelectMany(s => s.SalesItems
                    .Where(x => x.ItemMaster.ScheduleH1 == true)
                    .Select(i => new
                    {
                        s.BillNo,
                        s.BillDate,
                        CustomerName = s.Customers?.Name ?? "Walk-in",
                        Address = s.Customers?.Address ?? "",
                        Doctor = s.PharmacyDoctor?.Name ?? "",
                        Item = i.ItemMaster?.Name ?? "",
                        Type = i.ItemMaster?.Packing ?? "",
                        Qty = (int)i.Qty,
                        Batch = i.Batch,
                        Mfg = i.ItemMaster?.Company?.Name ?? "",
                        Expiry = i.Expirydate
                    }))
                .OrderBy(x => x.BillDate)
                .ThenBy(x => x.BillNo)
                .ToList();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fs = 9;

            // Company Name
            var user = await _usersManager.GetUserAsync(User);
            string companyName = "Company Name";

            if (user != null)
            {
                var tenant = await _tenantRepository.GetById(user.TenantId);
                if (tenant != null)
                    companyName = tenant.Name;
            }

            // HEADER
            document.Add(new Paragraph(companyName)
                .SetFont(bold).SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph("Schedule H1 Drugs Report")
                .SetFont(bold).SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"From {fromDate:dd-MM-yyyy} To {toDate:dd-MM-yyyy}")
                .SetFont(normal).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            // TABLE
            float[] widths = { 2, 4, 4, 6, 5, 6, 4, 3, 4, 5, 4 };

            Table table = new Table(widths).UseAllAvailableWidth();

            string[] headers = {
        "Sr","Bill No","Date","Patient","Doctor",
        "Medicine","Type","Qty","Batch","Mfg","Expiry"
    };

            foreach (var h in headers)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(fs))
                    .SetTextAlignment(TextAlignment.CENTER));
            }

            int sr = 1;
            int totalQty = 0;

            foreach (var item in reportData)
            {
                table.AddCell(new Paragraph(sr++.ToString()).SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(item.BillNo ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(item.BillDate?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fs));

                // ✅ FIXED MULTILINE PATIENT
                table.AddCell(new Paragraph((item.CustomerName ?? "") + "\n" + (item.Address ?? ""))
                    .SetFont(normal).SetFontSize(fs));

                table.AddCell(new Paragraph(item.Doctor ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(item.Item ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(item.Type ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(item.Qty.ToString()).SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(item.Batch ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(item.Mfg ?? "").SetFont(normal).SetFontSize(fs));
                table.AddCell(new Paragraph(item.Expiry?.ToString("dd-MM-yyyy") ?? "").SetFont(normal).SetFontSize(fs));

                totalQty += item.Qty;
            }

            // TOTAL ROW
            table.AddCell(new Cell(1, 7)
                .Add(new Paragraph("TOTAL QTY").SetFont(bold).SetFontSize(fs))
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(totalQty.ToString())
                .SetFont(bold).SetFontSize(fs)
                .SetTextAlignment(TextAlignment.RIGHT));

            table.AddCell(new Paragraph(""));
            table.AddCell(new Paragraph(""));
            table.AddCell(new Paragraph(""));

            document.Add(table);
            document.Close();

            return File(stream.ToArray(), "application/pdf", "ScheduleH1Report.pdf");
        }


        [HttpGet]
        public async Task<IActionResult> PrintH1Form(string billNo)
        {
            var sales = await _salesservice.GetAll();
            var sale = sales.FirstOrDefault(s => s.BillNo == billNo);

            if (sale == null)
                return Content("Bill Not Found");

            // Tenant Details
            var tenantId = User?.FindFirst("TenantId")?.Value;
            string tenantName = "PHARMA ITEM MASTER";
            string tenantAddress = "";

            if (!string.IsNullOrEmpty(tenantId))
            {
                var tenants = await _tenantRepository.GetAll();
                var tenant = tenants.FirstOrDefault(x => x.Id == tenantId);
                if (tenant != null)
                {
                    tenantName = tenant.Name;
                    tenantAddress = tenant.Address1 ?? "";
                }
            }

            // H1 Items only
            var h1Items = sale.SalesItems
                .Where(x => x.ItemMaster.ScheduleH1 == true)
                .ToList();

            // ViewModel
            var model = new SalesVM
            {
                BillNo = sale.BillNo,
                BillDate = sale.BillDate,
                CustomerName = sale.Customers?.Name ?? "Walk-in",
                Address = sale.Customers?.Address ?? "",
                DoctorName = sale.PharmacyDoctor?.Name ?? "",
                Tenantname = tenantName,
                TenantAddress = tenantAddress,
                SalesItemVMs = h1Items.Select(i => new SalesItemVM
                {
                    ItemName = i.ItemMaster?.Name,
                    Packing = i.ItemMaster?.Packing,
                    Batch = i.Batch,
                    Expirydate = i.Expirydate,
                    Qty = (int)i.Qty,
                    Mfg = i.ItemMaster?.Company?.Name
                }).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> NarcoticForm7(string billNo)
        {
            var sales = await _salesservice.GetAll();
            var sale = sales.FirstOrDefault(s => s.BillNo == billNo);
            var tenantId = User?.FindFirst("TenantId")?.Value;
            var user = await _tenantRepository.GetById(tenantId);
            if (sale == null)
                return NotFound();

            var vm = new SalesVM
            {
                Id = sale.Id,
                BillNo = sale.BillNo,
                BillDate = sale.BillDate,
                CustomerName = sale.billingType == "Cash" ? "Cash" : sale.Customers?.Name ?? "",
                Address = sale.Customers?.Address ?? "",
                MobileNo = sale.MobileNo,
                TotalGstAmt = sale.TotalGstAmt + (sale.TotalCessAmount),
                TotalAmount = sale.TotalPayable,
                Tenantname = user.Name,

                TenantAddress = user?.Address1 ?? "",
                SalesItemVMs = sale.SalesItems.Select(i => new SalesItemVM
                {
                    ItemName = i.ItemMaster?.Name,
                    Packing = i.ItemMaster?.Packing,
                    Qty = i.Qty,
                    Batch = i.Batch,
                    Mfg = i.ItemMaster?.Company?.Name,
                    Expirydate = i.Expirydate,
                }).ToList()
            };

            return View(vm);
        }
        private async Task<string> GenerateInvoicePdf(int saleId)
        {
            var saleData = await _salesService.GetById(saleId);

            if (saleData == null)
                throw new Exception("Invoice data not found");

            if (saleData.CustomerId.HasValue)
            {
                var pointSettings = await _unitofwork.GetRepository<PointSetting>().Query()
                    .Where(x => x.TenantId == saleData.TenantId)
                    .ToListAsync();
                var earliestSetting = pointSettings.OrderBy(x => x.Created).FirstOrDefault();
                var earliestCreated = earliestSetting?.Created ?? DateTime.MaxValue;
                var saleCompareDate = saleData.Created ?? saleData.BillDate ?? DateTime.Now;

                if (saleCompareDate >= earliestCreated)
                {
                    var pointRepo = _unitofwork.GetRepository<PointTransaction>();
                    var currentPointsBalance = await pointRepo.Query()
                        .Where(x => x.CustomerId == saleData.CustomerId.Value)
                        .SumAsync(x => x.EarnedPoints - x.RedeemedPoints);
                    ViewBag.CustomerPointsBalance = currentPointsBalance;

                    var pointTxForSale = await pointRepo.Query()
                        .FirstOrDefaultAsync(x => x.SaleId == saleData.Id);
                    if (pointTxForSale != null)
                    {
                        ViewBag.PointsEarned = pointTxForSale.EarnedPoints;
                        ViewBag.PointsRedeemed = pointTxForSale.RedeemedPoints;
                    }
                    else
                    {
                        ViewBag.PointsEarned = 0;
                        ViewBag.PointsRedeemed = 0;
                    }
                }
                else
                {
                    ViewBag.CustomerPointsBalance = null;
                    ViewBag.PointsEarned = null;
                    ViewBag.PointsRedeemed = null;
                }
            }

            var userId =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?.Value;

            // ============================================
            // GET THEME
            // ============================================

            var themeSetting =
                await _themeRepo.GetDefaultThemeAsync(userId);

            if (themeSetting == null)
            {
                themeSetting =
                    await _themeRepo
                    .GetThemeSettingAsync(userId, "A4");
            }

            ViewBag.ThemeSettings = themeSetting;

            // ============================================
            // BUSINESS DETAILS
            // ============================================

            var tenantId = User.FindFirstValue("TenantId");

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                var tenant =
                    (await _tenantRepository.GetAll())
                    .FirstOrDefault(x => x.Id == tenantId);

                if (tenant != null)
                {
                    var addressParts = new List<string>();

                    if (!string.IsNullOrWhiteSpace(tenant.Address1))
                        addressParts.Add(tenant.Address1);

                    if (!string.IsNullOrWhiteSpace(tenant.Address2))
                        addressParts.Add(tenant.Address2);

                    if (!string.IsNullOrWhiteSpace(tenant.Location))
                        addressParts.Add(tenant.Location);

                    if (tenant.City != null &&
                        !string.IsNullOrWhiteSpace(tenant.City.Name))
                    {
                        addressParts.Add(tenant.City.Name);
                    }

                    if (tenant.State != null &&
                        !string.IsNullOrWhiteSpace(tenant.State.Name))
                    {
                        addressParts.Add(tenant.State.Name);
                    }

                    if (!string.IsNullOrWhiteSpace(tenant.PinCode))
                    {
                        addressParts.Add($"Pin: {tenant.PinCode}");
                    }

                    var businessPhone =
                        !string.IsNullOrWhiteSpace(tenant.MobileNo)
                        ? tenant.MobileNo
                        : tenant.Phone;

                    ViewBag.BusinessName =
                        tenant.Name ?? "";

                    ViewBag.BusinessAddress =
                        string.Join(", ", addressParts);

                    ViewBag.BusinessPhone =
                        businessPhone ?? "";

                    ViewBag.BusinessEmail =
                        tenant.Email ?? "";

                    ViewBag.BusinessGSTIN =
                        tenant.GstNo ?? "";
                }
            }

            // ============================================
            // SELECT TEMPLATE
            // ============================================

            string selectedPaperSize =
                themeSetting?.PaperSize ?? "A4";

            string viewPath = "";

            if (string.Equals(selectedPaperSize, "Thermal", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.PaperSize =
                    themeSetting?.ThermalPaperSize == 58
                    ? "58mm"
                    : "80mm";

                viewPath =
                    "~/Views/Shared/InvoiceTemplates/Thermal_Print.cshtml";
            }
            else if (string.Equals(selectedPaperSize, "A5", StringComparison.OrdinalIgnoreCase))
            {
                viewPath =
                    "~/Views/Shared/InvoiceTemplates/A5_Print.cshtml";
            }
            else
            {
                viewPath =
                    "~/Views/Shared/InvoiceTemplates/A4_Print.cshtml";
            }

            // ============================================
            // RENDER HTML
            // ============================================

            ViewBag.HidePrintControls = true;

            string html =
                await RenderViewToStringAsync(
                    viewPath,
                    saleData);

            // ============================================
            // SAVE PDF
            // ============================================

            string folderPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "Invoices");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            CleanupOldGeneratedInvoiceFiles(folderPath);

            string fileName =
                $"Invoice_{saleId}.pdf";

            string filePath =
                Path.Combine(folderPath, fileName);

            //using FileStream fs =
            //    new FileStream(filePath, FileMode.Create);

            //PdfWriter writer = new PdfWriter(fs);

            //PdfDocument pdf = new PdfDocument(writer);

            //HtmlConverter.ConvertToPdf(html, pdf);

            try
            {
                using (FileStream fs =
                    new FileStream(filePath, FileMode.Create))
                {
                    ConverterProperties converterProperties =
                        new ConverterProperties();

                    converterProperties.SetBaseUri(
                        $"{Request.Scheme}://{Request.Host}");

                    HtmlConverter.ConvertToPdf(
                        html,
                        fs,
                        converterProperties
                    );
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

            // pdf.Close();

            return fileName;
        }

        private void ScheduleGeneratedInvoiceDeletion(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "Invoices",
                fileName);

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromDays(1));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                catch
                {
                    // keep non-blocking cleanup silent
                }
            });
        }

        private void CleanupOldGeneratedInvoiceFiles(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    return;
                }

                var cutoffUtc = DateTime.UtcNow.AddDays(-1);
                foreach (var filePath in Directory.GetFiles(folderPath, "Invoice_*.pdf"))
                {
                    if (System.IO.File.GetLastWriteTimeUtc(filePath) <= cutoffUtc)
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch
            {
                // do not interrupt invoice generation
            }
        }

        private async Task<string> RenderViewToStringAsync<TModel>(
     string viewName,
     TModel model)
        {
            ViewData.Model = model;

            using var sw = new StringWriter();

            var viewResult = _viewEngine.GetView(
                executingFilePath: null,
                viewPath: viewName,
                isMainPage: true);

            if (!viewResult.Success)
            {
                throw new Exception(
                    $"View '{viewName}' not found.");
            }

            var viewContext = new ViewContext(
                ControllerContext,
                viewResult.View,
                ViewData,
                TempData,
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);

            return sw.ToString();
        }
    }
}
