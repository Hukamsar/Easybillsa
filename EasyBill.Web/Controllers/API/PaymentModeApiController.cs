using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentModeApiController : ControllerBase
    {
        private readonly IModeOfPaymentRepository _modeOfPaymentService;

        public PaymentModeApiController(IModeOfPaymentRepository modeOfPaymentService)
        {
            _modeOfPaymentService = modeOfPaymentService;
        }
         
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _modeOfPaymentService.GetAll();
            return Ok(data);
        }
         
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid Id");

            var model = await _modeOfPaymentService.GetById(id);
            if (model == null)
                return NotFound("Payment mode not found");

            return Ok(model);
        }
         
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] ModeOfPaymentVM vm)
        {
            if (vm == null)
                return BadRequest("Invalid data");

            var model = new ModeOfPayment
            {
                Name = vm.Name,
                Description = vm.Description
            };

            await _modeOfPaymentService.Create(model);
            return Ok(new { success = true, message = "Payment mode created successfully" });
        }
         
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ModeOfPaymentVM vm)
        {
            if (id <= 0 || vm == null || id != vm.Id)
                return BadRequest("Invalid data");

            var model = await _modeOfPaymentService.GetById(id);
            if (model == null)
                return NotFound("Payment mode not found");

            model.Name = vm.Name;
            model.Description = vm.Description;

            await _modeOfPaymentService.Update(model);
            return Ok(new { success = true, message = "Payment mode updated successfully" });
        }
         
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid Id");

            var model = await _modeOfPaymentService.GetById(id);
            if (model == null)
                return NotFound("Payment mode not found");

            await _modeOfPaymentService.Delete(model);
            return Ok(new { success = true, message = "Payment mode deleted successfully" });
        }
    }
}
