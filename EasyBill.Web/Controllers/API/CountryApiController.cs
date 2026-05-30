using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class CountryApiController : ControllerBase
    {
        private readonly ICountryRepository _countryservice;

        public CountryApiController(ICountryRepository countryservice)
        {
            _countryservice = countryservice;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _countryservice.GetAll();
            return Ok(new { success = true, data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _countryservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Country not found." });

            return Ok(new { success = true, data = model });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CountryVM vm)
        {
            if (vm == null || string.IsNullOrWhiteSpace(vm.Name))
                return BadRequest(new { success = false, message = "Invalid country data." });

            var model = new Country
            {
                Name = vm.Name
            };

            await _countryservice.Create(model);

            return Ok(new { success = true, message = "Country created successfully.", data = model });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] CountryVM vm)
        {
            var model = await _countryservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Country not found." });

            model.Name = vm.Name;
            await _countryservice.Update(model);

            return Ok(new { success = true, message = "Country updated successfully.", data = model });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "Invalid Id for deletion." });

                var model = await _countryservice.GetById(id);
                if (model == null)
                    return NotFound(new { success = false, message = "Country not found." });

                await _countryservice.Delete(model);

                return Ok(new { success = true, message = "Country deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
