using AOne.DataAccess.ProfileService;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Filters;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    [HeadOfficeOnly]
    public class HSNController : Controller
    {
        private readonly IHSNRepository _hsnService;
        private readonly IProfileService _profileService;
        public HSNController(IHSNRepository hsnService, IProfileService profileService) 
        {
             _hsnService = hsnService;
            _profileService = profileService;
        }  
        public async Task<IActionResult> Index()
        {
           await _profileService.Set(User);
            var data = await _hsnService.GetAll();
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        { 
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(HSNVM HsnVm)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage;

                return View(HsnVm);
            }

            // GST Validation
            //if ((HsnVm.CGST ?? 0) + (HsnVm.SGST ?? 0) == 0 && (HsnVm.IGST ?? 0) == 0)
            //{
            //    ModelState.AddModelError("", "Either CGST + SGST or IGST is required.");
            //    return View(HsnVm);
            //}

            var model = new Hsn
            {
                HsnCode = HsnVm.HsnCode.Trim(),
                SGST = HsnVm.SGST ?? 0,
                CGST = HsnVm.CGST ?? 0,
                IGST = HsnVm.IGST ?? 0,
                Cess = HsnVm.Cess ?? 0,
                HsnType = HsnVm.HsnType
            };

            await _hsnService.Create(model);

            TempData["success"] = "HSN created successfully.";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> GetDetails(int Id)
        {
            if(Id > 0)
            {
                var hsndata = await _hsnService.GetByHSNId(Id);
                if(hsndata != null)
                {
                    var model = new
                    {
                        sgst = hsndata.SGST,
                        cgst = hsndata.CGST,
                        igst = hsndata.IGST,
                        cess = hsndata.Cess,
                    };
                    return Json(model);
                }
                else
                {
                  return Json(  new { error = "Hsn Details not found." });
                }
               
            }
            else
            {
                return Json(new { error = "Hsn Details not found." });
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            Hsn model = await _hsnService.GetByHSNId(Id);
            HSNVM itemMasterVM = new HSNVM();
            if (model != null)
            {
                itemMasterVM.Id = model.Id;
                itemMasterVM.HsnCode = model.HsnCode;
                itemMasterVM.SGST = model.SGST;
                itemMasterVM.CGST = model.CGST;
                itemMasterVM.IGST = model.IGST;
                itemMasterVM.Cess = model.Cess;
                itemMasterVM.HsnType = model.HsnType; 
            }
           
            return View(itemMasterVM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(HSNVM VM)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage;

                return View(VM);
            }

            Hsn model = await _hsnService.GetByHSNId(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.HsnCode = VM.HsnCode.Trim();
                model.SGST = VM.SGST ?? 0;
                model.CGST = VM.CGST ?? 0;
                model.IGST = VM.IGST ?? 0;
                model.Cess = VM.Cess ?? 0;
                model.HsnType = VM.HsnType;
                
                await _hsnService.Update(model);
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

                var model = await _hsnService.GetByHSNId(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                await _hsnService.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        public async Task<JsonResult> CheckDuplicate(string HsnCode, int id)
        {
            bool result = await _hsnService.CheckDuplicateAsync(HsnCode, id);

            return Json(new
            {
                exists = result
            });
        }
    }
}
