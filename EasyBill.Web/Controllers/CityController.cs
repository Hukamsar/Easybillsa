using DocumentFormat.OpenXml.Bibliography;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using City = EasyBill.Models.Entity.City;

namespace EasyBill.UI.Controllers
{
    public class CityController : Controller
    {
        private readonly IStateRepository _stateservice;
        private readonly ICityRepository _cityservice;
        private readonly ICountryRepository _countryservice;
        public CityController(IStateRepository stateservice, ICityRepository cityservice, ICountryRepository countryservice)
        {
            _stateservice = stateservice;
            _cityservice = cityservice;
            _countryservice = countryservice;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _cityservice.GetAll();
            var viewModel = new CityVM
            {
                City = data,
            };
            ViewBag.Country = new SelectList(await _countryservice.GetAll(), "Id", "Name");
            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new CityVM();
            ViewBag.Country = new SelectList(await _countryservice.GetAll(), "Id", "Name");
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CityVM VM)
        {
            if (VM.CountryId == 0)
            {
                TempData["error"] = "Country is required!!";
                await LoadCountryAndStateDropdown(VM.CountryId, VM.StateId);
                return View(VM);
            }

            if (VM.StateId == 0)
            {
                TempData["error"] = "State is required!!";
                await LoadCountryAndStateDropdown(VM.CountryId, VM.StateId);
                return View(VM);
            }

            if (!ModelState.IsValid)
            {
                TempData["error"] = "City name is required!";
                await LoadCountryAndStateDropdown(VM.CountryId, VM.StateId);
                return View(VM);
            }

            var countryData = await _countryservice.GetAll();
            var stateData = await _stateservice.GetByCountryId(VM.CountryId);
            var cityData = await _cityservice.GetAll();

            // 🔹 Check duplicate city in same country & state
            var isCityExists = cityData.Any(x =>
                x.CountryId == VM.CountryId &&
                x.StateId == VM.StateId &&
                x.Name.Trim().ToLower() == VM.Name.Trim().ToLower()
            );

            if (isCityExists)
            {
                TempData["error"] = "City already exists in selected State & Country.";
                await LoadCountryAndStateDropdown(VM.CountryId, VM.StateId);
                return View(VM);
            }

            var model = new City
            {
                Name = VM.Name,
                CountryId = VM.CountryId,
                StateId = VM.StateId
            };

            await _cityservice.Create(model);

            TempData["success"] = "City created successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            City model = await _cityservice.GetById(Id);
            CityVM VM = new CityVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.Name = model.Name;
                VM.CountryId = model.CountryId;
                VM.StateId = model.StateId;
            }
            ViewBag.Country = new SelectList(await _countryservice.GetAll(), "Id", "Name");
            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(CityVM VM)
        {
            if (VM.CountryId == 0)
            {
                TempData["error"] = "Country is required!!";
                await LoadCountryAndStateDropdown(VM.CountryId, VM.StateId);
                return View(VM);
            }

            if (VM.StateId == 0)
            {
                TempData["error"] = "State is required!!";
                await LoadCountryAndStateDropdown(VM.CountryId, VM.StateId);
                return View(VM);
            }

            if (!ModelState.IsValid)
            {
                TempData["error"] = "City name is required!";
                await LoadCountryAndStateDropdown(VM.CountryId, VM.StateId);
                return View(VM);
            }

            var countryData = await _countryservice.GetAll();
            var stateData = await _stateservice.GetByCountryId(VM.CountryId);
            var cityData = await _cityservice.GetAll();

            // 🔹 Check duplicate city in same country & state
            var isCityExists = cityData.Any(x =>
                x.CountryId == VM.CountryId &&
                x.StateId == VM.StateId &&
                x.Name.Trim().ToLower() == VM.Name.Trim().ToLower()
            );

            //if (isCityExists)
            //{
            //    TempData["error"] = "City already exists in selected State & Country.";
            //    await LoadCountryAndStateDropdown(VM.CountryId, VM.StateId);
            //    return View(VM);
            //}

            City model = await _cityservice.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.Name = VM.Name;
                model.CountryId = VM.CountryId;
                model.StateId = VM.StateId;
                await _cityservice.Update(model);
                TempData["success"] = "City updated successfully.";

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
                var model = await _cityservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _cityservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetStatesByCountry(int countryId)
        {
            var states = await _stateservice.GetByCountryId(countryId);

            var result = states.Select(s => new { s.Id, s.Name }).ToList();

            return Json(result);
        }
        private async Task LoadCountryAndStateDropdown(int? countryId = null, int? stateId = null)
        {
            ViewBag.Country = new SelectList(
                await _countryservice.GetAll(),
                "Id",
                "Name",
                countryId
            );

            ViewBag.State = new SelectList(
                await _stateservice.GetAll(),
                "Id",
                "Name",
                stateId
            );
        }
        [HttpGet]
        public async Task<JsonResult> CheckDuplicate(string name, int stateId, int id)
        {
            var exists = await _cityservice.CheckDuplicateAsync(name, stateId, id);
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

            bool exists = (await _countryservice.GetAll())
                .Any(c => c.Name.ToLower() == name.Trim().ToLower());

            if (exists)
                return Json(new { success = false, message = "Country already exists!" });

            if (id == 0)
            {
                var model = new EasyBill.Models.Entity.Country { Name = name.Trim() };
                await _countryservice.Create(model);
                return Json(new { success = true, newId = model.Id });
            }
            else
            {
                var model = await _countryservice.GetById(id);
                if (model == null)
                    return Json(new { success = false, message = "Country not found!" });

                model.Name = name.Trim();
                await _countryservice.Update(model);
                return Json(new { success = true, newId = model.Id });
            }
        }

        [HttpPost]
        public async Task<JsonResult> QuickSaveState(int id, string name, int countryId)
        {
            if (countryId <= 0)
                return Json(new { success = false, message = "Select country first!" });

            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name required!" });

            bool exists = (await _stateservice.GetAll())
                .Any(s => s.CountryId == countryId && s.Name.ToLower() == name.Trim().ToLower());

            if (exists)
                return Json(new { success = false, message = "State already exists in this country!" });

            if (id == 0)
            {
                var model = new EasyBill.Models.Entity.State { Name = name.Trim(), CountryId = countryId };
                await _stateservice.Create(model);
                return Json(new { success = true, newId = model.Id });
            }
            else
            {
                var model = await _stateservice.GetById(id);
                if (model == null)
                    return Json(new { success = false, message = "State not found!" });

                model.Name = name.Trim();
                model.CountryId = countryId;
                await _stateservice.Update(model);
                return Json(new { success = true, newId = model.Id });
            }
        }
    }
}
