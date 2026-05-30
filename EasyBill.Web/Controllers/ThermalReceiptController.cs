using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EasyBill.UI.Controllers
{
    [Authorize]
    public class ThermalReceiptController : Controller
    {
        private readonly ISalesRepository _salesService;
        private readonly IInvoiceThemeSettingRepository _themeRepo;
        private readonly ITenantRegistrationRepository _tenantRepository;

        public ThermalReceiptController(
            ISalesRepository salesService,
            IInvoiceThemeSettingRepository themeRepo,
            ITenantRegistrationRepository tenantRepository)
        {
            _salesService = salesService;
            _themeRepo = themeRepo;
            _tenantRepository = tenantRepository;
        }

        public async Task<IActionResult> Index(int saleId, string paperSize = "80mm")
        {
            var sale = await _salesService.GetById(saleId);
            if (sale == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            InvoiceThemeSetting? theme = null;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                theme = await _themeRepo.GetThemeSettingAsync(userId, "Thermal")
                    ?? await _themeRepo.GetDefaultThemeAsync(userId);
            }

            if (theme == null)
            {
                theme = new InvoiceThemeSetting
                {
                    PaperSize = "Thermal",
                    Title = "TAX INVOICE",
                    FontFamily = "'Courier New', monospace",
                    BaseFontSize = 11,
                    PrimaryColor = "#111827",
                    TextColor = "#111827",
                    ShowItemDiscount = true,
                    ShowGSTAmount = true,
                    ShowPaymentQR = false,
                    ShowAmountInWords = false,
                    SignatoryLabel = "Auth. Signatory",
                    ThermalPaperSize = string.Equals(paperSize, "58mm", StringComparison.OrdinalIgnoreCase) ? 58 : 80
                };
            }

            theme.ThermalPaperSize = string.Equals(paperSize, "58mm", StringComparison.OrdinalIgnoreCase) ? 58 : 80;

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

            // Legacy hardcoded values kept commented for reference.
            // ViewBag.ShopName = "EasyBill Pvt Ltd";
            // ViewBag.ShopAddress = "Noida Sector 63, near electronic city metro station";
            // ViewBag.ShopPhone = "+91 9876543210";

            ViewBag.ShopName = ViewBag.BusinessName;
            ViewBag.ShopAddress = ViewBag.BusinessAddress;
            ViewBag.ShopPhone = ViewBag.BusinessPhone;
            ViewBag.PaperSize = paperSize;
            ViewBag.ThemeSettings = theme;
            ViewBag.TermsAndConditions = theme.TermsAndConditions;
            ViewBag.BankDetails = theme.BankDetails;
            ViewBag.UPIDetails = theme.UPIDetails;
            ViewBag.SignatoryLabel = theme.SignatoryLabel;

            return View("~/Views/Shared/InvoiceTemplates/Thermal_Print.cshtml", sale);
        }
    }
}
