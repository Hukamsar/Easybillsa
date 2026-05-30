using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeservice;
        private readonly IDepartmentRepository _departmentservice;
        private readonly IDesignationRepository _designationService;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUsers> userManager;
      //  private readonly IMailService _mailService;

        public EmployeeController(IEmployeeRepository employeeservice,
            IDepartmentRepository departmentservice,
            IDesignationRepository designationService,
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUsers> userManager
           // IMailService mailService
            )
        {
            _employeeservice = employeeservice;
            _departmentservice = departmentservice;
            _designationService = designationService;
            this.roleManager = roleManager;
            this.userManager = userManager;
           // _mailService = mailService;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _employeeservice.GetAll();
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new EmployeeVM();

            ViewBag.Department = new SelectList(await _departmentservice.GetAll(), "Id", "Name");
            ViewBag.Designation = new SelectList(await _designationService.GetAll(), "Id", "Name");

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            List<IdentityRole> roles;

            if (isSuperAdmin)
            {
                roles = await roleManager.Roles.AsNoTracking().ToListAsync();
            }
            else
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                roles = await roleManager.Roles.AsNoTracking()
                    .Where(r => r.Name == "Admin" || r.Name == "User" || (!string.IsNullOrEmpty(tenantId) && r.Name.StartsWith(tenantId + "_")))
                    .ToListAsync();
            }

            var currentTenantId = User.FindFirst("TenantId")?.Value;
            viewModel.RoleList = roles.Select(role => new SelectListItem
            {
                Value = role.Id,
                Text = (!isSuperAdmin && !string.IsNullOrEmpty(currentTenantId) && role.Name.StartsWith(currentTenantId + "_"))
                       ? role.Name.Substring(currentTenantId.Length + 1)
                       : role.Name
            }).ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeVM EmpVM)
        {
            if (EmpVM == null)
            {
                TempData["error"] = "Invalid request.";
                return RedirectToAction("Index");
            }
            if (EmpVM != null)
            {
                var employeeData = await _employeeservice.GetAll();

                // Phone Duplicate check
                if (!string.IsNullOrEmpty(EmpVM.Phone))
                {
                    var isExist = !string.IsNullOrWhiteSpace(EmpVM.Phone) &&
                             employeeData.Any(x =>
                                 x.Id != EmpVM.Id &&
                                 !string.IsNullOrWhiteSpace(x.Phone) &&
                                 string.Equals(
                                     x.Phone.Trim(),
                                     EmpVM.Phone.Trim(),
                                     StringComparison.OrdinalIgnoreCase
                                 )
                             );

                    if (isExist)
                    {
                        TempData["error"] = "PhoneNo already exists.";

                        ViewBag.Department = new SelectList(await _departmentservice.GetAll(), "Id", "Name");
                        ViewBag.Designation = new SelectList(await _designationService.GetAll(), "Id", "Name");
                        return View(EmpVM);
                    }
                }

                var model = new Employee
                {
                    Name = EmpVM.Name.Trim(),
                    DepartMentId = EmpVM.DepartMentId,
                    DesignationId = EmpVM.DesignationId,
                    Email = EmpVM.Email?.Trim(),
                    Phone = EmpVM.Phone?.Trim(),
                    RequiredCreditional = EmpVM.RequiredCreditional,
                    Initials = EmpVM.Initials
                };
                await _employeeservice.Create(model);
                var employeeId = model.Id;
                string password = "Hospicarx@123";
             //   string encryptedPassword = CustomEncryptionHelper.Encrypt(password);
                if (EmpVM.RequiredCreditional)
                {
                    var user = new ApplicationUsers
                    {
                        UserName = model.Email?.Trim(),
                        NormalizedUserName = model.Email?.ToUpper(),
                        Email = model.Email,
                        NormalizedEmail = model.Email?.ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        TenantId = model.TenantId,
                        PhoneNumber = model.Phone?.Trim(),
                        PhoneNumberConfirmed = true,
                      //  Custom = encryptedPassword,
                      //  Login = DateTime.Now,
                        EmployeeId = employeeId,
                    };
                    var result = await userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                    {
                        var role = await roleManager.FindByIdAsync(EmpVM.RoleId);
                        await userManager.AddToRoleAsync(user, role.Name);
                        //if (!string.IsNullOrEmpty(model.Email))
                        //{
                        //    var mailRequest = new MailRequest
                        //    {
                        //        To = model.Email,
                        //        Subject = "Login creditionals.",
                        //        Body = $"Your UserName is {model.Email} and  password is {password}"
                        //    };
                        //    await _mailService.SendAsync(mailRequest);
                        //}
                    }
                }
            }
            TempData["success"] = "Employee created successfully!";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            Employee model = await _employeeservice.GetById(Id);
            EmployeeVM EmpVM = new EmployeeVM();
            if (model != null)
            {
                EmpVM.Id = model.Id;
                EmpVM.Name = model.Name;
                EmpVM.DepartMentId = model.DepartMentId;
                EmpVM.DesignationId = model.DesignationId;
                EmpVM.Email = model.Email;
                EmpVM.Phone = model.Phone;
                EmpVM.RequiredCreditional = model.RequiredCreditional;
                EmpVM.Initials = model.Initials;
            }
            ViewBag.Department = new SelectList(await _departmentservice.GetAll(), "Id", "Name");
            ViewBag.Designation = new SelectList(await _designationService.GetAll(), "Id", "Name");
            return View(EmpVM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(EmployeeVM VM)
        {
            var employeeData = await _employeeservice.GetAll();

            // Phone Duplicate check
            if (!string.IsNullOrEmpty(VM.Phone))
            {
                var isExist = !string.IsNullOrWhiteSpace(VM.Phone) &&
                         employeeData.Any(x =>
                             x.Id != VM.Id &&
                             !string.IsNullOrWhiteSpace(x.Phone) &&
                             string.Equals(
                                 x.Phone.Trim(),
                                 VM.Phone.Trim(),
                                 StringComparison.OrdinalIgnoreCase
                             )
                         );

                if (isExist)
                {
                    TempData["error"] = "PhoneNo already exists.";
                    return View(VM);
                }
            }

            Employee model = await _employeeservice.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.Name = VM.Name.Trim();
                model.DepartMentId = VM.DepartMentId;
                model.DesignationId = VM.DesignationId;
                model.Email = VM.Email?.Trim();
                model.Phone = VM.Phone?.Trim();
                model.RequiredCreditional = VM.RequiredCreditional;
                model.Initials = VM.Initials;
                await _employeeservice.Update(model);
            }
            TempData["success"] = "Employee updated successfully!";
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
                var model = await _employeeservice.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _employeeservice.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        [HttpGet]
        public async Task<JsonResult> CheckEmailAddress(string email, int id = 0)
        {
            bool exists = await _employeeservice.ExistEmail(email, id);
            return Json(new { exists });
        }

        [HttpGet]
        public async Task<JsonResult> CheckPhoneNo(string phoneNo, int id = 0)
        {
            bool exists = await _employeeservice.ExistMobile(phoneNo, id);
            return Json(new { exists });
        }
        public async Task<JsonResult> CheckDuplicate(string name)
        {
            bool result = await _employeeservice.CheckDuplicateAsync(name);

            return Json(new
            {
                exists = result
            });
        }
        // ================= ADDED FOR MODAL (F2 / F3) =================

        [HttpGet]
        public async Task<JsonResult> GetDepartments()
        {
            var list = (await _departmentservice.GetAll())
                .Select(d => new { value = d.Id, text = d.Name }).ToList();
            return Json(list);
        }

        [HttpPost]
        public async Task<JsonResult> QuickSaveDepartment(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name required!" });

            bool exists = (await _departmentservice.GetAll()).Any(d => d.Name.ToLower() == name.Trim().ToLower());
            if (exists) return Json(new { success = false, message = "Department already exists!" });

            if (id == 0)
            {
                var model = new Department { Name = name.Trim() };
                await _departmentservice.Create(model);
                return Json(new { success = true, newId = model.Id });
            }
            else
            {
                var model = await _departmentservice.GetById(id);
                if (model == null) return Json(new { success = false, message = "Not found!" });
                model.Name = name.Trim();
                await _departmentservice.Update(model);
                return Json(new { success = true, newId = model.Id });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDesignations()
        {
            var list = (await _designationService.GetAll())
                .Select(d => new { value = d.Id, text = d.Name }).ToList();
            return Json(list);
        }

        [HttpPost]
        public async Task<JsonResult> QuickSaveDesignation(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name required!" });

            bool exists = (await _designationService.GetAll()).Any(d => d.Name.ToLower() == name.Trim().ToLower());
            if (exists) return Json(new { success = false, message = "Designation already exists!" });

            if (id == 0)
            {
                var model = new Designation { Name = name.Trim() };
                await _designationService.Create(model);
                return Json(new { success = true, newId = model.Id });
            }
            else
            {
                var model = await _designationService.GetById(id);
                if (model == null) return Json(new { success = false, message = "Not found!" });
                model.Name = name.Trim();
                await _designationService.Update(model);
                return Json(new { success = true, newId = model.Id });
            }
        }
    }
}
