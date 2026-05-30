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
            var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            var user = _httpContextAccessor?.HttpContext?.User;
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
