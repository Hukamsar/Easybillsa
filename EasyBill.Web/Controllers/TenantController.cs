using AOne.Models.Entity;
using AOne.Utility;
using AOne.Utility.Enums;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AOneWeb.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class TenantController : Controller
    {
        private readonly ITenantRegistrationRepository _tenantRepository;
        private readonly ICountryRepository _countryRepo;
        private readonly IStateRepository _stateRepo;
        private readonly ICityRepository _cityRepo;
        private readonly UserManager<ApplicationUsers> _userManager;
        private readonly ApplicationDbContext _context;
        //private readonly IMailService _mailService;
        public TenantController(
            ITenantRegistrationRepository tenantRegistration, 
            ICountryRepository countryRepo,
            IStateRepository stateRepo,
            ICityRepository cityRepo,
            UserManager<ApplicationUsers> userManager,
            ApplicationDbContext context)
        {
            _tenantRepository = tenantRegistration;
            _countryRepo = countryRepo;
            _stateRepo = stateRepo;
            _cityRepo = cityRepo;
            _userManager = userManager;
            _context = context;
        }
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Index()
        {
            var data = await _tenantRepository.GetAll();
            return View(data);
        }
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create()
        {

            ViewBag.CountryList = new SelectList(await _countryRepo.GetAll(), "Id", "Name");
            ViewBag.StateList = new SelectList(await _stateRepo.GetAll(), "Id", "Name");
            ViewBag.CityList = new SelectList(await _cityRepo.GetAll(), "Id", "Name");

            ViewBag.SubscriptionPlanList = new SelectList(await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync(), "Id", "PlanName");
            ViewBag.Features = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();

            var model = new TenantRegistrationVM();

            DateTime today = DateTime.Today;

            if (today.Month < 4) // Jan, Feb, Mar
            {
                model.YearFrom = new DateTime(today.Year - 1, 4, 1);
                model.YearTo = new DateTime(today.Year, 3, 31);
            }
            else // Apr to Dec
            {
                model.YearFrom = new DateTime(today.Year, 4, 1);
                model.YearTo = new DateTime(today.Year + 1, 3, 31);
            }

            return View(model);
        }
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create(TenantRegistrationVM viewMode)
        {
            if (viewMode == null)
            {
                await PopulateLocationLookupsAsync();
                ViewBag.SubscriptionPlanList = new SelectList(await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync(), "Id", "PlanName");
                ViewBag.Features = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();
                return View(new TenantRegistrationVM());
            }

            var normalizedMobile = NormalizeMobile(viewMode.MobileNo);
            viewMode.MobileNo = normalizedMobile;
            viewMode.Email = viewMode.Email?.Trim();

            if (string.IsNullOrWhiteSpace(viewMode.Name))
                ModelState.AddModelError(nameof(viewMode.Name), "Company name is required.");
            if (string.IsNullOrWhiteSpace(viewMode.Address1))
                ModelState.AddModelError(nameof(viewMode.Address1), "Address1 is required.");
            if (!IsValidMobile(normalizedMobile))
                ModelState.AddModelError(nameof(viewMode.MobileNo), "Enter a valid 10-digit mobile number.");
            if (string.IsNullOrWhiteSpace(viewMode.Email) || !new EmailAddressAttribute().IsValid(viewMode.Email))
                ModelState.AddModelError(nameof(viewMode.Email), "Enter a valid email address.");

            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState)
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        Console.WriteLine($"[VALIDATION ERROR] {modelState.Key}: {error.ErrorMessage} / {error.Exception?.Message}");
                    }
                }
                await PopulateLocationLookupsAsync();
                ViewBag.SubscriptionPlanList = new SelectList(await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync(), "Id", "PlanName", viewMode.SubscriptionPlanId);
                ViewBag.Features = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();
                return View(viewMode);
            }

            bool exists = await _tenantRepository.CheckDuplicateAsync(
                       viewMode.Name,
                       viewMode.GstNo,
                       viewMode.MobileNo,
                       viewMode.Email,
                       null
                       );
            if (exists)
            {
                ModelState.AddModelError("", "Duplicate record already exists!");
                await PopulateLocationLookupsAsync();
                ViewBag.SubscriptionPlanList = new SelectList(await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync(), "Id", "PlanName", viewMode.SubscriptionPlanId);
                ViewBag.Features = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();
                return View(viewMode);
            }

            if (viewMode != null)
            {
                string fileName = "";
                if (viewMode.UploadAttachement != null && viewMode.UploadAttachement.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Tenant");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    fileName = viewMode.UploadAttachement.FileName;
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        viewMode.UploadAttachement.CopyTo(fileStream);
                    }
                    viewMode.Logo = $"docs/Tenant/{fileName}";
                }
                var model = new Tenant
                {
                   Name=viewMode.Name,
                   Email=viewMode.Email,
                   Address1 = viewMode.Address1,
                   Address2 = viewMode.Address2,
                   Location = viewMode.Location,
                   CountryId = viewMode.CountryId,
                   StateId = viewMode.StateId,
                   CityId  = viewMode.CityId,
                   PinCode = viewMode.PinCode,
                   ContactPerson = viewMode.ContactPerson,
                   Phone = viewMode.Phone,
                   MobileNo = viewMode.MobileNo,
                   GstNo = viewMode.GstNo,
                   StateCode = viewMode.StateCode,
                   CompanyType = viewMode.CompanyType ?? CompanyType.HeadOffice,
                   Branch = viewMode.Branch,
                   Logo = viewMode.Logo,
                   Description = viewMode.Description,
                   IFSSAINo = viewMode.IFSSAINo,
                   DrugLicNo = viewMode.DrugLicNo,
                   LicenceExpiryDate = viewMode.LicenceExpiryDate,
                   Jurisdiction = viewMode.Jurisdiction,
                   WorkingStyle = viewMode.WorkingStyle ?? WorkingStyle.Text,
                   BranchCode = viewMode.BranchCode,
                   BusinessType = viewMode.BusinessType ?? BusinessType.GroceryStore,
                   CalanderType = viewMode.CalenderType ?? CalenderType.English,
                   YearFrom = viewMode.YearFrom,
                   YearTo = viewMode.YearTo,
                   TaxType = viewMode.TaxType ?? TaxType.GST,
                   SubscriptionPlanId = viewMode.SubscriptionPlanId,
                   AllowedModulesJson = viewMode.SelectedFeatures != null && viewMode.SelectedFeatures.Any() ? JsonConvert.SerializeObject(viewMode.SelectedFeatures) : null
                };
               // await _tenantRepository.Create(model);
                var tenantresult = await _tenantRepository.Create(model);
                if (!string.IsNullOrEmpty(tenantresult.Id))
                {
                    var user = new ApplicationUsers
                    {
                        UserName = model.Email?.Trim() ?? string.Empty,
                        Email = model.Email?.Trim(),
                        EmailConfirmed = true,
                        TenantId = tenantresult.Id,
                        PhoneNumber = model.MobileNo?.Trim(),
                        PhoneNumberConfirmed = true
                    };
                    // Registration keeps password empty; user will set password + PIN after mobile login.
                    var result = await _userManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, RoleName.Admin);
                    }
                    else
                    {
                        await _tenantRepository.Delete(tenantresult);
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        await PopulateLocationLookupsAsync();
                        ViewBag.SubscriptionPlanList = new SelectList(await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync(), "Id", "PlanName", viewMode.SubscriptionPlanId);
                        ViewBag.Features = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();
                        return View(viewMode);
                    }
                }
                return RedirectToAction("Index");

            }
            return View(viewMode);
        }
        private async Task PopulateLocationLookupsAsync()
        {
            ViewBag.CountryList = new SelectList(await _countryRepo.GetAll(), "Id", "Name");
            ViewBag.StateList = new SelectList(await _stateRepo.GetAll(), "Id", "Name");
            ViewBag.CityList = new SelectList(await _cityRepo.GetAll(), "Id", "Name");
        }

        private static string NormalizeMobile(string? mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber))
                return string.Empty;

            var digits = Regex.Replace(mobileNumber, "[^0-9]", string.Empty);
            if (digits.Length > 10)
                digits = digits.Substring(digits.Length - 10);

            return digits;
        }

        private static bool IsValidMobile(string mobileNumber)
        {
            return Regex.IsMatch(mobileNumber, "^[6-9][0-9]{9}$");
        }
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Edit(string Id)
        {
            var model = await _tenantRepository.GetById(Id);

            TenantRegistrationVM Vm = new TenantRegistrationVM();
            if(model != null)
            {
                Vm.Id = model.Id;
                Vm.Name = model.Name;
                Vm.Email = model.Email;
                Vm.Address1 = model.Address1;
                Vm.Address2 = model.Address2;
                Vm.Location = model.Location;
                Vm.CountryId = model.CountryId;
                Vm.StateId = model.StateId;
                Vm.CityId = model.CityId;
                Vm.PinCode = model.PinCode;
                Vm.ContactPerson = model.ContactPerson;
                Vm.Phone = model.Phone;
                Vm.MobileNo = model.MobileNo;
                Vm.GstNo = model.GstNo;
                Vm.StateCode = model.StateCode;
                Vm.CompanyType = model.CompanyType;
                Vm.Branch = model.Branch;
                Vm.Logo = model.Logo;
                Vm.Description = model.Description;
                Vm.IFSSAINo = model.IFSSAINo;
                Vm.DrugLicNo = model.DrugLicNo;
                Vm.LicenceExpiryDate = model.LicenceExpiryDate;
                Vm.Jurisdiction = model.Jurisdiction;
                Vm.WorkingStyle = model.WorkingStyle;
                Vm.BranchCode = model.BranchCode;
                Vm.BusinessType = model.BusinessType;
                Vm.CalenderType = model.CalanderType;
                Vm.YearFrom = model.YearFrom;
                Vm.YearTo = model.YearTo;
                Vm.TaxType = model.TaxType;
                Vm.SubscriptionPlanId = model.SubscriptionPlanId;

                if (!string.IsNullOrEmpty(model.AllowedModulesJson))
                {
                    Vm.SelectedFeatures = JsonConvert.DeserializeObject<List<string>>(model.AllowedModulesJson);
                }
                else if (model.SubscriptionPlanId.HasValue)
                {
                    Vm.SelectedFeatures = await _context.PlanFeatures
                        .Where(pf => pf.PlanId == model.SubscriptionPlanId.Value && pf.Feature != null)
                        .Select(pf => pf.Feature!.FeatureKey)
                        .ToListAsync();
                }
                else
                {
                    Vm.SelectedFeatures = new List<string>();
                }
            }
            ViewBag.CountryList = new SelectList(await _countryRepo.GetAll(), "Id", "Name");
            ViewBag.StateList = new SelectList(await _stateRepo.GetAll(), "Id", "Name");
            ViewBag.CityList = new SelectList(await _cityRepo.GetAll(), "Id", "Name");

            ViewBag.SubscriptionPlanList = new SelectList(await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync(), "Id", "PlanName", model?.SubscriptionPlanId);
            ViewBag.Features = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();

            return View(Vm);

        }
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Edit(TenantRegistrationVM Vm)
        {
            bool exists = await _tenantRepository.CheckDuplicateAsync(
      Vm.Name,
      Vm.GstNo,
      Vm.MobileNo,
      Vm.Email,
      Vm.Id);

            if (exists)
            {
                ModelState.AddModelError("", "Duplicate record already exists!");
                await PopulateLocationLookupsAsync();
                ViewBag.SubscriptionPlanList = new SelectList(await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync(), "Id", "PlanName", Vm.SubscriptionPlanId);
                ViewBag.Features = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();
                return View(Vm);
            }
            Tenant model = await _tenantRepository.GetById(Vm.Id);
            string fileName = "";
            if (Vm.UploadAttachement != null && Vm.UploadAttachement.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Tenant");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                fileName = Vm.UploadAttachement.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    Vm.UploadAttachement.CopyTo(fileStream);
                }
                Vm.Logo = $"docs/Tenant/{fileName}";
            }
            if (model != null)
            { 
                model.Name = Vm.Name;
                model.Email = Vm.Email;
                model.Address1 = Vm.Address1;
                model.Address2 = Vm.Address2;
                model.Location = Vm.Location;
                model.CountryId = Vm.CountryId;
                model.StateId = Vm.StateId;
                model.CityId = Vm.CityId;
                model.PinCode = Vm.PinCode;
                model.ContactPerson = Vm.ContactPerson;
                model.Phone = Vm.Phone;
                model.MobileNo = Vm.MobileNo;
                model.GstNo = Vm.GstNo;
                model.StateCode = Vm.StateCode;
                model.CompanyType = Vm.CompanyType ?? CompanyType.HeadOffice;
                model.Branch = Vm.Branch;
                model.Logo = Vm.Logo;
                model.Description = Vm.Description;
                model.IFSSAINo = Vm.IFSSAINo;
                model.DrugLicNo = Vm.DrugLicNo;
                model.LicenceExpiryDate = Vm.LicenceExpiryDate;
                model.Jurisdiction = Vm.Jurisdiction;
                model.WorkingStyle = Vm.WorkingStyle ?? WorkingStyle.Text;
                model.BranchCode = Vm.BranchCode;
                model.BusinessType = Vm.BusinessType ?? BusinessType.GroceryStore; 
                model.CalanderType = Vm.CalenderType ?? CalenderType.English;
                model.YearFrom = Vm.YearFrom;
                model.YearTo = Vm.YearTo;
                model.TaxType = Vm.TaxType ?? TaxType.GST;
                model.SubscriptionPlanId = Vm.SubscriptionPlanId;
                model.AllowedModulesJson = Vm.SelectedFeatures != null && Vm.SelectedFeatures.Any() ? JsonConvert.SerializeObject(Vm.SelectedFeatures) : null;

                await _tenantRepository.Update(model);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return Json(new { success = false, message = "Invalid Id for deletion." });
                }

                var model = await _tenantRepository.GetById(id.ToString());

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                await _tenantRepository.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        public async Task<JsonResult> CheckDuplicate(string? Name, string? GstNo, string? MobileNo, string? Email, string? id)
        {
            bool result = await _tenantRepository.CheckDuplicateAsync(Name, GstNo, MobileNo, Email, id);

            return Json(new
            {
                exists = result
            });
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
                var entity = new City
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

        [HttpGet]
        public async Task<IActionResult> GetStatesByCountry(int countryId)
        {
            var list = (await _stateRepo.GetByCountryId(countryId))
                .Select(x => new { id = x.Id, name = x.Name })
                .OrderBy(x => x.name)
                .ToList();
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetCitiesByState(int stateId)
        {
            var list = (await _cityRepo.GetByStateId(stateId))
                .Select(x => new { id = x.Id, name = x.Name })
                .OrderBy(x => x.name)
                .ToList();
            return Json(list);
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProfile()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized();
            }

            var model = await _tenantRepository.GetById(tenantId);
            if (model == null)
            {
                return NotFound();
            }

            TenantRegistrationVM Vm = new TenantRegistrationVM
            {
                Id = model.Id,
                Name = model.Name,
                Email = model.Email,
                Address1 = model.Address1,
                Address2 = model.Address2,
                Location = model.Location,
                CountryId = model.CountryId,
                StateId = model.StateId,
                CityId = model.CityId,
                PinCode = model.PinCode,
                ContactPerson = model.ContactPerson,
                Phone = model.Phone,
                MobileNo = model.MobileNo,
                GstNo = model.GstNo,
                StateCode = model.StateCode,
                CompanyType = model.CompanyType,
                Branch = model.Branch,
                Logo = model.Logo,
                Description = model.Description,
                IFSSAINo = model.IFSSAINo,
                DrugLicNo = model.DrugLicNo,
                LicenceExpiryDate = model.LicenceExpiryDate,
                Jurisdiction = model.Jurisdiction,
                WorkingStyle = model.WorkingStyle,
                BranchCode = model.BranchCode,
                BusinessType = model.BusinessType,
                CalenderType = model.CalanderType,
                YearFrom = model.YearFrom,
                YearTo = model.YearTo,
                TaxType = model.TaxType,
                SubscriptionPlanId = model.SubscriptionPlanId
            };

            ViewBag.CountryList = new SelectList(await _countryRepo.GetAll(), "Id", "Name");
            ViewBag.StateList = new SelectList(await _stateRepo.GetAll(), "Id", "Name");
            ViewBag.CityList = new SelectList(await _cityRepo.GetAll(), "Id", "Name");
            ViewBag.SubscriptionPlanName = model.SubscriptionPlan?.PlanName ?? "N/A";

            var country = model.CountryId.HasValue ? (await _countryRepo.GetAll()).FirstOrDefault(x => x.Id == model.CountryId.Value) : null;
            var state = model.StateId.HasValue ? (await _stateRepo.GetAll()).FirstOrDefault(x => x.Id == model.StateId.Value) : null;
            var city = model.CityId.HasValue ? (await _cityRepo.GetAll()).FirstOrDefault(x => x.Id == model.CityId.Value) : null;

            ViewBag.CountryName = country?.Name ?? "-";
            ViewBag.StateName = state?.Name ?? "-";
            ViewBag.CityName = city?.Name ?? "-";

            return View(Vm);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProfile(TenantRegistrationVM Vm)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId) || tenantId != Vm.Id)
            {
                return Unauthorized();
            }

            Tenant model = await _tenantRepository.GetById(tenantId);
            if (model == null)
            {
                return NotFound();
            }

            // Enforce name and critical settings constraint on the server-side to prevent tampering
            Vm.Name = model.Name;
            Vm.SubscriptionPlanId = model.SubscriptionPlanId;

            bool exists = await _tenantRepository.CheckDuplicateAsync(
                model.Name,
                Vm.GstNo,
                Vm.MobileNo,
                Vm.Email,
                model.Id);

            if (exists)
            {
                ModelState.AddModelError("", "Duplicate record already exists!");
                ViewBag.CountryList = new SelectList(await _countryRepo.GetAll(), "Id", "Name");
                ViewBag.StateList = new SelectList(await _stateRepo.GetAll(), "Id", "Name");
                ViewBag.CityList = new SelectList(await _cityRepo.GetAll(), "Id", "Name");
                ViewBag.SubscriptionPlanName = model.SubscriptionPlan?.PlanName ?? "N/A";

                var country = Vm.CountryId.HasValue ? (await _countryRepo.GetAll()).FirstOrDefault(x => x.Id == Vm.CountryId.Value) : null;
                var state = Vm.StateId.HasValue ? (await _stateRepo.GetAll()).FirstOrDefault(x => x.Id == Vm.StateId.Value) : null;
                var city = Vm.CityId.HasValue ? (await _cityRepo.GetAll()).FirstOrDefault(x => x.Id == Vm.CityId.Value) : null;

                ViewBag.CountryName = country?.Name ?? "-";
                ViewBag.StateName = state?.Name ?? "-";
                ViewBag.CityName = city?.Name ?? "-";
                return View(Vm);
            }

            string fileName = "";
            if (Vm.UploadAttachement != null && Vm.UploadAttachement.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "Tenant");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                fileName = Vm.UploadAttachement.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Vm.UploadAttachement.CopyToAsync(fileStream);
                }
                model.Logo = $"docs/Tenant/{fileName}";
            }

            model.Email = Vm.Email;
            model.Address1 = Vm.Address1;
            model.Address2 = Vm.Address2;
            model.Location = Vm.Location;
            model.CountryId = Vm.CountryId;
            model.StateId = Vm.StateId;
            model.CityId = Vm.CityId;
            model.PinCode = Vm.PinCode;
            model.ContactPerson = Vm.ContactPerson;
            model.Phone = Vm.Phone;
            model.MobileNo = Vm.MobileNo;
            model.GstNo = Vm.GstNo;
            model.StateCode = Vm.StateCode;
            model.CompanyType = Vm.CompanyType ?? CompanyType.HeadOffice;
            model.Branch = Vm.Branch;
            model.Description = Vm.Description;
            model.IFSSAINo = Vm.IFSSAINo;
            model.DrugLicNo = Vm.DrugLicNo;
            model.LicenceExpiryDate = Vm.LicenceExpiryDate;
            model.Jurisdiction = Vm.Jurisdiction;
            model.WorkingStyle = Vm.WorkingStyle ?? WorkingStyle.Text;
            model.BranchCode = Vm.BranchCode;
            model.BusinessType = Vm.BusinessType ?? BusinessType.GroceryStore;
            model.CalanderType = Vm.CalenderType ?? CalenderType.English;
            model.YearFrom = Vm.YearFrom;
            model.YearTo = Vm.YearTo;
            model.TaxType = Vm.TaxType ?? TaxType.GST;

            await _tenantRepository.Update(model);

            TempData["success"] = "Company Profile updated successfully.";
            return RedirectToAction(nameof(EditProfile));
        }
    }
}