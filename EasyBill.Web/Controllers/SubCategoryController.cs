using AOne.Models.Entity;
using EasyBill.Models.Entity;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyBill.UI.Controllers
{
    public class SubCategoryController : Controller
    {
        private readonly ISubCategoryRepository _subCategoryservice;
        private readonly ICategoryMasterRepository _categoryservice;

        public SubCategoryController(ISubCategoryRepository subCategoryservice, ICategoryMasterRepository categoryservice)
        {
            _subCategoryservice = subCategoryservice;
            _categoryservice = categoryservice;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _subCategoryservice.GetAll();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategories();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(SubCategoryVM Vm)
        {
            if (!ModelState.IsValid || Vm.CategoryId == 0)
            {
                if (Vm.CategoryId == 0)
                    TempData["error"] = "Please select a category.";

                await LoadCategories(Vm.CategoryId);
                return View(Vm);
            }

            var subCategoriesData = await _subCategoryservice.GetAll();

            // ✅ Duplicate check
            var isExist = subCategoriesData.Any(x =>
                x.CategoryId == Vm.CategoryId &&
                x.Name.Trim().ToLower() == Vm.Name.Trim().ToLower()
            );

            if (isExist)
            {
                TempData["error"] = "Subcategory name already exists.";
                await LoadCategories(Vm.CategoryId);
                return View(Vm);
            }

            var model = new Models.Entity.SubCategory
            {
                Name = Vm.Name,
                CategoryId = Vm.CategoryId,
            };

            await _subCategoryservice.Create(model);
            TempData["success"] = "SubCategory created successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            var subcategorydata = await _subCategoryservice.GetById(Id);
            SubCategoryVM model = new SubCategoryVM();

            if (subcategorydata != null)
            {
                model.Id = subcategorydata.Id;
                model.CategoryId = subcategorydata.CategoryId;
                model.Name = subcategorydata.Name;
            }

            await LoadCategories(model.CategoryId);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SubCategoryVM Vm)
        {
            if (!ModelState.IsValid || Vm.CategoryId == 0)
            {
                if (Vm.CategoryId == 0)
                    TempData["error"] = "Please select a category.";

                await LoadCategories(Vm.CategoryId);
                return View(Vm);
            }

            var subCategoriesData = await _subCategoryservice.GetAll();

            // ✅ Duplicate check (excluding current record)
            var isExist = subCategoriesData.Any(x =>
                x.Id != Vm.Id &&
                x.CategoryId == Vm.CategoryId &&
                x.Name.Trim().ToLower() == Vm.Name.Trim().ToLower()
            );

            if (isExist)
            {
                TempData["error"] = "Subcategory name already exists.";
                await LoadCategories(Vm.CategoryId);
                return View(Vm);
            }

            var model = await _subCategoryservice.GetById(Vm.Id);
            if (model == null)
            {
                TempData["error"] = "No data found!!";
                await LoadCategories(Vm.CategoryId);
                return View(Vm);
            }

            model.CategoryId = Vm.CategoryId;
            model.Name = Vm.Name;
            await _subCategoryservice.Update(model);
            TempData["success"] = "SubCategory updated successfully.";
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

                var model = await _subCategoryservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                await _subCategoryservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // ✅ Get Categories for Dropdown
        [HttpGet]
        public async Task<JsonResult> GetCategories()
        {
            var list = await _categoryservice.GetAll();
            var data = list.Select(c => new
            {
                value = c.Id,
                text = c.CategoryName
            }).ToList();
            return Json(data);
        }

        // ✅ FIXED: Quick Save Category (New & Modify)
        [HttpPost]
        public async Task<JsonResult> QuickSaveCategory(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name is required" });

            try
            {
                if (id == 0) // F2: New Category Logic
                {
                    // Check duplicate
                    var allCategories = await _categoryservice.GetAll();
                    var isDuplicate = allCategories.Any(x =>
                        x.CategoryName.Trim().ToLower() == name.Trim().ToLower());

                    if (isDuplicate)
                    {
                        return Json(new { success = false, message = "Category name already exists!" });
                    }

                    var category = new AOne.Models.Entity.CategoryMaster
                    {
                        CategoryName = name.Trim()
                    };

                    await _categoryservice.Create(category);
                    return Json(new { success = true, newId = category.Id, message = "Category created successfully!" });
                }
                else // F3: Modify Category Logic
                {
                    var existing = await _categoryservice.GetByCategoryMasterId(id);
                    if (existing == null)
                    {
                        return Json(new { success = false, message = "Category not found!" });
                    }

                    // Check duplicate (excluding current)
                    var allCategories = await _categoryservice.GetAll();
                    var isDuplicate = allCategories.Any(x =>
                        x.Id != id &&
                        x.CategoryName.Trim().ToLower() == name.Trim().ToLower());

                    if (isDuplicate)
                    {
                        return Json(new { success = false, message = "Category name already exists!" });
                    }

                    existing.CategoryName = name.Trim();
                    await _categoryservice.Update(existing);
                    return Json(new { success = true, newId = existing.Id, message = "Category updated successfully!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Common Method (Category Dropdown)
        private async Task LoadCategories(int? selectedId = null)
        {
            IList<AOne.Models.Entity.CategoryMaster> categories = await _categoryservice.GetAll();

            ViewData["Categorydata"] = new SelectList(
                categories,
                "Id",
                "CategoryName",
                selectedId
            );
        }
    }
}