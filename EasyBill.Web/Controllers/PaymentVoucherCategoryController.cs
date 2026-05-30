using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class PaymentVoucherCategoryController : Controller
    {
        private readonly IPaymentVoucherCategoryRepository _paymentVouchercategoryservice;
        public PaymentVoucherCategoryController(IPaymentVoucherCategoryRepository paymentVouchercategoryservice)
        {
            _paymentVouchercategoryservice = paymentVouchercategoryservice;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _paymentVouchercategoryservice.GetAll();
           
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new PaymentVoucherCategoryVM();

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(PaymentVoucherCategoryVM VM)
        {
            if (VM != null)
            {
                var model = new PaymentVoucherCategory
                {
                    Name = VM.Name,
                    Description = VM.Description,
                };
                await _paymentVouchercategoryservice.Create(model);
            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            PaymentVoucherCategory model = await _paymentVouchercategoryservice.GetById(Id);
            PaymentVoucherCategoryVM VM = new PaymentVoucherCategoryVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.Name = model.Name;
                VM.Description = model.Description;
            }

            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(PaymentVoucherCategoryVM VM)
        {
            PaymentVoucherCategory model = await _paymentVouchercategoryservice.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.Name = VM.Name;
                model.Description = VM.Description;
                await _paymentVouchercategoryservice.Update(model);
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
                var model = await _paymentVouchercategoryservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _paymentVouchercategoryservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
