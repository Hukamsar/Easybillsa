using AOne.Models.Entity;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EasyBill.UI.Controllers
{
    [Authorize]
    public class InvoiceThemeController : Controller
    {
        private readonly IInvoiceThemeSettingRepository _themeRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // ?? BTR FIX: ???? ????? ???? (Tenant) ??????? ?? ??? Repository ??? ?? ??
        private readonly ITenantRepository _tenantRepo;

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".webp"
        };

        private const long MaxLogoBytes = 2 * 1024 * 1024;
        private const long MaxWatermarkBytes = 3 * 1024 * 1024;
        private const long MaxSignatureBytes = 2 * 1024 * 1024;

        // ?? BTR FIX: Constructor ??? ITenantRepository ?? Inject ?? ????
        public InvoiceThemeController(
            IInvoiceThemeSettingRepository themeRepo,
            IWebHostEnvironment webHostEnvironment,
            ITenantRepository tenantRepo)
        {
            _themeRepo = themeRepo;
            _webHostEnvironment = webHostEnvironment;
            _tenantRepo = tenantRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetTheme(string paperSize)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not found" });
            }

            var normalizedPaperSize = NormalizePaperSize(paperSize);
            var theme = await _themeRepo.GetThemeSettingAsync(userId, normalizedPaperSize);

            return Json(new { success = true, data = theme });
        }

        [HttpPost]
        public async Task<IActionResult> SaveTheme(
            [FromForm] InvoiceThemeSetting model,
            IFormFile? logoFile,
            IFormFile? watermarkFile,
            IFormFile? signatureFile)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not logged in" });
                }

                var normalizedPaperSize = NormalizePaperSize(model.PaperSize);
                var existingTheme = await _themeRepo.GetThemeSettingAsync(userId, normalizedPaperSize);

                var entity = existingTheme ?? new InvoiceThemeSetting
                {
                    ApplicationUserId = userId,
                    PaperSize = normalizedPaperSize,
                    IsDefault = false
                };

                if (existingTheme != null)
                {
                    entity.Id = existingTheme.Id;
                    entity.IsDefault = existingTheme.IsDefault;
                }

                entity.ApplicationUserId = userId;
                entity.PaperSize = normalizedPaperSize;
                entity.TenantId = existingTheme?.TenantId ?? model.TenantId;

                entity.TemplateName = model.TemplateName;
                entity.PrimaryColor = string.IsNullOrWhiteSpace(model.PrimaryColor) ? "#2b6ac9" : model.PrimaryColor;
                entity.TextColor = string.IsNullOrWhiteSpace(model.TextColor) ? "#1e293b" : model.TextColor;
                entity.FontFamily = string.IsNullOrWhiteSpace(model.FontFamily)
                    ? (normalizedPaperSize == "Thermal" ? "'Courier New', monospace" : "Arial, sans-serif")
                    : model.FontFamily;
                entity.BaseFontSize = ClampInt(model.BaseFontSize, 8, 24, normalizedPaperSize == "Thermal" ? 10 : 12);

                entity.Title = string.IsNullOrWhiteSpace(model.Title) ? "TAX INVOICE" : model.Title.Trim();
                entity.Margins = string.IsNullOrWhiteSpace(model.Margins) ? "Normal" : model.Margins;
                entity.Orientation = normalizedPaperSize == "Thermal"
                    ? "Portrait"
                    : (string.IsNullOrWhiteSpace(model.Orientation) ? "Portrait" : model.Orientation);

                entity.LogoSize = ClampInt(model.LogoSize, 40, 240, null);
                entity.LogoPosition = string.IsNullOrWhiteSpace(model.LogoPosition) ? "Left" : model.LogoPosition;
                entity.WatermarkOpacity = ClampInt(model.WatermarkOpacity, 5, 40, null);
                entity.WatermarkSize = ClampInt(model.WatermarkSize, 60, 600, null);
                entity.SignatoryLabel = string.IsNullOrWhiteSpace(model.SignatoryLabel) ? "Auth. Signatory" : model.SignatoryLabel.Trim();

                entity.ShowHSN = model.ShowHSN;
                entity.ShowGSTPercent = model.ShowGSTPercent;
                entity.ShowGSTAmount = model.ShowGSTAmount;
                entity.ShowTaxSummaryTable = model.ShowTaxSummaryTable;
                entity.ShowBusinessGSTIN = model.ShowBusinessGSTIN;
                entity.ShowItemDiscount = model.ShowItemDiscount;
                entity.IsZebraStriped = model.IsZebraStriped;
                entity.ShowAmountInWords = model.ShowAmountInWords;
                entity.ShowPaymentQR = model.ShowPaymentQR; 
                entity.BankDetails = model.BankDetails;
                entity.TermsAndConditions = model.TermsAndConditions;
                entity.UPIDetails = model.UPIDetails;
                entity.ThermalPaperSize = normalizedPaperSize == "Thermal"
                    ? NormalizeThermalPaperSize(model.ThermalPaperSize)
                    : null;
                entity.UpdatedAt = DateTime.UtcNow;

                entity.LogoPath = await SaveAssetIfUploadedAsync(logoFile, entity.LogoPath, userId, normalizedPaperSize, "logo");
                entity.WatermarkPath = await SaveAssetIfUploadedAsync(watermarkFile, entity.WatermarkPath, userId, normalizedPaperSize, "watermark");
                entity.SignaturePath = await SaveAssetIfUploadedAsync(signatureFile, entity.SignaturePath, userId, normalizedPaperSize, "signature");

                await _themeRepo.SaveOrUpdateThemeAsync(entity);

                return Json(new
                {
                    success = true,
                    message = $"{normalizedPaperSize} settings saved successfully!",
                    data = new
                    {
                        entity.Id,
                        entity.PaperSize,
                        entity.LogoPath,
                        entity.WatermarkPath,
                        entity.SignaturePath,
                        entity.IsDefault,
                        entity.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetDefaultTheme(string paperSize)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not logged in" });
            }

            var normalizedPaperSize = NormalizePaperSize(paperSize);
            var existing = await _themeRepo.GetThemeSettingAsync(userId, normalizedPaperSize);

            if (existing == null)
            {
                var source = await _themeRepo.GetDefaultThemeAsync(userId);
                var bootstrapTheme = source != null
                    ? CloneThemeForPaper(source, userId, normalizedPaperSize)
                    : CreateDefaultTheme(userId, normalizedPaperSize);

                await _themeRepo.SaveOrUpdateThemeAsync(bootstrapTheme);
            }

            await _themeRepo.SetDefaultThemeAsync(userId, normalizedPaperSize);
            return Json(new { success = true, message = $"{normalizedPaperSize} is now your default print size." });
        }


        [HttpGet]
        public async Task<IActionResult> PreviewInvoice(string paperSize = "A4")
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var normalizedPaperSize = NormalizePaperSize(paperSize);
            var themeSetting = await _themeRepo.GetThemeSettingAsync(userId, normalizedPaperSize);
            if (themeSetting == null)
            {
                themeSetting = await _themeRepo.GetDefaultThemeAsync(userId);
            }

            var tenantId = User.FindFirst("TenantId")?.Value ?? themeSetting?.TenantId;
            Tenant tenant = null;

            if (!string.IsNullOrEmpty(tenantId))
            {
                tenant = await _tenantRepo.GetById(tenantId) ?? await _tenantRepo.GetByTenantId(tenantId);
            }

            if (tenant == null)
            {
                var allTenants = await _tenantRepo.GetAll();
                tenant = allTenants.FirstOrDefault();
            }

            if (tenant != null)
            {
                ViewBag.BusinessName = tenant.Name;
                ViewBag.BusinessAddress = string.Join(", ", new[] { tenant.Address1, tenant.Address2, tenant.Location }.Where(s => !string.IsNullOrEmpty(s)));
                ViewBag.BusinessPhone = !string.IsNullOrWhiteSpace(tenant.MobileNo) ? tenant.MobileNo : tenant.Phone;
                ViewBag.BusinessEmail = tenant.Email;
                ViewBag.BusinessGSTIN = tenant.GstNo;
            }
            else
            {
                ViewBag.BusinessName = "Your Business Name";
                ViewBag.BusinessAddress = "Business Address Line 1, City";
                ViewBag.BusinessPhone = "00000 00000";
                ViewBag.BusinessEmail = "support@business.com";
                ViewBag.BusinessGSTIN = "29ABCDE1234F1Z5";
            }

            ViewBag.ThemeSettings = themeSetting;
            ViewBag.TermsAndConditions = themeSetting?.TermsAndConditions;
            ViewBag.BankDetails = themeSetting?.BankDetails;
            ViewBag.UPIDetails = themeSetting?.UPIDetails;
            ViewBag.SignatoryLabel = themeSetting?.SignatoryLabel;

            // ... (बाकी डमी सेल वाला कोड सेम रहेगा) ...
            var dummySale = new Sales
            {
                BillNo = "INV-2026-001",
                // ...
            };

            return View(GetTemplatePath(themeSetting?.PaperSize ?? normalizedPaperSize), dummySale);
        }

        //[HttpGet]
        //public async Task<IActionResult> PreviewInvoice(string paperSize = "A4")
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Unauthorized();
        //    }

        //    var normalizedPaperSize = NormalizePaperSize(paperSize);
        //    var themeSetting = await _themeRepo.GetThemeSettingAsync(userId, normalizedPaperSize);
        //    if (themeSetting == null)
        //    {
        //        themeSetting = await _themeRepo.GetDefaultThemeAsync(userId);
        //    }

        //    // ?? BTR FIX: ???? ????? (Tenant) ?? ???? ??????? ?? ?????? ?? ??? ??
        //    var tenantId = User.FindFirst("TenantId")?.Value ?? themeSetting?.TenantId;
        //    Tenant tenant = null;

        //    if (!string.IsNullOrEmpty(tenantId))
        //    {
        //        // ???? Id ?? ??? ?????, ? ???? ?? TenantId ?? (Safety ke liye)
        //        tenant = await _tenantRepo.GetById(tenantId) ?? await _tenantRepo.GetByTenantId(tenantId);
        //    }

        //    // ?? BTR FIX: ??? ???? ????? ???? Tenant ???? ViewBag ??? ??? ????
        //    if (tenant != null)
        //    {
        //        ViewBag.BusinessName = tenant.Name;
        //        ViewBag.BusinessAddress = tenant.FullAddress;
        //        ViewBag.BusinessPhone = !string.IsNullOrWhiteSpace(tenant.MobileNo) ? tenant.MobileNo : tenant.Phone;
        //        ViewBag.BusinessEmail = tenant.Email;
        //        ViewBag.BusinessGSTIN = tenant.GstNo;
        //    }
        //    else
        //    {
        //        // Fallback (??? ??? ????? ??????? ?? ? ??)
        //        ViewBag.BusinessName = "Your Business Name";
        //        ViewBag.BusinessAddress = "Business Address Line 1, City";
        //        ViewBag.BusinessPhone = "00000 00000";
        //        ViewBag.BusinessEmail = "support@business.com";
        //        ViewBag.BusinessGSTIN = "29ABCDE1234F1Z5";
        //    }

        //    ViewBag.ThemeSettings = themeSetting;
        //    ViewBag.TermsAndConditions = themeSetting?.TermsAndConditions;
        //    ViewBag.BankDetails = themeSetting?.BankDetails;
        //    ViewBag.UPIDetails = themeSetting?.UPIDetails;
        //    ViewBag.SignatoryLabel = themeSetting?.SignatoryLabel;

        //    // ?? ????? ???????? ?????? ?? ??? ??? ??? (Sale) ??, ????? ?? ??? ?? ???? ??!
        //    var dummySale = new Sales
        //    {
        //        BillNo = "INV-2026-001",
        //        BillDate = DateTime.Now,
        //        TotalPayable = 1250.00m,
        //        TotalGstAmt = 150.00m,
        //        Totaldiscount = 50.00m,
        //        Total = 1150.00m,
        //        Customers = new Customer
        //        {
        //            Name = "John Doe",
        //            PhoneNo = "+91 9876543210",
        //            Address = "New Delhi, NCR"
        //        },
        //        SalesItems = new List<SalesItem>
        //        {
        //            new SalesItem
        //            {
        //                ItemMaster = new ItemMaster { Name = "Paracetamol 500mg Strip" },
        //                Qty = 2,
        //                Rate = 50,
        //                Amount = 100,
        //                Gst = 12
        //            },
        //            new SalesItem
        //            {
        //                ItemMaster = new ItemMaster { Name = "Vitamin C Complete" },
        //                Qty = 1,
        //                Rate = 150,
        //                Amount = 150,
        //                Gst = 18
        //            }
        //        }
        //    };

        //    return View(GetTemplatePath(themeSetting?.PaperSize ?? normalizedPaperSize), dummySale);
        //}

        [HttpGet]
        public IActionResult GetPreviewHtml(string paperSize = "A4")
        {
            return View(GetTemplatePath(paperSize), new Sales());
        }

        private static string NormalizePaperSize(string? paperSize)
        {
            if (string.IsNullOrWhiteSpace(paperSize))
            {
                return "A4";
            }

            var value = paperSize.Trim().ToUpperInvariant();
            return value switch
            {
                "A5" => "A5",
                "THERMAL" => "Thermal",
                _ => "A4"
            };
        }

        private static string GetTemplatePath(string paperSize)
        {
            var normalized = NormalizePaperSize(paperSize);
            return $"~/Views/Shared/InvoiceTemplates/{normalized}_Print.cshtml";
        }

        private static int? ClampInt(int? value, int min, int max, int? fallback)
        {
            if (!value.HasValue) return fallback;
            if (value.Value < min) return min;
            if (value.Value > max) return max;
            return value.Value;
        }

        private static int? NormalizeThermalPaperSize(int? value)
        {
            if (!value.HasValue) return 80;
            return value.Value == 58 ? 58 : 80;
        }

        private InvoiceThemeSetting CreateDefaultTheme(string userId, string paperSize)
        {
            return new InvoiceThemeSetting
            {
                ApplicationUserId = userId,
                PaperSize = paperSize,
                IsDefault = false,
                TemplateName = "Classic",
                PrimaryColor = "#2b6ac9",
                TextColor = "#1e293b",
                FontFamily = paperSize == "Thermal" ? "'Courier New', monospace" : "Arial, sans-serif",
                BaseFontSize = paperSize == "Thermal" ? 10 : 12,
                Title = "TAX INVOICE",
                Margins = "Normal",
                Orientation = "Portrait",
                LogoPosition = "Left",
                SignatoryLabel = "Auth. Signatory",
                ShowHSN = true,
                ShowGSTPercent = true,
                ShowGSTAmount = true,
                ShowTaxSummaryTable = true,
                ShowBusinessGSTIN = true,
                ShowItemDiscount = true,
                IsZebraStriped = true,
                ShowAmountInWords = true,
                ShowPaymentQR = false,
                ThermalPaperSize = paperSize == "Thermal" ? 80 : null,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private InvoiceThemeSetting CloneThemeForPaper(InvoiceThemeSetting source, string userId, string paperSize)
        {
            return new InvoiceThemeSetting
            {
                ApplicationUserId = userId,
                PaperSize = paperSize,
                IsDefault = false,
                TenantId = source.TenantId,
                TemplateName = source.TemplateName,
                PrimaryColor = source.PrimaryColor,
                TextColor = source.TextColor,
                FontFamily = paperSize == "Thermal"
                    ? (string.IsNullOrWhiteSpace(source.FontFamily) ? "'Courier New', monospace" : source.FontFamily)
                    : source.FontFamily,
                BaseFontSize = source.BaseFontSize,
                Title = source.Title,
                Margins = source.Margins,
                Orientation = paperSize == "Thermal" ? "Portrait" : source.Orientation,
                LogoPath = source.LogoPath,
                LogoSize = source.LogoSize,
                LogoPosition = source.LogoPosition,
                WatermarkPath = source.WatermarkPath,
                WatermarkOpacity = source.WatermarkOpacity,
                WatermarkSize = source.WatermarkSize,
                SignaturePath = source.SignaturePath,
                SignatoryLabel = source.SignatoryLabel,
                ShowHSN = source.ShowHSN,
                ShowGSTPercent = source.ShowGSTPercent,
                ShowGSTAmount = source.ShowGSTAmount,
                ShowTaxSummaryTable = source.ShowTaxSummaryTable,
                ShowBusinessGSTIN = source.ShowBusinessGSTIN,
                ShowItemDiscount = source.ShowItemDiscount,
                IsZebraStriped = source.IsZebraStriped,
                ShowAmountInWords = source.ShowAmountInWords,
                BankDetails = source.BankDetails,
                TermsAndConditions = source.TermsAndConditions,
                ShowPaymentQR = source.ShowPaymentQR,
                UPIDetails = source.UPIDetails,
                ThermalPaperSize = paperSize == "Thermal" ? NormalizeThermalPaperSize(source.ThermalPaperSize) : null,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private async Task<string?> SaveAssetIfUploadedAsync(
             IFormFile? file,
             string? existingPath,
             string userId,
             string paperSize,
             string kind)
        {
            // ???? IFormFile ?? ???????? ?? ??? ??
            if (file == null || file.Length == 0)
            {
                return existingPath;
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImageExtensions.Contains(extension))
            {
                throw new Exception($"{kind} file type invalid. Allowed: png, jpg, jpeg, webp.");
            }

            long maxSize = kind switch
            {
                "watermark" => MaxWatermarkBytes,
                "signature" => MaxSignatureBytes,
                _ => MaxLogoBytes
            };

            if (file.Length > maxSize)
            {
                throw new Exception($"{kind} file is too large.");
            }
            var folder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "invoice_themes");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            var fileName = $"{kind}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var absolutePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            DeleteManagedAsset(existingPath);
            return $"/uploads/invoice_themes/{fileName}";
        }

        private void DeleteManagedAsset(string? existingPath)
        {
            if (string.IsNullOrWhiteSpace(existingPath))
            {
                return;
            }

            try
            {
                var cleanPath = existingPath.Split('?', '#')[0];
                if (!cleanPath.StartsWith("/uploads/invoice_themes/", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var relativePath = cleanPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                if (System.IO.File.Exists(absolutePath))
                {
                    System.IO.File.Delete(absolutePath);
                }
            }
            catch
            {
                // Old file cleanup failure should not break save flow.
            }
        }
    }
}