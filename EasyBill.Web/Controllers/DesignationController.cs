using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class DesignationController : Controller
    {
        private readonly IDesignationRepository _designation;
        public DesignationController(IDesignationRepository designation)
        {
            _designation = designation;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _designation.GetAll();

            var viewModel = new DesignationVM
            {
                Designations = data
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Index(DesignationVM designationVM)
        {
            if (!ModelState.IsValid)
            {
                designationVM.Designations = await _designation.GetAll();
                return View(designationVM);
            }

            var model = new Designation
            {
                Name = designationVM.Name.Trim()
            };

            await _designation.Create(model);

            TempData["success"] = "Designation saved successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            Designation model = await _designation.GetById(Id);
            DesignationVM designationVM = new DesignationVM();
            if (model != null)
            {
                designationVM.Id = model.Id;
                designationVM.Name = model.Name;
            }
            return View(designationVM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(DesignationVM VM)
        {
            if (!ModelState.IsValid)
                return View(VM);

            Designation model = await _designation.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.Name = VM.Name.Trim();
                await _designation.Update(model);
            }
            TempData["success"] = "Designation updated successfully!.";
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
                var model = await _designation.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Designation not found." });
                }
                await _designation.Delete(model);
                return Json(new { success = true, message = "Designation deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        [HttpGet]
        public async Task<JsonResult> ExistName(string name, int id = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { exists = false });
            }

            // Get all designations
            var designationData = await _designation.GetAll();

            // Duplicate check
            var isExist = designationData.Any(x =>
                x.Id != id &&
                !string.IsNullOrWhiteSpace(x.Name) &&
                string.Equals(
                    x.Name.Trim(),
                    name.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            );

            return Json(new { exists = isExist });
        }
    }
}
