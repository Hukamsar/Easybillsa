using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class GlobalSettingsController : Controller
    {
        private readonly ISalseSettingRepository _salesRepo;
        private readonly IPurchaseSettingRepository _purchaseRepo;

        public GlobalSettingsController(
            ISalseSettingRepository salesRepo,
            IPurchaseSettingRepository purchaseRepo)
        {
            _salesRepo = salesRepo;
            _purchaseRepo = purchaseRepo;
        }

        public IActionResult GlobalSettings()
        {
            return View();
        }
    }
}