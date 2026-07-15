using AOne.DataAccess.ProfileService;
using AOne.Models;
using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EasyBill.UI.Helpers
{
    public class PlanAccessFilter : IAsyncActionFilter
    {
        private readonly UserManager<ApplicationUsers> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public PlanAccessFilter(UserManager<ApplicationUsers> userManager, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var isSuperAdmin = user.IsInRole("SuperAdmin");
                if (!isSuperAdmin)
                {
                    var controllerName = context.RouteData.Values["controller"]?.ToString();
                    if (!string.IsNullOrEmpty(controllerName))
                    {
                        var reqFeature = GetRequiredFeatureForController(controllerName);
                        if (!string.IsNullOrEmpty(reqFeature))
                        {
                            var tenantId = user.FindFirst("TenantId")?.Value;
                            if (!string.IsNullOrEmpty(tenantId))
                            {
                                var allowedFeatures = await GetAllowedFeaturesAsync(tenantId);
                                if (!allowedFeatures.Contains(reqFeature))
                                {
                                    // Check if it is an API or AJAX request
                                    var isApiOrAjax = context.HttpContext.Request.Path.Value?.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) == true ||
                                                      context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

                                    if (isApiOrAjax)
                                    {
                                        context.Result = new JsonResult(new { success = false, message = "Access denied: This feature is not included in your subscription plan." }) { StatusCode = 403 };
                                    }
                                    else
                                    {
                                        context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                                    }
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            await next();
        }

        private async Task<HashSet<string>> GetAllowedFeaturesAsync(string tenantId)
        {
            var allowedFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tenant = await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant != null)
            {
                allowedFeatures.Add("Master");
                allowedFeatures.Add("Master.Settings");
                allowedFeatures.Add("Master.Staff");
                allowedFeatures.Add("Books");
                allowedFeatures.Add("Contra");

                bool hasParent = !string.IsNullOrEmpty(tenant.ParentTenantId);
                HashSet<string> parentAllowedFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (hasParent)
                {
                    var parentTenant = await _dbContext.Tenants
                        .FirstOrDefaultAsync(t => t.Id == tenant.ParentTenantId);
                    if (parentTenant != null)
                    {
                        if (!string.IsNullOrEmpty(parentTenant.AllowedModulesJson))
                        {
                            try
                            {
                                var parentKeys = JsonConvert.DeserializeObject<List<string>>(parentTenant.AllowedModulesJson);
                                if (parentKeys != null)
                                {
                                    foreach (var pk in parentKeys) parentAllowedFeatures.Add(pk);
                                }
                            }
                            catch { }
                        }
                        else if (parentTenant.SubscriptionPlanId.HasValue)
                        {
                            var planFeatures = await _dbContext.PlanFeatures
                                .Include(pf => pf.Feature)
                                .Where(pf => pf.PlanId == parentTenant.SubscriptionPlanId.Value && pf.Feature != null && pf.Feature.IsActive)
                                .Select(pf => pf.Feature!.FeatureKey)
                                .ToListAsync();

                            foreach (var pf in planFeatures) parentAllowedFeatures.Add(pf);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(tenant.AllowedModulesJson))
                {
                    try
                    {
                        var keys = JsonConvert.DeserializeObject<List<string>>(tenant.AllowedModulesJson);
                        if (keys != null)
                        {
                            foreach (var k in keys)
                            {
                                if (hasParent)
                                {
                                    if (k.Equals("Master.Area", StringComparison.OrdinalIgnoreCase) ||
                                        k.Equals("Master.Offers", StringComparison.OrdinalIgnoreCase))
                                    {
                                        continue;
                                    }

                                    if (parentAllowedFeatures.Contains(k))
                                    {
                                        allowedFeatures.Add(k);
                                    }
                                }
                                else
                                {
                                    allowedFeatures.Add(k);
                                }
                            }
                            return allowedFeatures;
                        }
                    }
                    catch { }
                }

                if (hasParent)
                {
                    foreach (var pk in parentAllowedFeatures)
                    {
                        if (pk.Equals("Master.Area", StringComparison.OrdinalIgnoreCase) ||
                            pk.Equals("Master.Offers", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        allowedFeatures.Add(pk);
                    }
                    return allowedFeatures;
                }

                if (tenant.SubscriptionPlanId.HasValue)
                {
                    var planFeatures = await _dbContext.PlanFeatures
                        .Include(pf => pf.Feature)
                        .Where(pf => pf.PlanId == tenant.SubscriptionPlanId.Value && pf.Feature != null && pf.Feature.IsActive)
                        .Select(pf => pf.Feature!.FeatureKey)
                        .ToListAsync();

                    foreach (var pf in planFeatures)
                    {
                        allowedFeatures.Add(pf);
                    }
                }
            }

            return allowedFeatures;
        }

        private string GetRequiredFeatureForController(string controllerName)
        {
            if (string.IsNullOrEmpty(controllerName)) return string.Empty;

            // Normalize
            controllerName = controllerName.ToLower();

            switch (controllerName)
            {
                // Master Item Master
                case "itemmaster":
                case "categorymaster":
                case "subcategory":
                case "company":
                case "hsn":
                case "division":
                    return "Master.ItemMaster";

                // Master Ledger Master
                case "customer":
                case "supplier":
                case "accountgroup":
                    return "Master.Ledger";

                // Master Area Master
                case "country":
                case "state":
                case "city":
                case "currency":
                    return "Master.Area";

                // Master Staff Master
                case "employee":
                case "department":
                case "designation":
                    return "Master.Staff";

                // Master Offers
                case "offer":
                    return "Master.Offers";

                // Master Payment Mode
                case "paymentmode":
                    return "Master.PaymentMode";

                // Master Opening Stock
                case "openingstock":
                    return "Master.OpeningStock";

                // Master Settings
                case "user":
                case "tenant":
                case "termconditions":
                case "companyplan":
                    return "Master.Settings";

                // Sales
                case "sales":
                    return "Sales";

                // Sales Order
                case "salesorder":
                    return "Sales.Order";

                // Sales Return
                case "stockreturn":
                    return "Sales.Return";

                // Sales Stock Issue
                case "stockissue":
                    return "Sales.StockIssue";

                // Purchase Entry
                case "purchase":
                    return "Purchase";

                // Purchase Return
                case "purchasereturn":
                    return "Purchase.Return";

                // Purchase Order
                case "purchaseorder":
                    return "PurchaseOrder";

                // Purchase Challan
                case "purchasechallan":
                    return "Purchase.Challan";

                // Purchase Stock Receive
                case "stockreceive":
                    return "Purchase.StockReceive";

                // Payment Voucher Category
                case "paymentvouchercategory":
                    return "PaymentVoucher.Category";

                // Payment Voucher
                case "paymentvoucher":
                    return "PaymentVoucher.Entry";

                // Receive Voucher
                case "receivevoucher":
                    return "ReceiveVoucher";

                // Reports
                case "report":
                    return "Reports";

                // Wallet
                case "wallet":
                    return "Wallet";

                // Global Settings
                case "globalsettings":
                    return "GlobalSettings";

                // Books
                case "cashbook":
                case "bankbook":
                case "daybook":
                    return "Books";

                // Contra
                case "contra":
                    return "Contra";

                default:
                    return string.Empty;
            }
        }
    }
}
