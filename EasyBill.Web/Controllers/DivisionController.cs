using AOne.DataAccess.ProfileService;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class DivisionController : Controller
    {
        private readonly IDivisionRepository _divisionservice;
        private readonly ICompanyRepository _companyservice;
        private readonly IProfileService _profileService;
        public DivisionController(IDivisionRepository divisionservice, IProfileService profileService, ICompanyRepository companyservice)
        {
            _divisionservice = divisionservice;
            _profileService = profileService;
            _companyservice = companyservice;
        }
        public async Task<IActionResult> Index()
        {
            await _profileService.Set(User);

            // Division List
            var data = await _divisionservice.GetAll();

            // Company Dropdown
            ViewBag.companydata = new SelectList(
                await _companyservice.GetAll(),
                "Id",
                "Name"
            );

            return View(data);
        }
        //[HttpGet]
        //public async Task<IActionResult> Create()
        //{
        //    DivisionVM vm = new DivisionVM();
        //    await LoadCompanies(vm.CompanyId);
        //    return View(vm);
        //}

        [HttpPost]
        public async Task<IActionResult> Create(DivisionVM vm)
        {
            if (!ModelState.IsValid || vm.CompanyId == 0)
            {
                TempData["error"] = "Please select a company.";

                await _profileService.Set(User);

                ViewBag.companydata = new SelectList(
                    await _companyservice.GetAll(),
                    "Id",
                    "Name",
                    vm.CompanyId
                );

                var data = await _divisionservice.GetAll();

                return View("Index", data);
            }

            var divisionsData = await _divisionservice.GetAll();

            var isExist = divisionsData.Any(x =>
                x.CompanyId == vm.CompanyId &&
                x.Name.Trim().ToLower() == vm.Name.Trim().ToLower()
            );

            if (isExist)
            {
                TempData["error"] = "Division already exists in this Company.";

                await _profileService.Set(User);

                ViewBag.companydata = new SelectList(
                    await _companyservice.GetAll(),
                    "Id",
                    "Name",
                    vm.CompanyId
                );

                var data = await _divisionservice.GetAll();

                return View("Index", data);
            }

            Division model = new Division
            {
                Name = vm.Name,
                CompanyId = vm.CompanyId
            };

            await _divisionservice.Create(model);

            TempData["success"] = "Division created successfully.";

            return RedirectToAction("Index");
        }
        private async Task LoadCompanies(int selectedCompanyId = 0)
        {
            IList<Company> companies = await _companyservice.GetAll();
            companies.Insert(0, new Company
            {
                Id = 0,
                Name = "-- Select Company --"
            });

            ViewBag.companydata = new SelectList(
                companies,
                "Id",
                "Name",
                selectedCompanyId
            );
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            Division model = await _divisionservice.GetById(Id);

            await LoadCompanies(model.CompanyId);

            DivisionVM itemMasterVM = new DivisionVM();
            if (model != null)
            {
                itemMasterVM.Id = model.Id;
                itemMasterVM.Name = model.Name;
                itemMasterVM.CompanyId = model.CompanyId;
            }

            return View(itemMasterVM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(DivisionVM VM)
        {
            if (!ModelState.IsValid || VM.CompanyId == 0)
            {
                if (VM.CompanyId == 0)
                {
                    //ModelState.AddModelError("CompanyId", "Please select a company.");
                    TempData["error"] = "Please select a company.";
                }

                await LoadCompanies(VM.CompanyId);
                return View(VM);
            }

            var companyData = await _companyservice.GetAll();
            var divisionsData = await _divisionservice.GetAll();

            var isExist = divisionsData.Any(x =>
     x.Id != VM.Id &&
     x.CompanyId == VM.CompanyId &&
     x.Name.Trim().ToLower() == VM.Name.Trim().ToLower()

            );

            if (isExist)
            {
                TempData["error"] = "Division already exists in this Company.";
                await LoadCompanies(VM.CompanyId);
                return View(VM);
            }

            Division model = await _divisionservice.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.Name = VM.Name;
                model.CompanyId = VM.CompanyId;

                await _divisionservice.Update(model);
                TempData["success"] = "Division updated successfully.";
            }

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

                var model = await _divisionservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                await _divisionservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
            // 1. Quick Save Company (F2/F3 ke liye)
            [HttpPost]
            public async Task<JsonResult> QuickSaveCompany(int id, string name)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        return Json(new { success = false, message = "Name is required!" });
                    }

                    var companies = await _companyservice.GetAll();

                    if (id == 0)
                    {
                        // Check duplicate
                        bool exists = companies.Any(c => c.Name.Trim().ToLower() == name.Trim().ToLower());
                        if (exists)
                        {
                            return Json(new { success = false, message = "Company already exists!" });
                        }

                        Company company = new Company { Name = name };
                        await _companyservice.Create(company);

                        return Json(new { success = true, newId = company.Id, message = "Company saved!" });
                    }
                    else
                    {
                        var company = await _companyservice.GetById(id);
                        if (company == null)
                        {
                            return Json(new { success = false, message = "Company not found!" });
                        }

                        // Check duplicate (exclude current)
                        bool exists = companies.Any(c => c.Name.Trim().ToLower() == name.Trim().ToLower() && c.Id != id);
                        if (exists)
                        {
                            return Json(new { success = false, message = "Company already exists!" });
                        }

                        company.Name = name;
                        await _companyservice.Update(company);

                        return Json(new { success = true, newId = company.Id, message = "Company updated!" });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error: " + ex.Message });
                }
            }

            [HttpGet]
            public async Task<JsonResult> GetCompanies()
            {
                var list = (await _companyservice.GetAll())
                    .OrderBy(c => c.Name)
                    .Select(c => new { value = c.Id, text = c.Name })
                    .ToList();

                return Json(list);
            }
        }
    }

