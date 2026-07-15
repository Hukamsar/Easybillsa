using AOne.DataAccess.ProfileService;
using AOne.Models.Entity;
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
    public class HSNApiController : ControllerBase
    {
        private readonly IHSNRepository _hsnService;
        private readonly IProfileService _profileService;

        public HSNApiController(IHSNRepository hsnService, IProfileService profileService)
        {
            _hsnService = hsnService;
            _profileService = profileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            await _profileService.Set(User);
            var data = await _hsnService.GetAll();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var hsndata = await _hsnService.GetByHSNId(id);
            if (hsndata == null)
                return NotFound(new { error = "HSN not found." });

            return Ok(hsndata);
        }

        [HttpPost]
        [HeadOfficeOnly]
        public async Task<IActionResult> Create([FromBody] HSNVM vm)
        {
            if (vm == null)
                return BadRequest(new { error = "Invalid HSN data." });

            var model = new Hsn
            {
                HsnCode = vm.HsnCode,
                SGST = vm.SGST ?? 0,
                CGST = vm.CGST ?? 0,
                IGST = vm.IGST ?? 0,
                Cess = vm.Cess ?? 0,
                HsnType = vm.HsnType,
            };

            await _hsnService.Create(model);
            return Ok(new { success = true, message = "HSN created successfully." });
        }

        [HttpPut("{id}")]
        [HeadOfficeOnly]
        public async Task<IActionResult> Edit(int id, [FromBody] HSNVM vm)
        {
            if (vm == null || id != vm.Id)
                return BadRequest(new { error = "Invalid request." });

            var model = await _hsnService.GetByHSNId(id);
            if (model == null)
                return NotFound(new { error = "HSN not found." });

            model.HsnCode = vm.HsnCode;
            model.SGST = vm.SGST ?? 0;
            model.CGST = vm.CGST ?? 0;
            model.IGST = vm.IGST ?? 0;
            model.Cess = vm.Cess ?? 0;
            model.HsnType = vm.HsnType;

            await _hsnService.Update(model);
            return Ok(new { success = true, message = "HSN updated successfully." });
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            if (id <= 0)
                return BadRequest(new { error = "Invalid Id." });

            var hsndata = await _hsnService.GetByHSNId(id);
            if (hsndata == null)
                return NotFound(new { error = "HSN details not found." });

            var model = new
            {
                sgst = hsndata.SGST,
                cgst = hsndata.CGST,
                igst = hsndata.IGST,
                cess = hsndata.Cess,
            };

            return Ok(model);
        }

        [HttpDelete("{id}")]
        [HeadOfficeOnly]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "Invalid Id for deletion." });

                var model = await _hsnService.GetByHSNId(id);
                if (model == null)
                    return NotFound(new { success = false, message = "HSN not found." });

                await _hsnService.Delete(model);
                return Ok(new { success = true, message = "HSN deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
