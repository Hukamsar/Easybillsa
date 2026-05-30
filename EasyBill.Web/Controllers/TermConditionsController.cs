using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class TermConditionsController : Controller
    {
        private readonly ITermConditionsRepository _termConditionsservice;
        public TermConditionsController(ITermConditionsRepository termConditionsservice)
        {
            _termConditionsservice = termConditionsservice;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _termConditionsservice.GetAll();
            var viewModel = new TermConditionsVM
            {
                TermsConditions = data,
            };
            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new TermConditionsVM();

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(TermConditionsVM VM)
        {
            if (VM != null)
            {
                var model = new TermsConditions
                {
                    Name = VM.Name,

                };
                await _termConditionsservice.Create(model);
            }
            TempData["success"] = "Item saved successfully!";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            TermsConditions model = await _termConditionsservice.GetById(Id);
            TermConditionsVM VM = new TermConditionsVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.Name = model.Name;

            }

            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(TermConditionsVM VM)
        {
            TermsConditions model = await _termConditionsservice.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.Name = VM.Name;

                await _termConditionsservice.Update(model);
            }
            TempData["success"] = "Record updated successfully!";
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
                var model = await _termConditionsservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _termConditionsservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        public async Task<JsonResult> CheckDuplicate(string Name, int id)
        {
            bool result = await _termConditionsservice.CheckDuplicateAsync(Name, id);

            return Json(new
            {
                exists = result
            });
        }
    }
}

