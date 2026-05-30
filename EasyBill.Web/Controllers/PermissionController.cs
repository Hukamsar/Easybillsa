using AOne.DataAccess.ProfileService;
using AOne.DataAccess.Data;
using AOne.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AOneWeb.Controllers
{
    [Authorize]
    public class PermissionController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IProfileService _profileservice;
        private readonly SignInManager<ApplicationUsers> _SignInManager; 
        private readonly UserManager<ApplicationUsers> _Usermanager;
        private readonly ApplicationDbContext _dbContext;
        private IList<Claim> _assignedClaims = default!;

        public PermissionController(
            RoleManager<IdentityRole> roleManager, 
            IProfileService profileservice, 
            SignInManager<ApplicationUsers> signInManager, 
            UserManager<ApplicationUsers> Usermanager,
            ApplicationDbContext dbContext)
        {
            _roleManager = roleManager;
            _profileservice = profileservice;
            _SignInManager = signInManager;
            _Usermanager = Usermanager;
            _dbContext = dbContext;
        }
        public async Task<IActionResult> AssignPermissions(string roleId)
        {
            var signrole = _SignInManager.IsSignedIn(User);
            var user = await _Usermanager.GetUserAsync(User);
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound();
            }

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var allowedFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            if (!isSuperAdmin)
            {
                var tenantId = _profileservice.Profile?.TenantId;
                if (!string.IsNullOrEmpty(tenantId))
                {
                    allowedFeatures = await GetAllowedFeaturesAsync(tenantId);
                }

                var isGlobal = role.Name == "Admin" || role.Name == "User" || role.Name == "SuperAdmin";
                var isTenantRole = !string.IsNullOrEmpty(tenantId) && role.Name.StartsWith(tenantId + "_");

                if (!isGlobal && !isTenantRole)
                {
                    return NotFound();
                }

                if (isGlobal)
                {
                    ViewBag.IsReadOnly = true;
                }
            }

            var assignedClaims = await _roleManager.GetClaimsAsync(role);
            var assignedPermissions = assignedClaims.Select(c => c.Value).ToList();

            var allPermissions = new List<PermissionModel>();
            var modules = typeof(Permissions).GetNestedTypes();

            foreach (var module in modules)
            {
                var moduleName = module.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? string.Empty;
                var moduleDescription = module.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

                var fields = module.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                foreach (var fi in fields)
                {
                    var propertyValue = fi.GetValue(null);
                    if (propertyValue is not null)
                    {
                        var claimValue = propertyValue.ToString();

                        // Check if permission is allowed based on plan
                        if (!isSuperAdmin)
                        {
                            var reqFeature = GetFeatureKeyForPermission(claimValue);
                            if (!string.IsNullOrEmpty(reqFeature) && !allowedFeatures.Contains(reqFeature))
                            {
                                continue; // Skip this permission
                            }
                        }

                        allPermissions.Add(
                            new PermissionModel
                            {
                                RoleId = role.Id,
                                ClaimValue = claimValue,
                                ClaimType = "Permission",
                                Group = moduleName,
                                Description = moduleDescription,
                                Assigned = assignedPermissions.Contains(claimValue) 
                            });
                    }
                }
            }

            return View(allPermissions);
        }

        [HttpPost]
        public async Task<IActionResult> SavePermissions([FromBody] PermissionRequest request)
        {
            if (request == null || request.UpdatedPermissions == null || request.UpdatedPermissions.Count == 0)
            {
                return BadRequest("No permissions received.");
            }

            var role = await _roleManager.FindByIdAsync(request.RoleId);
            if (role == null)
            {
                return NotFound("Role not found.");
            }

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var allowedFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            if (!isSuperAdmin)
            {
                var tenantId = _profileservice.Profile?.TenantId;
                if (!string.IsNullOrEmpty(tenantId))
                {
                    allowedFeatures = await GetAllowedFeaturesAsync(tenantId);
                }

                var isGlobal = role.Name == "Admin" || role.Name == "User" || role.Name == "SuperAdmin";
                if (isGlobal)
                {
                    return BadRequest("System roles cannot be modified.");
                }

                var isTenantRole = !string.IsNullOrEmpty(tenantId) && role.Name.StartsWith(tenantId + "_");
                if (!isTenantRole)
                {
                    return BadRequest("Unauthorized to modify this role.");
                }

                // Strict validation: Reject any attempt to save an unauthorized permission
                foreach (var permission in request.UpdatedPermissions)
                {
                    if (permission.Assigned)
                    {
                        var reqFeature = GetFeatureKeyForPermission(permission.ClaimValue);
                        if (!string.IsNullOrEmpty(reqFeature) && !allowedFeatures.Contains(reqFeature))
                        {
                            return BadRequest($"Unauthorized permission assignment for {permission.ClaimValue}. This feature is not included in your subscription plan.");
                        }
                    }
                }
            }

            var existingClaims = await _roleManager.GetClaimsAsync(role);

            foreach (var permission in request.UpdatedPermissions)
            {
                // Skip if not super admin and the feature is not allowed in plan
                if (!isSuperAdmin)
                {
                    var reqFeature = GetFeatureKeyForPermission(permission.ClaimValue);
                    if (!string.IsNullOrEmpty(reqFeature) && !allowedFeatures.Contains(reqFeature))
                    {
                        continue;
                    }
                }

                var claim = existingClaims.FirstOrDefault(c => c.Value == permission.ClaimValue);

                if (permission.Assigned)
                {
                    if (claim == null)
                    {
                        await _roleManager.AddClaimAsync(role, new Claim("Permission", permission.ClaimValue));
                    }
                }
                else
                {
                    if (claim != null)
                    {
                        await _roleManager.RemoveClaimAsync(role, claim);
                    }
                }
            }

            return Json(new { message = "Permissions updated successfully!" });
        }

        private async Task<HashSet<string>> GetAllowedFeaturesAsync(string tenantId)
        {
            var allowedFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant != null)
            {
                allowedFeatures.Add("Master");
                allowedFeatures.Add("Master.Settings");
                allowedFeatures.Add("Master.Staff");

                if (!string.IsNullOrEmpty(tenant.AllowedModulesJson))
                {
                    try
                    {
                        var keys = JsonConvert.DeserializeObject<List<string>>(tenant.AllowedModulesJson);
                        if (keys != null)
                        {
                            foreach (var k in keys) allowedFeatures.Add(k);
                            return allowedFeatures;
                        }
                    }
                    catch { }
                }

                if (tenant.SubscriptionPlanId.HasValue)
                {
                    var planFeatures = await _dbContext.PlanFeatures
                        .Include(pf => pf.Feature)
                        .Where(pf => pf.PlanId == tenant.SubscriptionPlanId.Value && pf.Feature != null && pf.Feature.IsActive)
                        .Select(pf => pf.Feature!.FeatureKey)
                        .ToListAsync();

                    foreach (var pf in planFeatures)
                    {
                        allowedFeatures.Add(pf);
                    }
                }
            }

            return allowedFeatures;
        }

        private string GetFeatureKeyForPermission(string claimValue)
        {
            if (string.IsNullOrEmpty(claimValue)) return string.Empty;

            if (claimValue.StartsWith("Permissions.ItemMaster.") ||
                claimValue.StartsWith("Permissions.CategoryMaster.") ||
                claimValue.StartsWith("Permissions.SubCategory.") ||
                claimValue.StartsWith("Permissions.Company.") ||
                claimValue.StartsWith("Permissions.HSN.") ||
                claimValue.StartsWith("Permissions.Division."))
            {
                return "Master.ItemMaster";
            }
            if (claimValue.StartsWith("Permissions.Customer.") ||
                claimValue.StartsWith("Permissions.Suppliers.") ||
                claimValue.StartsWith("Permissions.AccountGroup."))
            {
                return "Master.Ledger";
            }
            if (claimValue.StartsWith("Permissions.Country.") ||
                claimValue.StartsWith("Permissions.State.") ||
                claimValue.StartsWith("Permissions.City.") ||
                claimValue.StartsWith("Permissions.Currency."))
            {
                return "Master.Area";
            }
            if (claimValue.StartsWith("Permissions.Employee.") ||
                claimValue.StartsWith("Permissions.Department.") ||
                claimValue.StartsWith("Permissions.Designation."))
            {
                return "Master.Staff";
            }
            if (claimValue.StartsWith("Permissions.Offers."))
            {
                return "Master.Offers";
            }
            if (claimValue.StartsWith("Permissions.ModeOfPayment."))
            {
                return "Master.PaymentMode";
            }
            if (claimValue.StartsWith("Permissions.OpeningStock."))
            {
                return "Master.OpeningStock";
            }
            if (claimValue.StartsWith("Permissions.Roles.") ||
                claimValue.StartsWith("Permissions.Users.") ||
                claimValue.StartsWith("Permissions.CompanyRegistration.") ||
                claimValue.StartsWith("Permissions.TermCondition."))
            {
                return "Master.Settings";
            }
            if (claimValue.StartsWith("Permissions.Sales."))
            {
                return "Sales";
            }
            if (claimValue.StartsWith("Permissions.SalesOrder."))
            {
                return "Sales.Order";
            }
            if (claimValue.StartsWith("Permissions.StockReturn."))
            {
                return "Sales.Return";
            }
            if (claimValue.StartsWith("Permissions.StockIssue."))
            {
                return "Sales.StockIssue";
            }
            if (claimValue.StartsWith("Permissions.PurchaseEntry."))
            {
                return "Purchase";
            }
            if (claimValue.StartsWith("Permissions.PurchaseReturn."))
            {
                return "Purchase.Return";
            }
            if (claimValue.StartsWith("Permissions.PurchaseOrder."))
            {
                return "PurchaseOrder";
            }
            if (claimValue.StartsWith("Permissions.PurchaseChallan."))
            {
                return "Purchase.Challan";
            }
            if (claimValue.StartsWith("Permissions.StockReceive."))
            {
                return "Purchase.StockReceive";
            }
            if (claimValue.StartsWith("Permissions.PaymentVoucherCategory."))
            {
                return "PaymentVoucher.Category";
            }
            if (claimValue.StartsWith("Permissions.PaymentVoucher."))
            {
                return "PaymentVoucher.Entry";
            }
            if (claimValue.StartsWith("Permissions.ReceiveVoucher."))
            {
                return "ReceiveVoucher";
            }

            return string.Empty;
        }

        // New DTO for Request
        public class PermissionRequest
        {
            public string RoleId { get; set; }
            public List<PermissionModel> UpdatedPermissions { get; set; }
        }

        private async Task<List<PermissionModel>> GetAllPermissions(IdentityRole role)
        {
            _assignedClaims = await _roleManager.GetClaimsAsync(role);
            var allPermissions = new List<PermissionModel>();
            var modules = typeof(Permissions).GetNestedTypes();
            foreach (var module in modules)
            {
                var moduleName = string.Empty;
                var moduleDescription = string.Empty;
                if (module.GetCustomAttributes(typeof(DisplayNameAttribute), true)
                    .FirstOrDefault() is DisplayNameAttribute displayNameAttribute)
                    moduleName = displayNameAttribute.DisplayName;

                if (module.GetCustomAttributes(typeof(DescriptionAttribute), true)
                    .FirstOrDefault() is DescriptionAttribute descriptionAttribute)
                    moduleDescription = descriptionAttribute.Description;

                var fields = module.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                foreach (var fi in fields)
                {
                    var propertyValue = fi.GetValue(null);

                    if (propertyValue is not null)
                    {
                        var claimValue = propertyValue.ToString();
                        allPermissions.Add(
                            new PermissionModel
                            {
                                RoleId = role.Id,
                                ClaimValue = claimValue,
                                ClaimType = "Permission",
                                Group = moduleName,
                                Description = moduleDescription,
                                Assigned = _assignedClaims.Any(x => x.Value == claimValue)
                            });
                    }
                }
            }
            return allPermissions;
        }
    }
}
