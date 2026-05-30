using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AOne.DataAccess.ProfileService
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetEmail(this ClaimsPrincipal claimsPrincipal)
            => claimsPrincipal.FindFirstValue(ClaimTypes.Email);


        public static string GetPhoneNumber(this ClaimsPrincipal claimsPrincipal)
            => claimsPrincipal.FindFirstValue(ClaimTypes.MobilePhone);

        public static string GetUserId(this ClaimsPrincipal claimsPrincipal)
           => claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        public static string GetUserName(this ClaimsPrincipal claimsPrincipal)
           => claimsPrincipal.FindFirstValue(ClaimTypes.Name); 
        public static string[] GetRoles(this ClaimsPrincipal claimsPrincipal)
            => claimsPrincipal.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).ToArray();
        public static void SetDisplayName(this ClaimsPrincipal claimsPrincipal, string displayName)
        {
            var identity = claimsPrincipal.Identity as ClaimsIdentity;
            if (identity is not null)
            {
                var claim = identity.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName);
                if (claim is not null)
                {
                    identity.RemoveClaim(claim);
                }
                identity.AddClaim(new Claim(ClaimTypes.GivenName, displayName));
            }

        }

        public static void SetPhoneNumber(this ClaimsPrincipal claimsPrincipal, string phoneNumber)
        {
            var identity = claimsPrincipal.Identity as ClaimsIdentity;
            if (identity is not null)
            {
                var claim = identity.Claims.FirstOrDefault(x => x.Type == ClaimTypes.MobilePhone);
                if (claim is not null)
                {
                    identity.RemoveClaim(claim);
                }
                identity.AddClaim(new Claim(ClaimTypes.MobilePhone, phoneNumber));
            }
        }

    }
}
