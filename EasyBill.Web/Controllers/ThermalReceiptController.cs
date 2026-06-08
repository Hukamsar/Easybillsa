using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly IUnitOfWork _unitofwork;

        public ThermalReceiptController(
            ISalesRepository salesService,
            IInvoiceThemeSettingRepository themeRepo,
            ITenantRegistrationRepository tenantRepository,
            IUnitOfWork unitofwork)
        {
            _salesService = salesService;
            _themeRepo = themeRepo;
            _tenantRepository = tenantRepository;
            _unitofwork = unitofwork;
        }

        public async Task<IActionResult> Index(int saleId, string paperSize = "80mm")
        {
            var sale = await _salesService.GetById(saleId);
            if (sale == null) return NotFound();

            if (sale.CustomerId.HasValue)
            {
                var pointSettings = await _unitofwork.GetRepository<PointSetting>().Query()
                    .Where(x => x.TenantId == sale.TenantId)
                    .ToListAsync();
                var earliestSetting = pointSettings.OrderBy(x => x.Created).FirstOrDefault();
                var earliestCreated = earliestSetting?.Created ?? DateTime.MaxValue;
                var saleCompareDate = sale.Created ?? sale.BillDate ?? DateTime.Now;

                if (saleCompareDate >= earliestCreated)
                {
                    var pointRepo = _unitofwork.GetRepository<PointTransaction>();
                    var currentPointsBalance = await pointRepo.Query()
                        .Where(x => x.CustomerId == sale.CustomerId.Value)
                        .SumAsync(x => x.EarnedPoints - x.RedeemedPoints);
                    ViewBag.CustomerPointsBalance = currentPointsBalance;

                    var pointTxForSale = await pointRepo.Query()
                        .FirstOrDefaultAsync(x => x.SaleId == sale.Id);
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
