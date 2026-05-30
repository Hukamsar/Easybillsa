using AOne.DataAccess.ProfileService;
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
    public class DivisionApiController : ControllerBase
    {
        private readonly IDivisionRepository _divisionservice;
        private readonly ICompanyRepository _companyservice;
        private readonly IProfileService _profileService;

        public DivisionApiController(
            IDivisionRepository divisionservice,
            IProfileService profileService,
            ICompanyRepository companyservice)
        {
            _divisionservice = divisionservice;
            _profileService = profileService;
            _companyservice = companyservice;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            await _profileService.Set(User);
            var data = await _divisionservice.GetAll();
            return Ok(new { success = true, data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _divisionservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Division not found" });

            return Ok(new { success = true, data = model });
        }

        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _companyservice.GetAll();
            return Ok(new { success = true, data = companies });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DivisionVM Vm)
        {
            if (Vm == null)
                return BadRequest(new { success = false, message = "Invalid data." });

            var model = new Division
            {
                Name = Vm.Name,
                CompanyId = Vm.CompanyId
            };

            await _divisionservice.Create(model);
            return Ok(new { success = true, message = "Division created successfully.", data = model });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] DivisionVM Vm)
        {
            var model = await _divisionservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Division not found" });

            model.Name = Vm.Name;
            model.CompanyId = Vm.CompanyId;

            await _divisionservice.Update(model);
            return Ok(new { success = true, message = "Division updated successfully.", data = model });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "Invalid Id for deletion." });

                var model = await _divisionservice.GetById(id);
                if (model == null)
                    return NotFound(new { success = false, message = "Division not found." });

                await _divisionservice.Delete(model);
                return Ok(new { success = true, message = "Division deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
