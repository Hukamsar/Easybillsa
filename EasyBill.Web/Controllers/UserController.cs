using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUsers> _usersManager;
        private readonly RoleManager<IdentityRole> _rolemanager;
        public UserController(UserManager<ApplicationUsers> userManager, RoleManager<IdentityRole> rolemanager) 
        { 
            _usersManager = userManager;
            _rolemanager = rolemanager;
        }
        public async Task<IActionResult> UserList()
        {
            var userdata = await _usersManager.Users.ToListAsync();
            return View(userdata);
        }
        public async Task<IActionResult> RoleList()
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            List<IdentityRole> roleData;

            if (isSuperAdmin)
            {
                roleData = await _rolemanager.Roles.AsNoTracking().ToListAsync();
            }
            else
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                roleData = await _rolemanager.Roles.AsNoTracking()
                    .Where(x => x.Name == "Admin" || x.Name == "User" || (!string.IsNullOrEmpty(tenantId) && x.Name.StartsWith(tenantId + "_")))
                    .ToListAsync();

                // Strip the tenant prefix for displaying clean names in the UI
                foreach (var role in roleData)
                {
                    if (!string.IsNullOrEmpty(tenantId) && role.Name.StartsWith(tenantId + "_"))
                    {
                        role.Name = role.Name.Substring(tenantId.Length + 1);
                    }
                }
            }

            var viewModel = new RoleListVM
            {     
                ExistingRoles = roleData            
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(RoleListVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var roleName = model.RoleName.Trim();

            if (!isSuperAdmin)
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    TempData["error"] = "Tenant information is missing.";
                    return RedirectToAction("RoleList");
                }
                roleName = $"{tenantId}_{roleName}";
            }

            if (!await _rolemanager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole(roleName);
                var result = await _rolemanager.CreateAsync(role);

                if (result.Succeeded)
                {
                    TempData["success"] = "Role created successfully.";
                    return RedirectToAction("RoleList");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                TempData["error"] = "Role already exists.";
            }

            return RedirectToAction("RoleList");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string roleId)
        {
            var role = await _rolemanager.FindByIdAsync(roleId);
            if (role == null) { return NotFound(); }

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            if (!isSuperAdmin)
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var isGlobal = role.Name == "Admin" || role.Name == "User" || role.Name == "SuperAdmin";
                var isTenantRole = !string.IsNullOrEmpty(tenantId) && role.Name.StartsWith(tenantId + "_");

                if (!isGlobal && !isTenantRole)
                {
                    return NotFound();
                }
            }

            var displayName = role.Name;
            if (!isSuperAdmin)
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (!string.IsNullOrEmpty(tenantId) && displayName.StartsWith(tenantId + "_"))
                {
                    displayName = displayName.Substring(tenantId.Length + 1);
                }
            }

            var model = new RoleListVM
            {
                RoleName = displayName
            };
            ViewBag.RoleId = role.Id;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string roleId, RoleListVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoleId = roleId;
                return View(model);
            }

            var role = await _rolemanager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound();
            }

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            if (!isSuperAdmin)
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                var isGlobal = role.Name == "Admin" || role.Name == "User" || role.Name == "SuperAdmin";

                if (isGlobal)
                {
                    TempData["error"] = "System roles cannot be modified.";
                    return RedirectToAction("RoleList");
                }

                var isTenantRole = !string.IsNullOrEmpty(tenantId) && role.Name.StartsWith(tenantId + "_");
                if (!isTenantRole)
                {
                    return NotFound();
                }

                var newName = $"{tenantId}_{model.RoleName.Trim()}";
                if (role.Name != newName && await _rolemanager.RoleExistsAsync(newName))
                {
                    TempData["error"] = "Role already exists.";
                    ViewBag.RoleId = roleId;
                    return View(model);
                }

                role.Name = newName;
            }
            else
            {
                role.Name = model.RoleName.Trim();
            }

            role.NormalizedName = role.Name.ToUpper();
            var result = await _rolemanager.UpdateAsync(role);
            if (result.Succeeded)
            {
                TempData["success"] = "Role updated successfully.";
                return RedirectToAction("RoleList");
            }
            else
            {
                TempData["error"] = "Role couldn't be Updated.";
            }
            ViewBag.RoleId = roleId;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string? id)
        {
            try
            {
                if (id == null)
                {
                    return Json(new { success = false, message = "Invalid Id for deletion." });
                }
                var role = await _rolemanager.FindByIdAsync(id);
                if (role == null)
                {
                    return Json(new { success = false, message = "Role not found." });
                }

                var isSuperAdmin = User.IsInRole("SuperAdmin");
                if (!isSuperAdmin)
                {
                    var tenantId = User.FindFirst("TenantId")?.Value;
                    var isGlobal = role.Name == "Admin" || role.Name == "User" || role.Name == "SuperAdmin";

                    if (isGlobal)
                    {
                        return Json(new { success = false, message = "System roles cannot be deleted." });
                    }

                    var isTenantRole = !string.IsNullOrEmpty(tenantId) && role.Name.StartsWith(tenantId + "_");
                    if (!isTenantRole)
                    {
                        return Json(new { success = false, message = "Unauthorized to delete this role." });
                    }
                }

                await _rolemanager.DeleteAsync(role);
                return Json(new { success = true, message = "Role deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
