using AOne.DataAccess.ProfileService;
using DocumentFormat.OpenXml.Presentation;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Service.Loyalty;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ICustomerRepository _customerservice;
        private readonly IAccountGroupRepository _accountgroupRepo;
        private readonly CustomerLoyaltyService _customerLoyaltyService;
        public CustomerController(
            ICustomerRepository customerservice,
            IAccountGroupRepository accountgroupRepo,
            CustomerLoyaltyService customerLoyaltyService)
        {
            _customerservice = customerservice;
            _accountgroupRepo = accountgroupRepo;
            _customerLoyaltyService = customerLoyaltyService;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _customerservice.GetAll();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Loyalty(int id)
        {
            var customer = await _customerservice.GetById(id);
            if (customer == null)
            {
                return NotFound();
            }

            ViewBag.CustomerId = customer.Id;
            ViewBag.CustomerName = customer.Name;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetLoyaltyData(int id)
        {
            var report = await _customerLoyaltyService.GetCustomerLoyaltyReportAsync(id);
            if (report == null)
            {
                return Json(new { success = false, message = "Customer not found." });
            }

            return Json(new { success = true, data = report });
        }

        [HttpPost]
        public async Task<IActionResult> CreateFromModal(AccountGroupVM VM)
        {                // Validation

            try
            {
                if (string.IsNullOrWhiteSpace(VM.Name))
                {
                    return Json(new { success = false, message = "Name is required." });
                }

                if (!VM.ParentId.HasValue || VM.ParentId <= 0)
                {
                    return Json(new { success = false, message = "Please select Under." });
                }

                // Check duplicate name
                var existingGroup = await _accountgroupRepo.GetAll();
                if (existingGroup.Any(x => x.Name.Trim().ToLower() == VM.Name.Trim().ToLower()))
                {
                    return Json(new { success = false, message = "Account Group name already exists." });
                }

                var model = new AccountGroup
                {
                    Name = VM.Name.Trim(),
                    ParentId = VM.ParentId,
                    IsActive = VM.IsActive
                };

                await _accountgroupRepo.Create(model);

                return Json(new
                {
                    success = true,
                    message = "Account Group created successfully.",
                    data = new
                    {
                        id = model.Id,
                        name = model.Name
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var accountGroups = await _accountgroupRepo.GetAll();
            var sundryDebtor = accountGroups.FirstOrDefault(x => x.Name == "Sundry Debtor");

            // Create empty VM with default values
            var vm = new CustomerVM
            {
                GSTType = GSTType.UnRegistered,  // Default value
                Status = CustomerStatus.Active,   // Default value
                AccountGroupId = sundryDebtor?.Id
            };

            ViewBag.AccountGroupList = new SelectList(accountGroups, "Id", "Name", sundryDebtor?.Id);
            ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");

            ViewBag.GSTTypes = Enum.GetValues(typeof(GSTType))
                .Cast<GSTType>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = x.ToString()
                })
                .ToList();

            ViewBag.CustomerCategories = Enum.GetValues(typeof(CustomerCategory))
                 .Cast<CustomerCategory>()
                 .Select(x => new SelectListItem
                 {
                     Value = ((int)x).ToString(),
                     Text = x.ToString()
                 })
                 .ToList();

            return View(vm);  // ✅ Pass VM to view
        }

        //[HttpGet]
        //public async Task<IActionResult> Create()
        //{
        //    ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");

        //    // ✅ Parent list for modal dropdown
        //    ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");


        //    // GSTType enum → dropdown list
        //    ViewBag.GSTTypes = Enum.GetValues(typeof(GSTType))
        //        .Cast<GSTType>()
        //        .Select(x => new SelectListItem
        //        {
        //            Value = ((int)x).ToString(),
        //            Text = x.ToString()
        //        })
        //        .ToList();

        //    ViewBag.CustomerCategories = Enum.GetValues(typeof(CustomerCategory))
        //         .Cast<CustomerCategory>()
        //         .Select(x => new SelectListItem
        //         {
        //             Value = ((int)x).ToString(),
        //             Text = x.ToString()
        //         })
        //         .ToList();

        //    return View();
        //}

        [HttpPost]
        public async Task<IActionResult> Create(CustomerVM Vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
                ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
                ViewBag.GSTTypes = Enum.GetValues(typeof(GSTType))
                    .Cast<GSTType>()
                    .Select(x => new SelectListItem
                    {
                        Value = ((int)x).ToString(),
                        Text = x.ToString()
                    })
                    .ToList();
                ViewBag.CustomerCategories = Enum.GetValues(typeof(CustomerCategory))
                     .Cast<CustomerCategory>()
                     .Select(x => new SelectListItem
                     {
                         Value = ((int)x).ToString(),
                         Text = x.ToString()
                     })
                     .ToList();
                return View(Vm);
            }

            var customerList = await _customerservice.GetAll();

            // Phone duplicate check
            if (!string.IsNullOrWhiteSpace(Vm.PhoneNo))
            {
                bool isExist = customerList.Any(x =>
                    x.Id != Vm.Id &&
                    !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                    x.PhoneNo.Trim() == Vm.PhoneNo.Trim()
                );
                if (isExist)
                {
                    ModelState.AddModelError("PhoneNo", "Customer phone number already exists.");

                    ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
                    ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");
                    ViewBag.GSTTypes = Enum.GetValues(typeof(GSTType))
                        .Cast<GSTType>()
                        .Select(x => new SelectListItem
                        {
                            Value = ((int)x).ToString(),
                            Text = x.ToString()
                        })
                        .ToList();
                    ViewBag.CustomerCategories = Enum.GetValues(typeof(CustomerCategory))
                         .Cast<CustomerCategory>()
                         .Select(x => new SelectListItem
                         {
                             Value = ((int)x).ToString(),
                             Text = x.ToString()
                         })
                         .ToList();

                    return View(Vm);
                }
            }

            // GSTType safety (production rule)
            Vm.GSTType = string.IsNullOrWhiteSpace(Vm.GSTNo)
                ? GSTType.UnRegistered
                : GSTType.Registered;

            var model = new Customer
            {
                Name = Vm.Name,
                Address = Vm.Address,
                PhoneNo = Vm.PhoneNo,
                Email = Vm.Email,
                AccountGroupId = Vm.AccountGroupId,
                GSTNo = Vm.GSTNo?.ToUpper(),
                StateCode = Vm.StateCode,
                GSTType = Vm.GSTType,
                Category = Vm.Category,
                Status = Vm.Status,
                PaymentDays = Vm.PaymentDays
            };

            await _customerservice.Create(model);
            TempData["success"] = "Customer created successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            Customer model = await _customerservice.GetById(Id);

            CustomerVM VM = new CustomerVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.Name = model.Name;
                VM.Address = model.Address;
                VM.PhoneNo = model.PhoneNo;
                VM.Email = model.Email;
                VM.AccountGroupId = model.AccountGroupId;
                VM.GSTNo = model.GSTNo;
                VM.StateCode = model.StateCode;
                VM.GSTType = model.GSTType;
                VM.Category = model.Category;
                VM.Status = model.Status;
                VM.PaymentDays = model.PaymentDays;
            }

            // ViewBag populate karo
            ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name", VM.AccountGroupId);
            ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");

            ViewBag.GSTTypes = Enum.GetValues(typeof(GSTType))
                .Cast<GSTType>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = x.ToString()
                })
                .ToList();

            ViewBag.CustomerCategories = Enum.GetValues(typeof(CustomerCategory))
                .Cast<CustomerCategory>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = x.ToString()
                })
                .ToList();

            return View(VM);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CustomerVM VM)
        {
            if (!ModelState.IsValid)
            {
                // ViewBag reload karo validation error ke liye
                ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name", VM.AccountGroupId);
                ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");

                ViewBag.GSTTypes = Enum.GetValues(typeof(GSTType))
                    .Cast<GSTType>()
                    .Select(x => new SelectListItem
                    {
                        Value = ((int)x).ToString(),
                        Text = x.ToString()
                    })
                    .ToList();

                ViewBag.CustomerCategories = Enum.GetValues(typeof(CustomerCategory))
                    .Cast<CustomerCategory>()
                    .Select(x => new SelectListItem
                    {
                        Value = ((int)x).ToString(),
                        Text = x.ToString()
                    })
                    .ToList();

                return View(VM);
            }

            // Get all customers
            var CustomerData = await _customerservice.GetAll();

            // PhoneNo Duplicate check
            if (!string.IsNullOrEmpty(VM.PhoneNo))
            {
                var isExist = !string.IsNullOrWhiteSpace(VM.PhoneNo) &&
                         CustomerData.Any(x =>
                             x.Id != VM.Id &&
                             !string.IsNullOrWhiteSpace(x.PhoneNo) &&
                             string.Equals(
                                 x.PhoneNo.Trim(),
                                 VM.PhoneNo.Trim(),
                                 StringComparison.OrdinalIgnoreCase
                             )
                         );

                if (isExist)
                {
                    TempData["error"] = "Customer's PhoneNo already exists.";

                    // ViewBag reload karo error ke liye
                    ViewBag.AccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name", VM.AccountGroupId);
                    ViewBag.ParentAccountGroupList = new SelectList(await _accountgroupRepo.GetAll(), "Id", "Name");

                    ViewBag.GSTTypes = Enum.GetValues(typeof(GSTType))
                        .Cast<GSTType>()
                        .Select(x => new SelectListItem
                        {
                            Value = ((int)x).ToString(),
                            Text = x.ToString()
                        })
                        .ToList();

                    ViewBag.CustomerCategories = Enum.GetValues(typeof(CustomerCategory))
                        .Cast<CustomerCategory>()
                        .Select(x => new SelectListItem
                        {
                            Value = ((int)x).ToString(),
                            Text = x.ToString()
                        })
                        .ToList();

                    return View(VM);
                }
            }

            // GSTType safety (production rule)
            VM.GSTType = string.IsNullOrWhiteSpace(VM.GSTNo)
                ? GSTType.UnRegistered
                : GSTType.Registered;

            Customer model = await _customerservice.GetById(VM.Id);
            if (model != null)
            {
                model.Name = VM.Name;
                model.Address = VM.Address;
                model.PhoneNo = VM.PhoneNo;
                model.Email = VM.Email;
                model.AccountGroupId = VM.AccountGroupId;
                model.GSTNo = VM.GSTNo?.ToUpper();
                model.StateCode = VM.StateCode;
                model.GSTType = VM.GSTType;
                model.Category = VM.Category;
                model.Status = VM.Status;
                model.PaymentDays = VM.PaymentDays;

                await _customerservice.Update(model);
                TempData["success"] = "Customer updated successfully.";
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

                var model = await _customerservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                await _customerservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        public async Task<JsonResult> CheckDuplicate(string name, int id)
        {
            bool result = await _customerservice.CheckDuplicateAsync(name, id);

            return Json(new
            {
                exists = result
            });
        }
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

            var existingGroups = await _accountgroupRepo.GetAll();
            bool exists = existingGroups.Any(x => x.Name.Trim().ToLower() == name.Trim().ToLower() && x.Id != id);
            if (exists)
                return Json(new { success = false, message = "Name already exists!" });

            if (id == 0)
            {
                var entity = new AccountGroup
                {
                    Name = name.Trim(),
                    ParentId = parentId,
                    IsActive = isActive
                };
                await _accountgroupRepo.Create(entity);
                return Json(new { success = true, newId = entity.Id });
            }
            else
            {
                var entity = existingGroups.FirstOrDefault(x => x.Id == id);
                if (entity == null)
                    return Json(new { success = false, message = "Not found!" });
                entity.Name = name.Trim();
                entity.ParentId = parentId;
                entity.IsActive = isActive;
                await _accountgroupRepo.Update(entity);
                return Json(new { success = true, newId = id });
            }
        }

    }
}
