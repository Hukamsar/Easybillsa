using AOne.DataAccess.ProfileService;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace EasyBill.UI.Service
{
    public class UserProfileMiddleware
    {
        private readonly RequestDelegate _next;

        public UserProfileMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IProfileService profileService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                await profileService.Set(context.User);
            }
            await _next(context);
        }
    }
}
