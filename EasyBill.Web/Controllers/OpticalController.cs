using AOne.DataAccess.ProfileService;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class OpticalController : Controller
    {
        private readonly IOpticalRepository _opticalRepo; 
        public OpticalController(IOpticalRepository opticalRepo)
        {
            _opticalRepo = opticalRepo; 
        }
        public async Task<IActionResult> Index()
        { 
            var data = await _opticalRepo.GetAll();
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(OpticalVM VM)
        {
            if (VM != null)
            {
                var model = new Optical
                {
                    Sphere = VM.Sphere,
                    Cylinder = VM.Cylinder,
                    Axis = VM.Axis,
                    Prism = VM.Prism,
                    Add = VM.Add, 

                };
                await _opticalRepo.Create(model);

            }
            return RedirectToAction("Index");
        } 
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            Optical model = await _opticalRepo.GetById(Id);
            OpticalVM opticalVM = new OpticalVM();
            if (model != null)
            {
                opticalVM.Id = model.Id;
                opticalVM.Sphere = model.Sphere;
                opticalVM.Cylinder = model.Cylinder;
                opticalVM.Axis = model.Axis;
                opticalVM.Prism = model.Prism; 
                opticalVM.Add = model.Add;
            }

            return View(opticalVM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(OpticalVM VM)
        {
            Optical model = await _opticalRepo.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.Sphere = VM.Sphere;
                model.Cylinder = VM.Cylinder;
                model.Axis = VM.Axis;
                model.Prism = VM.Prism;
                model.Add = VM.Add; 

                await _opticalRepo.Update(model);
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

                var model = await _opticalRepo.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                await _opticalRepo.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
