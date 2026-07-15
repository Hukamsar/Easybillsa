using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBill.UI.Controllers
{
    public class ContraController : Controller
    {
        private readonly IContraRepository _contraRepository;
        private readonly IBankRepository _bankRepository;

        public ContraController(
            IContraRepository contraRepository,
            IBankRepository bankRepository)
        {
            _contraRepository = contraRepository;
            _bankRepository = bankRepository;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _contraRepository.GetAll();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new ContraVM()
            {
                VouncherNo = await GenerateVoucherNumber(),
                Date = DateTime.Now
            };
            ViewBag.BankList = new SelectList(await _bankRepository.GetAll(), "Id", "BankName");
            return View(viewModel);
        }

        public async Task<string> GenerateVoucherNumber()
        {
            var data = await _contraRepository.GetAll();
            var lastCode = data
                .Where(p => !string.IsNullOrEmpty(p.VouncherNo))
                .Select(p => p.VouncherNo)
                .LastOrDefault();

            if (string.IsNullOrEmpty(lastCode) || lastCode.Length < 2)
                return "CON0001";

            string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray());
            string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

            int number = 0;
            int.TryParse(numberPart, out number);

            string nextCode = prefix + (number + 1).ToString("D" + numberPart.Length);
            return nextCode;
        }

        [HttpPost]
        public async Task<IActionResult> Create(ContraVM VM)
        {
            if (VM != null)
            {
                string fileName = "";
                if (VM.UploadAttachments != null && VM.UploadAttachments.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Contra");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    fileName = VM.UploadAttachments.FileName;
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        VM.UploadAttachments.CopyTo(fileStream);
                    }
                    VM.Attachments = fileName;
                }

                var model = new Contra
                {
                    VouncherNo = VM.VouncherNo,
                    Date = VM.Date,
                    Category = VM.Category,
                    CashAndBank = VM.CashAndBank,
                    BankId = VM.BankId,
                    Amount = VM.Amount,
                    Description = VM.Description,
                    Attachments = VM.Attachments
                };

                await _contraRepository.Create(model);
            }
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            Contra model = await _contraRepository.GetById(Id);
            ContraVM VM = new ContraVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.VouncherNo = model.VouncherNo;
                VM.Date = model.Date;
                VM.Category = model.Category;
                VM.CashAndBank = model.CashAndBank;
                VM.BankId = model.BankId;
                VM.Amount = model.Amount;
                VM.Description = model.Description;
                VM.Attachments = model.Attachments;
            }
            ViewBag.BankList = new SelectList(await _bankRepository.GetAll(), "Id", "BankName");
            return View(VM);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ContraVM VM)
        {
            Contra model = await _contraRepository.GetById(VM.Id);
            string fileName = "";
            if (VM.UploadAttachments != null && VM.UploadAttachments.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Contra");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                fileName = VM.UploadAttachments.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    VM.UploadAttachments.CopyTo(fileStream);
                }
                VM.Attachments = fileName;
            }

            if (model != null)
            {
                model.VouncherNo = VM.VouncherNo;
                model.Date = VM.Date;
                model.Category = VM.Category;
                model.CashAndBank = VM.CashAndBank;
                model.BankId = VM.BankId;
                model.Amount = VM.Amount;
                model.Description = VM.Description;
                if (!string.IsNullOrEmpty(fileName))
                {
                    model.Attachments = VM.Attachments;
                }

                await _contraRepository.Update(model);
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
                var model = await _contraRepository.GetById(id);
                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _contraRepository.Delete(model);
                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
