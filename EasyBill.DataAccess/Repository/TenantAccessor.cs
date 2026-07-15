using AOne.Models;
using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public class TenantAccessor : ITenantAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        public TenantAccessor(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        public string? GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            
            // Get user's actual tenant from claim
            var claimTenantId = user?.FindFirst("TenantId")?.Value;
            var isSuperAdmin = user?.IsInRole("SuperAdmin") ?? false;
            var isCustomer = (user?.IsInRole("Customer") ?? false) || 
                             (user?.HasClaim(c => c.Type == "CustomerId") ?? false) ||
                             (user?.HasClaim(c => c.Type == "role" && c.Value == "Customer") ?? false);
            
            // Check if there is a query string or HttpContext item override
            string? requestedTenantId = null;
            if (httpContext != null)
            {
                if (httpContext.Items.TryGetValue("OverrideTenantId", out var overrideVal) && overrideVal is string oTenantId)
                {
                    requestedTenantId = oTenantId;
                }
                else if (httpContext.Request.Query.TryGetValue("tenantId", out var qTenantId) && !string.IsNullOrEmpty(qTenantId))
                {
                    requestedTenantId = qTenantId;
                }
            }
            
            if (!string.IsNullOrEmpty(requestedTenantId))
            {
                // If the user is SuperAdmin or Customer, allow the override.
                // If not, they can only request their own tenant ID.
                if (isSuperAdmin || isCustomer || requestedTenantId == claimTenantId)
                {
                    return requestedTenantId;
                }
            }

            var tenantId = claimTenantId;
            var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUsers>>();
            if (string.IsNullOrEmpty(tenantId) && user != null)
            {
                var applicationUser = userManager?.GetUserAsync(user).GetAwaiter().GetResult();
                tenantId = applicationUser?.TenantId;
            }
            return tenantId;
        }
    }
}
