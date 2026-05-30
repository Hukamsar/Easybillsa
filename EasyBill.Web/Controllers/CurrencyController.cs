using DocumentFormat.OpenXml.Office2010.Excel;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class CurrencyController : Controller
    {
        private readonly ICurrencyRepository _currencyRepo;
        public CurrencyController(ICurrencyRepository currencyRepo)
        {
            _currencyRepo = currencyRepo;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _currencyRepo.GetAll();

            var result = data.Select(x => new CurrencyVM
            {
                Id = x.Id,
                Code = x.Code,
                Symbol = x.Symbol,
                Name = x.Name,
                SubName = x.SubName
            }).ToList();

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CurrencyVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var currencyData = await _currencyRepo.GetAll();

            // ✅ Code duplicate check
            if (!string.IsNullOrWhiteSpace(vm.Code))
            {
                var isCodeExist = currencyData.Any(x =>
                    x.Id != vm.Id &&
                    !string.IsNullOrWhiteSpace(x.Code) &&
                    x.Code.Trim().Equals(vm.Code.Trim(), StringComparison.OrdinalIgnoreCase)
                );

                if (isCodeExist)
                {
                    ModelState.AddModelError("Code", "Code already exists.");
                    return View(vm);
                }
            }

            // ✅ Name duplicate check
            if (!string.IsNullOrWhiteSpace(vm.Name))
            {
                var isNameExist = currencyData.Any(x =>
                    x.Id != vm.Id &&
                    !string.IsNullOrWhiteSpace(x.Name) &&
                    x.Name.Trim().Equals(vm.Name.Trim(), StringComparison.OrdinalIgnoreCase)
                );

                if (isNameExist)
                {
                    ModelState.AddModelError("Name", "Name already exists.");
                    return View(vm);
                }
            }

            var model = new Currency
            {
                Code = vm.Code,
                Name = vm.Name,
                Symbol = vm.Symbol,
                SubName = vm.SubName,
            };

            await _currencyRepo.Create(model);

            TempData["success"] = "Currency created successfully.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _currencyRepo.GetById(id);

            if (model == null)
            {
                return NotFound();
            }

            var currencyVM = new CurrencyVM
            {
                Id = model.Id,
                Code = model.Code,
                Name = model.Name,
                Symbol = model.Symbol,
                SubName = model.SubName
            };

            return View(currencyVM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(CurrencyVM VM)
        {
            if (!ModelState.IsValid)
                return View(VM);

            var currencyData = await _currencyRepo.GetAll();

            // ✅ Code duplicate check
            if (!string.IsNullOrWhiteSpace(VM.Code))
            {
                var isCodeExist = currencyData.Any(x =>
                    x.Id != VM.Id &&
                    !string.IsNullOrWhiteSpace(x.Code) &&
                    x.Code.Trim().Equals(VM.Code.Trim(), StringComparison.OrdinalIgnoreCase)
                );

                if (isCodeExist)
                {
                    ModelState.AddModelError("Code", "Code already exists.");
                    return View(VM);
                }
            }

            // ✅ Name duplicate check
            if (!string.IsNullOrWhiteSpace(VM.Name))
            {
                var isNameExist = currencyData.Any(x =>
                    x.Id != VM.Id &&
                    !string.IsNullOrWhiteSpace(x.Name) &&
                    x.Name.Trim().Equals(VM.Name.Trim(), StringComparison.OrdinalIgnoreCase)
                );

                if (isNameExist)
                {
                    ModelState.AddModelError("Name", "Name already exists.");
                    return View(VM);
                }
            }

            // ✅ Fetch existing record
            var model = await _currencyRepo.GetById(VM.Id);
            if (model == null)
                return NotFound();

            // ✅ Update values
            model.Code = VM.Code;
            model.Name = VM.Name;
            model.Symbol = VM.Symbol;
            model.SubName = VM.SubName;

            await _currencyRepo.Update(model);

            TempData["success"] = "Currency updated successfully.";

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
                var model = await _currencyRepo.GetById(id);
                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _currencyRepo.Delete(model);
                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
