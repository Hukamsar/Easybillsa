using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EasyBill.UI.Controllers
{
    public class StateController : Controller
    {
        private readonly ICountryRepository _countryservice;
        private readonly IStateRepository _stateservice;
        public StateController(ICountryRepository countryservice, IStateRepository stateservice)
        {
            _countryservice = countryservice;
            _stateservice = stateservice;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _stateservice.GetAll();
            var viewModel = new StateVM
            {
                States = data,
            };
            ViewBag.Country = new SelectList(await _countryservice.GetAll(), "Id", "Name");
            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var data = await _stateservice.GetAll();
            var viewModel = new StateVM
            {
                States = data,
            };
            ViewBag.Country = new SelectList(await _countryservice.GetAll(), "Id", "Name");

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(StateVM VM)
        {
            // Country validation
            if (VM.CountryId == 0)
            {
                TempData["error"] = "Country is required!";
                await LoadCountryDropdown(VM.CountryId);
                return View(VM);
            }

            // Model validation
            if (!ModelState.IsValid)
            {
                await LoadCountryDropdown(VM.CountryId);
                return View(VM);
            }

            string stateName = VM.Name.Trim().ToLower();

            // ✅ Check duplicate state in same country
            var isExists = (await _stateservice.GetAll())
                .Any(x => x.CountryId == VM.CountryId &&
                          x.Name.ToLower() == stateName);

            if (isExists)
            {
                TempData["error"] = "State already exists in this country.";
                await LoadCountryDropdown(VM.CountryId);
                return View(VM);
            }

            // ✅ Save
            var model = new State
            {
                Name = VM.Name.Trim(),
                CountryId = VM.CountryId
            };

            await _stateservice.Create(model);

            TempData["success"] = "State created successfully.";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            State model = await _stateservice.GetById(Id);
            StateVM VM = new StateVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.Name = model.Name;
                VM.CountryId = model.CountryId;
            }
            ViewBag.Country = new SelectList(await _countryservice.GetAll(), "Id", "Name");
            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(StateVM VM)
        {
            if (VM.CountryId == 0)
            {
                TempData["error"] = "Country is required!!";
                ViewBag.Country = new SelectList(
                    await _countryservice.GetAll(),
                    "Id",
                    "Name",
                    VM.CountryId
                );
                return View(VM);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Country = new SelectList(
                    await _countryservice.GetAll(),
                    "Id", "Name", VM.CountryId
                );
                return View(VM);
            }

            State model = await _stateservice.GetById(VM.Id);
            if (model == null)
            {
                TempData["error"] = "State not found or already deleted.";
                return RedirectToAction("Edit");
            }

            model.Id = VM.Id;
            model.Name = VM.Name;
            model.CountryId = VM.CountryId;

            await _stateservice.Update(model);

            TempData["success"] = "State updated successfully.";
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
                var model = await _stateservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _stateservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        private async Task LoadCountryDropdown(int selectedCountryId = 0)
        {
            var countries = await _countryservice.GetAll();
            ViewBag.Country = new SelectList(countries, "Id", "Name", selectedCountryId);
        }
        [HttpGet]
        public async Task<JsonResult> CheckDuplicate(string name, int countryId, int id)
        {
            bool exists = await _stateservice.CheckDuplicateAsync(name, countryId, id);
            return Json(new { exists });
        }
        // ================= ADDED FOR MODAL (F2 / F3) =================

        [HttpGet]
        public async Task<JsonResult> GetCountries()
        {
            var list = (await _countryservice.GetAll())
                .Select(c => new { value = c.Id, text = c.Name })
                .ToList();
            return Json(list);
        }

        [HttpPost]
        public async Task<JsonResult> QuickSaveCountry(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name required!" });

            name = name.Trim();

            bool exists = (await _countryservice.GetAll())
                .Any(c => c.Name.ToLower() == name.ToLower());

            if (exists)
                return Json(new { success = false, message = "Country already exists!" });

            if (id == 0)
            {
                var model = new Country { Name = name };
                await _countryservice.Create(model);
                return Json(new { success = true, newId = model.Id });
            }
            else
            {
                var model = await _countryservice.GetById(id);
                if (model == null)
                    return Json(new { success = false, message = "Country not found!" });

                model.Name = name;
                await _countryservice.Update(model);
                return Json(new { success = true, newId = model.Id });
            }
        }
    }
}

