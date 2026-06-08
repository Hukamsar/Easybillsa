using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using AOne.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using EasyBill.Models.Entity;

namespace EasyBill.UI.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeservice;
        private readonly IDepartmentRepository _departmentservice;
        private readonly IDesignationRepository _designationService;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUsers> userManager;
        private readonly ApplicationDbContext _context;
      //  private readonly IMailService _mailService;

        public EmployeeController(IEmployeeRepository employeeservice,
            IDepartmentRepository departmentservice,
            IDesignationRepository designationService,
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUsers> userManager,
            ApplicationDbContext context
           // IMailService mailService
            )
        {
            _employeeservice = employeeservice;
            _departmentservice = departmentservice;
            _designationService = designationService;
            this.roleManager = roleManager;
            this.userManager = userManager;
            _context = context;
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
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (EmpVM.RequiredCreditional)
                {
                    if (!string.IsNullOrEmpty(tenantId))
                    {
                        var tenant = await _context.Tenants.Include(t => t.SubscriptionPlan).FirstOrDefaultAsync(t => t.Id == tenantId);
                        if (tenant != null)
                        {
                            bool extraUsersActive = tenant.ExtraUsersExpiryDate != null && tenant.ExtraUsersExpiryDate > DateTime.Now;
                            int maxAllowed = (tenant.SubscriptionPlan?.MaxDesktopLogins ?? 0) + (extraUsersActive ? tenant.ExtraUsers : 0);
                            
                            var currentUsers = await _context.Users.CountAsync(u => u.TenantId == tenantId);
                            if (currentUsers >= maxAllowed)
                            {
                                TempData["error"] = $"User limit reached (allowed: {maxAllowed}, current: {currentUsers}). Please purchase more user slots on the Home page.";
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
                                    roles = await roleManager.Roles.AsNoTracking()
                                        .Where(r => r.Name == "Admin" || r.Name == "User" || (!string.IsNullOrEmpty(tenantId) && r.Name.StartsWith(tenantId + "_")))
                                        .ToListAsync();
                                }
                                EmpVM.RoleList = roles.Select(role => new SelectListItem
                                {
                                    Value = role.Id,
                                    Text = (!isSuperAdmin && !string.IsNullOrEmpty(tenantId) && role.Name.StartsWith(tenantId + "_"))
                                           ? role.Name.Substring(tenantId.Length + 1)
                                           : role.Name
                                }).ToList();

                                return View(EmpVM);
                            }
                        }
                    }
                }
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

                try
                {
                    await _employeeservice.Create(model);
                    var employeeId = model.Id;
                    string password = "Hospicarx@123";
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
                            EmployeeId = employeeId,
                        };
                        var result = await userManager.CreateAsync(user, password);
                        if (result.Succeeded)
                        {
                            var role = await roleManager.FindByIdAsync(EmpVM.RoleId);
                            await userManager.AddToRoleAsync(user, role.Name);
                        }
                        else
                        {
                            // Delete the employee to prevent orphan entry
                            await _employeeservice.Delete(model);

                            TempData["error"] = "Failed to create login credentials: " + string.Join(", ", result.Errors.Select(e => e.Description));
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
                                roles = await roleManager.Roles.AsNoTracking()
                                    .Where(r => r.Name == "Admin" || r.Name == "User" || (!string.IsNullOrEmpty(tenantId) && r.Name.StartsWith(tenantId + "_")))
                                    .ToListAsync();
                            }
                            EmpVM.RoleList = roles.Select(role => new SelectListItem
                            {
                                Value = role.Id,
                                Text = (!isSuperAdmin && !string.IsNullOrEmpty(tenantId) && role.Name.StartsWith(tenantId + "_"))
                                       ? role.Name.Substring(tenantId.Length + 1)
                                       : role.Name
                            }).ToList();

                            return View(EmpVM);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = "Error creating employee: " + ex.Message;
                    return RedirectToAction("Index");
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
                string oldEmail = model.Email ?? string.Empty;
                string newEmail = VM.Email?.Trim() ?? string.Empty;
                string oldPhone = model.Phone ?? string.Empty;
                string newPhone = VM.Phone?.Trim() ?? string.Empty;

                if (model.RequiredCreditional)
                {
                    var user = await userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == model.Id);
                    if (user != null)
                    {
                        if (!string.Equals(oldEmail, newEmail, StringComparison.OrdinalIgnoreCase))
                        {
                            var existingByEmail = await userManager.FindByEmailAsync(newEmail);
                            if (existingByEmail != null && existingByEmail.Id != user.Id)
                            {
                                TempData["error"] = "Failed to update employee: Email is already taken by another user.";
                                ViewBag.Department = new SelectList(await _departmentservice.GetAll(), "Id", "Name");
                                ViewBag.Designation = new SelectList(await _designationService.GetAll(), "Id", "Name");
                                return View(VM);
                            }
                            user.Email = newEmail;
                            user.UserName = newEmail;
                            user.NormalizedEmail = newEmail.ToUpper();
                            user.NormalizedUserName = newEmail.ToUpper();
                        }

                        if (!string.Equals(oldPhone, newPhone, StringComparison.OrdinalIgnoreCase))
                        {
                            user.PhoneNumber = newPhone;
                        }

                        var userResult = await userManager.UpdateAsync(user);
                        if (!userResult.Succeeded)
                        {
                            TempData["error"] = "Failed to update user credentials: " + string.Join(", ", userResult.Errors.Select(e => e.Description));
                            ViewBag.Department = new SelectList(await _departmentservice.GetAll(), "Id", "Name");
                            ViewBag.Designation = new SelectList(await _designationService.GetAll(), "Id", "Name");
                            return View(VM);
                        }
                    }
                }

                model.Name = VM.Name.Trim();
                model.DepartMentId = VM.DepartMentId;
                model.DesignationId = VM.DesignationId;
                model.Email = newEmail;
                model.Phone = newPhone;
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
