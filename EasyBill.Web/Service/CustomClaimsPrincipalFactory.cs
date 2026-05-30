using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace EasyBill.UI.Service
{
    public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUsers, IdentityRole>
    {
        private readonly UserManager<ApplicationUsers> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CustomClaimsPrincipalFactory(
            UserManager<ApplicationUsers> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUsers user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            // Add base claims
            var roles = await _userManager.GetRolesAsync(user);
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
            identity.AddClaim(new Claim("TenantId", user.TenantId ?? ""));
            //identity.AddClaim(new Claim("TenantName", user.TenantName ?? ""));

            return identity;
        }
    }
}
