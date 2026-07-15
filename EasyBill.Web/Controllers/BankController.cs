using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBill.UI.Controllers
{
    public class BankController : Controller
    {
        private readonly IBankRepository _bankRepo;
        private readonly IAccountGroupRepository _accountgroupRepo;

        public BankController(IBankRepository bankRepo, IAccountGroupRepository accountgroupRepo)
        {
            _bankRepo = bankRepo;
            _accountgroupRepo = accountgroupRepo;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _bankRepo.GetAll();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            return View(new BankVM());
        }

        [HttpPost]
        public async Task<IActionResult> Create(BankVM VM)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name", VM.AccountGroupId);
                return View(VM);
            }

            var duplicate = await _bankRepo.CheckDuplicateAsync(VM.BankName, 0);
            if (duplicate)
            {
                ModelState.AddModelError("BankName", "Bank Name already exists.");
                ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name", VM.AccountGroupId);
                return View(VM);
            }

            var model = new Bank
            {
                BankName = VM.BankName,
                AccountGroupId = VM.AccountGroupId,
                Branch = VM.Branch,
                City = VM.City,
                AccountNo = VM.AccountNo,
                IFSCCode = VM.IFSCCode,
                SwiftNo = VM.SwiftNo,
                OpeningBalance = VM.OpeningBalance
            };

            await _bankRepo.Create(model);
            TempData["success"] = "Bank created successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            var model = await _bankRepo.GetById(Id);
            if (model == null)
            {
                return NotFound();
            }

            var VM = new BankVM
            {
                Id = model.Id,
                BankName = model.BankName,
                AccountGroupId = model.AccountGroupId,
                Branch = model.Branch,
                City = model.City,
                AccountNo = model.AccountNo,
                IFSCCode = model.IFSCCode,
                SwiftNo = model.SwiftNo,
                OpeningBalance = model.OpeningBalance
            };

            ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name", model.AccountGroupId);
            return View(VM);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(BankVM VM)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name", VM.AccountGroupId);
                return View(VM);
            }

            var duplicate = await _bankRepo.CheckDuplicateAsync(VM.BankName, VM.Id);
            if (duplicate)
            {
                ModelState.AddModelError("BankName", "Bank Name already exists.");
                ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name", VM.AccountGroupId);
                return View(VM);
            }

            var model = await _bankRepo.GetById(VM.Id);
            if (model != null)
            {
                model.BankName = VM.BankName;
                model.AccountGroupId = VM.AccountGroupId;
                model.Branch = VM.Branch;
                model.City = VM.City;
                model.AccountNo = VM.AccountNo;
                model.IFSCCode = VM.IFSCCode;
                model.SwiftNo = VM.SwiftNo;
                model.OpeningBalance = VM.OpeningBalance;

                await _bankRepo.Update(model);
                TempData["success"] = "Bank updated successfully.";
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

                var model = await _bankRepo.GetById(id);
                if (model == null)
                {
                    return Json(new { success = false, message = "Bank not found." });
                }

                await _bankRepo.Delete(model);
                return Json(new { success = true, message = "Bank deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> CheckDuplicate(string name, int? id)
        {
            bool result = await _bankRepo.CheckDuplicateAsync(name, id ?? 0);
            return Json(new { exists = result });
        }
    }
}
