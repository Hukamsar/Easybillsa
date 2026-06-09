using AOne.DataAccess.ProfileService;
using AOne.Models.Entity;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Service.ExcelService;
using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Data;
using System.Security.Claims;

namespace EasyBill.UI.Controllers
{
    public class ItemMasterController : Controller
    {
        private readonly ICategoryMasterRepository _categoryservice;
        private readonly IItemMasterRepository _itemmasterrepository;
        private readonly IItemImageRepository _itemimagerepository;
        private readonly IHSNRepository _Hsnservice;
        private readonly ISubCategoryRepository _subCategoryservice;
        private readonly IDivisionRepository _divisionservice;
        private readonly ICompanyRepository _companyservice;
        private readonly IExcelService _excelService;
        private readonly ITenantRegistrationRepository _tenantRepository;
        public ItemMasterController(
            IItemMasterRepository itemmasterrepository,
            IItemImageRepository itemimagerepository,
            ICategoryMasterRepository categoryservice,
            IHSNRepository Hsnservice,
            ISubCategoryRepository subCategoryservice,
            IDivisionRepository divisionservice,
            ICompanyRepository companyservice,
            IExcelService excelService,
            ITenantRegistrationRepository tenantRegistration)
        {

            _itemmasterrepository = itemmasterrepository;
            _itemimagerepository = itemimagerepository;
            _categoryservice = categoryservice;
            _Hsnservice = Hsnservice;
            _subCategoryservice = subCategoryservice;
            _divisionservice = divisionservice;
            _companyservice = companyservice;
            _excelService = excelService;
            _tenantRepository = tenantRegistration;
        }

