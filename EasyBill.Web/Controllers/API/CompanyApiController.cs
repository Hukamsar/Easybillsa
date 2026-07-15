using AOne.DataAccess.ProfileService;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EasyBill.UI.Filters;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyApiController : ControllerBase
    {
        private readonly ICompanyRepository _companyservice;
        private readonly IProfileService _profileService;

        public CompanyApiController(ICompanyRepository companyservice, IProfileService profileService)
        {
            _companyservice = companyservice;
            _profileService = profileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            await _profileService.Set(User);
            var data = await _companyservice.GetAll();
            return Ok(new { success = true, data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _companyservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Company not found." });

            return Ok(new { success = true, data = model });
        }

        [HttpPost]
        [HeadOfficeOnly]
        public async Task<IActionResult> Create([FromBody] Company vm)
        {
            if (vm == null || string.IsNullOrWhiteSpace(vm.Name))
                return BadRequest(new { success = false, message = "Invalid company data." });

            var model = new Company
            {
                Name = vm.Name
            };

            await _companyservice.Create(model);

            return Ok(new { success = true, message = "Company created successfully.", data = model });
        }

        [HttpPut("{id}")]
        [HeadOfficeOnly]
        public async Task<IActionResult> Update(int id, [FromBody] CompanyVM vm)
        {
            var model = await _companyservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Company not found." });

            model.Name = vm.Name;

            await _companyservice.Update(model);

            return Ok(new { success = true, message = "Company updated successfully.", data = model });
        }

        [HttpDelete("{id}")]
        [HeadOfficeOnly]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "Invalid Id for deletion." });

                var model = await _companyservice.GetById(id);
                if (model == null)
                    return NotFound(new { success = false, message = "Company not found." });

                await _companyservice.Delete(model);

                return Ok(new { success = true, message = "Company deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
