using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBill.UI.Controllers
{
    public class LoyaltySettingController : Controller
    {
        private readonly IPointSettingRepository _pointSettingRepository;

        public LoyaltySettingController(IPointSettingRepository pointSettingRepository)
        {
            _pointSettingRepository = pointSettingRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var settingsList = await _pointSettingRepository.GetAll();
            var setting = settingsList.OrderByDescending(x => x.Id).FirstOrDefault();

            if (setting == null)
            {
                return Json(new PointSettingVm
                {
                    Id = 0,
                    MinAmountToEarn = 100,
                    EarnPerAmount = 100,
                    PointValueInRs = 1,
                    AllowRedemption = true,
                    MinPointsToRedeem = 50,
                    EarningMultiplier = 1.0m,
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddYears(1),
                    IsActive = false
                });
            }

            return Json(new PointSettingVm
            {
                Id = setting.Id,
                MinAmountToEarn = setting.MinAmountToEarn,
                EarnPerAmount = setting.EarnPerAmount,
                PointValueInRs = setting.PointValueInRs,
                AllowRedemption = setting.AllowRedemption,
                MinPointsToRedeem = setting.MinPointsToRedeem,
                EarningMultiplier = setting.EarningMultiplier,
                StartDate = setting.StartDate,
                EndDate = setting.EndDate,
                IsActive = setting.IsActive
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] PointSettingVm model)
        {
            if (model == null)
            {
                return BadRequest("Invalid settings data.");
            }

            if (model.Id > 0)
            {
                var existing = await _pointSettingRepository.GetById(model.Id);
                if (existing != null)
                {
                    existing.MinAmountToEarn = model.MinAmountToEarn;
                    existing.EarnPerAmount = model.EarnPerAmount;
                    existing.PointValueInRs = model.PointValueInRs;
                    existing.AllowRedemption = model.AllowRedemption;
                    existing.MinPointsToRedeem = model.MinPointsToRedeem;
                    existing.EarningMultiplier = model.EarningMultiplier;
                    existing.StartDate = model.StartDate;
                    existing.EndDate = model.EndDate;
                    existing.IsActive = model.IsActive;

                    await _pointSettingRepository.Update(existing);
                    return Ok(new { success = true, message = "Settings updated successfully." });
                }
            }

            var newSetting = new PointSetting
            {
                MinAmountToEarn = model.MinAmountToEarn,
                EarnPerAmount = model.EarnPerAmount,
                PointValueInRs = model.PointValueInRs,
                AllowRedemption = model.AllowRedemption,
                MinPointsToRedeem = model.MinPointsToRedeem,
                EarningMultiplier = model.EarningMultiplier,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsActive = model.IsActive
            };

            await _pointSettingRepository.Create(newSetting);
            return Ok(new { success = true, message = "Settings created successfully." });
        }
    }
}
