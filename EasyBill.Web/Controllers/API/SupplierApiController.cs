using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierApiController : ControllerBase
    {
        private readonly ISupplierRepository _supplierRepo;

        public SupplierApiController(ISupplierRepository supplierRepo)
        {
            _supplierRepo = supplierRepo;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _supplierRepo.GetALL();
            return Ok(data);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] SupplierVM vm)
        {
            if (vm == null) return BadRequest("Invalid data.");

            var model = new Supplier
            {
                FirstName = vm.FirstName,
                Address = vm.Address,
                CityId = vm.CityId,
                Area = vm.Area,
                PinCode = vm.PinCode,
                PhoneNO = vm.PhoneNO,
                Email = vm.Email,
                GstNO = vm.GstNO,
                ManufacturingLicNO = vm.ManufacturingLicNO,
                DrugLicNO = vm.DrugLicNO,
                IFSSAINo = vm.IFSSAINo,
                Type = vm.Type
            };

            await _supplierRepo.Create(model);
            return Ok(new { success = true, message = "Supplier created successfully.", id = model.Id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _supplierRepo.GetBySupplierId(id);
            if (model == null) return NotFound("Supplier not found.");

            var vm = new SupplierVM
            {
                Id = model.Id,
                FirstName = model.FirstName,
                Address = model.Address,
                CityId = model.CityId,
                Area = model.Area,
                PinCode = model.PinCode,
                PhoneNO = model.PhoneNO,
                Email = model.Email,
                GstNO = model.GstNO,
                ManufacturingLicNO = model.ManufacturingLicNO,
                DrugLicNO = model.DrugLicNO,
                IFSSAINo = model.IFSSAINo,
                Type = model.Type
            };

            return Ok(vm);
        }

        [HttpPut("edit/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] SupplierVM vm)
        {
            if (vm == null) return BadRequest("Invalid data.");

            var model = await _supplierRepo.GetBySupplierId(id);
            if (model == null) return NotFound("Supplier not found.");

            model.FirstName = vm.FirstName;
            model.Address = vm.Address;
            model.CityId = vm.CityId;
            model.Area = vm.Area;
            model.PinCode = vm.PinCode;
            model.PhoneNO = vm.PhoneNO;
            model.Email = vm.Email;
            model.GstNO = vm.GstNO;
            model.ManufacturingLicNO = vm.ManufacturingLicNO;
            model.DrugLicNO = vm.DrugLicNO;
            model.IFSSAINo = vm.IFSSAINo;
            model.Type = vm.Type;

            await _supplierRepo.Update(model);
            return Ok(new { success = true, message = "Supplier updated successfully." });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "Invalid Id for deletion." });

            var model = await _supplierRepo.GetBySupplierId(id);
            if (model == null) return NotFound(new { success = false, message = "Supplier not found." });

            await _supplierRepo.Delete(model);
            return Ok(new { success = true, message = "Supplier deleted successfully." });
        }
    }
}