        public async Task<IActionResult> Index(string search = "", int page = 1, int pageSize = 10)
        {
            //await _profileService.Set(User);
            var allData = await _itemmasterrepository.GetAll(); // Or apply filtering in DB if possible

            // Filter
            if (!string.IsNullOrEmpty(search))
            {
                allData = allData.Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                             x.Code.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Pagination
            int totalItems = allData.Count;
            var paginatedData = allData
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Search = search;

            return View(paginatedData);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {

            var tenantId = User?.FindFirst("TenantId")?.Value;
            var user = (await _tenantRepository.GetAll())
           .FirstOrDefault(x => x.Id == tenantId);
            var businessType = user?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;

            IList<CategoryMaster> categories = await _categoryservice.GetAll();
            categories.Insert(0, new CategoryMaster { Id = 0, CategoryName = "Select Category" });
            ViewBag.Categories = new SelectList(categories, "Id", "CategoryName");

            ViewBag.SubCategories = new SelectList(new List<SelectListItem>{new SelectListItem { Value = "0", Text = "SelectSubCategory" }},
                "Value",
                "Text"
            );

            IList<Hsn> Hsns = await _Hsnservice.GetAll();
            Hsns.Insert(0, new Hsn { Id = 0, HsnCode = "Select Code" });
            ViewBag.Hsn = new SelectList(Hsns, "Id", "HsnCode");

            ViewBag.Local = Enum.GetValues(typeof(TaxStatus))
                            .Cast<TaxStatus>()
                            .Select(e => new SelectListItem
                            {
                                Value = ((int)e).ToString(),
                                Text = e.ToString()
                            })
                            .ToList();

            ViewBag.Central = Enum.GetValues(typeof(TaxStatus))
                .Cast<TaxStatus>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();

            ViewBag.division = new SelectList(new List<SelectListItem>{new SelectListItem { Value = "0", Text = "Select Division" }},
                "Value",
                "Text"
            );

            IList<Company> company = await _companyservice.GetAll();
            company.Insert(0, new Company { Id = 0, Name = "Select company" });
            ViewBag.company = new SelectList(company, "Id", "Name");

            var allItems = await _itemmasterrepository.GetAll();
            var parentItems = allItems.Where(x => x.ItemType == "Bulk" && x.IsActive == true).ToList();
            parentItems.Insert(0, new ItemMaster { Id = 0, Name = "Select Parent Bulk Item" });
            ViewBag.ParentItems = new SelectList(parentItems, "Id", "Name");

            var viewModel = new ItemMasterVM
            {
                Code = await GenerateNxtNumber(), 
                IsActive=true
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(ItemMasterVM itemMasterVM)
        {
            if (!ModelState.IsValid)
                return View(itemMasterVM);

            if (itemMasterVM != null)
            {
                var duplicateMessage = await GetItemMasterDuplicateMessageAsync(itemMasterVM.Code, itemMasterVM.Name, itemMasterVM.Barcode);
                if (!string.IsNullOrWhiteSpace(duplicateMessage))
                {
                    TempData["error"] = duplicateMessage;
                    return RedirectToAction("Create");
                }

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
                    Local = itemMasterVM.Local,
                    Central = itemMasterVM.Central,
                    CompanyId = itemMasterVM.CompanyId == 0 ? null : itemMasterVM.CompanyId,
                    Mrp = itemMasterVM.Mrp,
                    SalesRate1 = itemMasterVM.SalesRate1,
                    SalesRate2 = itemMasterVM.SalesRate2,
                    MinimumQty = itemMasterVM.MinimumQty,
                    MaximumQty = itemMasterVM.MaximumQty,
                    ShelfLife = itemMasterVM.ShelfLife,
                    ShelfLifeUnit = itemMasterVM.ShelfLifeUnit,
                    MaximumDiscount = itemMasterVM.MaximumDiscount,
                    DecemalAllowed = itemMasterVM.DecemalAllowed,
                    Conversion = itemMasterVM.Conversion,
                    IsActive = itemMasterVM.IsActive ?? true,
                    Narcotics = itemMasterVM.Narcotics,
                    ScheduleH = itemMasterVM.ScheduleH,
                    ScheduleH1 = itemMasterVM.ScheduleH1,
                    Salt = itemMasterVM.Salt,
                    ItemType = itemMasterVM.ItemType,
                    ParentItemId = itemMasterVM.ParentItemId == 0 ? null : itemMasterVM.ParentItemId,
                    ConversionFactor = itemMasterVM.ConversionFactor
                };

                await _itemmasterrepository.Create(model);
                await SaveItemImagesAsync(model.Id, itemMasterVM.Images, itemMasterVM.PrimaryIndex);

                TempData["success"] = "Item created successfully";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> QuickCreateFromImport(
            string? rowKey,
            string? itemName,
            string? barcode,
            string? batch,
            int? qty,
            int? freeQty,
            decimal? mrp,
            decimal? rate,
            decimal? gst,
            decimal? cgst,
            decimal? sgst,
            decimal? cess)
        {
            var matchedHsnId = await TryMatchHsnIdAsync(gst, cgst, sgst, cess);

            var vm = new QuickCreateItemFromImportVM
            {
                RowKey = rowKey,
                Name = itemName?.Trim() ?? string.Empty,
                Code = await GenerateUniqueItemCodeAsync(),
                Barcode = NormalizeOptionalText(barcode),
                Mrp = mrp ?? 0m,
                SalesRate1 = rate ?? mrp ?? 0m,
                SalesRate2 = rate ?? mrp ?? 0m,
                Conversion = 1,
                Local = TaxStatus.Taxable,
                Central = TaxStatus.Taxable,
                IsActive = true,
                HsnId = matchedHsnId,
                SourceItemName = itemName?.Trim(),
                SourceBatch = batch?.Trim(),
                SourceQty = qty,
                SourceFreeQty = freeQty,
                SourceRate = rate,
                SourceGst = gst,
                SourceCgst = cgst,
                SourceSgst = sgst,
                SourceCess = cess
            };

            await PopulateQuickCreateImportLookupsAsync(vm.CompanyId, vm.CategoryId, vm.Local, vm.Central);
            return PartialView("_QuickCreateFromImport", vm);
        }

        [HttpPost]
        public async Task<IActionResult> QuickCreateFromImport(QuickCreateItemFromImportVM vm)
        {
            vm.Name = vm.Name?.Trim() ?? string.Empty;
            vm.Code = vm.Code?.Trim() ?? string.Empty;
            vm.Barcode = NormalizeOptionalText(vm.Barcode);
            vm.Unit = NormalizeOptionalText(vm.Unit);
            vm.Packing = NormalizeOptionalText(vm.Packing);
            vm.ShelfLifeUnit = NormalizeOptionalText(vm.ShelfLifeUnit);
            vm.Salt = NormalizeOptionalText(vm.Salt);

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please fill the required item details.",
                    errors = GetModelStateErrors()
                });
            }

            var allItems = await _itemmasterrepository.GetAll();
            var activeItems = allItems.Where(x => x.IsActive).ToList();

            if (!string.IsNullOrWhiteSpace(vm.Barcode))
            {
                var existingBarcodeItem = activeItems.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(x.Barcode) &&
                    x.Barcode.Trim().Equals(vm.Barcode, StringComparison.OrdinalIgnoreCase));

                if (existingBarcodeItem != null)
                {
                    var existingHsn = existingBarcodeItem.Hsn;

                    if (existingHsn == null && existingBarcodeItem.HsnId.HasValue)
                    {
                        existingHsn = (await _Hsnservice.GetAll()).FirstOrDefault(x => x.Id == existingBarcodeItem.HsnId.Value);
                    }

                    return Json(new
                    {
                        success = true,
                        existingItemUsed = true,
                        rowKey = vm.RowKey,
                        message = "Barcode already exists, so the existing item was linked.",
                        item = new
                        {
                            id = existingBarcodeItem.Id,
                            name = existingBarcodeItem.Name,
                            code = existingBarcodeItem.Code,
                            barcode = existingBarcodeItem.Barcode ?? string.Empty,
                            mrp = existingBarcodeItem.Mrp,
                            salesRate1 = existingBarcodeItem.SalesRate1,
                            salesRate2 = existingBarcodeItem.SalesRate2,
                            hsnId = existingBarcodeItem.HsnId ?? 0,
                            gst = existingHsn?.IGST ?? 0m,
                            cgst = existingHsn?.CGST ?? 0m,
                            sgst = existingHsn?.SGST ?? 0m,
                            cess = existingHsn?.Cess ?? 0m
                        }
                    });
                }
            }

            if (string.IsNullOrWhiteSpace(vm.Code) || await _itemmasterrepository.ExistsByCode(vm.Code))
            {
                vm.Code = await GenerateUniqueItemCodeAsync();
            }

            var hsn = (await _Hsnservice.GetAll()).FirstOrDefault(x => x.Id == vm.HsnId);

            var model = new ItemMaster
            {
                Name = vm.Name,
                Code = vm.Code,
                Barcode = vm.Barcode,
                Unit1 = vm.Unit,
                Packing = vm.Packing,
                CategoryId = vm.CategoryId == 0 ? null : vm.CategoryId,
                SubCategoryId = vm.SubCategoryId == 0 ? null : vm.SubCategoryId,
                DivisionId = vm.DivisionId == 0 ? null : vm.DivisionId,
                HsnId = vm.HsnId == 0 ? null : vm.HsnId,
                Local = vm.Local,
                Central = vm.Central,
                CompanyId = vm.CompanyId == 0 ? null : vm.CompanyId,
                Mrp = vm.Mrp,
                SalesRate1 = vm.SalesRate1,
                SalesRate2 = vm.SalesRate2,
                MinimumQty = vm.MinimumQty,
                MaximumQty = vm.MaximumQty,
                ShelfLife = vm.ShelfLife,
                ShelfLifeUnit = vm.ShelfLifeUnit,
                MaximumDiscount = vm.MaximumDiscount,
                DecemalAllowed = vm.DecemalAllowed,
                Conversion = vm.Conversion <= 0 ? 1 : vm.Conversion,
                IsActive = vm.IsActive ?? true,
                Narcotics = vm.Narcotics,
                ScheduleH = vm.ScheduleH,
                ScheduleH1 = vm.ScheduleH1,
                Salt = vm.Salt
            };

            await _itemmasterrepository.Create(model);
            await SaveItemImagesAsync(model.Id, vm.Images, vm.PrimaryIndex);

            return Json(new
            {
                success = true,
                rowKey = vm.RowKey,
                message = "Item created successfully.",
                item = new
                {
                    id = model.Id,
                    name = model.Name,
                    code = model.Code,
                    barcode = model.Barcode ?? string.Empty,
                    mrp = model.Mrp,
                    salesRate1 = model.SalesRate1,
                    salesRate2 = model.SalesRate2,
                    hsnId = model.HsnId ?? 0,
                    gst = hsn?.IGST ?? 0m,
                    cgst = hsn?.CGST ?? 0m,
                    sgst = hsn?.SGST ?? 0m,
                    cess = hsn?.Cess ?? 0m
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> LoadItemMasterPartial()
        {
            await PopulateEmbeddedItemMasterLookupsAsync();

            var viewModel = new ItemMasterVM
            {
                Code = await GenerateUniqueItemCodeAsync(),
                IsActive = true
            };

            return PartialView("_ItemMasterPartial", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> LoadItemMasterEditPartial(int id)
        {
            var model = await _itemmasterrepository.GetByItemMasterId(id);
            if (model == null)
            {
                return NotFound();
            }

            await PopulateEmbeddedItemMasterLookupsAsync(model.CompanyId, model.CategoryId);
            return PartialView("_ItemMasterPartial", BuildEmbeddedItemMasterVm(model));
        }

        [HttpPost]
        public async Task<IActionResult> CreateItemFromSales([FromForm] ItemMasterVM vm)
        {
            if (vm == null)
            {
                return Json(new { success = false, message = "Invalid item data." });
            }

            NormalizeEmbeddedItemMasterVm(vm);

            var duplicateMessage = await GetItemMasterDuplicateMessageAsync(vm.Code, vm.Name, vm.Barcode);
            if (!string.IsNullOrWhiteSpace(duplicateMessage))
            {
                return Json(new { success = false, message = duplicateMessage });
            }

            var model = new ItemMaster();
            ApplyEmbeddedItemMasterVm(model, vm);

            await _itemmasterrepository.Create(model);
            await SaveItemImagesAsync(model.Id, vm.Images, vm.PrimaryIndex);

            var hsn = await GetEmbeddedItemHsnAsync(model.HsnId);

            return Json(BuildEmbeddedItemMasterResponse(model, hsn, "Item created successfully."));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateItemFromSales([FromForm] ItemMasterVM vm)
        {
            if (vm == null)
            {
                return Json(new { success = false, message = "Invalid item data." });
            }

            if (vm.Id <= 0)
            {
                return await CreateItemFromSales(vm);
            }

            NormalizeEmbeddedItemMasterVm(vm);

            var model = await _itemmasterrepository.GetByItemMasterId(vm.Id);
            if (model == null)
            {
                return Json(new { success = false, message = "Item not found." });
            }

            var duplicateMessage = await GetItemMasterDuplicateMessageAsync(vm.Code, vm.Name, vm.Barcode, vm.Id);
            if (!string.IsNullOrWhiteSpace(duplicateMessage))
            {
                return Json(new { success = false, message = duplicateMessage });
            }

            ApplyEmbeddedItemMasterVm(model, vm);

            await _itemmasterrepository.Update(model);
            await SaveItemImagesAsync(model.Id, vm.Images, vm.PrimaryIndex);

            var hsn = await GetEmbeddedItemHsnAsync(model.HsnId);

            return Json(BuildEmbeddedItemMasterResponse(model, hsn, "Item updated successfully."));
        }
        
        
        [HttpGet]
        public async Task<IActionResult> GetDivisionByCompany(int companyId)
        {
            if (companyId <= 0)
            {
                return Json(new List<SelectListItem>());
            }

            var divisions = (await _divisionservice.GetAll())
                            .Where(x => x.CompanyId == companyId)
                            .Select(x => new SelectListItem
                            {
                                Value = x.Id.ToString(),
                                Text = x.Name
                            })
                            .ToList();

            divisions.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "Select Division"
            });

            return Json(divisions);
        }
        [HttpGet]
        public async Task<IActionResult> GetSubCategoryByCategory(int categoryId)
        {
            if (categoryId <= 0)
            {
                return Json(new List<SelectListItem>());
            }

            var subCategories = (await _subCategoryservice.GetAll())
                                .Where(x => x.CategoryId == categoryId)
                                .Select(x => new SelectListItem
                                {
                                    Value = x.Id.ToString(),
                                    Text = x.Name
                                })
                                .ToList();

            subCategories.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "Select SubCategory"
            });

            return Json(subCategories);
        }

        public async Task<string> GenerateNxtNumber()
        {
            var data = await _itemmasterrepository.GetAll();
            var lastCode = data.Where(p => !string.IsNullOrEmpty(p.Code)).Select(p => p.Code).LastOrDefault();
            return GenerateNextProductCode(lastCode);
        }

        private string GenerateNextProductCode(string lastCode)
        {
            if (string.IsNullOrEmpty(lastCode) || lastCode.Length < 2)
                return "IT0001";

            string prefix = new string(lastCode.TakeWhile(c => !char.IsDigit(c)).ToArray());

            string numberPart = new string(lastCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

            int number = 0;
            int.TryParse(numberPart, out number);

            string nextCode = prefix + (number + 1).ToString("D" + numberPart.Length);

            return nextCode;
        }

        private async Task<string> GenerateUniqueItemCodeAsync()
        {
            var nextCode = await GenerateNxtNumber();

            while (await _itemmasterrepository.ExistsByCode(nextCode))
            {
                nextCode = GenerateNextProductCode(nextCode);
            }

            return nextCode;
        }

        private async Task PopulateQuickCreateImportLookupsAsync(
            int? companyId = null,
            int? categoryId = null,
            TaxStatus? local = TaxStatus.Taxable,
            TaxStatus? central = TaxStatus.Taxable)
        {
            var categories = await _categoryservice.GetAll();
            var categoryList = categories
                .OrderBy(x => x.CategoryName)
                .ToList();

            categoryList.Insert(0, new CategoryMaster
            {
                Id = 0,
                CategoryName = "Select Category"
            });

            var subCategories = (await _subCategoryservice.GetAll())
                .Where(x => !categoryId.HasValue || categoryId <= 0 || x.CategoryId == categoryId)
                .OrderBy(x => x.Name)
                .ToList();

            subCategories.Insert(0, new SubCategory
            {
                Id = 0,
                Name = "Select SubCategory"
            });

            var companies = await _companyservice.GetAll();
            var companyList = companies
                .OrderBy(x => x.Name)
                .ToList();

            companyList.Insert(0, new Company
            {
                Id = 0,
                Name = "Select Company"
            });

            var divisions = companyId.HasValue && companyId > 0
                ? (await _divisionservice.GetByCompanyList(companyId.Value)).OrderBy(x => x.Name).ToList()
                : new List<Division>();

            divisions.Insert(0, new Division
            {
                Id = 0,
                Name = "Select Division"
            });

            var hsns = await _Hsnservice.GetAll();
            var hsnList = hsns
                .OrderBy(x => x.HsnCode)
                .ToList();

            hsnList.Insert(0, new Hsn
            {
                Id = 0,
                HsnCode = "Select HSN"
            });

            ViewBag.ImportQuickCreateCategories = categoryList;
            ViewBag.ImportQuickCreateSubCategories = subCategories;
            ViewBag.ImportQuickCreateCompanies = companyList;
            ViewBag.ImportQuickCreateDivisions = divisions;
            ViewBag.ImportQuickCreateHsns = hsnList;
            PopulateQuickCreateTaxStatusLookups(local, central);
        }

        private Dictionary<string, string[]> GetModelStateErrors()
        {
            return ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Invalid value."
                            : error.ErrorMessage)
                        .Distinct()
                        .ToArray());
        }

        private static string? NormalizeOptionalText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        // Prevent duplicate Item Master save by code, name, or barcode.
        private async Task<string?> GetItemMasterDuplicateMessageAsync(string? code, string? name, string? barcode, int currentItemId = 0)
        {
            var items = await _itemmasterrepository.GetAll();
            var itemCode = code?.Trim();
            var itemName = name?.Trim();
            var itemBarcode = barcode?.Trim();

            if (!string.IsNullOrWhiteSpace(itemCode) &&
                items.Any(x => x.Id != currentItemId &&
                               !string.IsNullOrWhiteSpace(x.Code) &&
                               x.Code.Trim().Equals(itemCode, StringComparison.OrdinalIgnoreCase)))
            {
                return "Item code already exists.";
            }

            if (!string.IsNullOrWhiteSpace(itemName) &&
                items.Any(x => x.Id != currentItemId &&
                               !string.IsNullOrWhiteSpace(x.Name) &&
                               x.Name.Trim().Equals(itemName, StringComparison.OrdinalIgnoreCase)))
            {
                return "Item name already exists.";
            }

            if (!string.IsNullOrWhiteSpace(itemBarcode) &&
                items.Any(x => x.Id != currentItemId &&
                               !string.IsNullOrWhiteSpace(x.Barcode) &&
                               x.Barcode.Trim().Equals(itemBarcode, StringComparison.OrdinalIgnoreCase)))
            {
                return "Item barcode already exists.";
            }

            return null;
        }

        private async Task PopulateEmbeddedItemMasterLookupsAsync(int? companyId = null, int? categoryId = null)
        {
            var tenantId = User?.FindFirst("TenantId")?.Value;
            var user = (await _tenantRepository.GetAll())
                .FirstOrDefault(x => x.Id == tenantId);
            var businessType = user?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;

            IList<CategoryMaster> categories = await _categoryservice.GetAll();
            categories.Insert(0, new CategoryMaster { Id = 0, CategoryName = "Select Category" });
            ViewBag.Categories = new SelectList(categories, "Id", "CategoryName");

            if (categoryId.HasValue)
            {
                IList<SubCategory> subCategories = (await _subCategoryservice.GetAll())
                    .Where(x => x.CategoryId == categoryId.Value)
                    .ToList();
                subCategories.Insert(0, new SubCategory { Id = 0, Name = "Select SubCategory" });
                ViewBag.SubCategories = new SelectList(subCategories, "Id", "Name");
            }
            else
            {
                ViewBag.SubCategories = new SelectList(
                    new List<SelectListItem> { new SelectListItem { Value = "0", Text = "Select SubCategory" } },
                    "Value",
                    "Text"
                );
            }

            IList<Hsn> hsns = await _Hsnservice.GetAll();
            hsns.Insert(0, new Hsn { Id = 0, HsnCode = "Select Code" });
            ViewBag.Hsn = new SelectList(hsns, "Id", "HsnCode");

            ViewBag.Local = Enum.GetValues(typeof(TaxStatus))
                .Cast<TaxStatus>()
                .Select(e => new SelectListItem { Value = ((int)e).ToString(), Text = e.ToString() })
                .ToList();

            ViewBag.Central = Enum.GetValues(typeof(TaxStatus))
                .Cast<TaxStatus>()
                .Select(e => new SelectListItem { Value = ((int)e).ToString(), Text = e.ToString() })
                .ToList();

            if (companyId.HasValue)
            {
                IList<Division> divisions = (await _divisionservice.GetAll())
                    .Where(x => x.CompanyId == companyId.Value)
                    .ToList();
                divisions.Insert(0, new Division { Id = 0, Name = "Select Division" });
                ViewBag.division = new SelectList(divisions, "Id", "Name");
            }
            else
            {
                ViewBag.division = new SelectList(
                    new List<SelectListItem> { new SelectListItem { Value = "0", Text = "Select Division" } },
                    "Value",
                    "Text"
                );
            }

            IList<Company> companies = await _companyservice.GetAll();
            companies.Insert(0, new Company { Id = 0, Name = "Select Company" });
            ViewBag.company = new SelectList(companies, "Id", "Name");

            var allItems = await _itemmasterrepository.GetAll();
            var parentItems = allItems.Where(x => x.ItemType == "Bulk" && x.IsActive == true).ToList();
            parentItems.Insert(0, new ItemMaster { Id = 0, Name = "Select Parent Bulk Item" });
            ViewBag.ParentItems = new SelectList(parentItems, "Id", "Name");
        }

        private static ItemMasterVM BuildEmbeddedItemMasterVm(ItemMaster model)
        {
            return new ItemMasterVM
            {
                Id = model.Id,
                Code = model.Code,
                Name = model.Name,
                Barcode = model.Barcode,
                Unit = model.Unit1,
                Packing = model.Packing,
                CompanyId = model.CompanyId,
                DivisionId = model.DivisionId,
                CategoryId = model.CategoryId,
                SubCategoryId = model.SubCategoryId,
                HsnId = model.HsnId,
                Local = model.Local,
                Central = model.Central,
                Mrp = model.Mrp,
                SalesRate1 = model.SalesRate1,
                SalesRate2 = model.SalesRate2,
                MinimumQty = model.MinimumQty,
                MaximumQty = model.MaximumQty,
                ShelfLife = model.ShelfLife,
                ShelfLifeUnit = model.ShelfLifeUnit,
                MaximumDiscount = model.MaximumDiscount,
                DecemalAllowed = model.DecemalAllowed,
                Conversion = model.Conversion,
                IsActive = model.IsActive,
                Narcotics = model.Narcotics,
                ScheduleH = model.ScheduleH,
                ScheduleH1 = model.ScheduleH1,
                Salt = model.Salt,
                ItemType = model.ItemType,
                ParentItemId = model.ParentItemId,
                ConversionFactor = model.ConversionFactor
            };
        }

        private static void NormalizeEmbeddedItemMasterVm(ItemMasterVM vm)
        {
            vm.Name = vm.Name?.Trim() ?? string.Empty;
            vm.Code = vm.Code?.Trim() ?? string.Empty;
            vm.Barcode = NormalizeOptionalText(vm.Barcode);
            vm.Unit = NormalizeOptionalText(vm.Unit);
            vm.Packing = NormalizeOptionalText(vm.Packing);
            vm.ShelfLifeUnit = NormalizeOptionalText(vm.ShelfLifeUnit);
            vm.Salt = NormalizeOptionalText(vm.Salt);
        }

        private static void ApplyEmbeddedItemMasterVm(ItemMaster model, ItemMasterVM vm)
        {
            model.Name = vm.Name;
            model.Code = vm.Code;
            model.Barcode = vm.Barcode;
            model.Unit1 = vm.Unit;
            model.Packing = vm.Packing;
            model.CategoryId = vm.CategoryId == 0 ? null : vm.CategoryId;
            model.SubCategoryId = vm.SubCategoryId == 0 ? null : vm.SubCategoryId;
            model.DivisionId = vm.DivisionId == 0 ? null : vm.DivisionId;
            model.HsnId = vm.HsnId == 0 ? null : vm.HsnId;
            model.Local = vm.Local;
            model.Central = vm.Central;
            model.CompanyId = vm.CompanyId == 0 ? null : vm.CompanyId;
            model.Mrp = vm.Mrp;
            model.SalesRate1 = vm.SalesRate1;
            model.SalesRate2 = vm.SalesRate2;
            model.MinimumQty = vm.MinimumQty;
            model.MaximumQty = vm.MaximumQty;
            model.ShelfLife = vm.ShelfLife;
            model.ShelfLifeUnit = vm.ShelfLifeUnit;
            model.MaximumDiscount = vm.MaximumDiscount;
            model.DecemalAllowed = vm.DecemalAllowed;
            model.Conversion = vm.Conversion <= 0 ? 1 : vm.Conversion;
            model.IsActive = vm.IsActive ?? true;
            model.Narcotics = vm.Narcotics;
            model.ScheduleH = vm.ScheduleH;
            model.ScheduleH1 = vm.ScheduleH1;
            model.Salt = vm.Salt;
            model.ItemType = vm.ItemType;
            model.ParentItemId = vm.ParentItemId == 0 ? null : vm.ParentItemId;
            model.ConversionFactor = vm.ConversionFactor;
        }

        private async Task<Hsn?> GetEmbeddedItemHsnAsync(int? hsnId)
        {
            if (!hsnId.HasValue || hsnId.Value <= 0)
            {
                return null;
            }

            return (await _Hsnservice.GetAll()).FirstOrDefault(x => x.Id == hsnId.Value);
        }

        private static object BuildEmbeddedItemMasterResponse(ItemMaster model, Hsn? hsn, string message)
        {
            return new
            {
                success = true,
                message,
                itemId = model.Id,
                itemName = model.Name,
                gst = hsn?.IGST ?? 0m,
                cgst = hsn?.CGST ?? 0m,
                sgst = hsn?.SGST ?? 0m,
                cess = hsn?.Cess ?? 0m,
                hsn = model.HsnId ?? 0,
                hsnId = model.HsnId ?? 0,
                hsnCode = hsn?.HsnCode ?? string.Empty,
                maximumDiscount = model.MaximumDiscount,
                minimumQty = model.MinimumQty,
                conversion = model.Conversion > 0 ? model.Conversion : 1
            };
        }

        private void PopulateQuickCreateTaxStatusLookups(
            TaxStatus? local = TaxStatus.Taxable,
            TaxStatus? central = TaxStatus.Taxable)
        {
            ViewBag.Local = Enum.GetValues(typeof(TaxStatus))
                .Cast<TaxStatus>()
                .Select(status => new SelectListItem
                {
                    Value = ((int)status).ToString(),
                    Text = status.ToString(),
                    Selected = local.HasValue && status == local.Value
                })
                .ToList();

            ViewBag.Central = Enum.GetValues(typeof(TaxStatus))
                .Cast<TaxStatus>()
                .Select(status => new SelectListItem
                {
                    Value = ((int)status).ToString(),
                    Text = status.ToString(),
                    Selected = central.HasValue && status == central.Value
                })
                .ToList();
        }

        private async Task<int?> TryMatchHsnIdAsync(
            decimal? gst,
            decimal? cgst,
            decimal? sgst,
            decimal? cess)
        {
            var normalizedGst = Math.Round(gst ?? 0m, 2);
            var normalizedCgst = Math.Round(cgst ?? 0m, 2);
            var normalizedSgst = Math.Round(sgst ?? 0m, 2);
            var normalizedCess = Math.Round(cess ?? 0m, 2);

            if (normalizedGst <= 0m &&
                normalizedCgst <= 0m &&
                normalizedSgst <= 0m &&
                normalizedCess <= 0m)
            {
                return null;
            }

            var matches = (await _Hsnservice.GetAll())
                .Where(hsn => MatchesImportedTaxes(hsn, normalizedGst, normalizedCgst, normalizedSgst, normalizedCess))
                .Select(hsn => hsn.Id)
                .Distinct()
                .ToList();

            return matches.Count == 1 ? matches[0] : null;
        }

        private static bool MatchesImportedTaxes(
            Hsn hsn,
            decimal gst,
            decimal cgst,
            decimal sgst,
            decimal cess)
        {
            if (!AmountsEqual(hsn.Cess, cess))
            {
                return false;
            }

            if (cgst > 0m || sgst > 0m)
            {
                return AmountsEqual(hsn.CGST, cgst) &&
                       AmountsEqual(hsn.SGST, sgst) &&
                       (gst <= 0m || AmountsEqual(hsn.IGST, gst) || AmountsEqual(hsn.CGST + hsn.SGST, gst));
            }

            if (gst > 0m)
            {
                return AmountsEqual(hsn.IGST, gst) || AmountsEqual(hsn.CGST + hsn.SGST, gst);
            }

            return true;
        }

        private static bool AmountsEqual(decimal left, decimal right)
        {
            return Math.Abs(left - right) < 0.01m;
        }

        private async Task SaveItemImagesAsync(
            int itemId,
            List<Microsoft.AspNetCore.Http.IFormFile>? images,
            int primaryIndex)
        {
            if (images == null || !images.Any())
            {
                return;
            }

            string imageFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "ItemImages",
                itemId.ToString());

            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
            }

            var existingImages = await _itemimagerepository.GetByItemId(itemId);
            int sortOrder = existingImages.Any() ? existingImages.Max(x => x.SortOrder) + 1 : 1;
            bool makeNewPrimary = !existingImages.Any();
            int index = 0;
            var imageEntities = new List<ItemImage>();

            foreach (var image in images)
            {
                if (image.Length == 0)
                {
                    continue;
                }

                string fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                string fullPath = Path.Combine(imageFolder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                imageEntities.Add(new ItemImage
                {
                    ItemMasterId = itemId,
                    ImagePath = $"/ItemImages/{itemId}/{fileName}",
                    SortOrder = sortOrder++,
                    IsPrimary = makeNewPrimary && index == primaryIndex,
                    ImageHash = "Hashing."
                });

                index++;
            }

            if (imageEntities.Count > 0)
            {
                await _itemimagerepository.AddRange(imageEntities);
            }
        }
        [HttpGet]
        public async Task<JsonResult> GetCategories()
        {
            IList<CategoryMaster> categories = await _categoryservice.GetAll();
            categories.Insert(0, new CategoryMaster { Id = 0, CategoryName = "Select Category" });
            var categoriess = new SelectList(categories, "Id", "CategoryName");
            // var categories = _context.Categories.Select(c => new { value = c.Id, text = c.Name }).ToList();
            return Json(categoriess);
        }
        [HttpGet]
        public async Task<IActionResult> GetSubCategory()
        {
            IList<SubCategory> subCategories = await _subCategoryservice.GetAll();
            subCategories.Insert(0, new SubCategory { Id = 0, Name = "Select SubCategory" });
            var subCategory = new SelectList(subCategories, "Id", "Name");
            return Json(subCategory);
        }
        //[HttpGet]
        //public async Task<IActionResult> GetDivision()
        //{
        //    IList<Division> divisions = await _divisionservice.GetAll();
        //    divisions.Insert(0, new Division { Id = 0, Name = "select" });
        //    var division = new SelectList(divisions, "Id", "Name");
        //    return Json(division);
        //}

        [HttpGet]
        public async Task<IActionResult> GetDivision(int companyId)
        {
            var divisions = await _divisionservice.GetByCompanyList(companyId);

            var data = divisions.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name
            });

            return Json(data);
        }
        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            IList<Company> companies = await _companyservice.GetAll();
            companies.Insert(0, new Company { Id = 0, Name = "Select" });
            var company = new SelectList(companies, "Id", "Name");
            return Json(company);
        }
        [HttpGet]
        public async Task<IActionResult> GetHsn()
        {
            IList<Hsn> hsns = await _Hsnservice.GetAll();
            hsns.Insert(0, new Hsn { Id = 0, HsnCode = "select" });
            var hsn = new SelectList(hsns, "Id", "HsnCode");
            return Json(hsn);
        }
        [HttpPost]
        public async Task<IActionResult> CreateCategory(CategoryMasterVM model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.CategoryName))
            {
                return Json(new { success = false, message = "Category name is required." });
            }

            // 🔹 Duplicate check
            var categories = await _categoryservice.GetAll();

            bool isExist = categories.Any(x =>
                !string.IsNullOrWhiteSpace(x.CategoryName) &&
                x.CategoryName.Trim().Equals(model.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase)
            );

            if (isExist)
            {
                return Json(new { success = false, message = "Category name already exists." });
            }

            var entity = new CategoryMaster
            {
                CategoryName = model.CategoryName.Trim()
            };

            var result = await _categoryservice.Create(entity);

            return Json(new
            {
                success = true,
                message = "Category created successfully!",
                category = result
            });
        }
        [HttpPost]
        public async Task<IActionResult> CreateSubCategory(SubCategoryVM vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Category name is required." });
            }
            if (vm == null || string.IsNullOrWhiteSpace(vm.Name))
            {
                return Json(new { success = false, message = "Subcategory name is required." });
            }

            var subCategories = await _subCategoryservice.GetAll();

            bool isExist = subCategories.Any(x =>
                x.CategoryId == vm.CategoryId &&
                !string.IsNullOrWhiteSpace(x.Name) &&
                x.Name.Trim().Equals(vm.Name.Trim(), StringComparison.OrdinalIgnoreCase)
            );

            if (isExist)
            {
                return Json(new { success = false, message = "Subcategory name already exists." });
            }

            var model = new SubCategory
            {
                Name = vm.Name.Trim(),
                CategoryId = vm.CategoryId
            };

                var result = await _subCategoryservice.Create(model);

                if (result != null && result.Id > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Subcategory created successfully",
                        subCategory = result,
                        data = result
                    });
                }

            return Json(new { success = false, message = "Failed to create subcategory." });
        }
        [HttpPost]
        public async Task<IActionResult> CreateDivision(DivisionVM Vm)
        {
            if (Vm.CompanyId == 0)
            {
                return Json(new { success = false, message = "Company is required." });
            }
            if (Vm == null || string.IsNullOrEmpty(Vm.Name))
            {
                return Json(new { success = false, message = "Name is required" });
            }
            try
            {
                var model = new Division
                {
                    Name = Vm.Name,
                    CompanyId = Vm.CompanyId
                };
                var divisionsData = await _divisionservice.GetAll();

                bool isExist = divisionsData.Any(x =>
                    x.CompanyId == model.CompanyId &&
                    !string.IsNullOrWhiteSpace(x.Name) &&
                    x.Name.Trim().Equals(model.Name.Trim(), StringComparison.OrdinalIgnoreCase)
                );

                if (isExist)
                {
                    return Json(new { success = false, message = "Division name already exists." });
                }

                var result = await _divisionservice.Create(model);
                if (result.Id > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "division created successfully",
                        division = result
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Error: " + Response });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateHsn(HSNVM vm)
        {
            // Sales Item Master popup does not show HSN Type, so default it before validation.
            if (!vm.HsnType.HasValue)
            {
                vm.HsnType = AOne.Utility.Enums.HsnType.Goods;
                ModelState.Remove(nameof(vm.HsnType));
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Please enter valid data." });
            }

            string hsnCode = vm.HsnCode.Trim();

            try
            {
                // 🔹 Duplicate check
                var isExist = (await _Hsnservice.GetAll())
                    .Any(x => !string.IsNullOrWhiteSpace(x.HsnCode) &&
                              x.HsnCode.Trim().Equals(hsnCode, StringComparison.OrdinalIgnoreCase));

                if (isExist)
                {
                    return Json(new
                    {
                        success = false,
                        message = "HSN Code already exists."
                    });
                }

                var model = new Hsn
                {
                    HsnCode = hsnCode,
                    SGST = vm.SGST ?? 0,
                    CGST = vm.CGST ?? 0,
                    IGST = vm.IGST ?? 0,
                    Cess = vm.Cess ?? 0,
                    HsnType = vm.HsnType
                };

                var result = await _Hsnservice.Create(model);

                if (result?.Id > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"HSN Code {hsnCode} created successfully",
                        hsn = result
                    });
                }

                return Json(new { success = false, message = "Failed to create HSN code." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Something went wrong. Please try again." });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateCompany(CompanyVM vm)
        {
            if (vm == null || string.IsNullOrWhiteSpace(vm.Name))
            {
                return Json(new { success = false, message = "Company name is required." });
            }

            try
            {
                string companyName = vm.Name.Trim();

                // 🔹 Duplicate check
                var isExist = (await _companyservice.GetAll())
                    .Any(x => !string.IsNullOrWhiteSpace(x.Name) &&
                              x.Name.Trim().Equals(companyName, StringComparison.OrdinalIgnoreCase));

                if (isExist)
                {
                    return Json(new { success = false, message = "Company name already exists." });
                }

                var model = new Company
                {
                    Name = companyName
                };

                var result = await _companyservice.Create(model);

                if (result?.Id > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Company created successfully.",
                        company = result
                    });
                }

                return Json(new { success = false, message = "Failed to create company." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Something went wrong. Please try again." });
            }
        }
        private async Task LoadDropdowns()
        {
            var categories = await _categoryservice.GetAll();
            categories.Insert(0, new CategoryMaster { Id = 0, CategoryName = "Select Category" });
            ViewBag.Categories = new SelectList(categories, "Id", "CategoryName");

            var subcategories = await _subCategoryservice.GetAll();
            subcategories.Insert(0, new SubCategory { Id = 0, Name = "Select SubCategory" });
            ViewBag.SubCategories = new SelectList(subcategories, "Id", "Name");

            var hsn = await _Hsnservice.GetAll();
            hsn.Insert(0, new Hsn { Id = 0, HsnCode = "Select Code" });
            ViewBag.Hsn = new SelectList(hsn, "Id", "HsnCode");

            var division = await _divisionservice.GetAll();
            division.Insert(0, new Division { Id = 0, Name = "Select Division" });
            ViewBag.division = new SelectList(division, "Id", "Name");

            var company = await _companyservice.GetAll();
            company.Insert(0, new Company { Id = 0, Name = "Select Company" });
            ViewBag.company = new SelectList(company, "Id", "Name");

            var allItems = await _itemmasterrepository.GetAll();
            var parentItems = allItems.Where(x => x.ItemType == "Bulk" && x.IsActive == true).ToList();
            parentItems.Insert(0, new ItemMaster { Id = 0, Name = "Select Parent Bulk Item" });
            ViewBag.ParentItems = new SelectList(parentItems, "Id", "Name");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var tenantId = User?.FindFirst("TenantId")?.Value;
            var user = (await _tenantRepository.GetAll())
           .FirstOrDefault(x => x.Id == tenantId);
            var businessType = user?.BusinessType ?? 0;
            ViewBag.BusinessType = (int)businessType;

            var hasSale = await _itemmasterrepository.HasAnySaleAsync(id, tenantId);
            ViewBag.SaleCount = hasSale;

            var model = await _itemmasterrepository.GetByItemMasterId(id);

            if (model == null)
                return NotFound();

            ViewBag.Local = Enum.GetValues(typeof(TaxStatus))
                .Cast<TaxStatus>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();

            ViewBag.Central = Enum.GetValues(typeof(TaxStatus))
                .Cast<TaxStatus>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();

            var vm = new ItemMasterVM
            {
                Id = model.Id,
                Name = model.Name, 
                Code = model.Code,
                Barcode = model.Barcode,
                Unit = model.Unit1,
                Packing = model.Packing,
                CategoryId = model.CategoryId,
                SubCategoryId = model.SubCategoryId,
                DivisionId = model.DivisionId,
                HsnId = model.HsnId,
                Local = model.Local,
                Central = model.Central,
                CompanyId = model.CompanyId,
                Mrp = model.Mrp,
                SalesRate1 = model.SalesRate1,
                SalesRate2 = model.SalesRate2,
                MinimumQty = model.MinimumQty,
                MaximumQty = model.MaximumQty,
                ShelfLife = model.ShelfLife,
                ShelfLifeUnit=model.ShelfLifeUnit,
                MaximumDiscount = model.MaximumDiscount,
                DecemalAllowed = model.DecemalAllowed,
                Conversion = model.Conversion,
                UploadImage = model.UploadImage,
                IsActive = model.IsActive,
                Narcotics = model.Narcotics,
                ScheduleH = model.ScheduleH,
                ScheduleH1 = model.ScheduleH1,
                Salt = model.Salt,
                ItemType = model.ItemType,
                ParentItemId = model.ParentItemId,
                ConversionFactor = model.ConversionFactor,

                ExistingImages = model.ItemImages
                .OrderBy(x => x.SortOrder)
                .Select(x => new ItemImageVM
                {
                    Id = x.Id,
                    ImagePath = x.ImagePath,
                    IsPrimary = x.IsPrimary
                })
                .ToList()
            };

            await LoadDropdowns();

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ItemMasterVM VM)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    message = "Please fill all required fields"
                });
            }

            ItemMaster model = await _itemmasterrepository.GetByItemMasterId(VM.Id);
            if (model == null)
                return NotFound();

            var duplicateMessage = await GetItemMasterDuplicateMessageAsync(VM.Code, VM.Name, VM.Barcode, VM.Id);
            if (!string.IsNullOrWhiteSpace(duplicateMessage))
            {
                return Json(new
                {
                    success = false,
                    message = duplicateMessage
                });
            }

            // Update fields
            model.Name = VM.Name;
            model.Code = VM.Code;
            model.Barcode = VM.Barcode;
            model.Unit1 = VM.Unit;
            model.Packing = VM.Packing;
            model.CategoryId = VM.CategoryId == 0 ? null : VM.CategoryId;
            model.SubCategoryId = VM.SubCategoryId == 0 ? null : VM.SubCategoryId;
            model.DivisionId = VM.DivisionId == 0 ? null : VM.DivisionId;
            model.HsnId = VM.HsnId == 0 ? null : VM.HsnId;
            model.Local = VM.Local;
            model.Central = VM.Central;
            model.CompanyId = VM.CompanyId == 0 ? null : VM.CompanyId;
            model.Mrp = VM.Mrp;
            model.SalesRate1 = VM.SalesRate1;
            model.SalesRate2 = VM.SalesRate2;
            model.MinimumQty = VM.MinimumQty;
            model.MaximumQty = VM.MaximumQty;
            model.ShelfLife = VM.ShelfLife;
            model.ShelfLifeUnit = VM.ShelfLifeUnit;
            model.MaximumDiscount = VM.MaximumDiscount;
            model.DecemalAllowed = VM.DecemalAllowed;
            model.Conversion = VM.Conversion;
            model.IsActive = VM.IsActive ?? true;
            model.Narcotics = VM.Narcotics;
            model.ScheduleH = VM.ScheduleH;
            model.ScheduleH1 = VM.ScheduleH1;
            model.Salt = VM.Salt;
            model.ItemType = VM.ItemType;
            model.ParentItemId = VM.ParentItemId == 0 ? null : VM.ParentItemId;
            model.ConversionFactor = VM.ConversionFactor;

            await _itemmasterrepository.Update(model);

            // Primary Image Update karo (existing images mein se)
            if (VM.PrimaryImageId.HasValue && VM.PrimaryImageId.Value > 0)
            {
                var allImages = await _itemimagerepository.GetByItemId(model.Id);
                foreach (var img in allImages)
                {
                    img.IsPrimary = (img.Id == VM.PrimaryImageId.Value);
                }
                await _itemimagerepository.UpdateRange(allImages);
            }

            // Deleted Images handle karo
            if (VM.DeletedImageIds != null && VM.DeletedImageIds.Any())
            {
                foreach (var imageId in VM.DeletedImageIds)
                {
                    var img = await _itemimagerepository.GetById(imageId);
                    if (img != null)
                    {
                        var physicalPath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            img.ImagePath.TrimStart('/')
                                      .Replace('/', Path.DirectorySeparatorChar)
                        );

                        if (System.IO.File.Exists(physicalPath))
                            System.IO.File.Delete(physicalPath);

                        await _itemimagerepository.Delete(img);
                    }
                }
            }

            // New Images save karo
            if (VM.Images != null && VM.Images.Any())
            {
                string imageFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "ItemImages",
                    model.Id.ToString()
                );

                if (!Directory.Exists(imageFolder))
                    Directory.CreateDirectory(imageFolder);

                var existingImages = await _itemimagerepository.GetByItemId(model.Id);
                int sortOrder = existingImages.Any() ? existingImages.Max(x => x.SortOrder) + 1 : 1;

                // Agar new image primary hai to existing sab ko false karo
                bool newImageIsPrimary = VM.PrimaryImageId == null || VM.PrimaryImageId == 0;
                if (newImageIsPrimary && existingImages.Any())
                {
                    foreach (var img in existingImages)
                        img.IsPrimary = false;
                    await _itemimagerepository.UpdateRange(existingImages);
                }

                int index = 0;
                var newImages = new List<ItemImage>();

                foreach (var image in VM.Images)
                {
                    if (image.Length == 0) continue;

                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    string fullPath = Path.Combine(imageFolder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                        await image.CopyToAsync(stream);

                    newImages.Add(new ItemImage
                    {
                        ItemMasterId = model.Id,
                        ImagePath = $"/ItemImages/{model.Id}/{fileName}",
                        SortOrder = sortOrder++,
                        IsPrimary = newImageIsPrimary && (index == VM.PrimaryIndex),
                        ImageHash = "Hashing."
                    });

                    index++;
                }

                if (newImages.Any())
                    await _itemimagerepository.AddRange(newImages);
            }

            return Json(new
            {
                success = true,
                message = "Item updated successfully"
            });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                    return Json(new { success = false, message = "Invalid Id for deletion." });
                bool isUsed = await _itemmasterrepository.IsReferenced(id);

                // MERGED FROM TL: Check references in other tables before deletion
                bool isUsed = await _itemmasterrepository.IsReferenced(id);
                if (isUsed)
                {
                    return Json(new
                    {
                        success = false,
                        message = "This record is used in another table. Cannot delete."
                    });
                }

                var model = await _itemmasterrepository.GetByItemMasterId(id);
                if (model == null)
                    return Json(new { success = false, message = "Item not found." });

                // ✅ 1. Item ki saari images DB se lo
                var images = await _itemimagerepository.GetByItemId(id);

                // ✅ 2. Physical files delete karo
                if (images != null && images.Any())
                {
                    foreach (var img in images)
                    {
                        // ItemMaster delete fix: file cleanup failure should not stop DB item delete.
                        try
                        {
                            var physicalPath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                img.ImagePath.TrimStart('/')
                                          .Replace('/', Path.DirectorySeparatorChar)
                            );

                            if (System.IO.File.Exists(physicalPath))
                                System.IO.File.Delete(physicalPath);
                        }
                        catch
                        {
                            // ItemMaster delete fix: ignore missing/locked image file and continue delete.
                        }
                    }

                    // ✅ 3. Pura folder delete karo (ItemImages/22/)
                    var itemFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "ItemImages",
                        id.ToString()
                    );

                    // ItemMaster delete fix: folder cleanup is best-effort; item delete must continue.
                    try
                    {
                        if (Directory.Exists(itemFolder))
                            Directory.Delete(itemFolder, recursive: true);
                    }
                    catch
                    {
                        // ItemMaster delete fix: ignore missing/locked folder and continue delete.
                    }

                    // ✅ 4. DB se ItemImage records delete karo
                    await _itemimagerepository.DeleteByItemId(id);
                }

                // ✅ 5. Item delete karo
                await _itemmasterrepository.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> Import()
        {
            return View();
        }
         
        [HttpGet]
        public async Task<IActionResult> ExportToExcel()
        {
            var items = await _itemmasterrepository.GetAll();

            // Define dynamic column map
            var columnMap = new Dictionary<string, Func<ItemMaster, object?>>
            {
                { "Name", x => x.Name },
                { "Code", x => x.Code },
                { "Barcode", x => x.Barcode },
                { "Unit", x => x.Unit1 },
                { "Packing", x => x.Packing },
                { "Category", x => x.Category?.CategoryName },
                { "SubCategory", x => x.SubCategory?.Name },
                { "Division", x => x.Division?.Name },
                { "HSN", x => x.Hsn?.HsnCode },
                { "SGST", x => x.Hsn?.SGST },
                { "CGST", x => x.Hsn?.CGST },
                { "IGST", x => x.Hsn?.IGST },
                { "Cess", x => x.Hsn?.Cess },
                { "Company", x => x.Company?.Name },
                { "MRP", x => x.Mrp },
                { "Sales Rate 1", x => x.SalesRate1 },
                { "Sales Rate 2", x => x.SalesRate2 },
                { "Min Qty", x => x.MinimumQty },
                { "Max Qty", x => x.MaximumQty },
                { "Shelf Life", x => x.ShelfLife },
                { "ShelfLife Unit", x => x.ShelfLifeUnit },
                { "Max Discount", x => x.MaximumDiscount },
                { "Decimal Allowed", x => x.DecemalAllowed },
                { "Conversion", x => x.Conversion },
                { "Salt", x => x.Salt }
            };

            var bytes = _excelService.ExportToExcel(items, columnMap, "ItemMaster");
            var fileName = $"ItemMaster_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        
        [HttpPost]
        public async Task<IActionResult> ImportItemMaster(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "No file selected.";
                return RedirectToAction("Index");
            }

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            var columnMap = new Dictionary<string, Func<DataRow, ItemMasterVM, object?>>
            {
               
                { "Name", (row, item) => item.Name = GetSafeValue(row, "Name")?.ToString() },
                { "Code", (row, item) => item.Code = GetSafeValue(row, "Code")?.ToString() },
                { "Barcode", (row, item) => item.Barcode = GetSafeValue(row, "Barcode")?.ToString() },
                { "Unit", (row, item) => item.Unit = GetSafeValue(row, "Unit")?.ToString() },
                { "Packing", (row, item) => item.Packing = GetSafeValue(row, "Packing")?.ToString() },
                { "Category", (row, item) => item.CategoryName = GetSafeValue(row, "Category")?.ToString() },
                { "SubCategory", (row, item) => item.SubCategoryname = GetSafeValue(row, "SubCategory")?.ToString() },
                { "Division", (row, item) => item.DivisionName = GetSafeValue(row, "Division")?.ToString() },
                { "ShelfLife Unit", (row, item) => item.ShelfLifeUnit = GetSafeValue(row, "ShelfLife Unit")?.ToString() },
                { "Hsn", (row, item) => item.HsnCode = GetSafeValue(row, "Hsn")?.ToString() },
                { "SGST", (row, item) => item.SGST = TryParseDecimal(GetSafeValue(row, "SGST")) },
                { "CGST", (row, item) => item.CGST = TryParseDecimal(GetSafeValue(row, "CGST")) },
                { "IGST", (row, item) => item.IGST = TryParseDecimal(GetSafeValue(row, "IGST")) }, 
                { "Cess", (row, item) => item.Cess = TryParseDecimal(GetSafeValue(row, "Cess")) },
                { "Company", (row, item) => item.Companyname = GetSafeValue(row, "Company")?.ToString() },
                { "MRP", (row, item) => item.Mrp = TryParseDecimal(GetSafeValue(row, "MRP")) },
                { "Sales Rate 1", (row, item) => item.SalesRate1 = TryParseDecimal(GetSafeValue(row, "Sales Rate 1")) },
                { "Sales Rate 2", (row, item) => item.SalesRate2 = TryParseDecimal(GetSafeValue(row, "Sales Rate 2")) },
                { "Min Qty", (row, item) => item.MinimumQty = TryParseInt(GetSafeValue(row, "Min Qty")) ?? 0 },
                { "Max Qty", (row, item) => item.MaximumQty = TryParseInt(GetSafeValue(row, "Max Qty")) ?? 0 },
                { "Shelf Life", (row, item) => item.ShelfLife = TryParseInt(GetSafeValue(row, "Shelf Life")) ?? 0 },
                { "Max Discount", (row, item) => item.MaximumDiscount = TryParseDecimal(GetSafeValue(row, "Max Discount")) },
                { "Decimal Allowed", (row, item) => item.DecemalAllowed = GetSafeValue(row, "Decimal Allowed")?.ToString()?.ToLower() == "true" },
                { "Conversion", (row, item) => item.Conversion = TryParseInt(GetSafeValue(row, "Conversion")) ?? 0 },
                { "Salt", (row, item) => item.Salt = GetSafeValue(row, "Salt")?.ToString() }, 
            };


            var itemVMList = await _excelService.ImportAsync<ItemMasterVM>(stream.ToArray(), columnMap);
            var validItems = new List<ItemMaster>();
            var errors = new List<string>();
            var duplicateCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int rowIndex = 2; // starting after header
            foreach (var vm in itemVMList)
            {
                bool isValid = true;
                string error = $"Row {rowIndex}: ";

                // Required field validation
                if (string.IsNullOrWhiteSpace(vm.Name))
                {
                    isValid = false;
                    error += "Name is required. ";
                }

                if (string.IsNullOrWhiteSpace(vm.Code))
                {
                    isValid = false;
                    error += "Code is required. ";
                }

                // Duplicate Code Check
                if (!duplicateCodes.Add(vm.Code))
                {
                    isValid = false;
                    error += "Duplicate Code in Excel. ";
                }

                if (await _itemmasterrepository.ExistsByCode(vm.Code))
                {
                    isValid = false;
                    error += "Code already exists in database. ";
                }

                // Validate related master data
                vm.CategoryId = (await _categoryservice.GetByName(vm.CategoryName))?.Id;
               // vm.SubCategoryId = (await _subCategoryservice.GetByName(vm.SubCategoryname))?.Id;
                vm.DivisionId = (await _divisionservice.GetByName(vm.DivisionName))?.Id;
                vm.HsnId = (await _Hsnservice.GetByCode(vm.HsnCode))?.Id;
                vm.CompanyId = (await _companyservice.GetByName(vm.Companyname))?.Id;

                if (vm.CategoryId == null && !string.IsNullOrEmpty(vm.CategoryName) )
                { 
                    var model = new CategoryMaster
                    {
                        CategoryName = vm.CategoryName
                    };
                   var result = await _categoryservice.Create(model);
                   vm.CategoryId = result.Id;
                }
                if (!string.IsNullOrEmpty(vm.SubCategoryname) && vm.CategoryId != null)
                {
                    var subCategory = await _subCategoryservice
                        .GetByNameAndCategory(vm.SubCategoryname, (int)vm.CategoryId);

                    vm.SubCategoryId = subCategory?.Id;
                }
                if (vm.SubCategoryId == null && vm.CategoryId != null && !string.IsNullOrEmpty(vm.SubCategoryname))
                {
                    var subcategorymodel = new SubCategory
                    {
                        Name = vm.SubCategoryname,
                        CategoryId = (int)vm.CategoryId
                    };

                    var subcategoryresult = await _subCategoryservice.Create(subcategorymodel);
                    vm.SubCategoryId = subcategoryresult.Id;
                }
                
                if (vm.HsnId == null && !string.IsNullOrEmpty(vm.HsnCode))
                { 
                    var hsnmodel = new Hsn
                    {
                        HsnCode = vm.HsnCode,
                        CGST = vm.CGST,
                        SGST = vm.SGST,
                        IGST = vm.IGST,
                        Cess = vm.Cess
                    };
                    var hsnresult = await _Hsnservice.Create(hsnmodel);
                    vm.HsnId = hsnresult.Id;
                }
                if (vm.CompanyId == null && !string.IsNullOrEmpty(vm.Companyname))
                { 
                    var companymodel = new Company
                    {
                        Name = vm.Companyname
                    };
                    var companyresult = await _companyservice.Create(companymodel);
                    vm.CompanyId = companyresult.Id;
                }
                if (vm.DivisionId == null && vm.CompanyId != null && !string.IsNullOrEmpty(vm.DivisionName))
                { 
                    var divisionmodel = new Division
                    {
                        Name = vm.DivisionName,
                        CompanyId = (int)vm.CompanyId
                    };
                    var divisionresult = await _divisionservice.Create(divisionmodel);
                    vm.DivisionId = divisionresult.Id;
                }
                if (!isValid)
                {
                    errors.Add(error);
                }
                else
                {
                    validItems.Add(new ItemMaster
                    {
                        Name = vm.Name?.Trim(),
                        Code = vm.Code?.Trim(),
                        Barcode = vm.Barcode,
                        Unit1 = vm.Unit,
                        Packing = vm.Packing,

                        CategoryId = vm.CategoryId,
                        SubCategoryId = vm.SubCategoryId,
                        DivisionId = vm.DivisionId,
                        HsnId = vm.HsnId,
                        CompanyId = vm.CompanyId,

                        Mrp = vm.Mrp,
                        SalesRate1 = vm.SalesRate1,
                        SalesRate2 = vm.SalesRate2,
                        MinimumQty = vm.MinimumQty,
                        MaximumQty = vm.MaximumQty,

                        ShelfLife = vm.ShelfLife,
                        ShelfLifeUnit = string.IsNullOrWhiteSpace(vm.ShelfLifeUnit)
                        ? "Days"
                        : vm.ShelfLifeUnit,

                        // 🔥 VERY IMPORTANT (DB REQUIRED)
                        Local = TaxStatus.Taxable,
                        Central = TaxStatus.Taxable,

                        IsActive = true,  
                        MaximumDiscount = vm.MaximumDiscount,
                        DecemalAllowed = vm.DecemalAllowed,
                        Conversion = vm.Conversion,
                        Salt = vm.Salt
                    });

                }

                rowIndex++;
            }

            if (errors.Any())
            {
                TempData["error"] = string.Join("<br>", errors);
                return RedirectToAction("Index");
            }

            await _itemmasterrepository.ItemAddRange(validItems);
            TempData["success"] = $"{validItems.Count} items imported successfully!";
            return RedirectToAction("Index");
        } 
        // Helper methods
        private int? TryParseInt(object? value) => int.TryParse(value?.ToString(), out var v) ? v : null;
        private decimal TryParseDecimal(object? value) => decimal.TryParse(value?.ToString(), out var v) ? v : 0;
        //private static object? GetSafeValue(DataRow row, string columnName)
        //{
        //    return row.Table.Columns.Contains(columnName) ? row[columnName] : null;
        //}
        private static object? GetSafeValue(DataRow row, string columnName)
        {
            var col = row.Table.Columns
                .Cast<DataColumn>()
                .FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));

            return col != null ? row[col] : null;
        }

        //  public async Task<JsonResult> CheckDuplicate(string code, int id)
        //{
        //    bool result = await _itemmasterrepository.CheckDuplicateAsync(code, id);

        //    return Json(new
        //    {
        //        exists = result
        //    });
        //}
        // ══════════════════════════════════════════════════════════
        //  UPDATE COMPANY
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> UpdateCompany(CompanyVM vm)
        {
            if (vm.Id <= 0 || string.IsNullOrWhiteSpace(vm.Name))
                return Json(new { success = false, message = "Invalid data." });

            var existing = (await _companyservice.GetAll())
                .FirstOrDefault(x => x.Id == vm.Id);

            if (existing == null)
                return Json(new { success = false, message = "Company not found." });

            // Duplicate check (exclude current)
            var isExist = (await _companyservice.GetAll())
                .Any(x => x.Id != vm.Id &&
                           !string.IsNullOrWhiteSpace(x.Name) &&
                           x.Name.Trim().Equals(vm.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (isExist)
                return Json(new { success = false, message = "Company name already exists." });

            existing.Name = vm.Name.Trim();
            var result = await _companyservice.Update(existing);

            return Json(new
            {
                success = true,
                message = "Company updated successfully.",
                company = new { Id = existing.Id, Name = existing.Name }
            });
        }

        // ══════════════════════════════════════════════════════════
        //  UPDATE DIVISION
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> UpdateDivision(DivisionVM vm)
        {
            if (vm.Id <= 0 || string.IsNullOrWhiteSpace(vm.Name))
                return Json(new { success = false, message = "Invalid data." });

            if (vm.CompanyId == 0)
                return Json(new { success = false, message = "Company is required." });

            var existing = (await _divisionservice.GetAll())
                .FirstOrDefault(x => x.Id == vm.Id);

            if (existing == null)
                return Json(new { success = false, message = "Division not found." });

            // Duplicate check (exclude current)
            var isExist = (await _divisionservice.GetAll())
                .Any(x => x.Id != vm.Id &&
                           x.CompanyId == vm.CompanyId &&
                           !string.IsNullOrWhiteSpace(x.Name) &&
                           x.Name.Trim().Equals(vm.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (isExist)
                return Json(new { success = false, message = "Division name already exists for this company." });

            existing.Name = vm.Name.Trim();
            existing.CompanyId = vm.CompanyId;
            var result = await _divisionservice.Update(existing);

            return Json(new
            {
                success = true,
                message = "Division updated successfully.",
                division = new { Id = existing.Id, Name = existing.Name }
            });
        }

        // ══════════════════════════════════════════════════════════
        //  UPDATE CATEGORY
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> UpdateCategory(CategoryMasterVM model)
        {
            if (model.Id <= 0 || string.IsNullOrWhiteSpace(model.CategoryName))
                return Json(new { success = false, message = "Invalid data." });

            var existing = (await _categoryservice.GetAll())
                .FirstOrDefault(x => x.Id == model.Id);

            if (existing == null)
                return Json(new { success = false, message = "Category not found." });

            // Duplicate check (exclude current)
            var isExist = (await _categoryservice.GetAll())
                .Any(x => x.Id != model.Id &&
                           !string.IsNullOrWhiteSpace(x.CategoryName) &&
                           x.CategoryName.Trim().Equals(model.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (isExist)
                return Json(new { success = false, message = "Category name already exists." });

            existing.CategoryName = model.CategoryName.Trim();
            var result = await _categoryservice.Update(existing);

            return Json(new
            {
                success = true,
                message = "Category updated successfully!",
                category = new { Id = existing.Id, CategoryName = existing.CategoryName }
            });
        }

        // ══════════════════════════════════════════════════════════
        //  UPDATE SUBCATEGORY
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> UpdateSubCategory(SubCategoryVM vm)
        {
            if (vm.Id <= 0 || string.IsNullOrWhiteSpace(vm.Name))
                return Json(new { success = false, message = "Invalid data." });

            if (vm.CategoryId == 0)
                return Json(new { success = false, message = "Category is required." });

            var existing = (await _subCategoryservice.GetAll())
                .FirstOrDefault(x => x.Id == vm.Id);

            if (existing == null)
                return Json(new { success = false, message = "SubCategory not found." });

            // Duplicate check (exclude current)
            var isExist = (await _subCategoryservice.GetAll())
                .Any(x => x.Id != vm.Id &&
                           x.CategoryId == vm.CategoryId &&
                           !string.IsNullOrWhiteSpace(x.Name) &&
                           x.Name.Trim().Equals(vm.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (isExist)
                return Json(new { success = false, message = "SubCategory name already exists for this category." });

            existing.Name = vm.Name.Trim();
            existing.CategoryId = vm.CategoryId;
            var result = await _subCategoryservice.Update(existing);

            return Json(new
            {
                success = true,
                message = "SubCategory updated successfully!",
                subCategory = new { Id = existing.Id, Name = existing.Name }
            });
        }

        // ══════════════════════════════════════════════════════════
        //  UPDATE HSN
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> UpdateHsn(HSNVM vm)
        {
            if (vm.Id <= 0 || string.IsNullOrWhiteSpace(vm.HsnCode))
                return Json(new { success = false, message = "Invalid data." });

            var existing = (await _Hsnservice.GetAll())
                .FirstOrDefault(x => x.Id == vm.Id);

            if (existing == null)
                return Json(new { success = false, message = "HSN not found." });

            // Duplicate check (exclude current)
            var isExist = (await _Hsnservice.GetAll())
                .Any(x => x.Id != vm.Id &&
                           !string.IsNullOrWhiteSpace(x.HsnCode) &&
                           x.HsnCode.Trim().Equals(vm.HsnCode.Trim(), StringComparison.OrdinalIgnoreCase));

            if (isExist)
                return Json(new { success = false, message = "HSN Code already exists." });

            existing.HsnCode = vm.HsnCode.Trim();
            existing.SGST = vm.SGST ?? 0;
            existing.CGST = vm.CGST ?? 0;
            existing.IGST = vm.IGST ?? 0;
            existing.Cess = vm.Cess ?? 0;

            var result = await _Hsnservice.Update(existing);

            return Json(new
            {
                success = true,
                message = "HSN updated successfully!",
                hsn = new { Id = existing.Id, HsnCode = existing.HsnCode }
            });
        }

        // MERGED FROM TL: Action to display and query soft-deleted items for restoration
        public async Task<IActionResult> RestoreItem(string search = "", int page = 1, int pageSize = 10)
        {
            var allData = await _itemmasterrepository.GetAll();

            // Filter
            if (!string.IsNullOrEmpty(search))
            {
                allData = allData.Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || x.Code.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Pagination
            int totalItems = allData.Count;
            var paginatedData = allData.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.Search = search;

            return View(paginatedData);
        }
    }
}

