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
    public class StateApiController : ControllerBase
    {
        private readonly ICountryRepository _countryservice;
        private readonly IStateRepository _stateservice;

        public StateApiController(ICountryRepository countryservice, IStateRepository stateservice)
        {
            _countryservice = countryservice;
            _stateservice = stateservice;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _stateservice.GetAll();
            var countries = await _countryservice.GetAll();

            var viewModel = data.Select(s => new
            {
                s.Id,
                s.Name,
                s.Zone,
                CountryId = s.CountryId,
                CountryName = countries.FirstOrDefault(c => c.Id == s.CountryId)?.Name
            });

            return Ok(viewModel);
        }

        [HttpGet("Get/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _stateservice.GetById(id);
            if (model == null)
                return NotFound();

            var country = await _countryservice.GetById(model.CountryId);
            var vm = new
            {
                model.Id,
                model.Name,
                model.Zone,
                model.CountryId,
                CountryName = country?.Name
            };

            return Ok(vm);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] StateVM VM)
        {
            if (VM == null)
                return BadRequest("Invalid input");

            var model = new State
            {
                Name = VM.Name,
                CountryId = VM.CountryId,
                Zone = VM.Zone
            };

            await _stateservice.Create(model);
            return Ok(new { success = true, message = "State created successfully." });
        }

        [HttpPut("Edit/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] StateVM VM)
        {
            if (VM == null)
                return BadRequest("Invalid input");

            var model = await _stateservice.GetById(id);
            if (model == null)
                return NotFound();

            model.Name = VM.Name;
            model.CountryId = VM.CountryId;
            model.Zone = VM.Zone;

            await _stateservice.Update(model);
            return Ok(new { success = true, message = "State updated successfully." });
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "Invalid Id for deletion." });

                var model = await _stateservice.GetById(id);
                if (model == null)
                    return NotFound(new { success = false, message = "Item not found." });

                await _stateservice.Delete(model);
                return Ok(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
