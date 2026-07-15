using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using EasyBill.Models.Entity;

namespace EasyBill.UI.Filters
{
    public class HeadOfficeOnlyAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;

            // 1. Ultimate Platform SuperAdmin can do anything (The Software Provider)
            if (httpContext.User?.IsInRole("SuperAdmin") == true)
            {
                await next();
                return;
            }

            var tenantRepository = httpContext.RequestServices.GetRequiredService<ITenantRegistrationRepository>();
            var tenantId = httpContext.User?.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var tenant = await tenantRepository.GetById(tenantId);
            
            // 2. If it's a branch (ParentTenantId is NOT null), block access!
            // Even if the Supreme Owner logs into a branch, they act as an Admin inside the branch and cannot create items here.
            if (tenant != null && !string.IsNullOrEmpty(tenant.ParentTenantId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 3. We are in HO or Individual Company context (ParentTenantId == null). 
            // Now, we MUST verify if the current user is the Supreme "Owner" of this tenant.
            var userManager = httpContext.RequestServices.GetRequiredService<UserManager<ApplicationUsers>>();
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId) || tenant == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            bool isOwner = false;
            // The Owner is the one whose Email or Phone matches the Tenant's registered Email/Phone.
            if ((!string.IsNullOrEmpty(user.Email) && user.Email.Equals(tenant.Email, System.StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(user.PhoneNumber) && (user.PhoneNumber == tenant.MobileNo || user.PhoneNumber == tenant.Phone)))
            {
                isOwner = true;
            }

            // If they are not the Supreme Owner (e.g. an Ops Head created by the owner), they cannot modify master data.
            if (!isOwner)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();
        }
    }
}
