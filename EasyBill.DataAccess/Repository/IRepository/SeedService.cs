using AOne.DataAccess.Data;
using AOne.Models;
using AOne.Models.Entity;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AOneWeb.Service
{
    public class SeedService
    {

        public static async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            using var scope= serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUsers>>();
            var logger=scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();

            try
            {
                // Ensuring the database is ready
                logger.LogInformation("Ensuring the database is creaded.");
                await context.Database.EnsureCreatedAsync();

                // Add roles
                logger.LogInformation("Seeding roles.");
                await AddRoleAsync(roleManager, "SuperAdmin");
                await AddRoleAsync(roleManager, "Admin");
                await AddRoleAsync(roleManager, "User");

                // Seed role claims
                logger.LogInformation("Seeding role claims.");
                await SeedRoleClaimsAsync(roleManager, "SuperAdmin");
                await SeedRoleClaimsAsync(roleManager, "Admin");

                //Add admin User
                logger.LogInformation("Sedding admin user.");
                var adminEmail = "admin@gmail.com";
                if (await userManager.FindByEmailAsync(adminEmail)==null)
                {
                    var adminUser = new ApplicationUsers
                    {
                       // EmployeeId = 3,
                        UserName = adminEmail,
                        NormalizedUserName = adminEmail.ToUpper(),
                        Email = adminEmail,
                        NormalizedEmail = adminEmail.ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        TenantId = context.Tenants.First().Id,
                        TenantName = context.Tenants.First().Name,
                    };

                    var result=await userManager.CreateAsync(adminUser,"Admin@123" );
                    if(result.Succeeded)
                    {
                        logger.LogInformation("Assigning Admin role to the adin user.");
                        await userManager.AddToRoleAsync(adminUser, "SuperAdmin");

                    }
                    else
                    {
                        logger.LogError("Failed to create admin user:{Errors} ", string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                     
                }
            }
            catch (Exception ex) {

                logger.LogError(ex, "An error occurred while seeding the database");
            }

        }
        public static async Task SeedSampleDataAsync(ApplicationDbContext context)
        {
            if (!context.Tenants.Any())
            {
                context.Tenants.Add(new Tenant() { Name = "Master", Email= "admin@gmail.com", Description = "Master Site" });
                await context.SaveChangesAsync();
            }

            await SeedFeaturesAsync(context);
        }

        public static async Task SeedFeaturesAsync(ApplicationDbContext context)
        {
            if (await context.Features.AnyAsync()) return;

            var features = new List<Feature>
            {
                new Feature { FeatureKey = "Master", DisplayName = "Master", IsActive = true },
                new Feature { FeatureKey = "Sales", DisplayName = "Sales", IsActive = true },
                new Feature { FeatureKey = "Purchase", DisplayName = "Purchase", IsActive = true },
                new Feature { FeatureKey = "PurchaseOrder", DisplayName = "Purchase Order", IsActive = true },
                new Feature { FeatureKey = "PaymentVoucher", DisplayName = "Payment Voucher", IsActive = true },
                new Feature { FeatureKey = "ReceiveVoucher", DisplayName = "Receive Voucher", IsActive = true },
                new Feature { FeatureKey = "Reports", DisplayName = "Reports", IsActive = true },
                new Feature { FeatureKey = "Inventory", DisplayName = "Inventory Report", IsActive = true },
                new Feature { FeatureKey = "GSTR", DisplayName = "GST Report", IsActive = true },
                new Feature { FeatureKey = "GlobalSettings", DisplayName = "Global Settings", IsActive = true },
                new Feature { FeatureKey = "Wallet", DisplayName = "Wallet", IsActive = true }
            };

            context.Features.AddRange(features);
            await context.SaveChangesAsync();

            var featureDict = await context.Features.ToDictionaryAsync(f => f.FeatureKey);

            var children = new List<Feature>
            {
                new Feature { FeatureKey = "Master.ItemMaster", DisplayName = "Item Master", ParentFeatureId = featureDict["Master"].Id, IsActive = true },
                new Feature { FeatureKey = "Master.Ledger", DisplayName = "Ledger Master", ParentFeatureId = featureDict["Master"].Id, IsActive = true },
                new Feature { FeatureKey = "Master.Area", DisplayName = "Area Master", ParentFeatureId = featureDict["Master"].Id, IsActive = true },
                new Feature { FeatureKey = "Master.Staff", DisplayName = "Staff Master", ParentFeatureId = featureDict["Master"].Id, IsActive = true },
                new Feature { FeatureKey = "Master.Offers", DisplayName = "Offers", ParentFeatureId = featureDict["Master"].Id, IsActive = true },
                new Feature { FeatureKey = "Master.PaymentMode", DisplayName = "Mode Of Payment", ParentFeatureId = featureDict["Master"].Id, IsActive = true },
                new Feature { FeatureKey = "Master.OpeningStock", DisplayName = "Opening Stock", ParentFeatureId = featureDict["Master"].Id, IsActive = true },
                new Feature { FeatureKey = "Master.Settings", DisplayName = "Settings", ParentFeatureId = featureDict["Master"].Id, IsActive = true },

                new Feature { FeatureKey = "Sales.Create", DisplayName = "Sales Bill", ParentFeatureId = featureDict["Sales"].Id, IsActive = true },
                new Feature { FeatureKey = "Sales.Index", DisplayName = "Edit Sales", ParentFeatureId = featureDict["Sales"].Id, IsActive = true },
                new Feature { FeatureKey = "Sales.HoldList", DisplayName = "Sales Hold List", ParentFeatureId = featureDict["Sales"].Id, IsActive = true },
                new Feature { FeatureKey = "Sales.Order", DisplayName = "Sales Order", ParentFeatureId = featureDict["Sales"].Id, IsActive = true },
                new Feature { FeatureKey = "Sales.Return", DisplayName = "Sales Return(C/N)", ParentFeatureId = featureDict["Sales"].Id, IsActive = true },
                new Feature { FeatureKey = "Sales.StockIssue", DisplayName = "Stock Issue", ParentFeatureId = featureDict["Sales"].Id, IsActive = true },

                new Feature { FeatureKey = "Purchase.Entry", DisplayName = "Purchase Entry", ParentFeatureId = featureDict["Purchase"].Id, IsActive = true },
                new Feature { FeatureKey = "Purchase.Return", DisplayName = "Purchase Return(D/N)", ParentFeatureId = featureDict["Purchase"].Id, IsActive = true },
                new Feature { FeatureKey = "Purchase.StockReceive", DisplayName = "Stock Receive", ParentFeatureId = featureDict["Purchase"].Id, IsActive = true },
                new Feature { FeatureKey = "Purchase.Challan", DisplayName = "Purchase Challan", ParentFeatureId = featureDict["Purchase"].Id, IsActive = true },

                new Feature { FeatureKey = "PurchaseOrder.Manual", DisplayName = "Manual PO", ParentFeatureId = featureDict["PurchaseOrder"].Id, IsActive = true },
                new Feature { FeatureKey = "PurchaseOrder.AI", DisplayName = "AI PO", ParentFeatureId = featureDict["PurchaseOrder"].Id, IsActive = true },

                new Feature { FeatureKey = "PaymentVoucher.Entry", DisplayName = "Voucher Entry", ParentFeatureId = featureDict["PaymentVoucher"].Id, IsActive = true },
                new Feature { FeatureKey = "PaymentVoucher.Category", DisplayName = "Voucher Category", ParentFeatureId = featureDict["PaymentVoucher"].Id, IsActive = true },

                new Feature { FeatureKey = "Reports.Sales", DisplayName = "Sales Reports", ParentFeatureId = featureDict["Reports"].Id, IsActive = true },
                new Feature { FeatureKey = "Reports.SalesReturn", DisplayName = "Sales Return Reports", ParentFeatureId = featureDict["Reports"].Id, IsActive = true },
                new Feature { FeatureKey = "Reports.StockIssue", DisplayName = "Stock Issue Reports", ParentFeatureId = featureDict["Reports"].Id, IsActive = true },
                new Feature { FeatureKey = "Reports.StockReceive", DisplayName = "Stock Receive Reports", ParentFeatureId = featureDict["Reports"].Id, IsActive = true },
                new Feature { FeatureKey = "Reports.Purchase", DisplayName = "Purchase Reports", ParentFeatureId = featureDict["Reports"].Id, IsActive = true },
                new Feature { FeatureKey = "Reports.PurchaseReturn", DisplayName = "Purchase Return Reports", ParentFeatureId = featureDict["Reports"].Id, IsActive = true },

                new Feature { FeatureKey = "Inventory.CurrentStock", DisplayName = "Current Stock", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },
                new Feature { FeatureKey = "Inventory.NearExpiry", DisplayName = "Near Expiry", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },
                new Feature { FeatureKey = "Inventory.Expired", DisplayName = "Expired Item", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },
                new Feature { FeatureKey = "Inventory.MinimumStock", DisplayName = "Minimum Stock", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },
                new Feature { FeatureKey = "Inventory.DumpStock", DisplayName = "Dump Stock", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },
                new Feature { FeatureKey = "Inventory.FastMoving", DisplayName = "Fast Moving Items", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },
                new Feature { FeatureKey = "Inventory.SlowMoving", DisplayName = "Slow Moving Items", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },
                new Feature { FeatureKey = "Inventory.SupplierWise", DisplayName = "Supplier Wise Stock", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },
                new Feature { FeatureKey = "Inventory.CompanyItemWise", DisplayName = "Company ItemWise Stock", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },
                new Feature { FeatureKey = "Inventory.OverStock", DisplayName = "OverStock Report", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },
                new Feature { FeatureKey = "Inventory.BatchWise", DisplayName = "Batch Wise Stock", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },
                new Feature { FeatureKey = "Inventory.StockSales", DisplayName = "Stock & Sales", ParentFeatureId = featureDict["Inventory"].Id, IsActive = true },

                new Feature { FeatureKey = "GSTR.GSTR1", DisplayName = "GSTR1", ParentFeatureId = featureDict["GSTR"].Id, IsActive = true },
                new Feature { FeatureKey = "GSTR.GSTR2", DisplayName = "GSTR2", ParentFeatureId = featureDict["GSTR"].Id, IsActive = true },
                new Feature { FeatureKey = "GSTR.GSTR3", DisplayName = "GSTR3", ParentFeatureId = featureDict["GSTR"].Id, IsActive = true }
            };

            context.Features.AddRange(children);
            await context.SaveChangesAsync();

            var allFeatureList = await context.Features.ToListAsync();

            // 2. Smart Plan
            var smartPlan = await context.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanName == "Smart Plan");
            if (smartPlan == null)
            {
                smartPlan = new SubscriptionPlan
                {
                    PlanName = "Smart Plan",
                    MonthlyPrice = 300,
                    YearlyPrice = 3000,
                    DailyCustomerLimit = 100,
                    MaxDesktopLogins = 1,
                    MaxMobileLogins = 2,
                    IsActive = true
                };
                context.SubscriptionPlans.Add(smartPlan);
                await context.SaveChangesAsync();

                var smartKeys = new HashSet<string> { "Master", "Master.Settings", "Master.Staff", "Sales", "Sales.Create", "Sales.Index", "Sales.HoldList", "Sales.Order", "Sales.Return", "Sales.StockIssue", "Reports", "Reports.Sales", "Reports.SalesReturn", "Reports.StockIssue", "Reports.StockReceive", "Wallet" };
                foreach (var f in allFeatureList.Where(f => smartKeys.Contains(f.FeatureKey)))
                {
                    context.PlanFeatures.Add(new PlanFeature { PlanId = smartPlan.Id, FeatureId = f.Id });
                }
                await context.SaveChangesAsync();
            }

            // 3. Prime Plan
            var primePlan = await context.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanName == "Prime Plan");
            if (primePlan == null)
            {
                primePlan = new SubscriptionPlan
                {
                    PlanName = "Prime Plan",
                    MonthlyPrice = 600,
                    YearlyPrice = 6000,
                    DailyCustomerLimit = 500,
                    MaxDesktopLogins = 3,
                    MaxMobileLogins = 5,
                    IsActive = true
                };
                context.SubscriptionPlans.Add(primePlan);
                await context.SaveChangesAsync();

                // All features except GSTR and PO.AI
                foreach (var f in allFeatureList.Where(f => !f.FeatureKey.StartsWith("GSTR") && f.FeatureKey != "PurchaseOrder.AI"))
                {
                    context.PlanFeatures.Add(new PlanFeature { PlanId = primePlan.Id, FeatureId = f.Id });
                }
                await context.SaveChangesAsync();
            }

            // 4. Suprime Plan
            var suprimePlan = await context.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanName == "Suprime Plan");
            if (suprimePlan == null)
            {
                suprimePlan = new SubscriptionPlan
                {
                    PlanName = "Suprime Plan",
                    MonthlyPrice = 1200,
                    YearlyPrice = 12000,
                    DailyCustomerLimit = 5000,
                    MaxDesktopLogins = 10,
                    MaxMobileLogins = 20,
                    IsActive = true
                };
                context.SubscriptionPlans.Add(suprimePlan);
                await context.SaveChangesAsync();

                // All features
                foreach (var f in allFeatureList)
                {
                    context.PlanFeatures.Add(new PlanFeature { PlanId = suprimePlan.Id, FeatureId = f.Id });
                }
                await context.SaveChangesAsync();
            }

            // 5. Mobile Only plan
            var mobilePlan = await context.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanName == "Mobile Only plan");
            if (mobilePlan == null)
            {
                mobilePlan = new SubscriptionPlan
                {
                    PlanName = "Mobile Only plan",
                    MonthlyPrice = 150,
                    YearlyPrice = 1500,
                    DailyCustomerLimit = 25,
                    MaxDesktopLogins = 0,
                    MaxMobileLogins = 2,
                    IsActive = true
                };
                context.SubscriptionPlans.Add(mobilePlan);
                await context.SaveChangesAsync();

                var mobileKeys = new HashSet<string> { "Master", "Master.Settings", "Master.Staff", "Sales", "Sales.Create", "Sales.Return", "Reports", "Reports.Sales", "Wallet" };
                foreach (var f in allFeatureList.Where(f => mobileKeys.Contains(f.FeatureKey)))
                {
                    context.PlanFeatures.Add(new PlanFeature { PlanId = mobilePlan.Id, FeatureId = f.Id });
                }
                await context.SaveChangesAsync();
            }
        }

        private static async Task AddRoleAsync(RoleManager<IdentityRole> roleManager, string roleName )
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create role '{roleName}':{string.Join(", ",result.Errors.Select(e=>e.Description) )} " );
                }
            }
        }
        private static async Task SeedRoleClaimsAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var existingClaims = await roleManager.GetClaimsAsync(role);
                var existingClaimValues = new HashSet<string>(existingClaims.Select(c => c.Value));

                var permissions = typeof(AOne.Utility.Permissions).GetNestedTypes();
                foreach (var module in permissions)
                {
                    var fields = module.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
                    foreach (var fi in fields)
                    {
                        var propertyValue = fi.GetValue(null);
                        if (propertyValue is not null)
                        {
                            var claimValue = propertyValue.ToString();
                            if (!string.IsNullOrEmpty(claimValue) && !existingClaimValues.Contains(claimValue))
                            {
                                await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", claimValue));
                            }
                        }
                    }
                }
            }
        }
    }
}
