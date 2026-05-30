using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    
    public class PurchaseSettingController : Controller
    {
        private readonly IPurchaseSettingRepository _purchasesettingservice;
        public PurchaseSettingController(IPurchaseSettingRepository purchasesettingservice)
        {
            _purchasesettingservice = purchasesettingservice;
        }

        [HttpGet]
        public async Task<IActionResult> GetSidebarSettings( )
        {
            Console.WriteLine("🔥 GetSidebarSettings HIT");

            // Find by userId (passed from JS)
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();
            var setting = await _purchasesettingservice.GetByUserId(userId);

            // If not found, return default values (form will be filled accordingly)
            var model = new PurchaseSettingVM()
            {
                ApplicationUserId = userId,
                MinPurchaseExpiryDays = setting?.MinPurchaseExpiryDays ?? 0,
                PurchaseTax = setting?.SalesTax ?? 0
            };

            return Json(model);
        }

        public async Task<IActionResult> SaveSidebarSettings([FromBody] PurchaseSettingVM viewModel)
        {
            if (viewModel == null || string.IsNullOrEmpty(viewModel.ApplicationUserId))
                return BadRequest("Invalid input");

            var existing = await _purchasesettingservice.GetByUserId(viewModel.ApplicationUserId);

            if (existing == null)
            {
                // Create new
                var newSetting = new PurchaseSetting
                {
                    ApplicationUserId = viewModel.ApplicationUserId,
                    MinPurchaseExpiryDays = viewModel.MinPurchaseExpiryDays,
                    SalesTax = viewModel.PurchaseTax
                };
                await _purchasesettingservice.Create(newSetting);
            }
            else
            {
                // Update existing
                existing.MinPurchaseExpiryDays = viewModel.MinPurchaseExpiryDays;
                existing.SalesTax = viewModel.PurchaseTax;
                await _purchasesettingservice.Update(existing);
            }

            return Ok();
        }
      
    }
}
