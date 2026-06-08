using AOne.DataAccess.Data;
using AOne.DataAccess.ProfileService;
using AOne.DataAccess.Repository.IRepository;
using AOne.Models;
using AOne.Models.SideMenu;
using AOne.Utility;
using DocumentFormat.OpenXml.Wordprocessing;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AOne.DataAccess.Repository
{
    public class MenuService : IMenuService
    {

        private IList<Claim> _assignedClaims = default!;
        private IProfileService _profileService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IServiceProvider _serviceProvider;
        private List<PermissionModel> allPermissions = new List<PermissionModel>();
        private HashSet<string>? _allowedFeatures = null;

        public MenuService(IProfileService profileService, RoleManager<IdentityRole> roleManager, IServiceProvider serviceProvider)
        {
            _profileService = profileService;
            _roleManager = roleManager;
            _serviceProvider = serviceProvider;
        }

        private async Task LoadAllowedFeaturesAsync()
        {
            _allowedFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tenantId = _profileService.Profile?.TenantId;
            if (string.IsNullOrEmpty(tenantId)) return;

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var tenant = await context.Tenants
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                if (tenant != null)
                {
                    _allowedFeatures.Add("Master");
                    _allowedFeatures.Add("Master.Settings");
                    _allowedFeatures.Add("Master.Staff");

                    if (!string.IsNullOrEmpty(tenant.AllowedModulesJson))
                    {
                        try
                        {
                            var keys = JsonConvert.DeserializeObject<List<string>>(tenant.AllowedModulesJson);
                            if (keys != null)
                            {
                                foreach (var k in keys) _allowedFeatures.Add(k);
                                return;
                            }
                        }
                        catch { }
                    }

                    if (tenant.SubscriptionPlanId.HasValue)
                    {
                        var planFeatures = await context.PlanFeatures
                            .Include(pf => pf.Feature)
                            .Where(pf => pf.PlanId == tenant.SubscriptionPlanId.Value && pf.Feature != null && pf.Feature.IsActive)
                            .Select(pf => pf.Feature!.FeatureKey)
                            .ToListAsync();

                        foreach (var pf in planFeatures)
                        {
                            _allowedFeatures.Add(pf);
                        }
                    }
                }
            }
        }

        private bool IsFeatureAllowed(string key, string role)
        {
            if (IsSuperAdmin(role)) return true;
            return _allowedFeatures != null && _allowedFeatures.Contains(key);
        }

        private async Task<List<PermissionModel>> GetAllPermissions(IdentityRole role)
        {

            _assignedClaims = await _roleManager.GetClaimsAsync(role);

            foreach (var permission in _assignedClaims)
            {

            }


            var modules = typeof(Permissions).GetNestedTypes();
            foreach (var module in modules)
            {
                var moduleName = string.Empty;
                var moduleDescription = string.Empty;
                if (module.GetCustomAttributes(typeof(DisplayNameAttribute), true)
                    .FirstOrDefault() is DisplayNameAttribute displayNameAttribute)
                    moduleName = displayNameAttribute.DisplayName;

                if (module.GetCustomAttributes(typeof(DescriptionAttribute), true)
                    .FirstOrDefault() is DescriptionAttribute descriptionAttribute)
                    moduleDescription = descriptionAttribute.Description;

                var fields = module.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                foreach (var fi in fields)
                {
                    var propertyValue = fi.GetValue(null);

                    if (propertyValue is not null)
                    {

                        var claimValue = propertyValue.ToString();
                        if (_assignedClaims.Any(x => x.Value == claimValue))
                        {
                            allPermissions.Add(
                            new PermissionModel
                            {
                                RoleId = role.Id,
                                ClaimValue = claimValue,
                                ClaimType = "Permission",
                                Group = moduleName,
                                Description = moduleDescription,
                                Assigned = _assignedClaims.Any(x => x.Value == claimValue)
                            });
                        }

                    }
                }
            }
            return allPermissions;
        }
        public async Task<IEnumerable<MenuSectionModel>> LoadMenu(string role)
        {
            if (_features.Count() > 0)
                _features.Clear();

            _allowedFeatures = null;
            if (!IsSuperAdmin(role))
            {
                await LoadAllowedFeaturesAsync();
            }

            MenuSectionModel Home = new MenuSectionModel
            {
                Title = "",
                SectionItems = new List<MenuSectionItemModel>
                 {
                    new MenuSectionItemModel
                    {
                        Title = "Home",
                        ControllerName = "Home",
                        ActionName = "Index"
                    }
                }
            };

            _features.Add(Home);

            if (string.IsNullOrWhiteSpace(role))
                return _features;

            var roleresult = await _roleManager.FindByNameAsync(role);
            if (roleresult == null)
                return _features;

            allPermissions.Clear();
            var permissions = await GetAllPermissions(roleresult);

            if ((IsExist("Permissions.ItemMaster.View", role)
             || IsExist("Permissions.CategoryMaster.View", role)
             || IsExist("Permissions.SubCategory.View", role)
             || IsExist("Permissions.Company.View", role)
             || IsExist("Permissions.HSN.View", role)
             || IsExist("Permissions.Division.View", role)
             || IsExist("Permissions.Customer.View", role)
             || IsExist("Permissions.Suppliers.View", role)
             || IsExist("Permissions.AccountGroup.View", role)
             || IsExist("Permissions.Country.View", role)
             || IsExist("Permissions.State.View", role)
             || IsExist("Permissions.City.View", role)
             || IsExist("Permissions.Employee.View", role)
             || IsExist("Permissions.Department.View", role)
             || IsExist("Permissions.Designation.View", role)
             || IsExist("Permissions.Offers.View", role)
             || IsExist("Permissions.ModeOfPayment.View", role)
             || IsExist("Permissions.OpeningStock.View", role)
             || IsExist("Permissions.Roles.View", role)
             || IsExist("Permissions.Users.View", role)
             || IsExist("Permissions.CompanyRegistration.View", role)
             || IsExist("Permissions.TermCondition.View", role)) && IsFeatureAllowed("Master", role))
            {
                MenuSectionModel ItemMaster = new MenuSectionModel();
                ItemMaster.Title = "";
                ItemMaster.SectionItems = new List<MenuSectionItemModel>();

                var ItemMasterSubMenu = new MenuSectionItemModel()
                {
                    IsParent = true,
                    Title = "Master",
                    MenuItems = new List<MenuSectionSubItemModel>()
                };

                ItemMaster.SectionItems.Add(ItemMasterSubMenu);
                if ((IsExist("Permissions.ItemMaster.View", role)
                || IsExist("Permissions.CategoryMaster.View", role)
                || IsExist("Permissions.SubCategory.View", role)
                || IsExist("Permissions.Company.View", role)
                || IsExist("Permissions.HSN.View", role)
                || IsExist("Permissions.Division.View", role)) && IsFeatureAllowed("Master.ItemMaster", role))
                {
                    var ItemMasterSubMen1u = new MenuSectionSubItemModel
                    {
                        Title = "Item Master",
                        SubItems = new List<MenuSectionSubItemModel>()

                    };
                    if (IsExist("Permissions.ItemMaster.View", role))
                    {
                        ItemMasterSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Item Master",
                            ControllerName = "ItemMaster",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.CategoryMaster.View", role))
                    {
                        ItemMasterSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Category Master",
                            ControllerName = "CategoryMaster",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.SubCategory.View", role))
                    {
                        ItemMasterSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Sub Category",
                            ControllerName = "SubCategory",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.Company.View", role))
                    {
                        ItemMasterSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Company",
                            ControllerName = "Company",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.HSN.View", role))
                    {
                        ItemMasterSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "HSN",
                            ControllerName = "HSN",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsExist("Permissions.Division.View", role))
                    {
                        ItemMasterSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Division",
                            ControllerName = "Division",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed
                        });
                    }

                    ItemMasterSubMenu.MenuItems.Add(ItemMasterSubMen1u);
                }
                if ((IsExist("Permissions.Customer.View", role)
                || IsExist("Permissions.Suppliers.View", role)
                || IsExist("Permissions.AccountGroup.View", role)) && IsFeatureAllowed("Master.Ledger", role))
                {
                    var ledgerSubMen1u = new MenuSectionSubItemModel
                    {
                        Title = "Ledger Master",
                        SubItems = new List<MenuSectionSubItemModel>()
                    };
                    if (IsExist("Permissions.Customer.View", role))
                    {
                        ledgerSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Customer",
                            ControllerName = "Customer",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.Suppliers.View", role))
                    {
                        ledgerSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Supplier",
                            ControllerName = "Supplier",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.AccountGroup.View", role))
                    {
                        ledgerSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Account Group",
                            ControllerName = "AccountGroup",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                          
                    ItemMasterSubMenu.MenuItems.Add(ledgerSubMen1u);
                }
                if ((IsExist("Permissions.Country.View", role)
                || IsExist("Permissions.State.View", role)
                || IsExist("Permissions.City.View", role)) && IsFeatureAllowed("Master.Area", role))
                {
                    var areaSubMen1u = new MenuSectionSubItemModel
                    {
                        Title = "Area Master",
                        SubItems = new List<MenuSectionSubItemModel>()
                    };
                    if (IsExist("Permissions.Country.View", role))
                    {
                        areaSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Country",
                            ControllerName = "Country",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.State.View", role))
                    {
                        areaSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "State",
                            ControllerName = "State",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.City.View", role))
                    {
                        areaSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "City",
                            ControllerName = "City",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.Currency.View", role))
                    {
                        areaSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Currency",
                            ControllerName = "Currency",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }

                   ItemMasterSubMenu.MenuItems.Add(areaSubMen1u);
                }
                if ((IsExist("Permissions.Employee.View", role)
                || IsExist("Permissions.Department.View", role)
                || IsExist("Permissions.Designation.View", role)) && IsFeatureAllowed("Master.Staff", role))
                {
                    var staffSubMen1u = new MenuSectionSubItemModel
                    {
                        Title = "Staff Master",
                        SubItems = new List<MenuSectionSubItemModel>()
                    };
                    if (IsExist("Permissions.Employee.View", role))
                    {
                        staffSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Staff",
                            ControllerName = "Employee",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsExist("Permissions.Department.View", role))
                    {
                        staffSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Department",
                            ControllerName = "Department",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.Designation.View", role))
                    {
                        staffSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Designation",
                            ControllerName = "Designation",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed
                        });
                    }
                     
                    ItemMasterSubMenu.MenuItems.Add(staffSubMen1u);
                }
                if (IsExist("Permissions.Offers.View", role) && IsFeatureAllowed("Master.Offers", role))
                {
                    ItemMasterSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "Offers",
                        ControllerName = "Offer",
                        ActionName = "Index",
                        PageStatus = PageStatus.Completed,
                    });
                }

                if (IsExist("Permissions.ModeOfPayment.View", role) && IsFeatureAllowed("Master.PaymentMode", role))
                {
                    ItemMasterSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "Mode Of Payment",
                        ControllerName = "PaymentMode",
                        ActionName = "Index",
                        PageStatus = PageStatus.Completed,
                    });
                }
                if (IsExist("Permissions.OpeningStock.View", role) && IsFeatureAllowed("Master.OpeningStock", role))
                {
                    ItemMasterSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "Opening Stock",
                        ControllerName = "OpeningStock",
                        ActionName = "Index",
                        PageStatus = PageStatus.Completed,
                    });
                }
                if ((IsExist("Permissions.Roles.View", role)
                || IsExist("Permissions.Users.View", role)
                || IsExist("Permissions.CompanyRegistration.View", role)
                || IsExist("Permissions.TermCondition.View", role)
                || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)) && IsFeatureAllowed("Master.Settings", role))
                {
                    var settingsSubMen1u = new MenuSectionSubItemModel
                    {
                        Title = "Settings",
                        SubItems = new List<MenuSectionSubItemModel>()
                    };
                    if (IsExist("Permissions.Roles.View", role))
                    {
                        settingsSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Roles",
                            ControllerName = "User",
                            ActionName = "RoleList",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.Users.View", role))
                    {
                        settingsSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Users",
                            ControllerName = "Account",
                            ActionName = "UsersAccount",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsSuperAdmin(role))
                    {
                        settingsSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Company Registration",
                            ControllerName = "Tenant",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsExist("Permissions.TermCondition.View", role))
                    {
                        settingsSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Term Conditions",
                            ControllerName = "TermConditions",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (IsSuperAdmin(role))
                    {
                        settingsSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Company Plan",
                            ControllerName = "CompanyPlan",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                    if (!IsSuperAdmin(role))
                    {
                        settingsSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                        {
                            Title = "Company Profile",
                            ControllerName = "Tenant",
                            ActionName = "EditProfile",
                            PageStatus = PageStatus.Completed,
                        });
                    }
                   
                          
                    ItemMasterSubMenu.MenuItems.Add(settingsSubMen1u);
                }
                _features.Add(ItemMaster); 
            }
            //MenuSectionModel Home = new MenuSectionModel();
            //Home.Title = "";
            //Home.SectionItems = new List<MenuSectionItemModel>
            //{
            //    new()
            //    {
            //        Title = "Home",
            //        ControllerName = "Home",
            //        ActionName = "Index"
            //    },
            //};

            //var EmployeeSubMenu = new MenuSectionItemModel()
            //{
            //    IsParent = true,
            //    Title = "Master",

            //}; Home.SectionItems.Add(EmployeeSubMenu);
            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //        new(){
            //              Title = "Item Master",
            //            ControllerName="ItemMaster",
            //            ActionName = "Index",
            //            PageStatus = PageStatus.Completed,
            //        }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Item Master",
            //        ControllerName = "ItemMaster",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}

            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //        new(){
            //              Title = "Category Master",
            //            ControllerName="CategoryMaster",
            //            ActionName = "Index",
            //            PageStatus = PageStatus.Completed,
            //        }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Category Master",
            //        ControllerName = "CategoryMaster",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}

            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //               Title = "Sub Category",
            //             ControllerName="SubCategory",
            //             ActionName = "Index",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Sub Category",
            //        ControllerName = "SubCategory",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}


            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //             Title = "HSN",
            //             ControllerName = "HSN",
            //            ActionName = "Index",
            //            PageStatus = PageStatus.Completed
            //        }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "HSN",
            //        ControllerName = "HSN",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed
            //    });
            //}

            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //             Title = "Division",
            //             ControllerName = "Division",
            //            ActionName = "Index",
            //            PageStatus = PageStatus.Completed
            //        }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Division",
            //        ControllerName = "Division",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed
            //    });
            //}

            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //               Title = "Company",
            //             ControllerName="Company",
            //             ActionName = "Index",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Company",
            //        ControllerName = "Company",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}

            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //               Title = "Customer",
            //             ControllerName="Customer",
            //             ActionName = "Index",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Customer",
            //        ControllerName = "Customer",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}

            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //               Title = "Supplier",
            //             ControllerName="Supplier",
            //             ActionName = "Index",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Supplier",
            //        ControllerName = "Supplier",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}





            //EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel
            //{
            //    Title = "Staff",
            //    ControllerName = "Employee",
            //    ActionName = "Index",
            //    PageStatus = PageStatus.Completed

            //});

            //EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel
            //        {
            //            Title = "Department",
            //            ControllerName = "Department",
            //            ActionName = "Index",
            //            PageStatus = PageStatus.Completed
            //        });

            //EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel
            //        {
            //            Title = "Designation",
            //            ControllerName = "Designation",
            //            ActionName = "Index",
            //            PageStatus = PageStatus.Completed
            //        });




            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //               Title = "Country",
            //             ControllerName="Country",
            //             ActionName = "Index",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Country",
            //        ControllerName = "Country",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}

            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //               Title = "Currency",
            //             ControllerName="Currency",
            //             ActionName = "Index",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Currency",
            //        ControllerName = "Currency",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}
            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //               Title = "State",
            //             ControllerName="State",
            //             ActionName = "Index",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "State",
            //        ControllerName = "State",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}
            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //               Title = "City",
            //             ControllerName="City",
            //             ActionName = "Index",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "City",
            //        ControllerName = "City",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}

            //if (EmployeeSubMenu.MenuItems == null)
            //{
            //    EmployeeSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //             Title = "Account Group",
            //             ControllerName="AccountGroup",
            //             ActionName = "Index",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    EmployeeSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Account Group",
            //        ControllerName = "AccountGroup",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}
            //_features.Add(Home);
            if ((IsExist("Permissions.Sales.View", role)
            || IsExist("Permissions.SalesOrder.View", role)
            || IsExist("Permissions.StockReturn.View", role)) && IsFeatureAllowed("Sales", role))
            {

                MenuSectionModel sales = new MenuSectionModel();
                sales.Title = "";
                sales.SectionItems = new List<MenuSectionItemModel>();

                var SalesSubMenu = new MenuSectionItemModel()
                {
                    IsParent = true,
                    Title = "Sales",
                    MenuItems = new List<MenuSectionSubItemModel>()
                };

                sales.SectionItems.Add(SalesSubMenu);
                if (IsExist("Permissions.Sales.View", role))
                {
                    if (IsFeatureAllowed("Sales.Create", role))
                    {
                        SalesSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Sales Bill",
                            ControllerName = "Sales",
                            ActionName = "Create",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("Sales.Index", role))
                    {
                        SalesSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Edit Sales",
                            ControllerName = "Sales",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("Sales.HoldList", role))
                    {
                        SalesSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Sales Hold List",
                            ControllerName = "Sales",
                            ActionName = "HoldList",
                            PageStatus = PageStatus.Completed
                        });
                    }

                }
                if (IsExist("Permissions.SalesOrder.View", role) && IsFeatureAllowed("Sales.Order", role))
                { 
                    SalesSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "Sales Order",
                        ControllerName = "SalesOrder",
                        ActionName = "Index",
                        PageStatus = PageStatus.Completed
                    });
                   
                }
                if (IsExist("Permissions.StockReturn.View", role) && IsFeatureAllowed("Sales.Return", role))
                {
                    
                        SalesSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Sales Return(C/N)",
                            ControllerName = "StockReturn",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed,
                        });
                     
                }
                if (IsExist("Permissions.StockIssue.View", role) && IsFeatureAllowed("Sales.StockIssue", role))
                { 
                        SalesSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Stock Issue",
                            ControllerName = "StockIssue",
                            ActionName = "Index",
                            PageStatus = PageStatus.Completed
                        });
                    
                }
                SalesSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                {
                    Title = "Stock Conversion",
                    ControllerName = "StockConversion",
                    ActionName = "Index",
                    PageStatus = PageStatus.Completed
                });
                //if (IsExist("Permissions.Sales.View", role))
                //{
                     
                //    SalesSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                //    {
                //        Title = "Sales Book",
                //        ControllerName = "Sales",
                //        ActionName = "SalesBook",
                //        PageStatus = PageStatus.Completed
                //    });
                     
                //}
                //if (SalesSubMenu.MenuItems == null)
                //{
                //    SalesSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
                //    {
                //         new(){
                //             Title = "Invoice",
                //             ControllerName="Invoice",
                //             ActionName = "Index",
                //             PageStatus = PageStatus.Completed,
                //         }
                //    };
                //}
                //else
                //{
                //    SalesSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                //    {
                //        Title = "Invoice",
                //        ControllerName = "Invoice",
                //        ActionName = "Index",
                //        PageStatus = PageStatus.Completed
                //    });
                //}

                //if (SalesSubMenu.MenuItems == null)
                //{
                //    SalesSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
                //    {
                //         new(){
                //               Title = "Quotation",
                //             ControllerName="Quotation",
                //             ActionName = "Index",
                //             PageStatus = PageStatus.Completed,
                //         }
                //    };
                //}
                //else
                //{
                //    SalesSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                //    {
                //        Title = "Quotation",
                //        ControllerName = "Quotation",
                //        ActionName = "Index",
                //        PageStatus = PageStatus.Completed,
                //    });
                //}


                _features.Add(sales);
            }

            if (IsExist("Permissions.PurchaseEntry.View", role) && IsFeatureAllowed("Purchase", role))
            {
                MenuSectionModel Purchase = new MenuSectionModel();
                Purchase.Title = "";
                Purchase.SectionItems = new List<MenuSectionItemModel>();

                var PurchaseSubMenu = new MenuSectionItemModel()
                {
                    IsParent = true,
                    Title = "Purchase",
                    MenuItems = new List<MenuSectionSubItemModel>()
                };

                Purchase.SectionItems.Add(PurchaseSubMenu);

                if (IsExist("Permissions.PurchaseEntry.View", role) && IsFeatureAllowed("Purchase.Entry", role))
                { 
                    PurchaseSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "Purchase Entry",
                        ControllerName = "Purchase",
                        ActionName = "Index",
                        PageStatus = PageStatus.Completed
                    });
                    
                }
                if (IsExist("Permissions.PurchaseReturn.View", role) && IsFeatureAllowed("Purchase.Return", role))
                {
                     
                   PurchaseSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                   {
                       Title = "Purchase Return(D/N)",
                       ControllerName = "PurchaseReturn",
                       ActionName = "Index",
                       PageStatus = PageStatus.Completed
                   });
                     
                }
                if (IsExist("Permissions.StockReceive.View", role) && IsFeatureAllowed("Purchase.StockReceive", role))
                { 
                    PurchaseSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "Stock Receive",
                        ControllerName = "StockReceive",
                        ActionName = "Index",
                        PageStatus = PageStatus.Completed,
                    });

                }

                if (IsExist("Permissions.PurchaseChallan.View", role) && IsFeatureAllowed("Purchase.Challan", role))
                {
                    PurchaseSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "Purchase Challan",
                        ControllerName = "PurchaseChallan",
                        ActionName = "Index",
                        PageStatus = PageStatus.Completed
                    });
                }

                _features.Add(Purchase);
            }

            if (IsExist("Permissions.PurchaseOrder.View", role) && IsFeatureAllowed("PurchaseOrder", role))
            {
                MenuSectionModel Purchase = new MenuSectionModel();
                Purchase.Title = "";
                Purchase.SectionItems = new List<MenuSectionItemModel>();

                var PurchaseSubMenu = new MenuSectionItemModel()
                {
                    IsParent = true,
                    Title = "Purchase Order",
                    MenuItems = new List<MenuSectionSubItemModel>()
                };

                Purchase.SectionItems.Add(PurchaseSubMenu);
                 
                if (IsFeatureAllowed("PurchaseOrder.Manual", role))
                {
                    PurchaseSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "Manual",
                        ControllerName = "PurchaseOrder",
                        ActionName = "Index",
                        PageStatus = PageStatus.Completed
                    });
                }
                if (IsFeatureAllowed("PurchaseOrder.AI", role))
                {
                    PurchaseSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "With AI",
                        ControllerName = "PurchaseOrder",
                        ActionName = "GeneratePoWithAI",
                        PageStatus = PageStatus.Completed
                    });
                }

                _features.Add(Purchase);
            }

            //MenuSectionModel Stock = new MenuSectionModel();
            //Stock.Title = "";
            //Stock.SectionItems = new List<MenuSectionItemModel>();

            //var StockSubMenu = new MenuSectionItemModel()
            //{
            //    IsParent = true,
            //    Title = "Stock",
            //};

            //Stock.SectionItems.Add(StockSubMenu);


            //if (StockSubMenu.MenuItems == null)
            //{
            //    StockSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //             Title = "Stock",
            //             ControllerName="Purchase",
            //             ActionName = "CurrentStock",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    StockSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Stock",
            //        ControllerName = "Purchase",
            //        ActionName = "CurrentStock",
            //        PageStatus = PageStatus.Completed
            //    });
            //}
            //_features.Add(Stock);
            if ((IsExist("Permissions.PaymentVoucherCategory.View", role)
             || IsExist("Permissions.PaymentVoucher.View", role)
             || IsExist("Permissions.ReceiveVoucher.View", role))
             && (IsFeatureAllowed("PaymentVoucher", role) || IsFeatureAllowed("ReceiveVoucher", role)))
            {
                MenuSectionModel PaymentVoucher = new MenuSectionModel();
                PaymentVoucher.Title = "";
                PaymentVoucher.SectionItems = new List<MenuSectionItemModel>();

                var paymentvoucherSubMenu = new MenuSectionItemModel()
                {
                    IsParent = true,
                    Title = "Payment Voucher",
                    MenuItems = new List<MenuSectionSubItemModel>()
                };

                PaymentVoucher.SectionItems.Add(paymentvoucherSubMenu);
                if (IsExist("Permissions.PaymentVoucherCategory.View", role) && IsFeatureAllowed("PaymentVoucher.Category", role))
                { 
                    paymentvoucherSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "Payment Voucher Category",
                        ControllerName = "PaymentVoucherCategory",
                        ActionName = "Index",
                        PageStatus = PageStatus.Completed
                    }); 
                }
                if (IsExist("Permissions.PaymentVoucher.View", role) && IsFeatureAllowed("PaymentVoucher.Entry", role))
                { 
                   paymentvoucherSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                   {
                       Title = "Payment Voucher",
                       ControllerName = "PaymentVoucher",
                       ActionName = "Index",
                       PageStatus = PageStatus.Completed
                   }); 
                }
                if (IsExist("Permissions.ReceiveVoucher.View", role) && IsFeatureAllowed("ReceiveVoucher", role))
                {
                    
                   paymentvoucherSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                   {
                       Title = "Receive Voucher",
                       ControllerName = "ReceiveVoucher",
                       ActionName = "Index",
                       PageStatus = PageStatus.Completed
                   });
                     
                }
                _features.Add(PaymentVoucher);
            }

            if (IsExist("Permissions.Sales.View", role) && IsFeatureAllowed("Sales", role))
            {
                MenuSectionModel SalesBook = new MenuSectionModel
                {
                    Title = "",
                    SectionItems = new List<MenuSectionItemModel>
                    {
                        new MenuSectionItemModel
                        {
                            Title = "Sales Book",
                            ControllerName = "Sales",
                            ActionName = "SalesBook",
                            PageStatus = PageStatus.Completed
                        }
                    }
                };
                _features.Add(SalesBook);
            }
            if (IsExist("Permissions.Sales.View", role) && IsFeatureAllowed("Reports", role))
            {
                MenuSectionModel Sales = new MenuSectionModel
                {
                    Title = "",
                    SectionItems = new List<MenuSectionItemModel>
                    {
                        new MenuSectionItemModel
                        {
                            Title = "Ledger Report",
                            ControllerName = "Sales",
                            ActionName = "LedgerReportPage",
                            PageStatus = PageStatus.Completed
                        }
                    }
                };
                _features.Add(Sales);
            }


            if ((IsExist("Permissions.Sales.View", role)
            || IsExist("Permissions.StockReturn.View", role)
            || IsExist("Permissions.StockIssue.View", role)
            || IsExist("Permissions.StockReceive.View", role)
            || IsExist("Permissions.PurchaseEntry.View", role)
            || IsExist("Permissions.PurchaseReturn.View", role)
            || IsExist("Permissions.ModeOfPayment.View", role)) && IsFeatureAllowed("Reports", role))
            {
                MenuSectionModel Report = new MenuSectionModel();
                Report.Title = "";
                Report.SectionItems = new List<MenuSectionItemModel>();

                var ReportSubMenu = new MenuSectionItemModel()
                {
                    IsParent = true,
                    Title = "Report",
                    MenuItems = new List<MenuSectionSubItemModel>()
                };

                Report.SectionItems.Add(ReportSubMenu);
                if (IsExist("Permissions.Sales.View", role) && IsFeatureAllowed("Reports.Sales", role))
                {
                    var salsereportSubMen1u = new MenuSectionSubItemModel
                    {
                        Title = "Sales",
                        SubItems = new List<MenuSectionSubItemModel>()
                    };
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                        Title = "Itemwise Sales",
                        ControllerName = "Sales",
                        ActionName = "SalseReportItemwise",
                        PageStatus = PageStatus.Completed
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Billwise Sales",
                         ControllerName="Sales",
                         ActionName = "SalseReportBillwise",
                         PageStatus = PageStatus.Completed,
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Customer Sales",
                         ControllerName="Sales",
                         ActionName = "SalseReportCompanywise",
                         PageStatus = PageStatus.Completed,
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "ItemBatchwise Sales",
                         ControllerName="Sales",
                         ActionName = "ItemWiseBatchWiseSales",
                         PageStatus = PageStatus.Completed,
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Itemwise Party Sales",
                         ControllerName="Sales",
                         ActionName = "ItemWisePartySales",
                         PageStatus = PageStatus.Completed,
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Customerwise Sales",
                         ControllerName="Sales",
                         ActionName = "CustomerWiseSales",
                         PageStatus = PageStatus.Completed,
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Datewise Sales",
                         ControllerName="Sales",
                         ActionName = "DateWiseSalesReport",
                         PageStatus = PageStatus.Completed,
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Cash Collection",
                         ControllerName="Sales",
                         ActionName = "SalesBillReturnAndCollectionReport",
                         PageStatus = PageStatus.Completed,
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Billwise collection",
                         ControllerName="Sales",
                         ActionName = "Billwisecollectionreport",
                         PageStatus = PageStatus.Completed,
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Billwise Profit",
                         ControllerName="Sales",
                         ActionName = "BillwiseProfitReport",
                         PageStatus = PageStatus.Completed,
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Narcotic Drugs",
                         ControllerName="Sales",
                         ActionName = "NarcoticDrugsReport",
                         PageStatus = PageStatus.Completed,
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "ScheduleH Drugs",
                         ControllerName="Sales",
                         ActionName = "ScheduleHDrugsReport",
                         PageStatus = PageStatus.Completed,
                    });
                    salsereportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "ScheduleH1 Drugs",
                         ControllerName="Sales",
                         ActionName = "ScheduleH1DrugsReport",
                         PageStatus = PageStatus.Completed,
                    });

                    ReportSubMenu.MenuItems.Add(salsereportSubMen1u);
                }
                if (IsExist("Permissions.StockReturn.View", role) && IsFeatureAllowed("Reports.SalesReturn", role))
                {
                    var stockreturnreportSubMen1u = new MenuSectionSubItemModel
                    {
                        Title = "Sales Return",
                        SubItems = new List<MenuSectionSubItemModel>()
                    };
                    stockreturnreportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Customerwise SR",
                         ControllerName="StockReturn",
                         ActionName = "StockReturnReportCustomerWise",
                         PageStatus = PageStatus.Completed,
                    });
                    stockreturnreportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                        Title = "Itemwise SR",
                        ControllerName = "StockReturn",
                        ActionName = "StockReturnReportItemwise",
                        PageStatus = PageStatus.Completed
                    });

                    ReportSubMenu.MenuItems.Add(stockreturnreportSubMen1u);
                }
                if (IsExist("Permissions.StockIssue.View", role) && IsFeatureAllowed("Reports.StockIssue", role))
                {
                    var StockIssueReportSubMen1u = new MenuSectionSubItemModel
                    {
                        Title = "Stock Issue",
                        SubItems = new List<MenuSectionSubItemModel>()
                    };
                    StockIssueReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Stock Issue",
                         ControllerName="StockIssue",
                         ActionName = "StockIssueReportBillWise",
                         PageStatus = PageStatus.Completed,
                    });
                    StockIssueReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Item Wise Issue",
                         ControllerName="StockIssue",
                         ActionName = "StockIssueReportItemWise",
                         PageStatus = PageStatus.Completed,
                    });

                    ReportSubMenu.MenuItems.Add(StockIssueReportSubMen1u);
                }
                if (IsExist("Permissions.StockReceive.View", role) && IsFeatureAllowed("Reports.StockReceive", role))
                {
                    var StockReceiveReportSubMen1u = new MenuSectionSubItemModel
                    {
                        Title = "Stock Receive",
                        SubItems = new List<MenuSectionSubItemModel>()
                    };
                    StockReceiveReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Stock Receive",
                         ControllerName="StockReceive",
                         ActionName = "StockreceiveReportBillWise",
                         PageStatus = PageStatus.Completed,
                    });
                    StockReceiveReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Item Wise Receive",
                         ControllerName="StockReceive",
                         ActionName = "StockreceiveReportItemWise",
                         PageStatus = PageStatus.Completed,
                    });

                    ReportSubMenu.MenuItems.Add(StockReceiveReportSubMen1u);
                }
                if (IsExist("Permissions.PurchaseEntry.View", role) && IsFeatureAllowed("Reports.Purchase", role))
                {
                    var PurchaseEntryReportSubMen1u = new MenuSectionSubItemModel
                    {
                        Title = "Purchase",
                        SubItems = new List<MenuSectionSubItemModel>()
                    };
                    PurchaseEntryReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Purchase Register",
                         ControllerName="Purchase",
                         ActionName = "PurchaseReportBillWise",
                         PageStatus = PageStatus.Completed,
                    });
                    PurchaseEntryReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Itemwise Purchase",
                         ControllerName="Purchase",
                         ActionName = "PurchaseReportItemWise",
                         PageStatus = PageStatus.Completed
                    });
                    PurchaseEntryReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "CompanyItem Wise",
                         ControllerName="Purchase",
                         ActionName = "PurchaseReportCompanyWise",
                         PageStatus = PageStatus.Completed
                    });
                    PurchaseEntryReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Supplier Purchase ",
                         ControllerName="Purchase",
                         ActionName = "PurchaseReportSupplierWise",
                         PageStatus = PageStatus.Completed
                    });
                    PurchaseEntryReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "ItemWise Summary",
                         ControllerName="Purchase",
                         ActionName = "ItemWisePurchaseReport",
                         PageStatus = PageStatus.Completed
                    });
                    PurchaseEntryReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "ItemWise Details",
                         ControllerName="Purchase",
                         ActionName = "ItemWisePurchaseDetailsReport",
                         PageStatus = PageStatus.Completed
                    });

                    ReportSubMenu.MenuItems.Add(PurchaseEntryReportSubMen1u);
                }
                if (IsExist("Permissions.PurchaseReturn.View", role) && IsFeatureAllowed("Reports.PurchaseReturn", role))
                {
                    var PurchaseReturnReportSubMen1u = new MenuSectionSubItemModel
                    {
                        Title = "Purchase Return",
                        SubItems = new List<MenuSectionSubItemModel>()
                    };
                    PurchaseReturnReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Itemwise Purchase ",
                         ControllerName="PurchaseReturn",
                         ActionName = "PurchaseReturnReportItemWise",
                         PageStatus = PageStatus.Completed
                    });
                    PurchaseReturnReportSubMen1u.SubItems.Add(new MenuSectionSubItemModel
                    {
                         Title = "Supplierwise Purchase",
                         ControllerName="PurchaseReturn",
                         ActionName = "PurchaseReturnReportSupplierWise",
                         PageStatus = PageStatus.Completed
                    });

                    ReportSubMenu.MenuItems.Add(PurchaseReturnReportSubMen1u);
                }
                _features.Add(Report);

                if (IsExist("Permissions.ModeOfPayment.View", role) && IsFeatureAllowed("Reports.Sales", role))
                {
                    
                    ReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "Mode Of Payment",
                        ControllerName = "Sales",
                        ActionName = "SalseModeOfPaymentReport",
                        PageStatus = PageStatus.Completed
                    });
                    
                }
            }
            if ((IsExist("Permissions.PurchaseEntry.View", role) 
            || IsExist("Permissions.Sales.View", role)) && IsFeatureAllowed("Inventory", role))
            {
                MenuSectionModel InventoryReport = new MenuSectionModel();
                InventoryReport.Title = "";
                InventoryReport.SectionItems = new List<MenuSectionItemModel>();

                var InventoryReportSubMenu = new MenuSectionItemModel()
                {
                    IsParent = true,
                    Title = "Inventory Report",
                    MenuItems = new List<MenuSectionSubItemModel>()
                };

                InventoryReport.SectionItems.Add(InventoryReportSubMenu);
                if (IsExist("Permissions.PurchaseEntry.View", role))
                {
                    if (IsFeatureAllowed("Inventory.CurrentStock", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = " Current Stock",
                            ControllerName = "Purchase",
                            ActionName = "CurrentStock",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("Inventory.NearExpiry", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Near Expiry ",
                            ControllerName = "Purchase",
                            ActionName = "NearExpiryReport",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("Inventory.Expired", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Expired Item",
                            ControllerName = "Purchase",
                            ActionName = "ExpiredItemReport",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("Inventory.MinimumStock", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Minimum Stock",
                            ControllerName = "Purchase",
                            ActionName = "MinimumStockReport",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("Inventory.DumpStock", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Dump Stock",
                            ControllerName = "Purchase",
                            ActionName = "DumpStockReport",
                            PageStatus = PageStatus.Completed
                        });
                    }
                }
                if (IsExist("Permissions.Sales.View", role))
                {
                    if (IsFeatureAllowed("Inventory.FastMoving", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Fast Moving Items",
                            ControllerName = "Sales",
                            ActionName = "FastMovingItems",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("Inventory.SlowMoving", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Slow Moving Items",
                            ControllerName = "Sales",
                            ActionName = "SlowMovingItems",
                            PageStatus = PageStatus.Completed
                        });
                    }
                }
                if (IsExist("Permissions.PurchaseEntry.View", role))
                {
                    if (IsFeatureAllowed("Inventory.SupplierWise", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Supplier Wise",
                            ControllerName = "Purchase",
                            ActionName = "SupplierWiseStock",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("Inventory.CompanyItemWise", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Company ItemWise",
                            ControllerName = "Purchase",
                            ActionName = "CompanyWiseStock",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("Inventory.OverStock", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "OverStock Report",
                            ControllerName = "Purchase",
                            ActionName = "OverStockReport",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("Inventory.BatchWise", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Batch Wise Stock",
                            ControllerName = "Purchase",
                            ActionName = "BatchWiseStock",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("Inventory.StockSales", role))
                    {
                        InventoryReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "Stock & Sales",
                            ControllerName = "Sales",
                            ActionName = "StockInSalesStatmentReport",
                            PageStatus = PageStatus.Completed
                        });
                    }
                   
                }

                _features.Add(InventoryReport);
            }
            if (((IsExist("Permissions.Sales.View", role)
            || IsExist("Permissions.PurchaseEntry.View", role))) && IsFeatureAllowed("GSTR", role))
            {
                MenuSectionModel GSTReport = new MenuSectionModel();
                GSTReport.Title = "";
                GSTReport.SectionItems = new List<MenuSectionItemModel>();

                var GSTReportSubMenu = new MenuSectionItemModel()
                {
                    IsParent = true,
                    Title = "GST Report",
                    MenuItems = new List<MenuSectionSubItemModel>()
                };

                GSTReport.SectionItems.Add(GSTReportSubMenu);
                if (IsExist("Permissions.Sales.View", role) && IsFeatureAllowed("GSTR.GSTR1", role))
                {
                    GSTReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                    {
                        Title = "GSTR1",
                        ControllerName = "Sales",
                        ActionName = "GSTR1Report",
                        PageStatus = PageStatus.Completed
                    });
                }
                if (IsExist("Permissions.PurchaseEntry.View", role))
                {
                    if (IsFeatureAllowed("GSTR.GSTR2", role))
                    {
                        GSTReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "GSTR2",
                            ControllerName = "Purchase",
                            ActionName = "GSTR2Report",
                            PageStatus = PageStatus.Completed
                        });
                    }
                    if (IsFeatureAllowed("GSTR.GSTR3", role))
                    {
                        GSTReportSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
                        {
                            Title = "GSTR3",
                            ControllerName = "Purchase",
                            ActionName = "GSTR3Report",
                            PageStatus = PageStatus.Completed
                        });
                    }
                }
                
                _features.Add(GSTReport);
            }

            if (IsFeatureAllowed("GlobalSettings", role))
            {
                MenuSectionModel GlobalSettings = new MenuSectionModel
                {
                    Title = "",
                    SectionItems = new List<MenuSectionItemModel>
                        {
                            new MenuSectionItemModel
                            {
                                Title = "Global Settings",
                                ControllerName = "GlobalSettings",
                                ActionName = "GlobalSettings",
                                PageStatus = PageStatus.Completed
                            }
                        }
                };
                _features.Add(GlobalSettings);
            }

            if (IsFeatureAllowed("Wallet", role))
            {
                MenuSectionModel Wallet = new MenuSectionModel
                {
                    Title = "",
                    SectionItems = new List<MenuSectionItemModel>
                        {
                            new MenuSectionItemModel
                            {
                                Title = "Wallet",
                                ControllerName = "Wallet",
                                ActionName = "Index",
                                PageStatus = PageStatus.Completed
                            }
                        }
                };
                _features.Add(Wallet);
            }
            MenuSectionModel ItemBackup = new MenuSectionModel
            {
                Title = "",
                SectionItems = new List<MenuSectionItemModel>
                    {
                        new MenuSectionItemModel
                        {
                            Title = "Item Backup",
                            ControllerName = "ItemMaster",
                            ActionName = "RestoreItem",
                            PageStatus = PageStatus.Completed
                        }
                    }
            };
            _features.Add(ItemBackup);
            //MenuSectionModel Management = new MenuSectionModel();
            //Management.Title = "";
            //Management.SectionItems = new List<MenuSectionItemModel>();

            //var AuthorizationSubMenu = new MenuSectionItemModel()
            //{
            //    IsParent = true,
            //    Title = "Authorization",
            //};

            //Management.SectionItems.Add(AuthorizationSubMenu);

            //if (AuthorizationSubMenu.MenuItems == null)
            //{
            //    AuthorizationSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //               Title = "Roles",
            //             ControllerName="User",
            //             ActionName = "RoleList",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    AuthorizationSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Roles",
            //        ControllerName = "User",
            //        ActionName = "RoleList",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}

            //if (AuthorizationSubMenu.MenuItems == null)
            //{
            //    AuthorizationSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //               Title = "Users",
            //             ControllerName="Account",
            //             ActionName = "UsersAccount",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    AuthorizationSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Users",
            //        ControllerName = "Account",
            //        ActionName = "UsersAccount",
            //        PageStatus = PageStatus.Completed,
            //    });
            //}

            //if (AuthorizationSubMenu.MenuItems == null)
            //{
            //    AuthorizationSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //             Title = "Company Registration",
            //             ControllerName="Tenant",
            //             ActionName = "Index",
            //             PageStatus = PageStatus.Completed,
            //         }
            //    };
            //}
            //else
            //{
            //    AuthorizationSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Company Registration",
            //        ControllerName = "Tenant",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed
            //    });
            //}
            //if (AuthorizationSubMenu.MenuItems == null)
            //{
            //    AuthorizationSubMenu.MenuItems = new List<MenuSectionSubItemModel>()
            //    {
            //         new(){
            //                  Title = "Term Conditions",
            //             ControllerName="TermConditions",
            //             ActionName = "Index",
            //             PageStatus = PageStatus.Completed,
            //        },
            //    };
            //}
            //else
            //{
            //    AuthorizationSubMenu.MenuItems.Add(new MenuSectionSubItemModel()
            //    {
            //        Title = "Term Conditions",
            //        ControllerName="TermConditions",
            //        ActionName = "Index",
            //        PageStatus = PageStatus.Completed,

            //    });
            //}
            //_features.Add(Management);


            return _features;
        }
        //private bool IsExist(string permissionName)
        //{
        //    var results = allPermissions.Where(p => p.ClaimValue.Contains(permissionName)).Count();
        //    if (results > 0)
        //        return true;
        //    else
        //        return false;
        //}
        private bool IsSuperAdmin(string role)
        {
            return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }
        private bool IsExist(string permissionName, string role)
        {
            if (IsSuperAdmin(role))
                return true;

            var results = allPermissions.Where(p => p.ClaimValue.Contains(permissionName)).Count();
            if (results > 0)
                return true;
            else
                return false;
        }
        public async Task<bool> HasPermission(string permission, string role)
        {
            if (IsSuperAdmin(role))
                return true;

            var roleresult = await _roleManager.FindByNameAsync(role);
            var permissions = await GetAllPermissions(roleresult);
            return permissions.Any(p => p.ClaimValue == permission);
        }
        private readonly List<MenuSectionModel> _features = new List<MenuSectionModel>();
        public IEnumerable<MenuSectionModel> Features => _features;
    }
}
