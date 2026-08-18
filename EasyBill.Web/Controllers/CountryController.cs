using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EasyBill.UI.Controllers
{
    public class CountryController : Controller
    {
        private readonly ICountryRepository _countryservice;
        public CountryController(ICountryRepository countryservice)
        {
            _countryservice = countryservice;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _countryservice.GetAll();
            var viewModel = new CountryVM
            {
                Countries = data,
            };
            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new CountryVM();

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(CountryVM VM)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            string countryName = VM.Name.Trim();

            var existingCountry = (await _countryservice.GetAll())
                .FirstOrDefault(x => x.Name.ToLower() == countryName.ToLower());

            if (existingCountry != null)
            {
                //ModelState.AddModelError("Name", "Country already exists.");
                TempData["error"] = "Country already exists.";
                return View(VM);
            }

            if (VM != null)
            {
                var data = await _countryservice.GetAll();
                var model = new Country
                {
                    Name = VM.Name,

                };
                await _countryservice.Create(model);
                TempData["success"] = "Country created successfully.";
            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            Country model = await _countryservice.GetById(Id);
            CountryVM VM = new CountryVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.Name = model.Name;

            }

            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(CountryVM VM)
        {
            if (!ModelState.IsValid)
                return View();

            string countryName = VM.Name.Trim();
            var existingCountry = await _countryservice.ExistName(countryName, VM.Id);

            if (existingCountry)
            { 
                TempData["error"] = "Country already exists.";
                return View(VM);
            }

            Country model = await _countryservice.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.Name = VM.Name;

                await _countryservice.Update(model);
                TempData["success"] = "Country updated successfully.";
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
                var model = await _countryservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _countryservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
