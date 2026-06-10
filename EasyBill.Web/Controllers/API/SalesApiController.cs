using AOne.DataAccess.ProfileService;
using AOne.Models.Entity;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;
using EasyBill.UI.Service.Whatsapp;
using iText.Html2pdf;

namespace EasyBill.UI.Controllers.API
{ 
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class SalesApiController : Controller
    {
        private readonly ISalesRepository _salesservice;
        private readonly IProfileService _profileService;
        private readonly ICustomerRepository _customerservice;
        private readonly IItemMasterRepository _itemmasterservice;
        private readonly IPurchaseItemRepository _purchaseitemservice;
        private readonly ISalseSettingRepository _salsesettingservice;
        private readonly IPurchaseRepository _purchaseservice;
        private readonly IPurchaseReturnRepository _purchasereturnservice;
        private readonly IStockIssueRepository _stockissueservice;
        private readonly IStockReturnRepository _stockrturnservice;
        private readonly IStockReceiveRepository _stockreceiveservice;
        private readonly ICompanyRepository _companyService;
        private readonly IModeOfPaymentRepository _modeofpaymentservice;
        private readonly IOfferRepository _offerrepo;
        private readonly ISalesOrderRepository _salesOrderRepo;
        private readonly IStockService _currenstockService;
        private readonly ICustomerAdvanceRepository _customerAdvanceRepo;
        private readonly ITenantRegistrationRepository _tenantRepository;
        private readonly IInvoiceThemeSettingRepository _themeRepo;
        private readonly IUnitOfWork _unitofwork;
        private readonly WhatsAppService _whatsappservice;
        private readonly IRazorViewEngine _viewEngine;
        public SalesApiController(
            ISalesRepository salesservice,
            IProfileService profileService,
            ICustomerRepository customerservice,
            IItemMasterRepository itemmasterservice,
            IPurchaseItemRepository purchaseitemservice,
            ISalseSettingRepository salsesettingservice,
            IPurchaseRepository purchaseservice,
            IPurchaseReturnRepository purchasereturnservice,
            IStockIssueRepository stockissueservice,
            IStockReturnRepository stockrturnservice,
            IStockReceiveRepository stockreceiveservice,
            ICompanyRepository companyService,
            IModeOfPaymentRepository modeofpaymentservice,
            IOfferRepository offerrepo,
            ISalesOrderRepository salesOrderRepo,
            IStockService currenstockService,
            ICustomerAdvanceRepository customerAdvanceRepo,
            ITenantRegistrationRepository tenantRepository,
            IInvoiceThemeSettingRepository themeRepo,
            IUnitOfWork unitofwork,
            WhatsAppService whatsappservice,
            IRazorViewEngine viewEngine)
        {
            _salesservice = salesservice;
            _profileService = profileService;
            _customerservice = customerservice;
            _itemmasterservice = itemmasterservice;
            _purchaseitemservice = purchaseitemservice;
            _salsesettingservice = salsesettingservice;
            _purchaseservice = purchaseservice;
            _purchasereturnservice = purchasereturnservice;
            _stockissueservice = stockissueservice;
            _stockrturnservice = stockrturnservice;
            _stockreceiveservice = stockreceiveservice;
            _companyService = companyService;
            _modeofpaymentservice = modeofpaymentservice;
            _offerrepo = offerrepo;
            _salesOrderRepo = salesOrderRepo;
            _currenstockService=currenstockService;
            _customerAdvanceRepo = customerAdvanceRepo;
            _tenantRepository = tenantRepository;
            _themeRepo = themeRepo;
            _unitofwork = unitofwork;
            _whatsappservice = whatsappservice;
            _viewEngine = viewEngine;
        }

        // GET: api/Sales
        [HttpGet]
        public async Task<IActionResult> GetAll()
        { 
            await _profileService.Set(User);
             
            var data = await _salesservice.GetAll();
             
            var salesData = data.Select(x => new
            {
                Id = x.Id,
                BillNo = x.BillNo,
                BillDate = x.BillDate,
                CustomerId = x.CustomerId,
                CustomerName = x.Customers?.Name,
                MobileNo = x.MobileNo ?? string.Empty,
                Address = x.Address ?? string.Empty,
                Total = x.Total,
                TotalGstAmt = x.TotalGstAmt,
                TotalPayable = x.TotalPayable,
                TotalCessAmount = x.TotalCessAmount,
                PharmacyDoctorId = x.PharmacyDoctorId,
                DoctorMobileNumber = x.DoctorMobileNumber ?? string.Empty,
                DoctorRegNumber = x.DoctorRegNumber ?? string.Empty,
                TotalDiscount = x.Totaldiscount,
                DiscountPercent = x.discountPercent,
                DiscountAmount = x.discountAmount,
                PaidAmount = x.PaidAmount,
                ReturnAmount = x.ReturnAmount,
                OfferId = x.OfferId,
                Balance = x.Balance,
                PaymentStatus = x.PaidAmount == 0 ? "Unpaid" :
                x.PaidAmount < x.TotalPayable ? "Partial" : "Paid",
                SalesItems = x.SalesItems?.Select(MapSalesItemResponse).ToList(),
                 
                SalesPaymentDetails = x.SalsePaymentDetails?.Select(pay => new
                {
                    Id = pay.Id,
                    PaymentModeId = pay.PaymentModeId,
                    Amount = pay.Amount,
                    ReferenceNo = pay.ReferenceNo,
                    Description = pay.Description,
                    CustomerId = pay.CustomerId,
                    SalesId = pay.SalseId
                }).ToList()
            }).ToList();

            return Ok(new
            {
                success = true,
                data = salesData
            });
        }
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            await _profileService.Set(User);

            var data = await _salesservice.GetByCustomerId(customerId);

            var salesData = data
                .Select(x => new
                {
                    Id = x.Id,
                    BillNo = x.BillNo,
                    BillDate = x.BillDate,
                    CustomerId = x.CustomerId,
                    CustomerName = x.Customers?.Name,
                    MobileNo = x.MobileNo ?? string.Empty,
                    Address = x.Address ?? string.Empty,
                    Total = x.Total,
                    TotalGstAmt = x.TotalGstAmt,
                    TotalPayable = x.TotalPayable,
                    TotalCessAmount = x.TotalCessAmount,
                    PharmacyDoctorId = x.PharmacyDoctorId,
                    DoctorMobileNumber = x.DoctorMobileNumber ?? string.Empty,
                    DoctorRegNumber = x.DoctorRegNumber ?? string.Empty,
                    TotalDiscount = x.Totaldiscount,
                    DiscountPercent = x.discountPercent,
                    DiscountAmount = x.discountAmount,
                    PaidAmount = x.PaidAmount,
                    ReturnAmount = x.ReturnAmount,
                    OfferId = x.OfferId,
                    Balance = x.Balance, 
                    SalesItems = x.SalesItems?.Select(MapSalesItemResponse).ToList(), 
                    SalesPaymentDetails = x.SalsePaymentDetails?.Select(pay => new
                    {
                        Id = pay.Id,
                        PaymentModeId = pay.PaymentModeId,
                        Amount = pay.Amount,
                        ReferenceNo = pay.ReferenceNo,
                        Description = pay.Description,
                        CustomerId = pay.CustomerId,
                        SalesId = pay.SalseId
                    }).ToList()
                })
                .ToList();

            return Ok(new
            {
                success = true,
                customerId = customerId,
                totalRecords = salesData.Count,
                data = salesData
            });
        }

