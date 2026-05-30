using AOne.DataAccess.ProfileService;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Service.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes =JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly ITenantRepository _tenantRepo;
        private readonly UserManager<ApplicationUsers> _userManager;
        private readonly UserLoginAuthService _userLoginAuthService;
        public ProfileController(
            IProfileService profileService,
            ITenantRepository tenantRepo,
            UserManager<ApplicationUsers> userManager,
            UserLoginAuthService userLoginAuthService)
        {
            _profileService = profileService;
            _tenantRepo = tenantRepo;
            _userManager = userManager;
            _userLoginAuthService = userLoginAuthService;
        }
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            await _profileService.Set(User);
            var profile = _profileService.Profile;

            if (profile == null || profile.UserId == null)
                return NotFound(new { message = "Profile not found" });

            var tenantdata = await _tenantRepo.GetById(profile.TenantId);
            if (tenantdata != null && !string.IsNullOrEmpty(tenantdata.Logo))
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                profile.Avatar = $"{baseUrl}/{tenantdata.Logo}".Replace("\\", "/");
            }
            profile.TenantName = tenantdata?.Name ?? "";
            profile.BusinessType = tenantdata?.BusinessType;
            var user = await _userManager.FindByIdAsync(profile.UserId);
            var setupStatus = user == null
                ? new ProfileSetupStatusViewModel()
                : await _userLoginAuthService.GetProfileSetupStatusAsync(user);

            return Ok(new { profile, setupStatus });
        }
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] TenantRegistrationVM model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
             
            await _profileService.Set(User);
            var profile = _profileService.Profile;

            if (profile == null || profile.UserId == null)
                return NotFound(new { message = "Profile not found" });
             
            var tenantdata = await _tenantRepo.GetById(profile.TenantId);
            tenantdata.Name = model.Name;
            tenantdata.Phone = model.Phone;
            tenantdata.Address1 = model.Address1;
             
            if (model.Logo != null && model.Logo.Length > 0)
            {

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Tenant");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = model.UploadAttachement.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    model.UploadAttachement.CopyTo(fileStream);
                }
                tenantdata.Logo = $"docs/Tenant/{fileName}";

            }
            await _tenantRepo.Update(tenantdata);

            return Ok(new
            {
                message = "Profile updated successfully",
                profile
            });
        }

        [HttpPut("setup-security")]
        public async Task<IActionResult> SetupSecurity([FromBody] CompleteProfileSetupRequest request)
        {
            await _profileService.Set(User);
            var profile = _profileService.Profile;
            if (profile == null || string.IsNullOrWhiteSpace(profile.UserId))
                return NotFound(new { message = "Profile not found" });

            var user = await _userManager.FindByIdAsync(profile.UserId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var setupResult = await _userLoginAuthService.SetProfileCredentialsAsync(user, request);
            if (!setupResult.Success)
                return BadRequest(new { message = setupResult.Message });

            return Ok(new
            {
                message = setupResult.Message,
                setupStatus = setupResult.SetupStatus
            });
        }
    }
}
