using AOne.Models.Entity;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Service.ExcelService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Buffers.Text;
using System.Data;
using EasyBill.UI.Filters;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes =JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class ItemMasterApiController : ControllerBase
    {
        private readonly ICategoryMasterRepository _categoryservice;
        private readonly IItemMasterRepository _itemmasterrepository;
        private readonly IHSNRepository _hsnservice;
        private readonly ISubCategoryRepository _subCategoryservice;
        private readonly IDivisionRepository _divisionservice;
        private readonly ICompanyRepository _companyservice;
        private readonly IExcelService _excelService;

        public ItemMasterApiController(
            IItemMasterRepository itemmasterrepository,
            ICategoryMasterRepository categoryservice,
            IHSNRepository hsnservice,
            ISubCategoryRepository subCategoryservice,
            IDivisionRepository divisionservice,
            ICompanyRepository companyservice,
            IExcelService excelService)
        {
            _itemmasterrepository = itemmasterrepository;
            _categoryservice = categoryservice;
            _hsnservice = hsnservice;
            _subCategoryservice = subCategoryservice;
            _divisionservice = divisionservice;
            _companyservice = companyservice;
            _excelService = excelService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string search = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var allData = await _itemmasterrepository.GetAll();
            var itemImages = await _itemmasterrepository.GetAllItemImages();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var imageDict = itemImages
           .Where(x => !x.IsDeleted)
           .GroupBy(x => x.ItemMasterId)
           .ToDictionary(
               g => g.Key,
               g => g.OrderBy(x => x.SortOrder)
                     .Select(x => $"{baseUrl}{x.ImagePath}")
                     .ToList()
           );

            if (!string.IsNullOrEmpty(search))
            {
                allData = allData.Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                             x.Code.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }
           

            int totalItems = allData.Count;
            var paginatedData = allData
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .Select(item => new ItemMasterListVM
              {
                  Id = item.Id,
                  Name = item.Name,
                  Code = item.Code,
                  Barcode = item.Barcode,
                  Unit1 = item.Unit1,
                  Unit2 = item.Unit2,
                  Packing = item.Packing,
                  CategoryId = item.CategoryId,
                  Category = item.Category,
                  DivisionId = item.DivisionId,
                  Division = item.Division,
                  HsnId = item.HsnId,
                  Hsn = item.Hsn,
                  Mrp = item.Mrp,
                  SalesRate1 = item.SalesRate1,
                  SalesRate2 = item.SalesRate2,
                  MinimumQty = item.MinimumQty,
                  MaximumQty = item.MaximumQty,
                  ShelfLife = item.ShelfLife,
                  ShelfLifeUnit = item.ShelfLifeUnit,
                  MaximumDiscount = item.MaximumDiscount,
                  DecemalAllowed = item.DecemalAllowed,
                  Conversion = item.Conversion,
                  SubCategoryId = item.SubCategoryId,
                  SubCategory = item.SubCategory,
                  CompanyId = item.CompanyId,
                  Company = item.Company,
                  Tenant = item.Tenant,
                  TenantId = item.TenantId,
                  UploadImage = item.UploadImage,
                  Local = item.Local,
                  Central = item.Central,
                  IsActive = item.IsActive,
                  Narcotics = item.Narcotics,
                  ScheduleH = item.ScheduleH,
                  ScheduleH1 = item.ScheduleH1,
                  Salt = item.Salt,
                  ItemType = item.ItemType,
                  ParentItemId = item.ParentItemId,
                  ParentItem = item.ParentItem,
                  ConversionFactor = item.ConversionFactor,
              
                  ItemImages = imageDict.TryGetValue(item.Id, out var images)
                      ? images.Select(x => new ItemImageVM
                      {
                          ImagePath = x
                      }).ToList()
                      : new List<ItemImageVM>()
              })
              .ToList();


            return Ok(new
            {
                TotalItems = totalItems,
                CurrentPage = page,
                PageSize = pageSize,
                Data = paginatedData
            });
        }
        // Get Item by Id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _itemmasterrepository.GetByItemMasterId(id);
            if (model == null) return NotFound(new { success = false, message = "Item not found." });
            return Ok(model);
        }

        //  Create new item
        [HttpPost]
        [HeadOfficeOnly]
        public async Task<IActionResult> Create([FromForm] ItemMasterVM itemMasterVM)
        {
            if (itemMasterVM == null)
                return BadRequest(new { success = false, message = "Invalid data." });

            var model = new ItemMaster
            {
                Name = itemMasterVM.Name,
                Code = itemMasterVM.Code,
                Barcode = itemMasterVM.Barcode,
                Unit1 = itemMasterVM.Unit,
                Packing = itemMasterVM.Packing,
                CategoryId = itemMasterVM.CategoryId == 0 ? null : itemMasterVM.CategoryId,
                SubCategoryId = itemMasterVM.SubCategoryId == 0 ? null : itemMasterVM.SubCategoryId,
                DivisionId = itemMasterVM.DivisionId == 0 ? null : itemMasterVM.DivisionId,
                HsnId = itemMasterVM.HsnId == 0 ? null : itemMasterVM.HsnId,
                CompanyId = itemMasterVM.CompanyId == 0 ? null : itemMasterVM.CompanyId,
                Mrp = itemMasterVM.Mrp,
                SalesRate1 = itemMasterVM.SalesRate1,
                SalesRate2 = itemMasterVM.SalesRate2,
                MinimumQty = itemMasterVM.MinimumQty,
                MaximumQty = itemMasterVM.MaximumQty,
                ShelfLife = itemMasterVM.ShelfLife,
                MaximumDiscount = itemMasterVM.MaximumDiscount,
                DecemalAllowed = itemMasterVM.DecemalAllowed,
                Conversion = itemMasterVM.Conversion
            };
            if (itemMasterVM.Photo != null && itemMasterVM.Photo.Length > 0)
            {

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Product");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = itemMasterVM.Photo.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    itemMasterVM.Photo.CopyTo(fileStream);
                }
                model.UploadImage = $"docs/Product/{fileName}";

            }

            var result = await _itemmasterrepository.Create(model);
            return Ok(new { success = true, message = "Item created successfully.", data = result });
        }

        //  Update Item
        [HttpPut("{id}")]
        [HeadOfficeOnly]
        public async Task<IActionResult> Update(int id, [FromForm] ItemMasterVM vm)
        {
            var model = await _itemmasterrepository.GetByItemMasterId(id);
            if (model == null)
                return NotFound(new { success = false, message = "Item not found." });

            model.Name = vm.Name;
            model.Code = vm.Code;
            model.Barcode = vm.Barcode;
            model.Unit1 = vm.Unit;
            model.Packing = vm.Packing;
            model.CategoryId = vm.CategoryId == 0 ? null : vm.CategoryId;
            model.SubCategoryId = vm.SubCategoryId == 0 ? null : vm.SubCategoryId;
            model.DivisionId = vm.DivisionId == 0 ? null : vm.DivisionId;
            model.HsnId = vm.HsnId == 0 ? null : vm.HsnId;
            model.CompanyId = vm.CompanyId == 0 ? null : vm.CompanyId;
            model.Mrp = vm.Mrp;
            model.SalesRate1 = vm.SalesRate1;
            model.SalesRate2 = vm.SalesRate2;
            model.MinimumQty = vm.MinimumQty;
            model.MaximumQty = vm.MaximumQty;
            model.ShelfLife = vm.ShelfLife;
            model.MaximumDiscount = vm.MaximumDiscount;
            model.DecemalAllowed = vm.DecemalAllowed;
            model.Conversion = vm.Conversion;
            if (vm.Photo != null && vm.Photo.Length > 0)
            {

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Product");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = vm.Photo.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    vm.Photo.CopyTo(fileStream);
                }
                model.UploadImage = $"docs/Product/{fileName}";

            }
            await _itemmasterrepository.Update(model);

            return Ok(new { success = true, message = "Item updated successfully." });
        }

        //  Delete Item
        [HttpDelete("{id}")]
        [HeadOfficeOnly]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "Invalid Id for deletion." });

            var model = await _itemmasterrepository.GetByItemMasterId(id);

            if (model == null)
                return NotFound(new { success = false, message = "Item not found." });

            await _itemmasterrepository.Delete(model);

            return Ok(new { success = true, message = "Item deleted successfully." });
        }


        //============for app================
        [HttpGet("GetAllCategoryItems")]
        public async Task<IActionResult> GetAllCategoryItems()
        {
            var allData = await _itemmasterrepository.GetAllCategoryItems();
            return Ok(allData);
        }
        [HttpGet("GetItemsByCategory/{categoryId}")]
        public async Task<IActionResult> GetItemsByCategory(int categoryId)
        {
            if (categoryId <= 0)
                return BadRequest("Invalid categoryId");
            var allData = await _itemmasterrepository.GetItemsByCategory(categoryId);
            return Ok(new
            {
                Data = allData
            });
        }
        [HttpGet("SearchItems")]
        public async Task<IActionResult> SearchItems([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Ok(new { Data = new List<object>() });

            var allData = await _itemmasterrepository.GetItemsBySearch(query);

            return Ok(allData);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetOrdersByCustomer")]
        public async Task<IActionResult> GetOrdersByCustomer()
        {
            var customerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "CustomerId");

            if (customerIdClaim == null)
                return Unauthorized("Customer not authenticated");

            int customerId = int.Parse(customerIdClaim.Value);
            var orders = await _itemmasterrepository.GetOrdersByCustomer(customerId);
            return Ok(orders);
        }

        [HttpGet("GetProductById/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid product id");

            var product = await _itemmasterrepository.GetProductById(id);

            if (product == null)
                return NotFound("Product not found");

            return Ok(new
            {
                success = true,
                data = product
            });
        }

    }
}
