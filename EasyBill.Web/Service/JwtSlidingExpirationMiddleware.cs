using AOneWeb.Service.AuthService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EasyBill.UI.Service
{
    public class JwtSlidingExpirationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _config;

        public JwtSlidingExpirationMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            _config = config;
        }

        public async Task Invoke(HttpContext context, UserManager<ApplicationUsers> userManager, AuthService jwtService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var expClaim = context.User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;

                if (expClaim != null && long.TryParse(expClaim, out var expUnix))
                {
                    var expiry = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                    var remaining = expiry - DateTime.UtcNow;

                    // अगर token expire होने में 5 मिनट से कम बचे हैं
                    if (remaining.TotalMinutes < 5)
                    {
                        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (string.IsNullOrWhiteSpace(userId))
                        {
                            await _next(context);
                            return;
                        }

                        var user = await userManager.FindByIdAsync(userId);
                        if (user != null)
                        {
                            var role = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";
                            var newToken = jwtService.GenerateJwtToken(user, role);

                            // Send new token in response header
                            context.Response.OnStarting(() =>
                            {
                                context.Response.Headers["X-New-JWT-Token"] = newToken.Token;
                                context.Response.Headers["Access-Control-Expose-Headers"] = "X-New-JWT-Token";
                                return Task.CompletedTask;
                            });
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
