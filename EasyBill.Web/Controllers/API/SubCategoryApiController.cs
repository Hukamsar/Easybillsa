using AOne.Models.Entity;
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
    public class SubCategoryApiController : ControllerBase
    {
        private readonly ISubCategoryRepository _subCategoryservice;
        private readonly ICategoryMasterRepository _categoryservice;

        public SubCategoryApiController(ISubCategoryRepository subCategoryservice, ICategoryMasterRepository categoryservice)
        {
            _subCategoryservice = subCategoryservice;
            _categoryservice = categoryservice;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _subCategoryservice.GetAll();
            return Ok(data);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryservice.GetAll();
            categories.Insert(0, new CategoryMaster { Id = 0, CategoryName = "Select Category" });
            var result = categories.Select(c => new { c.Id, c.CategoryName });
            return Ok(result);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] SubCategoryVM vm)
        {
            if (vm == null) return BadRequest("Invalid data.");

            var model = new SubCategory
            {
                Name = vm.Name,
                CategoryId = vm.CategoryId
            };

            await _subCategoryservice.Create(model);
            return Ok(new { success = true, message = "SubCategory created successfully.", id = model.Id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var subcategorydata = await _subCategoryservice.GetById(id);
            if (subcategorydata == null) return NotFound("SubCategory not found.");

            var vm = new SubCategoryVM
            {
                Id = subcategorydata.Id,
                Name = subcategorydata.Name,
                CategoryId = subcategorydata.CategoryId
            };

            return Ok(vm);
        }

        [HttpPut("edit/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] SubCategoryVM vm)
        {
            if (vm == null) return BadRequest("Invalid data.");

            var subcategorydata = await _subCategoryservice.GetById(id);
            if (subcategorydata == null) return NotFound("SubCategory not found.");

            subcategorydata.Name = vm.Name;
            subcategorydata.CategoryId = vm.CategoryId;

            await _subCategoryservice.Update(subcategorydata);
            return Ok(new { success = true, message = "SubCategory updated successfully." });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "Invalid Id for deletion." });

            var model = await _subCategoryservice.GetById(id);
            if (model == null) return NotFound(new { success = false, message = "SubCategory not found." });

            await _subCategoryservice.Delete(model);
            return Ok(new { success = true, message = "SubCategory deleted successfully." });
        }
    }
}
