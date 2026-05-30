using AOne.Utility.Enums;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class SalseSettingApiController : ControllerBase
    {
        private readonly ISalseSettingRepository _salsesettingservice;
        private readonly ITenantRegistrationRepository _tenantRepository;

        public SalseSettingApiController(
            ISalseSettingRepository salsesettingservice,
            ITenantRegistrationRepository tenantRepository)
        {
            _salsesettingservice = salsesettingservice;
            _tenantRepository = tenantRepository;
        }

        [HttpGet("GetSettings")]
        public async Task<IActionResult> GetSettings()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var setting = await _salsesettingservice.GetByUserId(userId);

            var model = new SalseSettingVM
            {
                Id = setting.Id,
                ApplicationUserId = userId,
                DoctorRequired = setting?.DoctorRequired ?? false,
                ItemBarCodeBase = setting?.ItemBarCodeBase ?? false,
                ItemConversion = setting?.ItemConversion,
                RateRoundUp = setting?.RateRoundUp ?? false,
                AllowNegative = setting?.AllowNegative ?? false,
                ExpiryAllowedDays = setting?.ExpiryAllowedDays ?? 0,
                PrintType = setting?.PrintType ?? "normal",
                ThermalPaperSize = setting?.ThermalPaperSize ?? 80,
                IsNotConfigured = setting != null,
                SalesTax = setting?.SalesTax ?? AOne.Utility.Enums.SalesTax.Exclusive,
                showdiscount = setting?.showdiscount ?? false
            };

            return Ok(model);
        }

        // =========================
        // SAVE / UPDATE API
        // =========================
        [HttpPost("SaveSettings")]
        public async Task<IActionResult> SaveSettings([FromBody] SalseSettingVM viewModel)
        {
            if (viewModel == null)
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid data"
                });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var tenantId = User?.FindFirst("TenantId")?.Value;

            var tenant = (await _tenantRepository.GetAll())
                .FirstOrDefault(x => x.Id == tenantId);

            var businessType = tenant?.BusinessType ?? 0;

            var existing = await _salsesettingservice.GetByUserId(userId);

            if (existing == null)
            {
                var newSetting = new SalseSetting
                {
                    ApplicationUserId = userId,
                    DoctorRequired = viewModel.DoctorRequired,
                    ItemBarCodeBase = viewModel.ItemBarCodeBase,
                    ItemConversion = (int)businessType == 2
                        ? "TabletWise"
                        : viewModel.ItemConversion,

                    RateRoundUp = viewModel.RateRoundUp,
                    AllowNegative = viewModel.AllowNegative,
                    ExpiryAllowedDays = viewModel.ExpiryAllowedDays,
                    PrintType = viewModel.PrintType,

                    ThermalPaperSize = viewModel.PrintType == "thermal"
                        ? viewModel.ThermalPaperSize
                        : null,

                    SalesTax = viewModel.SalesTax,
                    showdiscount = viewModel.showdiscount
                };

                await _salsesettingservice.Create(newSetting);

                return Ok(new
                {
                    success = true,
                    message = "Settings saved successfully"
                });
            }

            // UPDATE
            existing.DoctorRequired = viewModel.DoctorRequired;
            existing.ItemBarCodeBase = viewModel.ItemBarCodeBase;
            existing.ItemConversion = viewModel.ItemConversion;
            existing.RateRoundUp = viewModel.RateRoundUp;
            existing.AllowNegative = viewModel.AllowNegative;
            existing.ExpiryAllowedDays = viewModel.ExpiryAllowedDays;
            existing.PrintType = viewModel.PrintType;

            existing.ThermalPaperSize = viewModel.PrintType == "thermal"
                ? viewModel.ThermalPaperSize
                : null;

            existing.SalesTax = viewModel.SalesTax;
            existing.showdiscount = viewModel.showdiscount;

            await _salsesettingservice.Update(existing);

            return Ok(new
            {
                success = true,
                message = "Settings updated successfully"
            });
        }

        //[HttpGet]
        //public async Task<IActionResult> GetAll()
        //{
        //    var currentUserId = GetCurrentUserId();
        //    if (string.IsNullOrWhiteSpace(currentUserId))
        //        return Unauthorized(new { success = false, message = "User not authorized." });

        //    var settings = await _salsesettingservice.GetAll();
        //    var data = settings
        //        .Where(CanAccessSetting)
        //        .Select(x => MapToResponse(x, false))
        //        .OrderBy(x => x.Id)
        //        .ToList();

        //    return Ok(new
        //    {
        //        success = true,
        //        count = data.Count,
        //        data
        //    });
        //}

        //[HttpGet("{id:int}")]
        //public async Task<IActionResult> GetById(int id)
        //{
        //    var currentUserId = GetCurrentUserId();
        //    if (string.IsNullOrWhiteSpace(currentUserId))
        //        return Unauthorized(new { success = false, message = "User not authorized." });

        //    var setting = await _salsesettingservice.GetById(id);
        //    if (setting == null)
        //        return NotFound(new { success = false, message = "Sales setting not found." });

        //    if (!CanAccessSetting(setting))
        //        return Forbid();

        //    return Ok(new
        //    {
        //        success = true,
        //        data = MapToResponse(setting, false)
        //    });
        //}

        //[HttpGet("GetSidebarSettings")]
        //public async Task<IActionResult> GetSidebarSettings()
        //{
        //    var currentUserId = GetCurrentUserId();
        //    if (string.IsNullOrWhiteSpace(currentUserId))
        //        return Unauthorized(new { success = false, message = "User not authorized." });

        //    var businessType = await GetCurrentBusinessTypeAsync();
        //    var setting = await _salsesettingservice.GetByUserId(currentUserId);

        //    if (setting == null)
        //    {
        //        var defaultModel = new SalseSettingApiResponseVM
        //        {
        //            ApplicationUserId = currentUserId,
        //            DoctorRequired = false,
        //            ItemBarCodeBase = false,
        //            ItemConversion = ResolveItemConversion(null, businessType),
        //            RateRoundUp = false,
        //            AllowNegative = false,
        //            ExpiryAllowedDays = 0,
        //            PrintType = "normal",
        //            ThermalPaperSize = 80,
        //            GstType = 0,
        //            GstTypeName = GetGstTypeName(0),
        //            IsNotConfigured = true
        //        };

        //        return Ok(new { success = true, data = defaultModel });
        //    }

        //    return Ok(new
        //    {
        //        success = true,
        //        data = MapToResponse(setting, false)
        //    });
        //}

        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] SalseSettingApiRequestVM request)
        //{
        //    var validationError = ValidateRequest(request, requireGstType: true);
        //    if (validationError != null)
        //        return BadRequest(new { success = false, message = validationError });

        //    var currentUserId = GetCurrentUserId();
        //    if (string.IsNullOrWhiteSpace(currentUserId))
        //        return Unauthorized(new { success = false, message = "User not authorized." });

        //    var existing = await _salsesettingservice.GetByUserId(currentUserId);
        //    if (existing != null)
        //    {
        //        return Conflict(new
        //        {
        //            success = false,
        //            message = "Sales setting already exists for this user. Please use edit API."
        //        });
        //    }

        //    var businessType = await GetCurrentBusinessTypeAsync();
        //    var setting = new SalseSetting
        //    {
        //        ApplicationUserId = currentUserId
        //    };

        //    ApplyRequestToEntity(setting, request, businessType, keepExistingGstType: false);
        //    await _salsesettingservice.Create(setting);

        //    return Ok(new
        //    {
        //        success = true,
        //        message = "Sales setting created successfully.",
        //        data = MapToResponse(setting, false)
        //    });
        //}

        //[HttpPut("{id:int}")]
        //public async Task<IActionResult> Edit(int id, [FromBody] SalseSettingApiRequestVM request)
        //{
        //    var validationError = ValidateRequest(request, requireGstType: true);
        //    if (validationError != null)
        //        return BadRequest(new { success = false, message = validationError });

        //    var currentUserId = GetCurrentUserId();
        //    if (string.IsNullOrWhiteSpace(currentUserId))
        //        return Unauthorized(new { success = false, message = "User not authorized." });

        //    var existing = await _salsesettingservice.GetById(id);
        //    if (existing == null)
        //        return NotFound(new { success = false, message = "Sales setting not found." });

        //    if (!CanAccessSetting(existing))
        //        return Forbid();

        //    var businessType = await GetCurrentBusinessTypeAsync();
        //    ApplyRequestToEntity(existing, request, businessType, keepExistingGstType: false);

        //    await _salsesettingservice.Update(existing);

        //    return Ok(new
        //    {
        //        success = true,
        //        message = "Sales setting updated successfully.",
        //        data = MapToResponse(existing, false)
        //    });
        //}

        //[HttpPost("SaveSidebarSettings")]
        //public async Task<IActionResult> SaveSidebarSettings([FromBody] SalseSettingApiRequestVM request)
        //{
        //    var validationError = ValidateRequest(request, requireGstType: false);
        //    if (validationError != null)
        //        return BadRequest(new { success = false, message = validationError });

        //    var currentUserId = GetCurrentUserId();
        //    if (string.IsNullOrWhiteSpace(currentUserId))
        //        return Unauthorized(new { success = false, message = "User not authorized." });

        //    var businessType = await GetCurrentBusinessTypeAsync();
        //    var existing = await _salsesettingservice.GetByUserId(currentUserId);

        //    if (existing == null)
        //    {
        //        var setting = new SalseSetting
        //        {
        //            ApplicationUserId = currentUserId
        //        };

        //        ApplyRequestToEntity(setting, request, businessType, keepExistingGstType: false);
        //        await _salsesettingservice.Create(setting);

        //        return Ok(new
        //        {
        //            success = true,
        //            message = "Sales setting created successfully.",
        //            data = MapToResponse(setting, false)
        //        });
        //    }

        //    if (!CanAccessSetting(existing))
        //        return Forbid();

        //    ApplyRequestToEntity(existing, request, businessType, keepExistingGstType: true);
        //    await _salsesettingservice.Update(existing);

        //    return Ok(new
        //    {
        //        success = true,
        //        message = "Sales setting saved successfully.",
        //        data = MapToResponse(existing, false)
        //    });
        //}

        //private string? GetCurrentUserId()
        //{
        //    return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //}

        //private string? GetCurrentTenantId()
        //{
        //    return User.FindFirst("TenantId")?.Value;
        //}

        //private bool IsPrivilegedUser()
        //{
        //    var role = User.FindFirst(ClaimTypes.Role)?.Value;
        //    return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
        //        || string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase)
        //        || string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        //}

        //private bool CanAccessSetting(SalseSetting setting)
        //{
        //    var currentUserId = GetCurrentUserId();
        //    if (string.IsNullOrWhiteSpace(currentUserId))
        //        return false;

        //    if (string.Equals(setting.ApplicationUserId, currentUserId, StringComparison.OrdinalIgnoreCase))
        //        return true;

        //    var currentTenantId = GetCurrentTenantId();
        //    return IsPrivilegedUser()
        //        && !string.IsNullOrWhiteSpace(currentTenantId)
        //        && string.Equals(setting.TenantId, currentTenantId, StringComparison.OrdinalIgnoreCase);
        //}

        //private async Task<int> GetCurrentBusinessTypeAsync()
        //{
        //    var tenantId = GetCurrentTenantId();
        //    if (string.IsNullOrWhiteSpace(tenantId))
        //        return 0;

        //    var tenant = await _tenantRepository.GetById(tenantId);
        //    return tenant == null ? 0 : (int)tenant.BusinessType;
        //}

        //private static string? ValidateRequest(SalseSettingApiRequestVM? request, bool requireGstType)
        //{
        //    if (request == null)
        //        return "Request body is required.";

        //    if (request.ExpiryAllowedDays < 0 || request.ExpiryAllowedDays > 365)
        //        return "ExpiryAllowedDays must be between 0 and 365.";

        //    if (!string.IsNullOrWhiteSpace(request.ItemConversion)
        //        && !string.Equals(request.ItemConversion, "TabletWise", StringComparison.OrdinalIgnoreCase)
        //        && !string.Equals(request.ItemConversion, "StripWise", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return "ItemConversion must be either 'TabletWise' or 'StripWise'.";
        //    }

        //    if (!string.IsNullOrWhiteSpace(request.PrintType)
        //        && !string.Equals(request.PrintType, "normal", StringComparison.OrdinalIgnoreCase)
        //        && !string.Equals(request.PrintType, "thermal", StringComparison.OrdinalIgnoreCase))
        //    {
        //        return "PrintType must be either 'normal' or 'thermal'.";
        //    }

        //    if (request.ThermalPaperSize.HasValue
        //        && request.ThermalPaperSize != 58
        //        && request.ThermalPaperSize != 80)
        //    {
        //        return "ThermalPaperSize must be either 58 or 80.";
        //    }

        //    if (requireGstType && !request.GstType.HasValue)
        //        return "GstType is required. Use 0 for Inclusive and 1 for Exclusive.";

        //    if (request.GstType.HasValue && request.GstType != 0 && request.GstType != 1)
        //        return "GstType must be 0 for Inclusive or 1 for Exclusive.";

        //    return null;
        //}

        //private void ApplyRequestToEntity(
        //    SalseSetting entity,
        //    SalseSettingApiRequestVM request,
        //    int businessType,
        //    bool keepExistingGstType)
        //{
        //    var printType = NormalizePrintType(request.PrintType);

        //    entity.DoctorRequired = request.DoctorRequired;
        //    entity.ItemBarCodeBase = request.ItemBarCodeBase;
        //    entity.ItemConversion = ResolveItemConversion(request.ItemConversion, businessType);
        //    entity.RateRoundUp = request.RateRoundUp;
        //    entity.AllowNegative = request.AllowNegative;
        //    entity.ExpiryAllowedDays = request.ExpiryAllowedDays;
        //    entity.PrintType = printType;
        //    entity.ThermalPaperSize = ResolveThermalPaperSize(printType, request.ThermalPaperSize);

        //    if (request.GstType.HasValue)
        //    {
        //        entity.SalesTax = MapApiGstTypeToSalesTax(request.GstType.Value);
        //    }
        //    else if (!keepExistingGstType)
        //    {
        //        entity.SalesTax = SalesTax.Inclusive;
        //    }
        //}

        //private static SalseSettingApiResponseVM MapToResponse(SalseSetting setting, bool isNotConfigured)
        //{
        //    var gstType = MapSalesTaxToApiGstType(setting.SalesTax);

        //    return new SalseSettingApiResponseVM
        //    {
        //        Id = setting.Id,
        //        ApplicationUserId = setting.ApplicationUserId,
        //        DoctorRequired = setting.DoctorRequired,
        //        ItemBarCodeBase = setting.ItemBarCodeBase,
        //        ItemConversion = string.IsNullOrWhiteSpace(setting.ItemConversion) ? "StripWise" : setting.ItemConversion,
        //        RateRoundUp = setting.RateRoundUp,
        //        AllowNegative = setting.AllowNegative,
        //        ExpiryAllowedDays = setting.ExpiryAllowedDays,
        //        PrintType = string.IsNullOrWhiteSpace(setting.PrintType) ? "normal" : setting.PrintType,
        //        ThermalPaperSize = setting.ThermalPaperSize,
        //        GstType = gstType,
        //        GstTypeName = GetGstTypeName(gstType),
        //        IsNotConfigured = isNotConfigured,
        //        showdiscount = setting.showdiscount,
        //        SalesTax = setting.SalesTax
        //    };
        //}

        //private static SalesTax MapApiGstTypeToSalesTax(int gstType)
        //{
        //    return gstType == 1 ? SalesTax.Exclusive : SalesTax.Inclusive;
        //}

        //private static int MapSalesTaxToApiGstType(SalesTax salesTax)
        //{
        //    return salesTax == SalesTax.Exclusive ? 1 : 0;
        //}

        //private static string GetGstTypeName(int gstType)
        //{
        //    return gstType == 1 ? "Exclusive" : "Inclusive";
        //}

        //private static string NormalizePrintType(string? printType)
        //{
        //    return string.Equals(printType, "thermal", StringComparison.OrdinalIgnoreCase)
        //        ? "thermal"
        //        : "normal";
        //}

        //private static int? ResolveThermalPaperSize(string printType, int? thermalPaperSize)
        //{
        //    if (!string.Equals(printType, "thermal", StringComparison.OrdinalIgnoreCase))
        //        return null;

        //    return thermalPaperSize == 58 ? 58 : 80;
        //}

        //private static string ResolveItemConversion(string? itemConversion, int businessType)
        //{
        //    if (businessType == 2)
        //        return "TabletWise";

        //    return string.Equals(itemConversion, "TabletWise", StringComparison.OrdinalIgnoreCase)
        //        ? "TabletWise"
        //        : "StripWise";
        //}
    }
}
