using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models
{
    public static class UserExtensions
    {
        public static bool HasPermission(this ClaimsPrincipal user, string permission)
        {
            if (user.IsInRole("SuperAdmin"))
                return true;
            return user.HasClaim("Permission", permission);
        }
    }
}