        // This keeps the legacy GST-only flow intact while allowing new clients to
        // send explicit HSN and tax-breakup values. Missing values fall back to the
        // stored sales item snapshot and then to the current ItemMaster HSN master.
        private static (int? HsnId, string? HsnCode, decimal? IGst, decimal? CGst, decimal? SGst, decimal Cess, decimal Gst) ResolveSalesItemTax(
            SalesItemVM itemVm,
            ItemMaster? itemMaster,
            SalesItem? existingItem = null)
        {
            var hsnId = itemVm.HsnId ?? existingItem?.HsnId ?? itemMaster?.HsnId;
            var hsnCode = itemVm.HsnCode ?? existingItem?.HsnCode ?? itemMaster?.Hsn?.HsnCode;
            var iGst = itemVm.IGst ?? existingItem?.IGst ?? itemMaster?.Hsn?.IGST;
            var cGst = itemVm.CGst ?? existingItem?.CGst ?? itemMaster?.Hsn?.CGST;
            var sGst = itemVm.SGst ?? existingItem?.SGst ?? itemMaster?.Hsn?.SGST;
            var cess = itemVm.Cess ?? existingItem?.Cess ?? itemMaster?.Hsn?.Cess ?? 0m;

            decimal legacyGst;
            if (itemVm.Gst > 0)
            {
                legacyGst = itemVm.Gst;
            }
            else if (existingItem is not null && existingItem.Gst > 0)
            {
                legacyGst = existingItem.Gst;
            }
            else
            {
                legacyGst = iGst ?? ((cGst ?? 0m) + (sGst ?? 0m));
            }

            return (hsnId, hsnCode, iGst, cGst, sGst, cess, legacyGst);
        }

        // Additive response mapping: we expose the new nullable tax fields without
        // removing the old properties that existing API consumers already expect.
        private static object MapSalesItemResponse(SalesItem item)
        {
            var hsn = item.Hsn ?? item.ItemMaster?.Hsn;

            return new
            {
                Id = item.Id,
                SalesId = item.SalesId,
                ItemMasterId = item.ItemMasterId,
                ItemName = item.ItemMaster?.Name,
                HsnId = item.HsnId ?? item.ItemMaster?.HsnId,
                HsnCode = item.HsnCode ?? hsn?.HsnCode,
                IGst = item.IGst ?? hsn?.IGST,
                CGst = item.CGst ?? hsn?.CGST,
                SGst = item.SGst ?? hsn?.SGST,
                PurchaseItemId = item.PurchaseItemId,
                Batch = item.Batch,
                Qty = item.Qty,
                Rate = item.Rate,
                StripRate = item.StripRate,
                Gst = item.Gst,
                Cess = item.Cess,
                Discount = item.Discount,
                Amount = item.Amount,
                ExpiryDate = item.Expirydate,
                Mrp = item.Mrp
            };
        }

        // Edit payload uses the same additive mapping so old update screens can keep
        // working and newer clients can round-trip the tax breakup safely.
        private static SalesItemVM MapSalesItemVm(SalesItem item)
        {
            var hsn = item.Hsn ?? item.ItemMaster?.Hsn;

            return new SalesItemVM
            {
                Id = item.Id,
                SalesId = item.SalesId,
                ItemMasterId = item.ItemMasterId,
                ItemName = item.ItemMaster?.Name,
                ItemBarcodeNumber = item.ItemMaster?.Name,
                HsnId = item.HsnId ?? item.ItemMaster?.HsnId,
                HsnCode = item.HsnCode ?? hsn?.HsnCode,
                IGst = item.IGst ?? hsn?.IGST,
                CGst = item.CGst ?? hsn?.CGST,
                SGst = item.SGst ?? hsn?.SGST,
                PurchaseItemId = item.PurchaseItemId,
                Batch = item.Batch,
                Expirydate = item.Expirydate,
                Mrp = item.Mrp,
                Qty = item.Qty,
                Rate = item.Rate,
                Gst = item.Gst,
                Cess = item.Cess,
                Discount = item.Discount,
                Amount = item.Amount,
                IsSoldInTablets = item.IsSoldInTablets,
                StripRate = item.StripRate
            };
        }

        // MERGED FROM TL: PDF generation and WhatsApp helper methods
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

        private async Task<string?> ShareInvoiceViaWhatsAppAndSmsAsync(
            int saleId,
            string billNo,
            string? saleMobileNo,
            int? customerId)
        {
            var tenantId = User?.FindFirst("TenantId")?.Value;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                await _profileService.Set(User);
                tenantId = _profileService?.Profile?.TenantId;
            }

            var tenant = !string.IsNullOrWhiteSpace(tenantId)
                ? await _tenantRepository.GetById(tenantId)
                : null;

            var destinationMobileNo = await ResolveWhatsAppMobileNoAsync(saleMobileNo, customerId);
            if (string.IsNullOrWhiteSpace(destinationMobileNo))
            {
                return "WhatsApp/SMS not sent. Customer mobile number not available.";
            }

            if (tenant != null && !tenant.IsWalletActive)
            {
                return "WhatsApp/SMS not sent because wallet is inactive.";
            }

            var messages = new List<string>();
            string pdfFileName = await GenerateInvoicePdf(saleId);
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string pdfUrl = $"{baseUrl}/Invoices/{pdfFileName}";
            string customerName = await ResolveCustomerNameAsync(customerId);

            var whatsappCharge = tenant == null
                ? 0m
                : decimal.Round(Math.Max(tenant.WhatsAppMessageCharge, 0m), 2, MidpointRounding.AwayFromZero);

            if (tenant != null && !tenant.IsWhatsAppChargeActive)
            {
                messages.Add("WhatsApp sending is disabled.");
            }
            else if (tenant != null && whatsappCharge > 0m && tenant.WalletBalance < whatsappCharge)
            {
                messages.Add("WhatsApp not sent due to low wallet balance.");
            }
            else
            {
                string whatsappMessage = $"Dear {customerName},Your invoice {billNo} is attached.Thank you!";
                var whatsAppResult = await _whatsappservice.SendWhatsAppMessageWithStatusAsync(
                    destinationMobileNo,
                    whatsappMessage,
                    pdfUrl);

                if (!whatsAppResult.IsSuccess)
                {
                    messages.Add(whatsAppResult.Message);
                }
                else
                {
                    messages.Add("WhatsApp sent successfully.");

                    if (tenant != null && whatsappCharge > 0m)
                    {
                        var chargeError = await DeductShareChargeAsync(
                            tenant,
                            whatsappCharge,
                            billNo,
                            saleId,
                            serviceType: "WhatsApp",
                            remarks: "WhatsApp share charge deducted.",
                            successPrefix: "WhatsApp");
                        if (!string.IsNullOrWhiteSpace(chargeError))
                        {
                            messages.Add(chargeError);
                        }
                    }
                }
            }

