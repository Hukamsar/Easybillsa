using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly IDepartmentRepository _departmentservice;
        public DepartmentController(IDepartmentRepository departmentservice)
        {
            _departmentservice = departmentservice;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _departmentservice.GetAll();

            var viewModel = new DepartmentVM
            {
                Departments = data.Select(x => new DepartmentVM
                {
                    Id = x.Id,
                    Name = x.Name
                }).ToList()
            };

            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(DepartmentVM VM)
        {
            if (ModelState.IsValid)
            {
                // Get all departments
                var departmentData = await _departmentservice.GetAll();
                
                // PhoneNo Duplicate check
                if (!string.IsNullOrEmpty(VM.Name))
                {
                    var isExist = !string.IsNullOrWhiteSpace(VM.Name) &&
                             departmentData.Any(x =>
                                 x.Id != VM.Id &&
                                 !string.IsNullOrWhiteSpace(x.Name) &&
                                 string.Equals(
                                     x.Name.Trim(),
                                     VM.Name.Trim(),
                                     StringComparison.OrdinalIgnoreCase
                                 )
                             );

                    if (isExist)
                    {
                        TempData["error"] = "Department name already exists.";
                        return View(VM);
                    }
                }

                var model = new Department
                {
                    Name = VM.Name.Trim(),
                };
                var result = await _departmentservice.Create(model);
                if (result.Id > 0)
                {
                    TempData["success"] = "Department created successfully";
                    return RedirectToAction("Index");
                }
                else
                {
                    return View(VM);
                }
            }
            else
            {
                return View(VM);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int? Id)
        {
            var model = await _departmentservice.GetById(Id);
            DepartmentVM departmentVM = new DepartmentVM();
            if (model != null)
            {
                departmentVM.Id = model.Id;
                departmentVM.Name = model.Name;
            }
            return View(departmentVM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(DepartmentVM VM)
        {
            Department model = await _departmentservice.GetById(VM.Id);
            if (model != null)
            {
                // Get all departments
                var departmentData = await _departmentservice.GetAll();

                // PhoneNo Duplicate check
                if (!string.IsNullOrEmpty(VM.Name))
                {
                    var isExist = !string.IsNullOrWhiteSpace(VM.Name) &&
                             departmentData.Any(x =>
                                 x.Id != VM.Id &&
                                 !string.IsNullOrWhiteSpace(x.Name) &&
                                 string.Equals(
                                     x.Name.Trim(),
                                     VM.Name.Trim(),
                                     StringComparison.OrdinalIgnoreCase
                                 )
                             );

                    if (isExist)
                    {
                        TempData["error"] = "Department name already exists.";
                        return View(VM);

                    }
                }

                model.Name = VM.Name.Trim();
                var result = await _departmentservice.Update(model);
                if (result.Id > 0)
                {
                    TempData["success"] = "Department updated successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    return View(VM);
                }
            }
            else
            {
                return View(VM);
            }

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
                var model = await _departmentservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _departmentservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        [HttpGet]
        public async Task<JsonResult> ExistName(string name, int id = 0)
        {
            bool exists = await _departmentservice.ExistName(name, id);
            return Json(new { exists });
        }
    }
}
