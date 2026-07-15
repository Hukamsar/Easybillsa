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
    public class CategoryMasterApiController : ControllerBase
    {
        private readonly ICategoryMasterRepository _categoryservice;

        public CategoryMasterApiController(ICategoryMasterRepository categoryservice)
        {
            _categoryservice = categoryservice;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _categoryservice.GetAll();
            return Ok(new { success = true, data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _categoryservice.GetByCategoryMasterId(id);
            if (model == null)
            {
                return NotFound(new { success = false, message = "Category not found." });
            }

            return Ok(new { success = true, data = model });
        }

        [HttpPost]
        [HeadOfficeOnly]
        public async Task<IActionResult> Create([FromBody] CategoryMasterVM vm)
        {
            if (vm == null || string.IsNullOrWhiteSpace(vm.CategoryName))
            {
                return BadRequest(new { success = false, message = "Invalid category data." });
            }

            var model = new CategoryMaster
            {
                CategoryName = vm.CategoryName
            };

            await _categoryservice.Create(model);

            return Ok(new { success = true, message = "Category created successfully.", data = model });
        }

        [HttpPut("{id}")]
        [HeadOfficeOnly]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryMasterVM vm)
        {
            var model = await _categoryservice.GetByCategoryMasterId(id);
            if (model == null)
            {
                return NotFound(new { success = false, message = "Category not found." });
            }

            model.CategoryName = vm.CategoryName;

            await _categoryservice.Update(model);

            return Ok(new { success = true, message = "Category updated successfully.", data = model });
        }

        [HttpDelete("{id}")]
        [HeadOfficeOnly]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid Id for deletion." });
            }

            var model = await _categoryservice.GetByCategoryMasterId(id);

            if (model == null)
            {
                return NotFound(new { success = false, message = "Category not found." });
            }

            await _categoryservice.Delete(model);

            return Ok(new { success = true, message = "Category deleted successfully." });
        }
    }
}
