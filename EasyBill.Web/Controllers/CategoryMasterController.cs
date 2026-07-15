using AOne.Models.Entity;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Filters;
using Microsoft.AspNetCore.Mvc;
using static AOne.Utility.Permissions;

namespace EasyBill.UI.Controllers
{
    [HeadOfficeOnly]
    public class CategoryMasterController : Controller
    {
        private readonly ICategoryMasterRepository _categoryservice;
        public CategoryMasterController(ICategoryMasterRepository categoryservice)
        {
            _categoryservice = categoryservice;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _categoryservice.GetAll();
            var viewModel = new CategoryMasterVM
            {
                Categories = data,
            };
            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CategoryMasterVM itemMasterVM)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage ?? "Invalid data";

                return View(itemMasterVM);
            }

            var categoryList = await _categoryservice.GetAll();

            // ✅ Duplicate check
            var isExist = categoryList.Any(x =>
                x.CategoryName.Trim().ToLower() ==
                itemMasterVM.CategoryName.Trim().ToLower()
            );

            if (isExist)
            {
                TempData["error"] = "Category name already exists.";
                return View(itemMasterVM);
            }

            // ✅ Map ViewModel → Model
            var model = new AOne.Models.Entity.CategoryMaster
            {
                CategoryName = itemMasterVM.CategoryName.Trim()
            };

            await _categoryservice.Create(model);

            TempData["success"] = "Category created successfully.";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            var model = await _categoryservice.GetByCategoryMasterId(Id);
            CategoryMasterVM itemMasterVM = new CategoryMasterVM();
            if (model != null)
            {
                itemMasterVM.Id = model.Id;
                itemMasterVM.CategoryName = model.CategoryName;

            }
            return View(itemMasterVM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(CategoryMasterVM VM)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage ?? "Invalid data";

                return View(VM);
            }

            // 🔹 Get all categories (for duplicate check)
            var categoryList = await _categoryservice.GetAll();

            // 🔹 Duplicate check (excluding current record)
            var isExist = categoryList.Any(x =>
                x.Id != VM.Id &&
                x.CategoryName.Trim().ToLower() == VM.CategoryName.Trim().ToLower()
            );

            if (isExist)
            {
                TempData["error"] = "Category name already exists.";
                return View(VM);
            }

            // 🔹 Get actual DB model
            var model = await _categoryservice.GetByCategoryMasterId(VM.Id);

            if (model == null)
            {
                TempData["error"] = "Category not found.";
                return RedirectToAction("Index");
            }

            // 🔹 Map ViewModel → Model
            model.CategoryName = VM.CategoryName.Trim();

            await _categoryservice.Update(model);

            TempData["success"] = "Category updated successfully.";
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
                var model = await _categoryservice.GetByCategoryMasterId(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _categoryservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
