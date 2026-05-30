using AOne.DataAccess.ProfileService;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Claims;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceThemeApiController : ControllerBase
    {
        private readonly IInvoiceThemeSettingRepository _themeRepo;
        private readonly IWebHostEnvironment _environment;
        private readonly IProfileService _profileService;

        public InvoiceThemeApiController(
            IInvoiceThemeSettingRepository themeRepo,
            IWebHostEnvironment environment,
            IProfileService profileservice)
        {
            _themeRepo = themeRepo;
            _environment = environment;
            _profileService = profileservice;
        }

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".png",
                ".jpg",
                ".jpeg",
                ".webp"
            };

        private const long MaxLogoSize = 2 * 1024 * 1024;
        private const long MaxWatermarkSize = 3 * 1024 * 1024;
        private const long MaxSignatureSize = 2 * 1024 * 1024;


        [HttpGet("GetInvoiceDesign")]
        public async Task<IActionResult> GetInvoiceDesign()
        {
            await _profileService.Set(User);
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // DEFAULT THEME GET
            var theme = (await _themeRepo.GetAllThemesByUserIdAsync(userId)).FirstOrDefault();
                //.FirstOrDefault(x =>
                //    x.ApplicationUserId == userId &&
                //    x.IsDefault);

            // AGAR DEFAULT NA MILE
            if (theme == null)
            {
                theme = (await _themeRepo.GetAllThemesByUserIdAsync(userId)).FirstOrDefault();
                    //.FirstOrDefault(x =>
                    //    x.ApplicationUserId == userId);
            }

            var model = new InvoiceThemeSetting
            {
                Id = theme?.Id ?? 0,
                ApplicationUserId = userId,

                PaperSize = theme?.PaperSize ?? "A4",

                TemplateName = theme?.TemplateName ?? "Classic",
                PrimaryColor = theme?.PrimaryColor ?? "#2b6ac9",
                TextColor = theme?.TextColor ?? "#1e293b",

                FontFamily = theme?.FontFamily ?? "Arial",
                BaseFontSize = theme?.BaseFontSize ?? 12,

                Title = theme?.Title ?? "TAX INVOICE",

                Margins = theme?.Margins ?? "Normal",
                Orientation = theme?.Orientation ?? "Portrait",

                LogoPath = theme?.LogoPath,
                LogoSize = theme?.LogoSize ?? 100,
                LogoPosition = theme?.LogoPosition ?? "Left",

                WatermarkPath = theme?.WatermarkPath,
                WatermarkOpacity = theme?.WatermarkOpacity ?? 15,
                WatermarkSize = theme?.WatermarkSize ?? 180,

                SignaturePath = theme?.SignaturePath,
                SignatoryLabel = theme?.SignatoryLabel ?? "Auth. Signatory",

                ShowHSN = theme?.ShowHSN ?? false,
                ShowGSTPercent = theme?.ShowGSTPercent ?? false,
                ShowGSTAmount = theme?.ShowGSTAmount ?? false,
                ShowTaxSummaryTable = theme?.ShowTaxSummaryTable ?? false,
                ShowBusinessGSTIN = theme?.ShowBusinessGSTIN ?? false,
                ShowItemDiscount = theme?.ShowItemDiscount ?? false,
                IsZebraStriped = theme?.IsZebraStriped ?? false,
                ShowAmountInWords = theme?.ShowAmountInWords ?? false,
                ShowPaymentQR = theme?.ShowPaymentQR ?? false,

                BankDetails = theme?.BankDetails,
                TermsAndConditions = theme?.TermsAndConditions,
                UPIDetails = theme?.UPIDetails,

                ThermalPaperSize = theme?.ThermalPaperSize ?? 80,

                IsDefault = theme?.IsDefault ?? false,
                UpdatedAt = theme?.UpdatedAt ?? DateTime.UtcNow
            };

            return Ok(model);
        }

        // =========================
        // SAVE / UPDATE THEME
        // =========================
        [HttpPost("SaveTheme")]
        public async Task<IActionResult> SaveTheme([FromForm] InvoiceThemeSettingVM model, IFormFile? logoFile, IFormFile? watermarkFile, IFormFile? signatureFile)
        {
            await _profileService.Set(User);
            if (model == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid data"
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var existing = await _themeRepo
                .GetThemeSettingAsync(userId, model.PaperSize);

            // =========================
            // CREATE
            // =========================
            if (existing == null)
            {
                var newTheme = new InvoiceThemeSetting
                {
                    ApplicationUserId = userId,
                    PaperSize = model.PaperSize,
                    IsDefault = model.IsDefault,

                    TemplateName = model.TemplateName,
                    PrimaryColor = model.PrimaryColor,
                    TextColor = model.TextColor,
                    FontFamily = model.FontFamily,
                    BaseFontSize = model.BaseFontSize,

                    Title = model.Title,
                    Margins = model.Margins,
                    Orientation = model.Orientation,

                    LogoSize = model.LogoSize,
                    LogoPosition = model.LogoPosition,

                    WatermarkOpacity = model.WatermarkOpacity,
                    WatermarkSize = model.WatermarkSize,

                    SignatoryLabel = model.SignatoryLabel,

                    ShowHSN = model.ShowHSN,
                    ShowGSTPercent = model.ShowGSTPercent,
                    ShowGSTAmount = model.ShowGSTAmount,
                    ShowTaxSummaryTable = model.ShowTaxSummaryTable,
                    ShowBusinessGSTIN = model.ShowBusinessGSTIN,
                    ShowItemDiscount = model.ShowItemDiscount,
                    IsZebraStriped = model.IsZebraStriped,
                    ShowAmountInWords = model.ShowAmountInWords,

                    BankDetails = model.BankDetails,
                    TermsAndConditions = model.TermsAndConditions,
                    ShowPaymentQR = model.ShowPaymentQR,
                    UPIDetails = model.UPIDetails,

                    ThermalPaperSize = model.ThermalPaperSize,
                    UpdatedAt = DateTime.UtcNow
                };

                // FILES
                if (logoFile != null)
                    newTheme.LogoPath = await SaveFile(logoFile);

                if (watermarkFile != null)
                    newTheme.WatermarkPath = await SaveFile(watermarkFile);

                if (signatureFile != null)
                    newTheme.SignaturePath = await SaveFile(signatureFile);

                await _themeRepo.SaveOrUpdateThemeAsync(newTheme);

                return Ok(new
                {
                    success = true,
                    message = "Theme saved successfully"
                });
            }

            // =========================
            // UPDATE
            // =========================
            existing.PaperSize = model.PaperSize;
            existing.IsDefault = model.IsDefault;
            existing.TemplateName = model.TemplateName;
            existing.PrimaryColor = model.PrimaryColor;
            existing.TextColor = model.TextColor;
            existing.FontFamily = model.FontFamily;
            existing.BaseFontSize = model.BaseFontSize;

            existing.Title = model.Title;
            existing.Margins = model.Margins;
            existing.Orientation = model.Orientation;

            existing.LogoSize = model.LogoSize;
            existing.LogoPosition = model.LogoPosition;

            existing.WatermarkOpacity = model.WatermarkOpacity;
            existing.WatermarkSize = model.WatermarkSize;

            existing.SignatoryLabel = model.SignatoryLabel;

            existing.ShowHSN = model.ShowHSN;
            existing.ShowGSTPercent = model.ShowGSTPercent;
            existing.ShowGSTAmount = model.ShowGSTAmount;
            existing.ShowTaxSummaryTable = model.ShowTaxSummaryTable;
            existing.ShowBusinessGSTIN = model.ShowBusinessGSTIN;
            existing.ShowItemDiscount = model.ShowItemDiscount;
            existing.IsZebraStriped = model.IsZebraStriped;
            existing.ShowAmountInWords = model.ShowAmountInWords;

            existing.BankDetails = model.BankDetails;
            existing.TermsAndConditions = model.TermsAndConditions;
            existing.ShowPaymentQR = model.ShowPaymentQR;
            existing.UPIDetails = model.UPIDetails;

            existing.ThermalPaperSize = model.ThermalPaperSize;
            existing.IsDefault = model.IsDefault;

            existing.UpdatedAt = DateTime.UtcNow;

            // FILES
            if (logoFile != null)
                existing.LogoPath = await SaveFile(logoFile);

            if (watermarkFile != null)
                existing.WatermarkPath = await SaveFile(watermarkFile);

            if (signatureFile != null)
                existing.SignaturePath = await SaveFile(signatureFile);

            await _themeRepo.SaveOrUpdateThemeAsync(existing);

            return Ok(new
            {
                success = true,
                message = "Theme updated successfully"
            });
        }

        // =========================
        // FILE SAVE
        // =========================
        private async Task<string> SaveFile(IFormFile file)
        {
            var folder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/uploads/invoice");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName =
                Guid.NewGuid() +
                Path.GetExtension(file.FileName);

            var path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/uploads/invoice/" + fileName;
        }

        // =====================================================
        // GET THEME
        // =====================================================
        //[HttpGet("GetTheme")]
        //public async Task<IActionResult> GetTheme(string paperSize = "A4")
        //{
        //    var userId = GetUserId();

        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized(ApiResponse(false, "User not found"));

        //    paperSize = NormalizePaperSize(paperSize);

        //    var theme = await _themeRepo
        //        .GetThemeSettingAsync(userId, paperSize);

        //    return Ok(ApiResponse(true, "Success", theme));
        //}

        //[HttpGet("GetInvoiceDesign")]
        //public async Task<IActionResult> GetInvoiceDesign(string paperSize = "A4")
        //{
        //    return await GetTheme(paperSize);
        //}

        // =====================================================
        // SAVE THEME
        // =====================================================
        //[HttpPost("SaveTheme")]
        //public async Task<IActionResult> SaveTheme(
        //    [FromForm] InvoiceThemeSetting model,
        //    IFormFile? logoFile,
        //    IFormFile? watermarkFile,
        //    IFormFile? signatureFile)
        //{
        //    try
        //    {
        //        var userId = GetUserId();

        //        if (string.IsNullOrEmpty(userId))
        //            return Unauthorized(ApiResponse(false, "User not logged in"));

        //        var paperSize = NormalizePaperSize(model.PaperSize);

        //        var entity = await _themeRepo
        //            .GetThemeSettingAsync(userId, paperSize)
        //            ?? CreateDefaultTheme(userId, paperSize);

        //        MapThemeProperties(entity, model, paperSize);

        //        entity.LogoPath = await SaveFileAsync(
        //            logoFile,
        //            entity.LogoPath,
        //            "logo",
        //            MaxLogoSize);

        //        entity.WatermarkPath = await SaveFileAsync(
        //            watermarkFile,
        //            entity.WatermarkPath,
        //            "watermark",
        //            MaxWatermarkSize);

        //        entity.SignaturePath = await SaveFileAsync(
        //            signatureFile,
        //            entity.SignaturePath,
        //            "signature",
        //            MaxSignatureSize);

        //        entity.UpdatedAt = DateTime.UtcNow;

        //        await _themeRepo.SaveOrUpdateThemeAsync(entity);

        //        return Ok(ApiResponse(
        //            true,
        //            "Theme saved successfully",
        //            entity));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ApiResponse(false, ex.Message));
        //    }
        //}

        //[HttpPost("SaveInvoiceDesign")]
        //public async Task<IActionResult> SaveInvoiceDesign(
        //    [FromForm] InvoiceThemeSetting model,
        //    IFormFile? logoFile,
        //    IFormFile? watermarkFile,
        //    IFormFile? signatureFile)
        //{
        //    return await SaveTheme(model, logoFile, watermarkFile, signatureFile);
        //}

        //[HttpPut("UpdateInvoiceDesign")]
        //public async Task<IActionResult> UpdateInvoiceDesign(
        //    [FromForm] InvoiceThemeSetting model,
        //    IFormFile? logoFile,
        //    IFormFile? watermarkFile,
        //    IFormFile? signatureFile)
        //{
        //    try
        //    {
        //        var userId = GetUserId();

        //        if (string.IsNullOrEmpty(userId))
        //            return Unauthorized(ApiResponse(false, "User not logged in"));

        //        var paperSize = NormalizePaperSize(model.PaperSize);

        //        var entity = await _themeRepo.GetThemeSettingAsync(userId, paperSize);
        //        if (entity == null)
        //        {
        //            return NotFound(ApiResponse(false, "Invoice design not found. Please save first."));
        //        }

        //        MapThemeProperties(entity, model, paperSize);

        //        entity.LogoPath = await SaveFileAsync(
        //            logoFile,
        //            entity.LogoPath,
        //            "logo",
        //            MaxLogoSize);

        //        entity.WatermarkPath = await SaveFileAsync(
        //            watermarkFile,
        //            entity.WatermarkPath,
        //            "watermark",
        //            MaxWatermarkSize);

        //        entity.SignaturePath = await SaveFileAsync(
        //            signatureFile,
        //            entity.SignaturePath,
        //            "signature",
        //            MaxSignatureSize);

        //        entity.UpdatedAt = DateTime.UtcNow;

        //        await _themeRepo.SaveOrUpdateThemeAsync(entity);

        //        return Ok(ApiResponse(
        //            true,
        //            "Theme updated successfully",
        //            entity));
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ApiResponse(false, ex.Message));
        //    }
        //}

        // =====================================================
        // SET DEFAULT THEME
        // =====================================================

        //[HttpPost("SetDefaultTheme")]
        //public async Task<IActionResult> SetDefaultTheme(string paperSize)
        //{
        //    var userId = GetUserId();

        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized(ApiResponse(false, "User not logged in"));

        //    paperSize = NormalizePaperSize(paperSize);

        //    var theme = await _themeRepo
        //        .GetThemeSettingAsync(userId, paperSize);

        //    if (theme == null)
        //    {
        //        theme = CreateDefaultTheme(userId, paperSize);

        //        await _themeRepo.SaveOrUpdateThemeAsync(theme);
        //    }

        //    await _themeRepo.SetDefaultThemeAsync(userId, paperSize);

        //    return Ok(ApiResponse(
        //        true,
        //        $"{paperSize} set as default"));
        //}

        // =====================================================
        // COMMON METHODS
        // =====================================================

        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private object ApiResponse(
            bool success,
            string message,
            object? data = null)
        {
            return new
            {
                success,
                message,
                data
            };
        }

        private static string NormalizePaperSize(string? paperSize)
        {
            return paperSize?.Trim().ToUpper() switch
            {
                "A5" => "A5",
                "THERMAL" => "Thermal",
                _ => "A4"
            };
        }

        private static int? Clamp(
            int? value,
            int min,
            int max,
            int? fallback = null)
        {
            if (!value.HasValue)
                return fallback;

            return Math.Min(Math.Max(value.Value, min), max);
        }

        // =====================================================
        // MAP MODEL
        // =====================================================
        private void MapThemeProperties(
            InvoiceThemeSetting entity,
            InvoiceThemeSetting model,
            string paperSize)
        {
            entity.ApplicationUserId = entity.ApplicationUserId;
            entity.PaperSize = paperSize;

            entity.TemplateName = model.TemplateName;
            entity.PrimaryColor = model.PrimaryColor ?? "#2b6ac9";
            entity.TextColor = model.TextColor ?? "#1e293b";

            entity.FontFamily = string.IsNullOrWhiteSpace(model.FontFamily)
                ? "Arial, sans-serif"
                : model.FontFamily;

            entity.BaseFontSize = Clamp(model.BaseFontSize, 8, 24, 12);

            entity.Title = string.IsNullOrWhiteSpace(model.Title)
                ? "TAX INVOICE"
                : model.Title.Trim();

            entity.Margins = model.Margins ?? "Normal";

            entity.Orientation = string.IsNullOrWhiteSpace(model.Orientation)
                ? "Portrait"
                : model.Orientation;

            entity.LogoSize = Clamp(model.LogoSize, 40, 240);

            entity.LogoPosition = model.LogoPosition ?? "Left";

            entity.WatermarkOpacity =
                Clamp(model.WatermarkOpacity, 5, 40);

            entity.WatermarkSize =
                Clamp(model.WatermarkSize, 60, 600);

            entity.SignatoryLabel =
                string.IsNullOrWhiteSpace(model.SignatoryLabel)
                    ? "Auth. Signatory"
                    : model.SignatoryLabel.Trim();

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

            entity.ThermalPaperSize =
                paperSize == "Thermal"
                    ? model.ThermalPaperSize == 58 ? 58 : 80
                    : null;
        }

        // =====================================================
        // CREATE DEFAULT THEME
        // =====================================================
        private InvoiceThemeSetting CreateDefaultTheme(
            string userId,
            string paperSize)
        {
            return new InvoiceThemeSetting
            {
                ApplicationUserId = userId,
                PaperSize = paperSize,
                IsDefault = false,
                TemplateName = "Classic",
                PrimaryColor = "#2b6ac9",
                TextColor = "#1e293b",
                FontFamily = "Arial, sans-serif",
                BaseFontSize = 12,
                Title = "TAX INVOICE",
                Margins = "Normal",
                Orientation = "Portrait",
                LogoPosition = "Left",
                SignatoryLabel = "Auth. Signatory",
                UpdatedAt = DateTime.UtcNow
            };
        }

        // =====================================================
        // FILE UPLOAD
        // =====================================================
        //private async Task<string?> SaveFileAsync(
        //    IFormFile? file,
        //    string? oldPath,
        //    string fileType,
        //    long maxSize)
        //{
        //    if (file == null || file.Length == 0)
        //        return oldPath;

        //    var extension =
        //        Path.GetExtension(file.FileName);

        //    if (!AllowedExtensions.Contains(extension))
        //    {
        //        throw new Exception(
        //            $"{fileType} invalid file type");
        //    }

        //    if (file.Length > maxSize)
        //    {
        //        throw new Exception(
        //            $"{fileType} file too large");
        //    }

        //    DeleteOldFile(oldPath);

        //    var uploadsFolder = Path.Combine(
        //        _environment.WebRootPath,
        //        "uploads",
        //        "invoice_themes");

        //    if (!Directory.Exists(uploadsFolder))
        //    {
        //        Directory.CreateDirectory(uploadsFolder);
        //    }

        //    var fileName =
        //        $"{fileType}_{Guid.NewGuid():N}{extension}";

        //    var fullPath =
        //        Path.Combine(uploadsFolder, fileName);

        //    await using var stream =
        //        new FileStream(fullPath, FileMode.Create);

        //    await file.CopyToAsync(stream);

        //    return $"/uploads/invoice_themes/{fileName}";
        //}

        // =====================================================
        // DELETE OLD FILE
        // =====================================================
        private void DeleteOldFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                var cleanPath = path
                    .Replace("/", Path.DirectorySeparatorChar.ToString());

                var fullPath = Path.Combine(
                    _environment.WebRootPath,
                    cleanPath.TrimStart(Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch
            {
            }
        }
    }
}
