using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using City = EasyBill.Models.Entity.City;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class CityApiController : ControllerBase
    {
        private readonly IStateRepository _stateservice;
        private readonly ICityRepository _cityservice;
        private readonly ICountryRepository _countryservice;

        public CityApiController(IStateRepository stateservice, ICityRepository cityservice, ICountryRepository countryservice)
        {
            _stateservice = stateservice;
            _cityservice = cityservice;
            _countryservice = countryservice;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _cityservice.GetAll();
            return Ok(new { success = true, data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _cityservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "City not found." });

            return Ok(new { success = true, data = model });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CityVM vm)
        {
            if (vm == null || string.IsNullOrWhiteSpace(vm.Name))
                return BadRequest(new { success = false, message = "Invalid city data." });

            var model = new City
            {
                Name = vm.Name,
                CountryId = vm.CountryId,
                StateId = vm.StateId
            };

            await _cityservice.Create(model);
            return Ok(new { success = true, message = "City created successfully.", data = model });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CityVM vm)
        {
            var model = await _cityservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "City not found." });

            model.Name = vm.Name;
            model.CountryId = vm.CountryId;
            model.StateId = vm.StateId;

            await _cityservice.Update(model);

            return Ok(new { success = true, message = "City updated successfully.", data = model });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "Invalid Id for deletion." });

            var model = await _cityservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "City not found." });

            await _cityservice.Delete(model);
            return Ok(new { success = true, message = "City deleted successfully." });
        }

        [HttpGet("GetStatesByCountry/{countryId}")]
        public async Task<IActionResult> GetStatesByCountry(int countryId)
        {
            var states = await _stateservice.GetByCountryId(countryId);
            var result = states.Select(s => new { s.Id, s.Name }).ToList();

            return Ok(new { success = true, data = result });
        }
    }
}
