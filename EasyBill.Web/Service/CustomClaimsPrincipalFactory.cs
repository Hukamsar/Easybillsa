using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using AOne.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace EasyBill.UI.Service
{
    public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUsers, IdentityRole>
    {
        private readonly UserManager<ApplicationUsers> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IServiceProvider _serviceProvider;

        public CustomClaimsPrincipalFactory(
            UserManager<ApplicationUsers> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor,
            IServiceProvider serviceProvider)
            : base(userManager, roleManager, optionsAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _serviceProvider = serviceProvider;
        }
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUsers user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            // Add base claims
            var roles = await _userManager.GetRolesAsync(user);

            // Self-healing: if the user logs in and matches the Tenant's email or mobile, ensure they are Admin
            if (roles.Count == 0 && !string.IsNullOrEmpty(user.TenantId))
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId);
                    if (tenant != null)
                    {
                        if ((!string.IsNullOrEmpty(user.Email) && user.Email.Equals(tenant.Email, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(user.PhoneNumber) && (user.PhoneNumber == tenant.MobileNo || user.PhoneNumber == tenant.Phone)))
                        {
                            var roleExists = await _roleManager.RoleExistsAsync("Admin");
                            if (roleExists)
                            {
                                await _userManager.AddToRoleAsync(user, "Admin");
                                roles.Add("Admin");
                            }
                        }
                    }
                }
            }

            // Self-healing for HO Users: If they have AllowedBranches, dynamically treat them as Admin
            if (roles.Count == 0 && !string.IsNullOrEmpty(user.AllowedBranches))
            {
                roles.Add("Admin");
            }

            foreach (var roleName in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));

                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    foreach (var claim in roleClaims)
                    {
                        identity.AddClaim(claim);
                    }
                }
            }

            // Add custom claims
            var finalTenantId = !string.IsNullOrEmpty(user.SelectedTenantIdForLogin) ? user.SelectedTenantIdForLogin : (user.TenantId ?? "");
            identity.AddClaim(new Claim("TenantId", finalTenantId));
            //identity.AddClaim(new Claim("TenantName", user.TenantName ?? ""));

            return identity;
        }
    }
}
