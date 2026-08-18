using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.UI.Filters;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Service.ExcelService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace EasyBill.UI.Controllers
{
    public class SupplierController : Controller
    {
        private readonly ISupplierRepository _supplierRepo;
        private readonly IAccountGroupRepository _accountgroupRepo;
        private readonly ICountryRepository _countryRepo;
        private readonly ICurrencyRepository _currencyRepo;
        private readonly IStateRepository _stateRepo;
        private readonly ICityRepository _cityRepo;
        private readonly IExcelService _excelService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public SupplierController(
            ISupplierRepository supplierRepo,
            IAccountGroupRepository accountgroupRepo,
            ICountryRepository countryRepo,
            ICurrencyRepository currencyRepo,
            IStateRepository stateRepo,
            ICityRepository cityRepo,
            IExcelService excelService,
            IWebHostEnvironment webHostEnvironment)
        {
            _supplierRepo = supplierRepo;
            _accountgroupRepo = accountgroupRepo;
            _countryRepo = countryRepo;
            _currencyRepo = currencyRepo;
            _stateRepo = stateRepo;
            _cityRepo = cityRepo;
            _excelService = excelService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _supplierRepo.GetALL();
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var accountGroups = await _accountgroupRepo.GetAll();
            var sundryCreditorGroup = accountGroups.FirstOrDefault(x => x.Name != null && x.Name.Trim().ToLower() == "sundry creditor");

            var supplierVM = new SupplierVM
            {
                AccountGroupId = sundryCreditorGroup?.Id
            };

            ViewBag.AccountGroupList = new SelectList(accountGroups, "Id", "Name", supplierVM.AccountGroupId);
            ViewBag.ParentAccountGroupList = new SelectList(accountGroups, "Id", "Name");
            ViewBag.CountryList = new SelectList(await _countryRepo.GetAll(), "Id", "Name");
            ViewBag.CurrencyList = new SelectList(await _currencyRepo.GetAll(), "Id", "Name");

            ViewBag.ParentSupplierList = new SelectList(await _supplierRepo.GetALL(), "Id", "FirstName");
            string templatePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Views", "Supplier", "InvoiceType");

            var templates = new List<string>();

            if (Directory.Exists(templatePath))
            {
                // Sirf .cshtml files uthana aur extension hatana
                templates = Directory.GetFiles(templatePath, "*.cshtml")
                                     .Select(Path.GetFileNameWithoutExtension)
                                     .ToList();
            }

            ViewBag.InvoiceTemplates = templates;
            return View(supplierVM);
        }
        [HttpPost]
        public async Task<IActionResult> Create(SupplierVM supplierVM)
        {
            if (!ModelState.IsValid)
            {
                return View(supplierVM);
            }

            var supplierData = await _supplierRepo.GetALL();

            // PhoneNO Duplicate check
            if (!string.IsNullOrEmpty(supplierVM.PhoneNO))
            {
                var isExist = !string.IsNullOrWhiteSpace(supplierVM.PhoneNO) &&
                         supplierData.Any(x =>
                             x.Id != supplierVM.Id &&
                             !string.IsNullOrWhiteSpace(x.PhoneNO) &&
                             string.Equals(
                                 x.PhoneNO.Trim(),
                                 supplierVM.PhoneNO.Trim(),
                                 StringComparison.OrdinalIgnoreCase
                             )
                         );

                if (isExist)
                {
                    TempData["error"] = "PhoneNo already exists.";
                    return View(supplierVM);
                }
            }

            // GstNO Duplicate check
            if (!string.IsNullOrWhiteSpace(supplierVM.GstNO))
            {
                var isExistGSTNo = supplierData.Any(x =>
                    x.Id != supplierVM.Id &&  
                    !string.IsNullOrWhiteSpace(x.GstNO) &&
                    string.Equals(
                        x.GstNO.Trim(),
                        supplierVM.GstNO.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
                );

                if (isExistGSTNo)
                {
                    TempData["error"] = "GSTNo already exists.";
                    return View(supplierVM);
                }
            }


            if (supplierVM != null)
            {
                var model = new Supplier
                {
                    FirstName = supplierVM.FirstName,
                    Address = supplierVM.Address,
                    CityId = supplierVM.CityId,
                    Area = supplierVM.Area,
                    PinCode = supplierVM.PinCode,
                    PhoneNO = supplierVM.PhoneNO,
                    Email = supplierVM.Email,
                    GstNO = supplierVM.GstNO,
                    ManufacturingLicNO = supplierVM.ManufacturingLicNO,
                    DrugLicNO = supplierVM.DrugLicNO,
                    IFSSAINo = supplierVM.IFSSAINo,
                    Type = supplierVM.Type,
                    AccountGroupId = supplierVM.AccountGroupId,
                    CountryId   = supplierVM.CountryId,
                    StateId = supplierVM?.StateId, 
                    CurrencyId = supplierVM?.CurrencyId,
                   // ParentSupplierId = supplierVM?.ParentSupplierId,
                    Balance = supplierVM?.Balance ?? 0,
                    AccountNo = supplierVM?.AccountNo,
                    RTGSNo = supplierVM?.RTGSNo,
                    IFSCCode = supplierVM?.IFSCCode,
                    Branch = supplierVM?.Branch,
                    MICRNo = supplierVM?.MICRNo,
                    PaymentDays = supplierVM?.PaymentDays ?? 0
                };
                await _supplierRepo.Create(model);
                TempData["success"] = "Supplier created successfuly.";
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<JsonResult> CreateSupplier(SupplierVM model)
        {
            var supplierData = await _supplierRepo.GetALL();

            // PhoneNO Duplicate check
            if (!string.IsNullOrEmpty(model.PhoneNO))
            {
                var isExist = !string.IsNullOrWhiteSpace(model.PhoneNO) &&
                         supplierData.Any(x =>
                             x.Id != model.Id &&
                             !string.IsNullOrWhiteSpace(x.PhoneNO) &&
                             string.Equals(
                                 x.PhoneNO.Trim(),
                                 model.PhoneNO.Trim(),
                                 StringComparison.OrdinalIgnoreCase
                             )
                         );

                if (isExist)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mobile number already exists"
                    });
                }
            }

            var supplier = new Supplier
            {
                FirstName = model.FirstName,
                Address = model.Address,
                CityId = model.CityId,
                Area = model.Area,
                PinCode = model.PinCode,
                PhoneNO = model.PhoneNO,
                Email = model.Email,
                GstNO = model.GstNO,
                ManufacturingLicNO = model.ManufacturingLicNO,
                DrugLicNO = model.DrugLicNO,
                IFSSAINo = model.IFSSAINo,
                Type = model.Type,
                AccountGroupId = model.AccountGroupId,
                CountryId = model.CountryId,
                StateId = model?.StateId, 
                CurrencyId = model?.CurrencyId,
              //  ParentSupplierId = model?.ParentSupplierId,
                Balance = model?.Balance ?? 0,
                AccountNo = model?.AccountNo,
                RTGSNo = model?.RTGSNo,
                IFSCCode = model?.IFSCCode,
                Branch = model?.Branch,
                MICRNo = model?.MICRNo,
                PaymentDays = model?.PaymentDays ?? 0
            };

            var result = await _supplierRepo.Create(supplier);

            return Json(new
            {
                success = true,
                id = result.Id,
                firstName = result.FirstName
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            Supplier model = await _supplierRepo.GetBySupplierId(Id);
            SupplierVM supplierVM = new SupplierVM();
            if (model != null)
            {
                supplierVM.Id = model.Id;
                supplierVM.FirstName = model.FirstName;
                supplierVM.Address = model.Address;
                supplierVM.CityId = model.CityId;
                supplierVM.Area = model.Area;
                supplierVM.PinCode = model.PinCode;
                supplierVM.PhoneNO = model.PhoneNO;
                supplierVM.Email = model.Email;
                supplierVM.GstNO = model.GstNO;
                supplierVM.ManufacturingLicNO = model.ManufacturingLicNO;
                supplierVM.DrugLicNO = model.DrugLicNO;
                supplierVM.IFSSAINo = model.IFSSAINo;
                supplierVM.Type = model.Type;
                supplierVM.AccountGroupId = model.AccountGroupId;
                supplierVM.CountryId = model.CountryId;
                supplierVM.StateId = model?.StateId;
                supplierVM.CityId = model.CityId;
                supplierVM.CurrencyId = model?.CurrencyId;
              //  supplierVM.ParentSupplierId = model?.ParentSupplierId;
                supplierVM.Balance = model?.Balance ?? 0;
                supplierVM.AccountNo = model?.AccountNo;
                supplierVM.RTGSNo = model?.RTGSNo;
                supplierVM.IFSCCode = model?.IFSCCode;
                supplierVM.Branch = model?.Branch;
                supplierVM.MICRNo = model?.MICRNo;
                supplierVM.PaymentDays = model?.PaymentDays ?? 0;
            }
            ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
            ViewBag.CountryList = new SelectList(await _countryRepo.GetAll(), "Id", "Name");
            ViewBag.CurrencyList = new SelectList(await _currencyRepo.GetAll(), "Id", "Name");
            ViewBag.ParentSupplierList = new SelectList(await _supplierRepo.GetALL(), "Id", "FirstName");
            return View(supplierVM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(SupplierVM VM)
        {

            if (!ModelState.IsValid)
                return View(VM);

            var supplierData = await _supplierRepo.GetALL();

            // PhoneNO Duplicate check
            if (!string.IsNullOrEmpty(VM.PhoneNO))
            {
                var isExist = !string.IsNullOrWhiteSpace(VM.PhoneNO) &&
                         supplierData.Any(x =>
                               x.Id != VM.Id &&

                             !string.IsNullOrWhiteSpace(x.PhoneNO) &&
                             string.Equals(
                                 x.PhoneNO.Trim(),
                                 VM.PhoneNO.Trim(),
                                 StringComparison.OrdinalIgnoreCase
                             )
                         );

                if (isExist)
                {
                    TempData["error"] = "PhoneNo already exists.";
                    return View(VM);
                }
            }

         
            if (!string.IsNullOrWhiteSpace(VM.GstNO))
            {
                var isExistGSTNo = supplierData.Any(x =>
                    x.Id != VM.Id && 
                    !string.IsNullOrWhiteSpace(x.GstNO) &&
                    x.GstNO.Trim().ToLower() == VM.GstNO.Trim().ToLower()
                );

                if (isExistGSTNo)
                {
                    TempData["error"] = "GSTNo already exists.";
                    return View(VM);
                }
            }


            Supplier model = await _supplierRepo.GetBySupplierId(VM.Id);
            if (model != null)
            {
                model.FirstName = VM.FirstName;
                model.Address = VM.Address;
                model.CityId = VM.CityId;
                model.Area = VM.Area;
                model.PinCode = VM.PinCode;
                model.PhoneNO = VM.PhoneNO;
                model.Email = VM.Email;
                model.GstNO = VM.GstNO;
                model.ManufacturingLicNO = VM.ManufacturingLicNO;
                model.DrugLicNO = VM.DrugLicNO;
                model.IFSSAINo = VM.IFSSAINo;
                model.Type = VM.Type;
                model.AccountGroupId = VM.AccountGroupId;
                model.CountryId = VM.CountryId;
                model.StateId = VM?.StateId;
                model.CityId = VM.CityId;
                model.CurrencyId = VM?.CurrencyId;
              //  model.ParentSupplierId = VM?.ParentSupplierId;
                model.Balance = VM?.Balance ?? 0;
                model.AccountNo = VM?.AccountNo;
                model.RTGSNo = VM?.RTGSNo;
                model.IFSCCode = VM?.IFSCCode;
                model.Branch = VM?.Branch;
                model.MICRNo = VM?.MICRNo;
                model.PaymentDays = VM?.PaymentDays ?? 0;
                await _supplierRepo.Update(model);
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid Id for deletion." });
                }
                var model = await _supplierRepo.GetBySupplierId(id);
                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _supplierRepo.Delete(model);
                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetStatesByCountry(int countryId)
        {
            var states =(await _stateRepo.GetByCountryId(countryId))
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name
                }).ToList();

            return Json(states);
        }

        [HttpGet]
        public async Task<IActionResult> GetCitiesByState(int stateId)
        {
            var cities = (await _cityRepo.GetByStateId(stateId))
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name
                }).ToList();

            return Json(cities);
        }
        [HttpGet]
        public async Task<IActionResult> Import()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ImportSupplier(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "No file selected.";
                return RedirectToAction("Index");
            }
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".xls" && ext != ".xlsx")
            {
                TempData["Error"] = "Sirf Excel (.xls / .xlsx) file upload karein.";
                return RedirectToAction("Index");
            }


            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            var columnMap = new Dictionary<string, Func<DataRow, SupplierVM, object?>>
            {
                { "supplier", (row, item) => item.FirstName = GetSafeValue(row, "supplier")?.ToString() },
                { "Address", (row, item) => item.Address = GetSafeValue(row, "Address")?.ToString() },
                { "Area", (row, item) => item.Area = GetSafeValue(row, "Area")?.ToString() },
                { "PinCode", (row, item) => item.PinCode = GetSafeValue(row, "PinCode")?.ToString() },
                { "PhoneNO", (row, item) => item.PhoneNO = GetSafeValue(row, "PhoneNO")?.ToString() },
                { "Email", (row, item) => item.Email = GetSafeValue(row, "Email")?.ToString() },
                { "GstNO", (row, item) => item.GstNO = GetSafeValue(row, "GstNO")?.ToString() },
                { "ManufacturingLicNO", (row, item) => item.ManufacturingLicNO = GetSafeValue(row, "ManufacturingLicNO")?.ToString() },
                { "DrugLicNO", (row, item) => item.DrugLicNO = GetSafeValue(row, "DrugLicNO")?.ToString() },
                { "IFSSAINo", (row, item) => item.IFSSAINo = GetSafeValue(row, "IFSSAINo")?.ToString() },
               // { "Type", (row, item) => item.Type = ParseEnum<GSTType>(GetSafeValue(row, "Type")) },
                { "Type", (row, item) =>  item.Type = ParseEnum(GetSafeValue(row, "Type"), GSTType.UnRegistered) },
                { "AccountGroupName", (row, item) => item.AccountGroupName = GetSafeValue(row, "AccountGroupName")?.ToString() },
                { "CountryName", (row, item) => item.CountryName = GetSafeValue(row, "CountryName")?.ToString() },
                { "StateName", (row, item) => item.StateName = GetSafeValue(row, "StateName")?.ToString() },
                { "CityName", (row, item) => item.CityName = GetSafeValue(row, "CityName")?.ToString() },
                { "CurrencyName", (row, item) => item.CurrencyName = GetSafeValue(row, "CurrencyName")?.ToString() },
                { "Balance", (row, item) => item.Balance = TryParseDecimal(GetSafeValue(row, "Balance")) },
                { "AccountNo", (row, item) => item.AccountNo = GetSafeValue(row, "AccountNo")?.ToString() },
                { "RTGSNo", (row, item) => item.RTGSNo = GetSafeValue(row, "RTGSNo")?.ToString() },
                { "IFSCCode", (row, item) => item.IFSCCode = GetSafeValue(row, "IFSCCode")?.ToString() },
                { "Branch", (row, item) => item.Branch = GetSafeValue(row, "Branch")?.ToString() },
                { "MICRNo", (row, item) => item.MICRNo = GetSafeValue(row, "MICRNo")?.ToString() },
                
            };


            var itemVMList = await _excelService.ImportAsync<SupplierVM>(stream.ToArray(), columnMap);
            var validItems = new List<Supplier>();
            var errors = new List<string>();
            var duplicateCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int rowIndex = 2; // starting after header
            foreach (var vm in itemVMList)
            {
                bool isValid = true;
                string error = $"Row {rowIndex}: ";

                // Required field validation
                if (string.IsNullOrWhiteSpace(vm.FirstName))
                {
                    isValid = false;
                    error += "Name is required. ";
                }
                //if (!TryParseEnumStrict(GetSafeValue(vm.Type, "Type"), out GSTType type))
                //{
                //    isValid = false;
                //    error += "Invalid Type value. ";
                //}
                //else
                //{
                //    vm.Type = type;
                //}

                //if (string.IsNullOrWhiteSpace(vm.Code))
                //{
                //    isValid = false;
                //    error += "Code is required. ";
                //}

                //// Duplicate Code Check
                //if (!duplicateCodes.Add(vm.Code))
                //{
                //    isValid = false;
                //    error += "Duplicate Code in Excel. ";
                //}

                //if (await _supplierRepo.ExistsByCode(vm.Code))
                //{
                //    isValid = false;
                //    error += "Code already exists in database. ";
                //}

                // Validate related master data
                vm.AccountGroupId = (await _accountgroupRepo.GetByName(vm.AccountGroupName))?.Id;
                vm.CountryId = (await _countryRepo.GetByName(vm.CountryName))?.Id;
                vm.StateId = (await _stateRepo.GetByName(vm.StateName))?.Id;
                vm.CityId = (await _cityRepo.GetByName(vm.CityName))?.Id;
                vm.CurrencyId = (await _currencyRepo.GetByName(vm.CurrencyName))?.Id;

                if (vm.AccountGroupId == null && !string.IsNullOrEmpty(vm.AccountGroupName))
                {
                    var model = new AccountGroup
                    {
                        Name = vm.AccountGroupName
                    };
                    var result = await _accountgroupRepo.Create(model);
                    vm.AccountGroupId = result.Id;
                }
                if (vm.CountryId == null && !string.IsNullOrEmpty(vm.CountryName))
                {
                    var model = new Country
                    {
                        Name = vm.CountryName
                    };
                    var result = await _countryRepo.Create(model);
                    vm.CountryId = result.Id;
                }
                if (vm.StateId == null && vm.CountryId != null && !string.IsNullOrEmpty(vm.StateName))
                {
                    var statemodel = new State
                    {
                        Name = vm.StateName,
                        CountryId = (int)vm.CountryId
                    };
                    var stateresult = await _stateRepo.Create(statemodel);
                    vm.StateId = stateresult.Id;
                }
                if (vm.CityId == null && vm.StateId != null && vm.CountryId != null && !string.IsNullOrEmpty(vm.CityName))
                {
                    var citymodel = new Models.Entity.City
                    {
                        Name = vm.CityName,
                        CountryId = (int)vm.CountryId,
                        StateId = (int)vm.StateId
                    };
                    var cityresult = await _cityRepo.Create(citymodel);
                    vm.CityId = cityresult.Id;
                }
                 
                if (!isValid)
                {
                    errors.Add(error);
                }
                else
                {
                    validItems.Add(new Supplier
                    {
                        FirstName = vm.FirstName,
                        Address = vm.Address,
                        Area = vm.Area,
                        PinCode = vm.PinCode,
                        PhoneNO = vm.PhoneNO,
                        Email = vm.Email,
                        GstNO = vm.GstNO,
                        ManufacturingLicNO = vm.ManufacturingLicNO,
                        DrugLicNO = vm.DrugLicNO,
                        IFSSAINo = vm.IFSSAINo,
                        Type = vm.Type,
                        AccountGroupId = vm.AccountGroupId,
                        CountryId = vm.CountryId,
                        StateId = vm.StateId,
                        CityId = vm.CityId,
                        CurrencyId = vm.CurrencyId,
                        Balance = vm.Balance,
                        AccountNo = vm.AccountNo,
                        RTGSNo = vm.RTGSNo,
                        IFSCCode = vm.IFSCCode,
                        Branch = vm.Branch,
                        MICRNo = vm.MICRNo
                    });
                }

                rowIndex++;
            }

            if (errors.Any())
            {
                TempData["error"] = string.Join("<br>", errors);
                return RedirectToAction("Index");
            }

            await _supplierRepo.ItemAddRange(validItems);
            TempData["success"] = $"{validItems.Count} suppliers imported successfully!";
            return RedirectToAction("Index");
        }
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

        private TEnum ParseEnum<TEnum>(object? value, TEnum defaultValue) where TEnum : struct, Enum
        {
            if (value == null) return defaultValue;

            var str = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(str)) return defaultValue;

            if (Enum.TryParse<TEnum>(str, true, out var result))
                return result;

            return defaultValue;
        }
        private bool TryParseEnumStrict<TEnum>(object? value, out TEnum result)where TEnum : struct, Enum
        {
            result = default;

            if (value == null) return false;

            return Enum.TryParse(value.ToString(), true, out result);
        }



        [HttpGet]
        public IActionResult GetTemplatePreview(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return Content("No template selected");
            return PartialView($"~/Views/Supplier/InvoiceType/{templateName}.cshtml");
        }
        // ==========================================
        //          F2 / F3 QUICK SAVE METHODS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> GetAccountGroups()
        {
            var list = (await _accountgroupRepo.GetAll())
                .Select(x => new { value = x.Id, text = x.Name, parentId = x.ParentId, isActive = x.IsActive })
                .OrderBy(x => x.text)
                .ToList();

            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> QuickSaveAccountGroup(int id, string name, int? parentId, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name is required!" });

            bool exists = (await _accountgroupRepo.GetAll())
                .Any(x => x.Name.Trim().ToLower() == name.Trim().ToLower() && x.Id != id);

            if (exists)
                return Json(new { success = false, message = "Name already exists!" });

            if (id == 0)
            {
                var entity = new AccountGroup { Name = name.Trim(), ParentId = parentId, IsActive = isActive };
                await _accountgroupRepo.Create(entity);
                return Json(new { success = true, newId = entity.Id });
            }
            else
            {
                var entity = (await _accountgroupRepo.GetAll()).FirstOrDefault(x => x.Id == id);
                if (entity == null)
                    return Json(new { success = false, message = "Not found!" });

                entity.Name = name.Trim();
                entity.ParentId = parentId;
                entity.IsActive = isActive;
                await _accountgroupRepo.Update(entity);

                return Json(new { success = true, newId = id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCountries()
        {
            var list = (await _countryRepo.GetAll())
                .Select(x => new { value = x.Id, text = x.Name })
                .OrderBy(x => x.text)
                .ToList();

            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> QuickSaveCountry(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name is required!" });

            bool exists = (await _countryRepo.GetAll())
                .Any(x => x.Name.Trim().ToLower() == name.Trim().ToLower() && x.Id != id);

            if (exists)
                return Json(new { success = false, message = "Name already exists!" });

            if (id == 0)
            {
                var entity = new Country { Name = name.Trim() };
                await _countryRepo.Create(entity);
                return Json(new { success = true, newId = entity.Id });
            }
            else
            {
                var entity = (await _countryRepo.GetAll()).FirstOrDefault(x => x.Id == id);
                if (entity == null)
                    return Json(new { success = false, message = "Not found!" });

                entity.Name = name.Trim();
                await _countryRepo.Update(entity);

                return Json(new { success = true, newId = id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> QuickSaveState(int id, string name, int countryId)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name is required!" });

            if (countryId <= 0)
                return Json(new { success = false, message = "Select Country first!" });

            bool exists = (await _stateRepo.GetByCountryId(countryId))
                .Any(x => x.Name.Trim().ToLower() == name.Trim().ToLower() && x.Id != id);

            if (exists)
                return Json(new { success = false, message = "State already exists in this country!" });

            if (id == 0)
            {
                var entity = new State { Name = name.Trim(), CountryId = countryId };
                await _stateRepo.Create(entity);
                return Json(new { success = true, newId = entity.Id });
            }
            else
            {
                var entity = (await _stateRepo.GetAll()).FirstOrDefault(x => x.Id == id);
                if (entity == null)
                    return Json(new { success = false, message = "Not found!" });

                entity.Name = name.Trim();
                entity.CountryId = countryId;
                await _stateRepo.Update(entity);

                return Json(new { success = true, newId = id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> QuickSaveCity(int id, string name, int stateId, int countryId)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name is required!" });

            if (stateId <= 0 || countryId <= 0)
                return Json(new { success = false, message = "Select State/Country first!" });

            bool exists = (await _cityRepo.GetByStateId(stateId))
                .Any(x => x.Name.Trim().ToLower() == name.Trim().ToLower() && x.Id != id);

            if (exists)
                return Json(new { success = false, message = "City already exists in this state!" });

            if (id == 0)
            {
                var entity = new Models.Entity.City
                {
                    Name = name.Trim(),
                    StateId = stateId,
                    CountryId = countryId
                };
                await _cityRepo.Create(entity);
                return Json(new { success = true, newId = entity.Id });
            }
            else
            {
                var entity = (await _cityRepo.GetAll()).FirstOrDefault(x => x.Id == id);
                if (entity == null)
                    return Json(new { success = false, message = "Not found!" });

                entity.Name = name.Trim();
                entity.StateId = stateId;
                entity.CountryId = countryId;

                await _cityRepo.Update(entity);

                return Json(new { success = true, newId = id });
            }
        }
    }
}
