using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBill.UI.Controllers
{
    public class PaymentModeController : Controller
    {
        private readonly IModeOfPaymentRepository _modeofpaymentservice;
        private readonly IBankRepository _bankRepo;

        public PaymentModeController(IModeOfPaymentRepository modeofpaymentservice, IBankRepository bankRepo)
        {
            _modeofpaymentservice = modeofpaymentservice;
            _bankRepo = bankRepo;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _modeofpaymentservice.GetAll();
            var viewModel = new ModeOfPaymentVM
            {
                ModeOfPayments = data,
            };
            ViewBag.BankList = new SelectList(await _bankRepo.GetAll(), "Id", "BankName");
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new ModeOfPaymentVM();
            ViewBag.BankList = new SelectList(await _bankRepo.GetAll(), "Id", "BankName");
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ModeOfPaymentVM VM)
        {
            if (VM != null)
            {
                var model = new ModeOfPayment
                {
                    Name = VM.Name,
                    Description = VM.Description,
                    PaymentType = VM.PaymentType,
                    BankId = VM.BankId
                };
                await _modeofpaymentservice.Create(model);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            ModeOfPayment model = await _modeofpaymentservice.GetById(Id);
            ModeOfPaymentVM VM = new ModeOfPaymentVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.Name = model.Name;
                VM.Description = model.Description;
                VM.PaymentType = model.PaymentType;
                VM.BankId = model.BankId;
            }
            ViewBag.BankList = new SelectList(await _bankRepo.GetAll(), "Id", "BankName", VM.BankId);
            return View(VM);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ModeOfPaymentVM VM)
        {
            ModeOfPayment model = await _modeofpaymentservice.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.Name = VM.Name;
                model.Description = VM.Description;
                model.PaymentType = VM.PaymentType;
                model.BankId = VM.BankId;
                await _modeofpaymentservice.Update(model);
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
                var model = await _modeofpaymentservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _modeofpaymentservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        public async Task<JsonResult> CheckDuplicate(string Name, int id)
        {
            bool result = await _modeofpaymentservice.CheckDuplicateAsync(Name, id);
            return Json(new
            {
                exists = result
            });
        }

        public async Task<JsonResult> GetPaymentModes()
        {
            var data = await _modeofpaymentservice.GetAll();
            return Json(new
            {
                success = true,
                data = data.Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    description = x.Description,
                    paymentType = x.PaymentType.HasValue ? (int?)((int)x.PaymentType.Value) : null,
                    bankId = x.BankId,
                    bankName = x.Bank?.BankName ?? string.Empty
                })
            });
        }

        public async Task<JsonResult> GetPaymentMode(int id)
        {
            var model = await _modeofpaymentservice.GetById(id);
            if (model == null)
            {
                return Json(new { success = false, message = "Payment mode not found." });
            }
            return Json(new
            {
                success = true,
                data = new
                {
                    id = model.Id,
                    name = model.Name,
                    description = model.Description,
                    paymentType = model.PaymentType.HasValue ? (int?)((int)model.PaymentType.Value) : null,
                    bankId = model.BankId
                }
            });
        }

        [HttpPost]
        public async Task<JsonResult> PaymentModeSave(ModeOfPaymentVM VM)
        {
            if (VM == null || string.IsNullOrWhiteSpace(VM.Name))
            {
                return Json(new { success = false, message = "Please enter Name." });
            }
            if (!VM.PaymentType.HasValue)
            {
                return Json(new { success = false, message = "Please select Payment Type." });
            }
            bool duplicate = await _modeofpaymentservice.CheckDuplicateAsync(VM.Name, VM.Id);
            if (duplicate)
            {
                return Json(new { success = false, message = "Mode Of Payment already exists." });
            }
            if (VM.Id > 0)
            {
                ModeOfPayment model = await _modeofpaymentservice.GetById(VM.Id);
                if (model == null)
                {
                    return Json(new { success = false, message = "Payment mode not found." });
                }
                model.Name = VM.Name;
                model.Description = VM.Description;
                model.PaymentType = VM.PaymentType;
                model.BankId = VM.BankId;
                await _modeofpaymentservice.Update(model);
                return Json(new
                {
                    success = true,
                    message = "Payment mode updated successfully.",
                    data = new
                    {
                        id = model.Id,
                        name = model.Name,
                        description = model.Description,
                        paymentType = model.PaymentType.HasValue ? (int?)((int)model.PaymentType.Value) : null,
                        bankId = model.BankId
                    }
                });
            }
            else
            {
                var model = new ModeOfPayment
                {
                    Name = VM.Name,
                    Description = VM.Description,
                    PaymentType = VM.PaymentType,
                    BankId = VM.BankId
                };
                await _modeofpaymentservice.Create(model);
                return Json(new
                {
                    success = true,
                    message = "Payment mode created successfully.",
                    data = new
                    {
                        id = model.Id,
                        name = model.Name,
                        description = model.Description,
                        paymentType = model.PaymentType.HasValue ? (int?)((int)model.PaymentType.Value) : null,
                        bankId = model.BankId
                    }
                });
            }
        }
    }
}
