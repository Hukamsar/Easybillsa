using AOne.DataAccess.ProfileService;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Filters;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    [HeadOfficeOnly]
    public class CompanyController : Controller
    {
        private readonly ICompanyRepository _companyservice;
        private readonly IProfileService _profileService;
        public CompanyController(ICompanyRepository companyservice, IProfileService profileService)
        {
            _companyservice = companyservice;
            _profileService = profileService;
        }
        public async Task<IActionResult> Index()
        {
            await _profileService.Set(User);
            var data = await _companyservice.GetAll();
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(CompanyVM HsnVm)
        {
            if (!ModelState.IsValid)
                return View(HsnVm);

            var companyList = await _companyservice.GetAll();

            // Duplicate check from list
            var isExist = companyList.Any(x =>
                x.Name.Trim().ToLower() == HsnVm.Name.Trim().ToLower());

            if (isExist)
            {
                TempData["error"] = "Company already created";
                return View(HsnVm);
            }

            // Save new company
            var model = new Company
            {
                Name = HsnVm.Name.Trim()
            };

            await _companyservice.Create(model);

            TempData["success"] = "Company created successfully.";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            Company model = await _companyservice.GetById(Id);
            CompanyVM itemMasterVM = new CompanyVM();
            if (model != null)
            {
                itemMasterVM.Id = model.Id;
                itemMasterVM.Name = model.Name;
            }

            return View(itemMasterVM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(CompanyVM VM)
        {
            if (!ModelState.IsValid)
                return View();

            var companyList = await _companyservice.GetAll();

            // Duplicate check from list
            var isExist = companyList.Any(x =>
                x.Name.Trim().ToLower() == VM.Name.Trim().ToLower());

            if (isExist)
            {
                TempData["error"] = "Company already created";
                return View(VM);
            }

            Company model = await _companyservice.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.Name = VM.Name;

                await _companyservice.Update(model);
            }
            TempData["success"] = "Company updated successfully.";
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid Id for deletion." });
                }

                var model = await _companyservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                await _companyservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