            ScheduleGeneratedInvoiceDeletion(pdfFileName);
            return messages.Count > 0 ? string.Join(" ", messages) : null;
        }

        private async Task<string> ResolveCustomerNameAsync(int? customerId)
        {
            if (!customerId.HasValue || customerId.Value <= 0)
            {
                return "Customer";
            }

            var customer = await _customerservice.GetById(customerId.Value);
            if (customer == null || string.IsNullOrWhiteSpace(customer.Name))
            {
                return "Customer";
            }

            return customer.Name.Trim();
        }

        private async Task<string?> DeductShareChargeAsync(
            Tenant tenant,
            decimal charge,
            string billNo,
            int saleId,
            string serviceType,
            string remarks,
            string successPrefix)
        {
            try
            {
                tenant.WalletBalance = decimal.Round(
                    Math.Max(tenant.WalletBalance - charge, 0m),
                    2,
                    MidpointRounding.AwayFromZero);

                await _tenantRepository.Update(tenant);
                await LogTenantWalletHistoryAsync(
                    tenant.Id,
                    charge,
                    paymentMode: "Auto",
                    referenceNo: billNo,
                    serviceType: serviceType,
                    remarks: remarks,
                    closingBalance: tenant.WalletBalance,
                    referenceSaleId: saleId);
                return null;
            }
            catch (Exception ex)
            {
                return $"{successPrefix} sent successfully, but wallet update/history failed. Error: {ex.Message}";
            }
        }

        private async Task LogTenantWalletHistoryAsync(
            string tenantId,
            decimal amount,
            string paymentMode,
            string? referenceNo,
            string serviceType,
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
                ServiceType = string.IsNullOrWhiteSpace(serviceType) ? "WhatsApp" : serviceType.Trim(),
                ServiceCharge = decimal.Round(Math.Max(amount, 0m), 2, MidpointRounding.AwayFromZero),
                Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
                ClosingBalance = decimal.Round(Math.Max(closingBalance, 0m), 2, MidpointRounding.AwayFromZero)
            });

            using var transaction = historyRepo.BeginTransaction();
            await historyRepo.SaveChangesAsync();
            transaction.Commit();
        }

        private async Task<string> GenerateInvoicePdf(int saleId)
        {
            var saleData = await _salesservice.GetById(saleId);

            if (saleData == null)
            {
                throw new Exception("Invoice data not found");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var themeSetting = await _themeRepo.GetDefaultThemeAsync(userId);

            if (themeSetting == null)
            {
                themeSetting = await _themeRepo.GetThemeSettingAsync(userId, "A4");
            }

            ViewBag.ThemeSettings = themeSetting;

            var tenantId = User.FindFirstValue("TenantId");
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                await _profileService.Set(User);
                tenantId = _profileService?.Profile?.TenantId;
            }

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                var tenant = (await _tenantRepository.GetAll()).FirstOrDefault(x => x.Id == tenantId);

                if (tenant != null)
                {
                    var addressParts = new List<string>();

                    if (!string.IsNullOrWhiteSpace(tenant.Address1))
                        addressParts.Add(tenant.Address1);

                    if (!string.IsNullOrWhiteSpace(tenant.Address2))
                        addressParts.Add(tenant.Address2);

                    if (!string.IsNullOrWhiteSpace(tenant.Location))
                        addressParts.Add(tenant.Location);

                    if (tenant.City != null && !string.IsNullOrWhiteSpace(tenant.City.Name))
                        addressParts.Add(tenant.City.Name);

                    if (tenant.State != null && !string.IsNullOrWhiteSpace(tenant.State.Name))
                        addressParts.Add(tenant.State.Name);

                    if (!string.IsNullOrWhiteSpace(tenant.PinCode))
                        addressParts.Add($"Pin: {tenant.PinCode}");

                    var businessPhone = !string.IsNullOrWhiteSpace(tenant.MobileNo)
                        ? tenant.MobileNo
                        : tenant.Phone;

                    ViewBag.BusinessName = tenant.Name ?? "";
                    ViewBag.BusinessAddress = string.Join(", ", addressParts);
                    ViewBag.BusinessPhone = businessPhone ?? "";
                    ViewBag.BusinessEmail = tenant.Email ?? "";
                    ViewBag.BusinessGSTIN = tenant.GstNo ?? "";
                }
            }

            string selectedPaperSize = themeSetting?.PaperSize ?? "A4";
            string viewPath;

            if (string.Equals(selectedPaperSize, "Thermal", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.PaperSize = themeSetting?.ThermalPaperSize == 58 ? "58mm" : "80mm";
                viewPath = "~/Views/Shared/InvoiceTemplates/Thermal_Print.cshtml";
            }
            else if (string.Equals(selectedPaperSize, "A5", StringComparison.OrdinalIgnoreCase))
            {
                viewPath = "~/Views/Shared/InvoiceTemplates/A5_Print.cshtml";
            }
            else
            {
                viewPath = "~/Views/Shared/InvoiceTemplates/A4_Print.cshtml";
            }

            ViewBag.HidePrintControls = true;

            string html = await RenderViewToStringAsync(viewPath, saleData);

            string folderPath = System.IO.Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "Invoices");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            CleanupOldGeneratedInvoiceFiles(folderPath);

            string fileName = $"Invoice_{saleId}.pdf";
            string filePath = System.IO.Path.Combine(folderPath, fileName);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                ConverterProperties converterProperties = new ConverterProperties();
                converterProperties.SetBaseUri($"{Request.Scheme}://{Request.Host}");
                HtmlConverter.ConvertToPdf(html, fileStream, converterProperties);
            }

            return fileName;
        }

        private void ScheduleGeneratedInvoiceDeletion(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var fullPath = System.IO.Path.Combine(
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

            using var stringWriter = new StringWriter();

            var viewResult = _viewEngine.GetView(
                executingFilePath: null,
                viewPath: viewName,
                isMainPage: true);

            if (!viewResult.Success)
            {
                throw new Exception($"View '{viewName}' not found.");
            }

            var viewContext = new ViewContext(
                ControllerContext,
                viewResult.View,
                ViewData,
                TempData,
                stringWriter,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);

            return stringWriter.ToString();
        }

        [HttpGet("GenerateNextBillNo")]
        public async Task<IActionResult> GenerateNextBillNo()
        {
            try
            { 
                var data = await _salesservice.GetAll();
                 
                var lastCode = data.Where(p => !string.IsNullOrEmpty(p.BillNo)).Select(p => p.BillNo).OrderBy(b => b).LastOrDefault();

                var nextCode = GenerateNextProductCode(lastCode);

                return Ok(new
                {
                    success = true,
                    nextBillNo = nextCode
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Error generating next bill number",
                    error = ex.Message
                });
            }
        }

        private string GenerateNextProductCode(string lastCode)
        {
            if (string.IsNullOrEmpty(lastCode) || lastCode.Length < 2)
                return "SA0001";

            string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray());
            string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

            int number = 0;
            int.TryParse(numberPart, out number);

            return prefix + (number + 1).ToString("D" + numberPart.Length);
        }

        // POST: api/Sales/Create
        //[HttpPost("Create")]
        //public async Task<IActionResult> Create([FromBody] SalesVM Vm)
        //{
        //    if (Vm == null)
        //        return BadRequest(new { success = false, message = "Invalid data." });

        //    var model = new Sales
        //    {
        //        CustomerId = Vm.CustomerId,
        //        BillDate = Vm.BillDate,
        //        BillNo = Vm.BillNo,
        //        MobileNo = Vm.MobileNo,
        //        Address = Vm.Address,
        //        PharmacyDoctorId = Vm.PharmacyDoctorId,
        //        DoctorMobileNumber = Vm.DoctorMobileNumber,
        //        DoctorRegNumber = Vm.DoctorRegNumber,
        //        Total = Vm.Total,
        //        TotalGstAmt = Vm.TotalGstAmt,
        //        discountPercent = Vm.discountPercent,
        //        discountAmount = Vm.discountAmount,
        //        Totaldiscount = Vm.Totaldiscount,
        //        TotalPayable = Vm.TotalPayable,
        //        PaidAmount = Vm.PaidAmount,
        //        ReturnAmount = Vm.ReturnAmount,
        //        OfferId = Vm.OfferId,
        //        //Balance = Vm.Balance,
        //        PaymentStatus = "Unpaid",
        //        SalesItems = Vm.SalesItemVMs?.Select(x => new SalesItem()
        //        {
        //            ItemMasterId = x.ItemMasterId,
        //            PurchaseItemId = x.PurchaseItemId,
        //            Batch = x.Batch,
        //            Expirydate = x.Expirydate,
        //            Mrp = x.Mrp,
        //            Qty = x.Qty,
        //            Rate = x.Rate,
        //            Gst = x.Gst,
        //            Discount = x.Discount,
        //            Amount = x.Amount,
        //        }).ToList() ?? new List<SalesItem>(),
        //        SalsePaymentDetails = Vm.SalsePaymentDetails?.Select(pd => new SalsePaymentDetails
        //        {
        //            PaymentModeId = pd.PaymentModeId,
        //            Amount = pd.Amount,
        //            ReferenceNo = pd.ReferenceNo,
        //            Description = pd.Description,
        //            CustomerId = Vm.CustomerId,
        //        }).ToList() ?? new List<SalsePaymentDetails>()
        //    };
        //    var createdSale = await _salesservice.Create(model);
        //    return Ok(new { success = true, saleId = createdSale.Id, message = "Sale created successfully." });
        //}

        //Sakshi Create

        //[HttpPost("Create")]
        //public async Task<IActionResult> Create([FromBody] SalesVM Vm)
        //{
        //    if (Vm == null)
        //        return BadRequest(new { success = false, message = "Invalid data." });

        //    var model = new Sales
        //    {
        //        CustomerId = Vm.CustomerId,
        //        BillDate = Vm.BillDate,
        //        BillNo = Vm.BillNo,
        //        MobileNo = Vm.MobileNo,
        //        Address = Vm.Address,
        //        PharmacyDoctorId = Vm.PharmacyDoctorId,
        //        DoctorMobileNumber = Vm.DoctorMobileNumber,
        //        DoctorRegNumber = Vm.DoctorRegNumber,
        //        Total = Vm.Total,
        //        TotalGstAmt = Vm.TotalGstAmt,
        //        TotalPayable = Vm.TotalPayable,
        //        discountPercent = Vm.discountPercent,
        //        discountAmount = Vm.discountAmount,
        //        Totaldiscount = Vm.Totaldiscount,
        //        PaidAmount = Vm.PaidAmount,
        //        Balance = Vm.TotalPayable - Vm.PaidAmount,
        //        PaymentStatus = Vm.PaidAmount == 0 ? "Unpaid" :
        //                        Vm.PaidAmount < Vm.TotalPayable ? "Partial" : "Paid",
        //        SalesItems = Vm.SalesItemVMs?.Select(x => new SalesItem
        //        {
        //            ItemMasterId = x.ItemMasterId,
        //            PurchaseItemId = x.PurchaseItemId,
        //            Batch = x.Batch,
        //            Expirydate = x.Expirydate,
        //            Mrp = x.Mrp,
        //            Qty = x.Qty,
        //            Rate = x.Rate,
        //            Gst = x.Gst,
        //            Discount = x.Discount,
        //            Amount = x.Amount,
        //        }).ToList() ?? new List<SalesItem>(),
        //        SalsePaymentDetails = Vm.SalsePaymentDetails?.Select(pd => new SalsePaymentDetails
        //        {
        //            PaymentModeId = pd.PaymentModeId,
        //            Amount = pd.Amount,
        //            ReferenceNo = pd.ReferenceNo,
        //            Description = pd.Description,
        //            CustomerId = Vm.CustomerId,
        //        }).ToList() ?? new List<SalsePaymentDetails>()
        //    };

        //    var createdSale = await _salesservice.Create(model);
        //    return Ok(new { success = true, saleId = createdSale.Id, message = "Sale created successfully." });
        //}

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] SalesVM Vm, bool isShare = false)
        {
            if (Vm == null)
                return BadRequest(new { success = false, message = "Invalid data." });

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
                    errors
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

            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => x.IsActive)
                .ToList();
            var isRegisteredBilling = string.Equals(Vm.billingType, "registered", StringComparison.OrdinalIgnoreCase)
                || (string.IsNullOrWhiteSpace(Vm.billingType) && Vm.CustomerId.HasValue);

            // ? REGISTERED CUSTOMER VALIDATION
            if (isRegisteredBilling)
            {
                if (string.IsNullOrWhiteSpace(Vm.MobileNo))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Mobile number is required for registered customer."
                    });
                }

                var customerList = await _customerservice.GetAll();

                var isExist = customerList.Any(x =>
                    !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                    x.PhoneNo.Trim() == Vm.MobileNo.Trim()
                );

                if (!isExist)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Customer does not exist. Please create customer first."
                    });
                }
            }

            Vm.SalesItemVMs ??= new List<SalesItemVM>();
            Vm.SalsePaymentDetails ??= new List<SalsePaymentDetailsVM>();

            var model = new Sales
            {
                // ? BILLING TYPE FIX
                CustomerId = isRegisteredBilling ? Vm.CustomerId : null,
                MobileNo = isRegisteredBilling ? Vm.MobileNo : null,
                Address = isRegisteredBilling ? Vm.Address : null,
                billingType = isRegisteredBilling ? "registered" : "Cash",
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
                TotalPayable = Vm.TotalPayable,
                TotalCessAmount = Vm.TotalCessAmount,
                RoundOffAmount = Vm.RoundOffAmount,

                discountPercent = Vm.discountPercent,
                discountAmount = Vm.discountAmount,
                Totaldiscount = Vm.Totaldiscount,

                PaidAmount = Vm.PaidAmount,
                ReturnAmount = Math.Max(0, Vm.PaidAmount - Vm.TotalPayable),
                NetCollection = Vm.PaidAmount - Math.Max(0, Vm.PaidAmount - Vm.TotalPayable),
                SalesOrderId = Vm.SalesOrderId,
                OfferId = Vm.OfferId,
                Balance = Vm.Balance,

                PaymentStatus = Vm.PaidAmount == 0 ? "Unpaid" :
                                Vm.PaidAmount < Vm.TotalPayable ? "Partial" : "Paid",

                // ? ITEMS WITH CONVERSION
                SalesItems = Vm.SalesItemVMs?.Select(x =>
                {
                    var item = itemMasters.FirstOrDefault(i => i.Id == x.ItemMasterId);
                    var resolvedTax = ResolveSalesItemTax(x, item);

                    decimal finalQty = x.Qty;

                    if (isTabletWise && item != null && item.Conversion > 0)
                    {
                        finalQty = x.Qty + ((decimal)x.TabletQty / item.Conversion);
                    }

                    return new SalesItem
                    {
                        ItemMasterId = x.ItemMasterId,
                        HsnId = resolvedTax.HsnId,
                        HsnCode = resolvedTax.HsnCode,
                        PurchaseItemId = x.PurchaseItemId,
                        Batch = x.Batch,
                        Expirydate = x.Expirydate,
                        Mrp = x.Mrp,
                        Qty = finalQty,
                        Rate = x.Rate,
                        StripRate = x.StripRate,
                        Gst = resolvedTax.Gst,
                        IGst = resolvedTax.IGst,
                        CGst = resolvedTax.CGst,
                        SGst = resolvedTax.SGst,
                        Cess = resolvedTax.Cess,
                        Discount = x.Discount,
                        Amount = x.Amount,
                        IsSoldInTablets = isTabletWise
                    };
                }).ToList() ?? new List<SalesItem>(),

                // ? PAYMENT FIX
                SalsePaymentDetails = Vm.SalsePaymentDetails?.Select(pd => new SalsePaymentDetails
                {
                    PaymentModeId = pd.PaymentModeId,
                    Amount = pd.Amount,
                    ReferenceNo = pd.ReferenceNo,
                    Description = pd.Description,
                    CustomerId = isRegisteredBilling ? Vm.CustomerId : null,
                }).ToList() ?? new List<SalsePaymentDetails>()
            };

            try
            {
                string? walletMessage = null;

                // ? STOCK UPDATE
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

                // UPDATE SALES ORDER STATUS TO BILLED
                if (Vm.SalesOrderId.HasValue && Vm.SalesOrderId.Value > 0)
                {
                    var salesOrder = await _salesOrderRepo.GetById(Vm.SalesOrderId.Value);
                    if (salesOrder != null)
                    {
                        salesOrder.OrderStatus = AOne.Utility.Enums.OrderStatus.Billed;
                        await _salesOrderRepo.Update(salesOrder);
                    }
                }

                if (isShare)
                {
                    walletMessage = await ShareInvoiceViaWhatsAppAndSmsAsync(
                        createdSale.Id,
                        createdSale.BillNo ?? model.BillNo ?? string.Empty,
                        model.MobileNo,
                        model.CustomerId);
                }

                return Ok(new
                {
                    success = true,
                    saleId = createdSale.Id,
                    message = "Sale created successfully.",
                    walletMessage
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

        //[HttpPost("Create")]
        //public async Task<IActionResult> Create([FromBody] SalesVM Vm)
        //{
        //    if (Vm == null)
        //        return BadRequest(new { success = false, message = "Invalid data." });

        //    var model = new Sales
        //    {
        //        CustomerId = Vm.billingType == "registered"
        //            ? Vm.CustomerId
        //            : null,

        //        MobileNo = Vm.billingType == "registered"
        //            ? Vm.MobileNo
        //            : null,

        //        Address = Vm.billingType == "registered"
        //            ? Vm.Address
        //            : null,

        //        billingType = Vm.billingType == "registered"
        //            ? "registered"
        //            : "Cash",

        //        PaymentType = Vm.PaymentType,

        //        BillDate = Vm.BillDate,
        //        BillNo = Vm.BillNo,

        //        PharmacyDoctorId = Vm.PharmacyDoctorId,
        //        DoctorMobileNumber = Vm.DoctorMobileNumber,
        //        DoctorRegNumber = Vm.DoctorRegNumber,

        //        Total = Vm.Total,
        //        TotalGstAmt = Vm.TotalGstAmt,
        //        TotalPayable = Vm.TotalPayable,

        //        discountPercent = Vm.discountPercent,
        //        discountAmount = Vm.discountAmount,
        //        Totaldiscount = Vm.Totaldiscount,

        //        PaidAmount = Vm.PaidAmount,

        //        ReturnAmount = Math.Max(0, Vm.PaidAmount - Vm.TotalPayable),

        //        Balance = Vm.TotalPayable - Vm.PaidAmount,

        //        PaymentStatus = Vm.PaidAmount == 0 ? "Unpaid" :
        //                        Vm.PaidAmount < Vm.TotalPayable ? "Partial" : "Paid",

        //        SalesItems = Vm.SalesItemVMs?.Select(x => new SalesItem
        //        {
        //            ItemMasterId = x.ItemMasterId,
        //            PurchaseItemId = x.PurchaseItemId,
        //            Batch = x.Batch,
        //            Expirydate = x.Expirydate,
        //            Mrp = x.Mrp,
        //            Qty = x.Qty,
        //            Rate = x.Rate,
        //            Gst = x.Gst,
        //            Discount = x.Discount,
        //            Amount = x.Amount,
        //        }).ToList() ?? new List<SalesItem>(),

        //        SalsePaymentDetails = Vm.SalsePaymentDetails?.Select(pd => new SalsePaymentDetails
        //        {
        //            PaymentModeId = pd.PaymentModeId,
        //            Amount = pd.Amount,
        //            ReferenceNo = pd.ReferenceNo,
        //            Description = pd.Description,

        //            CustomerId = Vm.billingType == "registered"
        //                ? Vm.CustomerId
        //                : null,
        //        }).ToList() ?? new List<SalsePaymentDetails>()
        //    };

        //    var createdSale = await _salesservice.Create(model);

        //    return Ok(new
        //    {
        //        success = true,
        //        saleId = createdSale.Id,
        //        message = "Sale created successfully."
        //    });
        //}

        // GET: api/Sales/Edit/{id}
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> GetEditData(int id)
        {
            var model = await _salesservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Sale not found." });

            var VM = new SalesVM
            {
                Id = model.Id,
                CustomerId = model.CustomerId,
                CustomerName = model.Customers?.Name,
                BillNo = model.BillNo,
                BillDate = model.BillDate,
                MobileNo = model.MobileNo,
                PharmacyDoctorId = model.PharmacyDoctorId,
                DoctorName = model.PharmacyDoctor?.Name,
                DoctorMobileNumber = model.DoctorMobileNumber,
                DoctorRegNumber = model.DoctorRegNumber,
                Address = model.Address,
                Total = model.Total,
                TotalGstAmt = model.TotalGstAmt,
                TotalCessAmount = model.TotalCessAmount,
                discountPercent = model.discountPercent,
                discountAmount = model.discountAmount,
                Totaldiscount = model.Totaldiscount,
                TotalPayable = model.TotalPayable,
                PaidAmount = model.PaidAmount,
                ReturnAmount = model.ReturnAmount,
                OfferId = model.OfferId,
                Balance = model.Balance,
                SalesItemVMs = model.SalesItems != null
                    ? model.SalesItems.Select(MapSalesItemVm).ToList() : new List<SalesItemVM>(),
                SalsePaymentDetails = model.SalsePaymentDetails?.Select(pd => new SalsePaymentDetailsVM
                {
                    Id = pd.Id,
                    PaymentModeId = pd.PaymentModeId,
                    Amount = pd.Amount,
                    ReferenceNo = pd.ReferenceNo,
                    Description = pd.Description,
                    CustomerId = pd.CustomerId
                }).ToList() ?? new List<SalsePaymentDetailsVM>()
            };
             
            return Ok(new
            {
                success = true,
                data = new
                {
                    sales = VM, 
                }
            });
        }


        // PUT: api/Sales/Edit
        //[HttpPut("Edit")]
        //public async Task<IActionResult> Edit([FromBody] SalesVM VM)
        //{
        //    if (VM == null)
        //        return BadRequest(new { success = false, message = "Invalid data." });

        //    var model = await _salesservice.GetById(VM.Id);
        //    if (model == null)
        //        return NotFound(new { success = false, message = "Sale not found." });

        //    model.CustomerId = VM.CustomerId;
        //    model.BillDate = VM.BillDate;
        //    model.BillNo = VM.BillNo;
        //    model.MobileNo = VM.MobileNo;
        //    model.Address = VM.Address;
        //    model.PharmacyDoctorId = VM.PharmacyDoctorId;
        //    model.DoctorMobileNumber = VM.DoctorMobileNumber;
        //    model.DoctorRegNumber = VM.DoctorRegNumber;
        //    model.Total = VM.Total;
        //    model.TotalGstAmt = VM.TotalGstAmt;
        //    model.TotalPayable = VM.TotalPayable;
        //    model.discountPercent = VM.discountPercent;
        //    model.discountAmount = VM.discountAmount;
        //    model.Totaldiscount = VM.Totaldiscount;
        //    model.PaidAmount = VM.PaidAmount;
        //    model.ReturnAmount = VM.ReturnAmount;
        //    model.OfferId = VM.OfferId;
        //    model.Balance = VM.Balance;
        //    // Update Sales Items
        //    if (model.SalesItems == null) model.SalesItems = new List<SalesItem>();
        //    if (VM.SalesItemVMs == null) VM.SalesItemVMs = new List<SalesItemVM>();

        //    var removedItems = model.SalesItems.Where(dbItem => !VM.SalesItemVMs.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();
        //    foreach (var item in removedItems)
        //    {
        //        model.SalesItems.Remove(item);
        //    }

        //    foreach (var item in VM.SalesItemVMs)
        //    {
        //        if (item.Id > 0)
        //        {
        //            var existingItem = model.SalesItems.FirstOrDefault(x => x.Id == item.Id);
        //            if (existingItem != null)
        //            {
        //                existingItem.ItemMasterId = item.ItemMasterId;
        //                existingItem.PurchaseItemId = item.PurchaseItemId;
        //                existingItem.Batch = item.Batch;
        //                existingItem.Expirydate = item.Expirydate;
        //                existingItem.Mrp = item.Mrp;
        //                existingItem.Qty = item.Qty;
        //                existingItem.Rate = item.Rate;
        //                existingItem.Gst = item.Gst;
        //                existingItem.Discount = item.Discount;
        //                existingItem.Amount = item.Amount;
        //            }
        //        }
        //        else
        //        {
        //            model.SalesItems.Add(new SalesItem
        //            {
        //                ItemMasterId = item.ItemMasterId,
        //                PurchaseItemId = item.PurchaseItemId,
        //                Batch = item.Batch,
        //                Expirydate = item.Expirydate,
        //                Mrp = item.Mrp,
        //                Qty = item.Qty,
        //                Rate = item.Rate,
        //                Gst = item.Gst,
        //                Discount = item.Discount,
        //                Amount = item.Amount,
        //            });
        //        }
        //    }

        //    // Update Payment Details
        //    if (model.SalsePaymentDetails == null) model.SalsePaymentDetails = new List<SalsePaymentDetails>();
        //    if (VM.SalsePaymentDetails == null) VM.SalsePaymentDetails = new List<SalsePaymentDetailsVM>();

        //    var removedPaymentDetails = model.SalsePaymentDetails
        //       .Where(dbPayment => !VM.SalsePaymentDetails.Any(vmItem => vmItem.Id == dbPayment.Id)).ToList();
        //    foreach (var payment in removedPaymentDetails)
        //    {
        //        model.SalsePaymentDetails.Remove(payment);
        //    }

        //    foreach (var data in VM.SalsePaymentDetails)
        //    {
        //        if (data.Id > 0)
        //        {
        //            var existingPayment = model.SalsePaymentDetails.FirstOrDefault(x => x.Id == data.Id);
        //            if (existingPayment != null)
        //            {
        //                existingPayment.PaymentModeId = data.PaymentModeId;
        //                existingPayment.Amount = data.Amount;
        //                existingPayment.ReferenceNo = data.ReferenceNo;
        //                existingPayment.Description = data.Description;
        //                existingPayment.CustomerId = (int)VM.CustomerId;
        //            }
        //        }
        //        else
        //        {
        //            model.SalsePaymentDetails.Add(new SalsePaymentDetails
        //            {
        //                PaymentModeId = data.PaymentModeId,
        //                Amount = data.Amount,
        //                ReferenceNo = data.ReferenceNo,
        //                Description = data.Description,
        //                CustomerId = (int)VM.CustomerId,
        //            });
        //        }
        //    }

        //    await _salesservice.Update(model);
        //    return Ok(new { success = true, message = "Sale updated successfully." });
        //}
        [HttpPut("Edit")]
        public async Task<IActionResult> Edit([FromBody] SalesVM VM, bool isShare = false)
        {
            if (VM == null)
                return BadRequest(new { success = false, message = "Invalid data." });

            var model = await _salesservice.GetById(VM.Id);
            if (model == null)
                return NotFound(new { success = false, message = "Sale not found." });

            var isRegisteredBilling = string.Equals(VM.billingType, "registered", StringComparison.OrdinalIgnoreCase)
                || (string.IsNullOrWhiteSpace(VM.billingType) && VM.CustomerId.HasValue);

            if (isRegisteredBilling)
            {
                if (string.IsNullOrWhiteSpace(VM.MobileNo))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Mobile number is required for registered customer."
                    });
                }

                var customerList = await _customerservice.GetAll();
                var isExist = customerList.Any(x =>
                    !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                    x.PhoneNo.Trim() == VM.MobileNo.Trim()
                );

                if (!isExist)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Customer does not exist. Please create customer first."
                    });
                }
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var setting = await _salsesettingservice.GetByUserId(userId);
            bool isTabletWise = setting != null && setting.ItemConversion == "TabletWise";

            var itemMasters = (await _itemmasterservice.GetAll())
                .Where(x => x.IsActive)
                .ToList();

            // Update main fields
            model.CustomerId = isRegisteredBilling ? VM.CustomerId : null;
            model.MobileNo = isRegisteredBilling ? VM.MobileNo : null;
            model.Address = isRegisteredBilling ? VM.Address : null;
            model.billingType = isRegisteredBilling ? "registered" : "Cash";
            model.PaymentType = VM.PaymentType;
            model.BillDate = VM.BillDate;
            model.BillNo = VM.BillNo;
            model.PharmacyDoctorId = VM.PharmacyDoctorId;
            model.DoctorMobileNumber = VM.DoctorMobileNumber;
            model.DoctorRegNumber = VM.DoctorRegNumber;
            model.Total = VM.Total;
            model.TotalGstAmt = VM.TotalGstAmt;
            model.TotalPayable = VM.TotalPayable;
            model.TotalCessAmount = VM.TotalCessAmount;
            model.RoundOffAmount = VM.RoundOffAmount;
            model.discountPercent = VM.discountPercent;
            model.discountAmount = VM.discountAmount;
            model.Totaldiscount = VM.Totaldiscount;
            model.PaidAmount = VM.PaidAmount;
            model.ReturnAmount = Math.Max(0, VM.PaidAmount - VM.TotalPayable);
            model.OfferId = VM.OfferId;
            model.Balance = VM.Balance;
            model.NetCollection = VM.PaidAmount - model.ReturnAmount;
            model.PaymentStatus = VM.PaidAmount == 0 ? "Unpaid" :
                                  VM.PaidAmount < VM.TotalPayable ? "Partial" : "Paid";

            if (isRegisteredBilling && VM.CustomerId.HasValue)
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

            // Handle SalesItems
            var removedItems = model.SalesItems
                .Where(dbItem => !VM.SalesItemVMs.Any(vmItem => vmItem.Id == dbItem.Id))
                .ToList();

            foreach (var item in removedItems)
            {
                await _currenstockService.UpdateStock(
                    item.ItemMasterId,
                    item.Batch?.Trim() ?? "",
                    item.Qty,
                    item.Expirydate,
                    item.Mrp,
                    item.Rate
                );
                model.SalesItems.Remove(item);
            }

            foreach (var item in VM.SalesItemVMs)
            {
                var itemMaster = itemMasters.FirstOrDefault(x => x.Id == item.ItemMasterId);
                decimal finalQty = item.Qty;

                if (isTabletWise && itemMaster != null && itemMaster.Conversion > 0)
                {
                    finalQty = item.Qty + ((decimal)item.TabletQty / itemMaster.Conversion);
                }

                if (item.Id > 0)
                {
                    var existingItem = model.SalesItems.FirstOrDefault(x => x.Id == item.Id);
                    if (existingItem != null)
                    {
                        var resolvedTax = ResolveSalesItemTax(item, itemMaster, existingItem);
                        decimal oldQty = existingItem.Qty;

                        await _currenstockService.UpdateStock(
                            existingItem.ItemMasterId,
                            existingItem.Batch?.Trim() ?? "",
                            oldQty,
                            existingItem.Expirydate,
                            existingItem.Mrp,
                            existingItem.Rate
                        );

                        await _currenstockService.UpdateStock(
                            item.ItemMasterId,
                            item.Batch?.Trim() ?? "",
                            -finalQty,
                            item.Expirydate,
                            item.Mrp,
                            item.Rate
                        );

                        existingItem.ItemMasterId = item.ItemMasterId;
                        existingItem.HsnId = resolvedTax.HsnId;
                        existingItem.HsnCode = resolvedTax.HsnCode;
                        existingItem.PurchaseItemId = item.PurchaseItemId;
                        existingItem.Batch = item.Batch;
                        existingItem.Expirydate = item.Expirydate;
                        existingItem.Mrp = item.Mrp;
                        existingItem.Qty = finalQty;
                        existingItem.Rate = item.Rate;
                        existingItem.Gst = resolvedTax.Gst;
                        existingItem.IGst = resolvedTax.IGst;
                        existingItem.CGst = resolvedTax.CGst;
                        existingItem.SGst = resolvedTax.SGst;
                        existingItem.Cess = resolvedTax.Cess;
                        existingItem.Discount = item.Discount;
                        existingItem.Amount = item.Amount;
                        existingItem.StripRate = item.StripRate;
                        existingItem.IsSoldInTablets = isTabletWise;
                    }
                }
                else
                {
                    var resolvedTax = ResolveSalesItemTax(item, itemMaster);

                    await _currenstockService.UpdateStock(
                        item.ItemMasterId,
                        item.Batch?.Trim() ?? "",
                        -finalQty,
                        item.Expirydate,
                        item.Mrp,
                        item.Rate
                    );

                    model.SalesItems.Add(new SalesItem
                    {
                        ItemMasterId = item.ItemMasterId,
                        HsnId = resolvedTax.HsnId,
                        HsnCode = resolvedTax.HsnCode,
                        PurchaseItemId = item.PurchaseItemId,
                        Batch = item.Batch,
                        Expirydate = item.Expirydate,
                        Mrp = item.Mrp,
                        Qty = finalQty,
                        Rate = item.Rate,
                        StripRate = item.StripRate,
                        Gst = resolvedTax.Gst,
                        IGst = resolvedTax.IGst,
                        CGst = resolvedTax.CGst,
                        SGst = resolvedTax.SGst,
                        Cess = resolvedTax.Cess,
                        Discount = item.Discount,
                        Amount = item.Amount,
                        IsSoldInTablets = isTabletWise
                    });
                }
            }

            if (model.SalsePaymentDetails == null)
                model.SalsePaymentDetails = new List<SalsePaymentDetails>();

            if (VM.SalsePaymentDetails == null)
                VM.SalsePaymentDetails = new List<SalsePaymentDetailsVM>();

            // Handle Payments
            var removedPaymentDetails = model.SalsePaymentDetails
                .Where(dbPayment => !VM.SalsePaymentDetails.Any(vmItem => vmItem.Id == dbPayment.Id))
                .ToList();

            foreach (var payment in removedPaymentDetails)
            {
                model.SalsePaymentDetails.Remove(payment);
            }

            foreach (var pd in VM.SalsePaymentDetails)
            {
                if (pd.Id > 0)
                {
                    var existingPayment = model.SalsePaymentDetails.FirstOrDefault(x => x.Id == pd.Id);
                    if (existingPayment != null)
                    {
                        existingPayment.PaymentModeId = pd.PaymentModeId;
                        existingPayment.Amount = pd.Amount;
                        existingPayment.ReferenceNo = pd.ReferenceNo;
                        existingPayment.Description = pd.Description;
                        existingPayment.CustomerId = isRegisteredBilling ? VM.CustomerId : null;
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
                        CustomerId = isRegisteredBilling ? VM.CustomerId : null,
                    });
                }
            }

            await _salesservice.Update(model);

            string? walletMessage = null;
            if (isShare)
            {
                walletMessage = await ShareInvoiceViaWhatsAppAndSmsAsync(
                    model.Id,
                    model.BillNo ?? string.Empty,
                    model.MobileNo,
                    model.CustomerId);
            }

            return Ok(new { success = true, message = "Sale updated successfully.", saleId = model.Id, walletMessage });
        }
        // DELETE: api/Sales/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "Invalid Id for deletion." });

            var model = await _salesservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Sale not found." });

            foreach (var item in model.SalesItems ?? new List<SalesItem>())
            {
                await _currenstockService.UpdateStock(
                    item.ItemMasterId,
                    item.Batch ?? "",
                    item.Qty,
                    item.Expirydate,
                    item.Mrp,
                    item.Rate
                );
            }

            await _salesservice.Delete(model);
            return Ok(new { success = true, message = "Sale deleted successfully." });
        }

        // GET: api/Sales/SalesOrdersByCustomer/{customerId}
        [HttpGet("SalesOrdersByCustomer/{customerId}")]
        public async Task<IActionResult> GetSalesOrdersByCustomer(int customerId)
        {
            // First, check if the customer exists using your existing repository method
            var customer = await _customerservice.GetById(customerId);

            if (customer == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Customer with ID {customerId} not found"
                });
            }

            var orders = (await _salesOrderRepo.GetByCustomerId(customerId))
                .Select(o => new
                {
                    id = o.Id,
                    billNo = o.BillNo,
                    billDate = o.BillDate,
                    totalPayable = o.TotalPayable,
                    items = o.salesOrderItems.Select(i => new
                    {
                        itemId = i.ItemMasterId,
                        itemName = i.ItemMaster?.Name ?? "",
                        batchNo = i.Batch,
                        expiryDate = i.Expirydate.HasValue ? i.Expirydate.Value.ToString("MM/yyyy") : "",
                        qty = i.Qty,
                        mrp = i.Mrp,
                        rate = i.Rate,
                        gstPercent = i.Gst,
                        discountPercent = i.Discount,
                        amount = i.Amount
                    }).ToList()
                })
                .ToList();

            var response = new
            {
                success = true,
                count = orders.Count,
                message = orders.Count > 0 ? "Orders retrieved successfully" : "No orders found for this customer",
                data = orders
            };

            return Ok(response);
        }

        // GET: api/Sales/BatchesByItemId/{id}
        [HttpGet("BatchesByItemId/{id}")]
        public async Task<IActionResult> GetBatchesByItemId(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);

            if (item == null)
                return NotFound(new { success = false, message = "Item not found." });

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

            var result = new
            {
                hsnId = item.HsnId,
                hsnCode = item.Hsn?.HsnCode,
                gst = item.Hsn?.IGST ?? 0,
                igst = item.Hsn?.IGST,
                cgst = item.Hsn?.CGST,
                sgst = item.Hsn?.SGST,
                cess = item.Hsn?.Cess,
                groupedResult = groupedResult
            };

            return Ok(new { success = true, data = result });
        }

        // GET: api/Sales/ItemDetails/{itemId}
        [HttpGet("ItemDetails/{itemId}")]
        public async Task<IActionResult> GetItemDetails(int itemId)
        {
            if (itemId <= 0)
                return BadRequest(new { success = false, message = "Invalid item ID." });

            var hsndata = await _itemmasterservice.GetByItemMasterId(itemId);
            if (hsndata == null)
                return NotFound(new { success = false, message = "Item not found." });

            var result = new
            {
                hsnId = hsndata.HsnId,
                hsnCode = hsndata.Hsn?.HsnCode,
                gst = hsndata.Hsn?.IGST,
                igst = hsndata.Hsn?.IGST,
                cgst = hsndata.Hsn?.CGST,
                sgst = hsndata.Hsn?.SGST,
                cess = hsndata.Hsn?.Cess,
                purchaseItems = (await _purchaseitemservice.GetByItemMasterId(itemId)).Select(x => new
                {
                    x.Id,
                    Text = x.Batch
                }).ToList(),
            };

            return Ok(new { success = true, data = result });
        }

        // GET: api/Sales/PurchaseItemDetails/{id}
        [HttpGet("PurchaseItemDetails/{id}")]
        public async Task<IActionResult> GetPurchaseItemDetails(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "Invalid ID." });

            var purchasedata = await _purchaseitemservice.GetById(id);
            if (purchasedata == null)
                return NotFound(new { success = false, message = "Purchase item not found." });

            var result = new
            {
                qty = purchasedata.Qty,
                rate = purchasedata.Rate,
                mrp = purchasedata.Mrp,
                expirydate = purchasedata.ExpiryDate
            };

            return Ok(new { success = true, data = result });
        }

        


        // GET: api/Sales/CustomerByMobile/{mobileNo}
        [HttpGet("CustomerByMobile/{mobileNo}")]
        public async Task<IActionResult> GetCustomerByMobile(string mobileNo)
        {
            var customer = await _customerservice.GetCustomerByMobileno(mobileNo);
            if (customer == null)
                return Ok(new { success = true, found = false });

            return Ok(new
            {
                success = true,
                found = true,
                data = new
                {
                    customer.Name,
                    customer.Id,
                    customer.Address
                }
            });
        }

        // POST: api/Sales/CreateCustomer
        [HttpPost("CreateCustomer")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerVM Vm)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid customer data" });

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

        // GET: api/Sales/ItemByBarcode/{barcode}
        [HttpGet("ItemByBarcode/{barcode}")]
        public async Task<IActionResult> GetItemByBarcode(string barcode)
        {
            var item = await _itemmasterservice.GetByBarcode(barcode);
            if (item == null)
                return NotFound(new { success = false, message = "Item not found" });

            return Ok(new { success = true, itemId = item.Id, itemname = item.Name });
        }

        // GET: api/Sales/ItemById/{id}
        [HttpGet("ItemById/{id}")]
        public async Task<IActionResult> GetItemById(int id)
        {
            var item = await _itemmasterservice.GetByItemMasterId(id);
            if (item == null)
                return NotFound(new { success = false, message = "Item not found" });

            return Ok(new { success = true, itemId = item.Id, itemname = item.Name });
        }

        // GET: api/Sales/AllItems
        [HttpGet("AllItems")]
        public async Task<IActionResult> GetAllItems()
        {
            var itemMasters = (await _itemmasterservice.GetAll()).OrderBy(x => x.Name);
            var purchases = await _purchaseservice.GetAll();
            var purchasereturns = await _purchasereturnservice.GetAll();
            var sales = await _salesservice.GetAll();
            var stockIssue = await _stockissueservice.GetAll();
            var stockReturn = await _stockrturnservice.GetAll();
            var stockreceive = await _stockreceiveservice.GetAll();

            var purchaseItems = purchases.Where(p => p.PurchaseItems != null).SelectMany(p => p.PurchaseItems);
            var purchaseReturnItems = purchasereturns.Where(r => r.PurchaseReturnItems != null).SelectMany(r => r.PurchaseReturnItems);
            var salesItems = sales.Where(s => s.SalesItems != null).SelectMany(s => s.SalesItems);
            var stockissueItems = stockIssue.Where(s => s.StockIssuesItems != null).SelectMany(s => s.StockIssuesItems);
            var stockreturnItems = stockReturn.Where(s => s.StockReturnItems != null).SelectMany(s => s.StockReturnItems);
            var stockreceiveItems = stockreceive.Where(s => s.StockReceiveItems != null).SelectMany(s => s.StockReceiveItems);

            var stockList = itemMasters.Select(item =>
            {
                var itemId = item.Id;

                var totalPurchasedQty = purchaseItems
                    .Where(x => x.ItemId == itemId)
                    .Sum(x => x.Qty + x.FreeQty);

                var totalpurchaseReturnedQty = purchaseReturnItems
                    .Where(x => x.ItemId == itemId)
                    .Sum(x => x.Qty + x.FreeQty);

                var totalsalseQty = salesItems
                    .Where(x => x.ItemMasterId == itemId)
                    .Sum(x => x.Qty);

                var totalstockissueQty = stockissueItems
                    .Where(x => x.ItemMasterId == itemId)
                    .Sum(x => x.Qty);

                var totalstockreturnQty = stockreturnItems
                    .Where(x => x.ItemMasterId == itemId)
                    .Sum(x => x.Qty);

                var totalstockreceiveQty = stockreceiveItems
                    .Where(x => x.ItemMasterId == itemId)
                    .Sum(x => x.Qty);

                var currentStock = totalPurchasedQty + totalstockreturnQty + totalstockreceiveQty
                                 - totalpurchaseReturnedQty - totalsalseQty - totalstockissueQty;

                return new
                {
                    id = itemId,
                    barcode = item.Barcode,
                    code = item.Code,
                    name = item.Name,
                    gst = item.Hsn?.IGST ?? 0,
                    qty = currentStock
                };
            }).ToList();

            return Ok(new { success = true, data = stockList });
        }

        // GET: api/Sales/BuyXGetXFreeItems/{offerId}
        [HttpGet("BuyXGetXFreeItems/{offerId}")]
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

            return Ok(new { success = true, data = cartFreeItems });
        }

        // GET: api/Sales/ApplyComboOffer/{offerId}
        [HttpGet("ApplyComboOffer/{offerId}")]
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

            return Ok(new { success = true, data = comboItemsList });
        }

        // POST: api/Sales/EvaluateOffers
        [HttpPost("EvaluateOffers")]
        public async Task<IActionResult> EvaluateOffers([FromBody] SalesOfferRequest request)
        {
            var result = await EvaluateOffer(request.SalesItemVMs, request.TotalAmount);
            return Ok(new { success = true, data = result });
        }

        private async Task<AppliedOfferResult> EvaluateOffer(IList<SalesItemVM> salesItems, decimal totalAmount)
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

        public class SalesOfferRequest
        {
            public IList<SalesItemVM> SalesItemVMs { get; set; } = new List<SalesItemVM>();
            public decimal TotalAmount { get; set; }
        }

        public class AppliedOfferResult
        {
            public decimal TotalDiscount { get; set; }
            public List<OfferVM> Offers { get; set; } = new List<OfferVM>();
            public List<SalesItemVM> FreeItems { get; set; } = new List<SalesItemVM>();
        }
    }
}

