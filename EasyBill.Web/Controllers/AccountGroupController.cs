using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class AccountGroupController : Controller
    {
        private readonly IAccountGroupRepository _accountgroupRepo;
        public AccountGroupController(IAccountGroupRepository accountgroupRepo)
        {
            _accountgroupRepo = accountgroupRepo;

        }
        public async Task<IActionResult> Index()
        {
            var data = await _accountgroupRepo.GetAll();
            var viewModel = new AccountGroupVM
            {
                AccountGroups = data,
            };
            ViewBag.ParentList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new AccountGroupVM();
            ViewBag.ParentList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(AccountGroupVM VM)
        {


            var model = new AccountGroup
            {
                Name = VM.Name,
                ParentId = VM.ParentId,
                IsActive = VM.IsActive
            };

            await _accountgroupRepo.Create(model);

            TempData["success"] = "Created successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            AccountGroup model = await _accountgroupRepo.GetById(Id);
            AccountGroupVM VM = new AccountGroupVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.Name = model.Name;
                VM.ParentId = model.ParentId;
                VM.IsActive = model.IsActive;
            }
            ViewBag.ParentList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(AccountGroupVM VM)
        {

            AccountGroup model = await _accountgroupRepo.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.Name = VM.Name;
                model.ParentId = VM.ParentId;
                model.IsActive = VM.IsActive;
                await _accountgroupRepo.Update(model);
                TempData["success"] = "Updated successfully.";

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
                var model = await _accountgroupRepo.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _accountgroupRepo.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        [HttpGet]
        public async Task<JsonResult> CheckDuplicate(string name, int? id)
        {
            bool result = await _accountgroupRepo.CheckDuplicateAsync(name, id ?? 0);

            return Json(new { exists = result });
        }
    }
}
