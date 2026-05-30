using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using EasyBill.Models.Model;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using System.Security.Claims;
using EasyBill.UI.Service.Auth;


namespace EasyBill.UI.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUsers>signInManager;
        private readonly UserManager<ApplicationUsers> userManager;
        private readonly RoleManager<IdentityRole>roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantRepository _tenantRepo;
        private readonly UserLoginAuthService _userLoginAuthService;

        public AccountController(
            SignInManager<ApplicationUsers>signInManager,
            UserManager<ApplicationUsers>userManager,
            RoleManager<IdentityRole>roleManager,
            IUnitOfWork unitOfWork,
            ITenantRepository tenantRepo,
            UserLoginAuthService userLoginAuthService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this._unitOfWork = unitOfWork;
            _tenantRepo = tenantRepo;
            _userLoginAuthService = userLoginAuthService;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (returnUrl != null && returnUrl.Contains("Account/Login"))
            {
                return RedirectToAction("Index", "Home"); 
            }

            return View(new LoginViewModels
            {
                LoginType = LoginTypes.EmailPassword,
                RememberMe = true
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModels model)
        {
            model.LoginType = string.IsNullOrWhiteSpace(model.LoginType) ? LoginTypes.EmailPassword : model.LoginType.Trim();
            if (!ModelState.IsValid)
                return View(model);

            var rememberMe = model.RememberMe;
            ApplicationUsers? user = null;

            if (model.LoginType.Equals(LoginTypes.EmailPassword, StringComparison.OrdinalIgnoreCase))
            {
                user = await userManager.FindByEmailAsync(model.Email ?? string.Empty);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }

                var result = await signInManager.PasswordSignInAsync(user, model.Password ?? string.Empty, rememberMe, lockoutOnFailure: false);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }
            else if (model.LoginType.Equals(LoginTypes.MobilePin, StringComparison.OrdinalIgnoreCase))
            {
                var pinLogin = await _userLoginAuthService.VerifyMobilePinAsync(model.MobileNumber, model.Pin);
                if (!pinLogin.Success || pinLogin.User == null)
                {
                    ModelState.AddModelError(string.Empty, pinLogin.Message);
                    return View(model);
                }

                user = pinLogin.User;
                await signInManager.SignInAsync(user, isPersistent: rememberMe);
            }
            else if (model.LoginType.Equals(LoginTypes.MobileOtp, StringComparison.OrdinalIgnoreCase))
            {
                var otpLogin = await _userLoginAuthService.VerifyLoginOtpAsync(model.MobileNumber, model.Otp);
                if (!otpLogin.Success || otpLogin.User == null)
                {
                    ModelState.AddModelError(string.Empty, otpLogin.Message);
                    return View(model);
                }

                user = otpLogin.User;
                await signInManager.SignInAsync(user, isPersistent: rememberMe);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login type.");
                return View(model);
            }

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            await SetUserSessionAsync(user);
            var setupStatus = await _userLoginAuthService.GetProfileSetupStatusAsync(user);
            // Keep existing users unaffected; force security setup for new password-less registrations.
            if (!setupStatus.HasPassword)
            {
                return RedirectToAction(nameof(SetupSecurity));
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendLoginOtp(string mobileNumber)
        {
            var otpResult = await _userLoginAuthService.SendLoginOtpAsync(mobileNumber);
            return Json(new
            {
                success = otpResult.Success,
                message = otpResult.Message,
                otp = otpResult.OtpCode
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCompanyFromLogin(CompanyRegistrationRequest request)
        {
            if (!ModelState.IsValid)
            {
                TempData["RegisterError"] = "Please fill all required registration fields correctly.";
                return RedirectToAction(nameof(Login));
            }

            var registerResult = await _userLoginAuthService.RegisterCompanyAsync(request);
            if (!registerResult.Success)
            {
                TempData["RegisterError"] = registerResult.Message;
                return RedirectToAction(nameof(Login));
            }

            TempData["RegisterSuccess"] = "Company registered successfully. Please login with mobile and complete PIN/password setup.";
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SetupSecurity()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction(nameof(Login));

            var setupStatus = await _userLoginAuthService.GetProfileSetupStatusAsync(user);
            if (setupStatus.IsProfileSetupComplete)
                return RedirectToAction("Index", "Home");

            var viewModel = new SetupSecurityViewModel
            {
                Email = user.Email,
                HasPassword = setupStatus.HasPassword,
                HasPin = setupStatus.HasPin
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupSecurity(SetupSecurityViewModel model)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction(nameof(Login));

            var currentStatus = await _userLoginAuthService.GetProfileSetupStatusAsync(user);
            model.HasPassword = currentStatus.HasPassword;
            model.HasPin = currentStatus.HasPin;

            if (!ModelState.IsValid)
                return View(model);

            var request = new CompleteProfileSetupRequest
            {
                Email = model.Email,
                Password = model.Password,
                Pin = model.Pin
            };

            var result = await _userLoginAuthService.SetProfileCredentialsAsync(user, request);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            await SetUserSessionAsync(user);
            TempData["Success"] = "Security setup completed successfully.";
            return RedirectToAction("Index", "Home");
        }

        private async Task SetUserSessionAsync(ApplicationUsers user)
        {
            HttpContext.Session.SetString("UserName", user.UserName ?? string.Empty);
            HttpContext.Session.SetString("UserEmail", user.Email ?? string.Empty);
            HttpContext.Session.SetString("UserRole", (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User");
        }

        public async Task <IActionResult> UserProfile()
        {
            var user = User;

            var model = new UserProfileModel
            {
                Name = user?.Identity?.Name ?? "",
                Email = user?.FindFirst(ClaimTypes.Email)?.Value ?? "",
                Role = user?.FindFirst(ClaimTypes.Role)?.Value ?? ""
            };

            return PartialView("_UserProfilePartial", model);
        }
        //[HttpGet]
        //public IActionResult Register()
        //{
        //    RegisterViewModel vm =new RegisterViewModel();
        //    vm.EmployeeList = _unitOfWork.Employee.GetAll()
        //        .Where(d => !d.IsDeleted)
        //        .Select(emp => new SelectListItem
        //        {
        //            Value = emp.Id.ToString(),
        //            Text = emp.Name
        //        })
        //        .ToList();
        //    vm.RoleList = roleManager.Roles.Select(role => new SelectListItem
        //    {
        //        Value = role.Id, Text=role.Name
        //    }).ToList();

        //    return View(vm);
        //}
        //public IActionResult GetEmployeeDetails(int employeeId)
        //{
        //    var data = _unitOfWork.Employee.Get(u => u.Id == employeeId);
        //    if (data == null)
        //    {
        //        return Json(new { error = "Project Details not found." });
        //    }
        //    var empEmail = data.Email;
        //    var Details = new
        //    {
        //        email=empEmail,
        //    };
        //    return Json(Details);
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]       
        //public async Task<IActionResult> Register(RegisterViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }
        //    var user = new Users
        //    {
        //        EmployeeId = model.EmployeeId,
        //        UserName = model.Email,
        //        NormalizedUserName = model.Email.ToUpper(),
        //        Email = model.Email,
        //        NormalizedEmail = model.Email.ToUpper(),
        //        EmailConfirmed = true,
        //        PhoneNumberConfirmed = true,
        //    };

        //    var result = await userManager.CreateAsync(user, model.Password);
        //    if (result.Succeeded)
        //    {
        //        var role = await roleManager.FindByIdAsync(model.RoleId);
        //        if (role == null)
        //        {
        //            ModelState.AddModelError("", "Selected role does not exist.");
        //            return View(model);
        //        }
        //        await userManager.AddToRoleAsync(user, role.Name);

        //        await signInManager.SignInAsync(user, isPersistent: false);
        //        return RedirectToAction("Register", "Account");
        //    }
        //    foreach (var error in result.Errors)
        //    {
        //        ModelState.TryAddModelError(string.Empty, error.Description);
        //    }
        //    return View(model);
        //}


        [HttpGet]
        public IActionResult VerifyEmail()
        {
            return View();
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult>VerifyEmail(VerifyEmailViewModel model)
        //{
        //    if(!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }
        //    var user= await userManager.FindByNameAsync(model.Email);
        //    if (user == null)
        //    {
        //        ModelState.AddModelError("", "User not found!");
        //        return View(model);
        //    }
        //    else
        //    {
        //        return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
        //    }

        //}

        [HttpGet]
        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View(new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                return View(model);
            }
            var result = await userManager.RemovePasswordAsync(user);
            if (result.Succeeded)
            {
                result = await userManager.AddPasswordAsync(user, model.NewPassword);
                return RedirectToAction("Login", "Account");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            HttpContext.Session.Clear(); 
            return RedirectToAction("Login", "Account");
        }
        [HttpGet]
        public async Task<IActionResult> UsersAccount()
        {
            var users = await _tenantRepo.GetAllUsers();
            var userList = new List<ApplicationUserVM>();

            if (!User.IsInRole("SuperAdmin"))
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                users = users.Where(u => u.TenantId == tenantId).ToList();
            }

            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);

                // Strip tenant prefix for non-SuperAdmins when displaying the role names
                var roleNames = new List<string>();
                foreach (var role in roles)
                {
                    if (!User.IsInRole("SuperAdmin") && !string.IsNullOrEmpty(user.TenantId) && role.StartsWith(user.TenantId + "_"))
                    {
                        roleNames.Add(role.Substring(user.TenantId.Length + 1));
                    }
                    else
                    {
                        roleNames.Add(role);
                    }
                }

                userList.Add(new ApplicationUserVM
                {
                    Id = user.Id,
                    TenantId = user.TenantId,
                    TenantName = user.TenantName,
                    Email = user.Email,
                    MobileNo = user.PhoneNumber,
                    EmployeeId = user.EmployeeId,
                    EmployeeName = user.Employee?.Name, 
                    Role = string.Join(", ", roleNames)
                });
            }
            return View(userList);
        }

        [HttpGet]
        public async Task<IActionResult> EditUsersAccount(string id)
        {
            var users = await userManager.Users.FirstOrDefaultAsync(x => x.Id == id);

            if (users == null)
                return View();

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (!isSuperAdmin)
            {
                if (users.TenantId != tenantId)
                {
                    return NotFound();
                }
            }

            var roles = await userManager.GetRolesAsync(users);
            List<IdentityRole> dbRoles;

            if (isSuperAdmin)
            {
                dbRoles = await roleManager.Roles.AsNoTracking().ToListAsync();
            }
            else
            {
                dbRoles = await roleManager.Roles.AsNoTracking()
                    .Where(r => r.Name == "Admin" || r.Name == "User" || (!string.IsNullOrEmpty(tenantId) && r.Name.StartsWith(tenantId + "_")))
                    .ToListAsync();
            }

            ViewBag.roleList = dbRoles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = (!isSuperAdmin && !string.IsNullOrEmpty(tenantId) && r.Name.StartsWith(tenantId + "_"))
                       ? r.Name.Substring(tenantId.Length + 1)
                       : r.Name
            }).ToList();

            var vm = new ApplicationUserVM
            {
                Id = users.Id,
                TenantId = users.TenantId,
                TenantName = users.TenantName,
                Email = users.Email,
                MobileNo = users.PhoneNumber,
                Role = roles.FirstOrDefault() ?? string.Empty
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> EditUsersAccount(ApplicationUserVM viewModel)
        {
            if (viewModel == null || string.IsNullOrEmpty(viewModel.Id))
            {
                TempData["error"] = "Data is invalid!";
                return RedirectToAction("UsersAccount", "Account");
            }

            var user = await userManager.FindByIdAsync(viewModel.Id);
            if (user == null)
                return NotFound();

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (!isSuperAdmin)
            {
                if (user.TenantId != tenantId)
                {
                    return NotFound();
                }
            }

            user.Email = viewModel.Email;
            user.UserName = viewModel.Email;
            user.PhoneNumber = viewModel.MobileNo;

            var updateResult = await userManager.UpdateAsync(user);

            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await userManager.RemoveFromRolesAsync(user, currentRoles);

            var selectedRole = await roleManager.FindByNameAsync(viewModel.Role);
            if (selectedRole == null)
            {
                ModelState.AddModelError("Role", "Selected role does not exist.");
            }
            else
            {
                if (!isSuperAdmin)
                {
                    var isGlobal = selectedRole.Name == "Admin" || selectedRole.Name == "User" || selectedRole.Name == "SuperAdmin";
                    var isTenantRole = !string.IsNullOrEmpty(tenantId) && selectedRole.Name.StartsWith(tenantId + "_");
                    if (!isGlobal && !isTenantRole)
                    {
                        ModelState.AddModelError("Role", "Unauthorized role selection.");
                    }
                }

                if (ModelState.IsValid)
                {
                    await userManager.AddToRoleAsync(user, selectedRole.Name);
                }
            }

            if (updateResult.Succeeded && ModelState.IsValid)
            {
                TempData["Success"] = "User updated successfully.";
                return RedirectToAction("UsersAccount", "Account");
            }

            List<IdentityRole> dbRoles;
            if (isSuperAdmin)
            {
                dbRoles = await roleManager.Roles.AsNoTracking().ToListAsync();
            }
            else
            {
                dbRoles = await roleManager.Roles.AsNoTracking()
                    .Where(r => r.Name == "Admin" || r.Name == "User" || (!string.IsNullOrEmpty(tenantId) && r.Name.StartsWith(tenantId + "_")))
                    .ToListAsync();
            }

            ViewBag.roleList = dbRoles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = (!isSuperAdmin && !string.IsNullOrEmpty(tenantId) && r.Name.StartsWith(tenantId + "_"))
                       ? r.Name.Substring(tenantId.Length + 1)
                       : r.Name
            }).ToList();

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
