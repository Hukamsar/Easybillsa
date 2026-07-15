using AOne.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EasyBill.UI.Helpers
{
    public class AuditLogFilter : IAsyncActionFilter
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUsers> _userManager;

        public AuditLogFilter(IUnitOfWork unitOfWork, UserManager<ApplicationUsers> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            // Store in Items so ApplicationDbContext can access it without routing dependencies
            httpContext.Items["ControllerName"] = controllerName;
            httpContext.Items["ActionName"] = actionName;

            var resultContext = await next();

            if (resultContext.Exception == null && !resultContext.Canceled)
            {
                var method = httpContext.Request.Method;

                // Log only Login and Logout POST requests in this filter (normal CRUD is logged by DbContext)
                if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(controllerName, "Account", StringComparison.OrdinalIgnoreCase))
                {
                    string? userId = null;
                    string? username = null;
                    string? tenantId = null;
                    string? description = null;

                    if (string.Equals(actionName, "Login", StringComparison.OrdinalIgnoreCase))
                    {
                        if (context.ActionArguments.TryGetValue("model", out var modelObj) && modelObj is LoginViewModels loginModel)
                        {
                            username = loginModel.Email ?? loginModel.MobileNumber;
                        }

                        if (!string.IsNullOrEmpty(username))
                        {
                            var userObj = await _userManager.FindByEmailAsync(username) ?? await _userManager.FindByNameAsync(username);
                            if (userObj != null)
                            {
                                userId = userObj.Id;
                                tenantId = userObj.TenantId;
                                username = userObj.UserName;
                            }
                        }
                        description = "Logged in successfully";
                    }
                    else if (string.Equals(actionName, "Logout", StringComparison.OrdinalIgnoreCase))
                    {
                        var user = httpContext.User;
                        if (user?.Identity?.IsAuthenticated == true)
                        {
                            userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                            username = user.Identity.Name;
                            tenantId = user.FindFirst("TenantId")?.Value;
                        }
                        description = "Logged out successfully";
                    }

                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(description))
                    {
                        try
                        {
                            var auditRepo = _unitOfWork.GetRepository<AuditLog>();
                            var auditLog = new AuditLog
                            {
                                UserId = userId,
                                Username = username,
                                Action = actionName,
                                ControllerName = controllerName,
                                ActionName = actionName,
                                Description = description,
                                Timestamp = DateTime.Now,
                                TenantId = tenantId
                            };
                            auditRepo.Add(auditLog);
                            await _unitOfWork.SaveAsync();
                        }
                        catch (Exception)
                        {
                            // Ignore error to avoid blocking the login/logout pipeline
                        }
                    }
                }
            }
        }
    }
}
