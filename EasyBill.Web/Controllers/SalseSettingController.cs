using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class SalseSettingController : Controller
    {
        private readonly ISalseSettingRepository _salsesettingservice;
        private readonly ITenantRegistrationRepository _tenantRepository;
        public SalseSettingController(ISalseSettingRepository salsesettingservice, ITenantRegistrationRepository tenantRepository)
        {
            _salsesettingservice = salsesettingservice;
            _tenantRepository = tenantRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetSidebarSettings()
        {
            // Find by userId (passed from JS)
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();
            var setting = await _salsesettingservice.GetByUserId(userId);

            // If not found, return default values (form will be filled accordingly)
            var model = new SalseSettingVM()
            {
                ApplicationUserId = userId,
                DoctorRequired = setting?.DoctorRequired ?? false,
                ItemBarCodeBase = setting?.ItemBarCodeBase ?? false,
                ItemConversion = setting?.ItemConversion,
                RateRoundUp = setting?.RateRoundUp ?? false,
                AllowNegative = setting?.AllowNegative ?? false,
                ExpiryAllowedDays = setting?.ExpiryAllowedDays ?? 0,
                PrintType = setting?.PrintType ?? "normal",
                ThermalPaperSize = setting?.ThermalPaperSize ?? 80,
                IsNotConfigured = setting != null ? true : false,
                SalesTax = setting?.SalesTax ?? AOne.Utility.Enums.SalesTax.Exclusive,
                showdiscount = setting?.showdiscount ?? false
            };

            return Json(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSidebarSettings([FromBody] SalseSettingVM viewModel)
        {
            if (viewModel == null || string.IsNullOrEmpty(viewModel.ApplicationUserId))
                return BadRequest("Invalid input");

            // Check bussiness type
            var tenantId = User?.FindFirst("TenantId")?.Value;
            var user = (await _tenantRepository.GetAll())
           .FirstOrDefault(x => x.Id == tenantId);
            var businessType = user?.BusinessType ?? 0;

            var existing = await _salsesettingservice.GetByUserId(viewModel.ApplicationUserId);

            if (existing == null)
            {
                // Create new
                var newSetting = new SalseSetting
                {
                    ApplicationUserId = viewModel.ApplicationUserId,
                    DoctorRequired = viewModel.DoctorRequired,
                    ItemBarCodeBase = viewModel.ItemBarCodeBase,
                    ItemConversion = (int)businessType == 2 ? "TabletWise" : viewModel.ItemConversion,
                    RateRoundUp = viewModel.RateRoundUp,
                    AllowNegative = viewModel.AllowNegative,
                    ExpiryAllowedDays = viewModel.ExpiryAllowedDays,
                    PrintType = viewModel.PrintType,
                    ThermalPaperSize = viewModel.PrintType == "thermal" ? viewModel.ThermalPaperSize: null,
                    SalesTax = viewModel.SalesTax,
                    showdiscount = viewModel.showdiscount
                };
                await _salsesettingservice.Create(newSetting);
            }
            else
            {
                // Update existing
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
            }

            return Ok();
        }

    }
}
