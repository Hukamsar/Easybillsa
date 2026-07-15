using AOne.DataAccess.Repository.IRepository;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBill.UI.Controllers.API
{
    [ApiController]
    [Route("api/sa")]
    public class SaTenantApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUsers> _userManager;
        private readonly ITenantRegistrationRepository _tenantRepository;
        private readonly IUnitOfWork _unitOfWork;

        private const string SaApiKeyHeaderName = "X-SA-API-KEY";
        private const string AllowedApiKey = "SuperAdminSecureSecretKey123";

        public SaTenantApiController(
            ApplicationDbContext context,
            UserManager<ApplicationUsers> userManager,
            ITenantRegistrationRepository tenantRepository,
            IUnitOfWork unitOfWork)
        {
            _context = context;
            _userManager = userManager;
            _tenantRepository = tenantRepository;
            _unitOfWork = unitOfWork;
        }

        private bool IsAuthorized()
        {
            if (!Request.Headers.TryGetValue(SaApiKeyHeaderName, out var extractedApiKey))
            {
                return false;
            }
            return extractedApiKey.ToString() == AllowedApiKey;
        }

        private DateTime? ParseSaDate(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return null;
            if (DateTime.TryParse(dateStr, out var d)) return d;
            return null;
        }

        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies()
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            var list = await _context.Tenants
                .Include(t => t.State)
                .Include(t => t.Country)
                .Include(t => t.SubscriptionPlan)
                .ToListAsync();
            var mapped = list.Select(MapToSaCompany).ToList();

            return Ok(new { items = mapped, total = list.Count });
        }

        [HttpGet("companies/{id}")]
        public async Task<IActionResult> GetCompany(string id)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            var tenant = await _context.Tenants
                .Include(t => t.State)
                .Include(t => t.Country)
                .Include(t => t.SubscriptionPlan)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Company not found." });
            }

            return Ok(new { company = MapToSaCompany(tenant) });
        }

        [HttpPost("companies")]
        public async Task<IActionResult> CreateCompany([FromBody] SaCompanyPayload payload)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            if (payload == null || payload.Fields == null)
            {
                return BadRequest(new { success = false, message = "Invalid payload." });
            }

            var fields = payload.Fields;
            var email = fields.PrimaryEmail?.Trim();

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { success = false, message = "Primary email is required." });
            }

            // Check duplicate
            var duplicate = await _context.Tenants.AnyAsync(x => x.Email.ToLower() == email.ToLower());
            if (duplicate)
            {
                return BadRequest(new { success = false, message = "Email is already registered." });
            }

            // Lookup City
            City? city = null;
            if (!string.IsNullOrEmpty(fields.City))
            {
                city = await _context.Cities.FirstOrDefaultAsync(x => x.Name != null && x.Name.ToLower() == fields.City.ToLower());
            }

            int? stateId = city?.StateId;
            int? countryId = city?.CountryId;

            if (stateId == null && await _context.States.AnyAsync(x => x.Id == 24))
            {
                stateId = 24;
            }
            if (countryId == null && await _context.Countries.AnyAsync(x => x.Id == 1))
            {
                countryId = 1;
            }

            var tenant = new Tenant
            {
                Id = Guid.NewGuid().ToString(),
                Name = fields.DisplayName ?? fields.LegalName,
                Email = email,
                Address1 = fields.AddressLine1,
                Address2 = fields.AddressLine2,
                Location = fields.City,
                CityId = city?.Id,
                StateId = stateId,
                CountryId = countryId,
                PinCode = fields.PostalCode,
                ContactPerson = fields.PrimaryContactName,
                Phone = fields.PrimaryPhone,
                MobileNo = fields.PrimaryPhone,
                GstNo = fields.GstNo,
                Description = fields.Notes,
                WalletBalance = 0,
                IsWalletActive = true,
                Status = "Active",
                BillingModel = fields.BillingModel,
                InventoryMode = fields.InventoryMode,
                OutletCount = fields.OutletCount,
                SupportTier = fields.SupportTier,
                TenantCode = fields.TenantCode,
                BranchCode = fields.BranchCode ?? fields.TenantCode,
                IFSSAINo = fields.FssaiNo,
                DrugLicNo = fields.DrugLic,
                LicenceExpiryDate = ParseSaDate(fields.LicExp),
                YearFrom = ParseSaDate(fields.FinYearFrom),
                YearTo = ParseSaDate(fields.FinYearTo),
                IsHeadOffice = fields.IsHeadOffice ?? false,
                ParentTenantId = fields.ParentTenantId
            };

            if (!string.IsNullOrEmpty(fields.CompType) && Enum.TryParse<CompanyType>(fields.CompType, true, out var compType))
            {
                tenant.CompanyType = compType;
                tenant.IsHeadOffice = (compType == CompanyType.HeadOffice || compType == CompanyType.Standalone);
                tenant.ParentTenantId = (compType == CompanyType.Branch) ? fields.ParentTenantId : null;
            }
            if (!string.IsNullOrEmpty(fields.StateCode) && Enum.TryParse<GstStateCode>(fields.StateCode, true, out var stateCode))
            {
                tenant.StateCode = stateCode;
            }
            if (!string.IsNullOrEmpty(fields.BusinessType) && Enum.TryParse<BusinessType>(fields.BusinessType, true, out var businessType))
            {
                tenant.BusinessType = businessType;
            }
            if (!string.IsNullOrEmpty(fields.Calendar) && Enum.TryParse<CalenderType>(fields.Calendar, true, out var calendarType))
            {
                tenant.CalanderType = calendarType;
            }
            if (!string.IsNullOrEmpty(fields.TaxType) && Enum.TryParse<TaxType>(fields.TaxType, true, out var taxType))
            {
                tenant.TaxType = taxType;
            }

            // Link Subscription Plan
            string planName = payload.Subscription?.PlanName ?? "Smart Plan";
            var plan = await _context.SubscriptionPlans
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .FirstOrDefaultAsync(p => p.PlanName.ToLower() == planName.ToLower());

            if (tenant.CompanyType == CompanyType.Branch && !string.IsNullOrEmpty(tenant.ParentTenantId))
            {
                var parentHo = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.ParentTenantId);
                if (parentHo != null)
                {
                    tenant.SubscriptionPlanId = parentHo.SubscriptionPlanId;
                    tenant.AllowedModulesJson = parentHo.AllowedModulesJson;
                }
            }
            else if (plan != null)
            {
                tenant.SubscriptionPlanId = plan.Id;
                
                // Copy default features
                var planFeatures = plan.PlanFeatures
                    .Where(pf => pf.Feature != null)
                    .Select(pf => pf.Feature.FeatureKey)
                    .ToList();
                tenant.AllowedModulesJson = JsonConvert.SerializeObject(planFeatures);
            }

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // Create admin user for tenant
            var adminUser = new ApplicationUsers
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = fields.PrimaryPhone,
                PhoneNumberConfirmed = true,
                TenantId = tenant.Id,
                TenantName = tenant.Name
            };

            var userResult = await _userManager.CreateAsync(adminUser, "EasyBill@123");
            if (userResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else
            {
                // rollback tenant creation
                _context.Tenants.Remove(tenant);
                await _context.SaveChangesAsync();
                return BadRequest(new { success = false, message = "Failed to create Admin User: " + userResult.Errors.FirstOrDefault()?.Description });
            }

            // Reload to populate SubscriptionPlan relation for serialization
            tenant.SubscriptionPlan = plan;

            return Ok(new { company = MapToSaCompany(tenant) });
        }

        [HttpPut("companies/{id}")]
        public async Task<IActionResult> UpdateCompany(string id, [FromBody] SaCompanyPayload payload)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            if (payload == null || payload.Fields == null)
            {
                return BadRequest(new { success = false, message = "Invalid payload." });
            }

            var tenant = await _context.Tenants.Include(t => t.SubscriptionPlan).FirstOrDefaultAsync(x => x.Id == id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Company not found." });
            }

            var fields = payload.Fields;

            // Lookup City if city changed
            if (!string.Equals(tenant.Location, fields.City, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(fields.City))
            {
                var city = await _context.Cities.FirstOrDefaultAsync(x => x.Name != null && x.Name.ToLower() == fields.City.ToLower());
                if (city != null)
                {
                    tenant.CityId = city.Id;
                    tenant.StateId = city.StateId;
                    tenant.CountryId = city.CountryId;
                }
            }

            tenant.Name = fields.DisplayName ?? fields.LegalName;
            tenant.Address1 = fields.AddressLine1;
            tenant.Address2 = fields.AddressLine2;
            tenant.Location = fields.City;
            tenant.PinCode = fields.PostalCode;
            tenant.ContactPerson = fields.PrimaryContactName;
            tenant.Phone = fields.PrimaryPhone;
            tenant.MobileNo = fields.PrimaryPhone;
            tenant.GstNo = fields.GstNo;
            tenant.Description = fields.Notes;
            tenant.BillingModel = fields.BillingModel;
            tenant.InventoryMode = fields.InventoryMode;
            tenant.OutletCount = fields.OutletCount;
            tenant.SupportTier = fields.SupportTier;
            tenant.TenantCode = fields.TenantCode;
            tenant.BranchCode = fields.BranchCode ?? fields.TenantCode;

            tenant.IFSSAINo = fields.FssaiNo;
            tenant.DrugLicNo = fields.DrugLic;
            tenant.LicenceExpiryDate = ParseSaDate(fields.LicExp);
            tenant.YearFrom = ParseSaDate(fields.FinYearFrom);
            tenant.YearTo = ParseSaDate(fields.FinYearTo);
            
            if (fields.IsHeadOffice.HasValue)
            {
                tenant.IsHeadOffice = fields.IsHeadOffice.Value;
            }
            if (fields.ParentTenantId != null)
            {
                tenant.ParentTenantId = fields.ParentTenantId;
            }

            if (!string.IsNullOrEmpty(fields.CompType) && Enum.TryParse<CompanyType>(fields.CompType, true, out var compType))
            {
                tenant.CompanyType = compType;
                tenant.IsHeadOffice = (compType == CompanyType.HeadOffice || compType == CompanyType.Standalone);
                tenant.ParentTenantId = (compType == CompanyType.Branch) ? fields.ParentTenantId : null;
            }
            if (!string.IsNullOrEmpty(fields.StateCode) && Enum.TryParse<GstStateCode>(fields.StateCode, true, out var stateCode))
            {
                tenant.StateCode = stateCode;
            }
            if (!string.IsNullOrEmpty(fields.BusinessType) && Enum.TryParse<BusinessType>(fields.BusinessType, true, out var businessType))
            {
                tenant.BusinessType = businessType;
            }
            if (!string.IsNullOrEmpty(fields.Calendar) && Enum.TryParse<CalenderType>(fields.Calendar, true, out var calendarType))
            {
                tenant.CalanderType = calendarType;
            }
            if (!string.IsNullOrEmpty(fields.TaxType) && Enum.TryParse<TaxType>(fields.TaxType, true, out var taxType))
            {
                tenant.TaxType = taxType;
            }

            if (tenant.CompanyType == CompanyType.Branch && !string.IsNullOrEmpty(tenant.ParentTenantId))
            {
                var parentHo = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.ParentTenantId);
                if (parentHo != null)
                {
                    tenant.SubscriptionPlanId = parentHo.SubscriptionPlanId;
                    tenant.AllowedModulesJson = parentHo.AllowedModulesJson;
                }
            }
            else
            {
                // Update Subscription Plan if changed
                if (payload.Subscription != null && !string.IsNullOrEmpty(payload.Subscription.PlanName))
                {
                    var newPlanName = payload.Subscription.PlanName;
                    if (tenant.SubscriptionPlan == null || !string.Equals(tenant.SubscriptionPlan.PlanName, newPlanName, StringComparison.OrdinalIgnoreCase))
                    {
                        var plan = await _context.SubscriptionPlans
                            .Include(p => p.PlanFeatures)
                                .ThenInclude(pf => pf.Feature)
                            .FirstOrDefaultAsync(p => p.PlanName.ToLower() == newPlanName.ToLower());
                        if (plan != null)
                        {
                            tenant.SubscriptionPlanId = plan.Id;
                            tenant.SubscriptionPlan = plan;
                            
                            // Copy default plan features
                            var planFeatures = plan.PlanFeatures
                                .Where(pf => pf.Feature != null)
                                .Select(pf => pf.Feature.FeatureKey)
                                .ToList();
                            tenant.AllowedModulesJson = JsonConvert.SerializeObject(planFeatures);
                        }
                    }
                }

                if (tenant.IsHeadOffice)
                {
                    var childBranches = await _context.Tenants.Where(t => t.ParentTenantId == tenant.Id).ToListAsync();
                    foreach (var branch in childBranches)
                    {
                        branch.SubscriptionPlanId = tenant.SubscriptionPlanId;
                        branch.AllowedModulesJson = tenant.AllowedModulesJson;
                        _context.Tenants.Update(branch);
                    }
                }
            }

            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();

            return Ok(new { company = MapToSaCompany(tenant) });
        }

        [HttpDelete("companies/{id}")]
        public async Task<IActionResult> DeleteCompany(string id)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Company not found." });
            }

            // 1. Delete associated TenantWalletHistory records first to avoid foreign key violations
            var historyRecords = await _context.TenantWalletHistories.Where(x => x.TenantId == id).ToListAsync();
            if (historyRecords.Any())
            {
                _context.TenantWalletHistories.RemoveRange(historyRecords);
                await _context.SaveChangesAsync();
            }

            // 2. Delete associated users for this tenant
            var users = await _userManager.Users.Where(x => x.TenantId == id).ToListAsync();
            foreach (var user in users)
            {
                await _userManager.DeleteAsync(user);
            }

            // 3. Perform safety delete of the tenant using repository
            await _tenantRepository.Delete(tenant);

            return Ok(new { success = true, message = "Company deleted successfully." });
        }

        [HttpPost("companies/{id}/recharge")]
        public async Task<IActionResult> RechargeCompany(string id, [FromBody] RechargeRequest request)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            if (request == null || request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Amount must be greater than zero." });
            }

            var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Company not found." });
            }

            tenant.WalletBalance += request.Amount;
            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();

            // Log ledger transaction in TenantWalletHistory
            await LogTenantWalletHistoryAsync(tenant.Id, request.Amount, isDebit: false, "Manual", request.Remarks ?? "Wallet Recharged via SA Control Plane", tenant.WalletBalance);

            return Ok(new { company = MapToSaCompany(tenant) });
        }

        [HttpPost("companies/{id}/user-limits")]
        public async Task<IActionResult> PurchaseUserLimits(string id, [FromBody] UserLimitsRequest request)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            if (request == null || request.ExtraUsersBlock < 0 || request.ExtraUsersBlock % 5 != 0)
            {
                return BadRequest(new { success = false, message = "Extra user block must be a multiple of 5." });
            }

            var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Company not found." });
            }

            // Enforce guard clauses
            var hasPendingSa = await _context.TenantWalletHistories.AnyAsync(h => h.TenantId == id && h.ServiceType == "PendingSaUpgrade");
            if (hasPendingSa)
            {
                return BadRequest(new { success = false, message = "A pending upgrade is already awaiting Admin redemption." });
            }

            var hasPendingAdmin = await _context.TenantWalletHistories.AnyAsync(h => h.TenantId == id && h.ServiceType == "PendingLimitUpgrade");
            if (hasPendingAdmin)
            {
                return BadRequest(new { success = false, message = "A pending request from the Admin is awaiting your approval. Please approve or reject it first." });
            }

            // Check current active paid users
            bool currentActive = tenant.ExtraUsersExpiryDate.HasValue && tenant.ExtraUsersExpiryDate.Value > DateTime.Now;
            int maxPaidUsers = currentActive ? await GetMaxPaidExtraUsersInActivePeriodAsync(tenant.Id, tenant.ExtraUsersExpiryDate.Value) : 0;
            if (maxPaidUsers == 0 && currentActive)
            {
                maxPaidUsers = tenant.ExtraUsers;
            }

            // Verify license is active
            if (!tenant.LicenceExpiryDate.HasValue || tenant.LicenceExpiryDate.Value <= DateTime.Now)
            {
                return BadRequest(new { success = false, message = "The company license is expired or not active. Please renew/activate the license before purchasing extra staff slots." });
            }

            double totalDays = (tenant.LicenceExpiryDate.Value - DateTime.Now).TotalDays;
            int remainingMonths = (int)Math.Max(1, Math.Round(totalDays / 30.4375));

            int targetUsers = request.ExtraUsersBlock;
            decimal monthlyCost = 0;

            if (targetUsers > maxPaidUsers)
            {
                // Charge only for the additional users:
                decimal newPrice = GetPriceForExtraUsers(targetUsers);
                decimal oldPrice = GetPriceForExtraUsers(maxPaidUsers);
                monthlyCost = newPrice - oldPrice;
            }
            else
            {
                // Downgrade, same count, or restore within active period is free
                monthlyCost = 0;
            }

            decimal cost = monthlyCost * remainingMonths;

            if (cost > 0 && tenant.WalletBalance < cost)
            {
                return BadRequest(new { success = false, message = $"Insufficient wallet balance. Upgrading by {targetUsers - maxPaidUsers} extra users for {remainingMonths} months requires INR {cost}. Current balance: INR {tenant.WalletBalance}" });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (cost > 0)
                    {
                        tenant.WalletBalance -= cost;
                        await LogTenantWalletHistoryAsync(tenant.Id, cost, isDebit: true, "PendingSaUpgrade", $"SA Upgrade Demand: {targetUsers} Extra Users (Pending Admin Redemption) for {remainingMonths} months", tenant.WalletBalance);
                    }
                    else
                    {
                        await LogTenantWalletHistoryAsync(tenant.Id, 0, isDebit: true, "PendingSaUpgrade", $"SA Upgrade Demand: {targetUsers} Extra Users (No Charge) (Pending Admin Redemption)", tenant.WalletBalance);
                    }

                    // Do NOT update tenant.ExtraUsers and tenant.ExtraUsersExpiryDate immediately.
                    // The Admin must redeem it.
                    _context.Tenants.Update(tenant);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { success = false, message = "Database error: " + ex.Message });
                }
            }

            return Ok(new { company = MapToSaCompany(tenant) });
        }

        [HttpPost("companies/{id}/approve-user-limits")]
        public async Task<IActionResult> ApproveUserLimits(string id)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Company not found." });
            }

            var tx = await _context.TenantWalletHistories
                .FirstOrDefaultAsync(h => h.TenantId == id && h.ServiceType == "PendingLimitUpgrade");
            if (tx == null)
            {
                return BadRequest(new { success = false, message = "No pending limit upgrade request found for this company." });
            }

            int count = ParseExtraUsersFromRemarks(tx.Remarks);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    tx.ServiceType = "LimitUpgrade";
                    tx.Remarks = $"Purchased {count} Extra Users (Approved by SA) via {tx.PaymentMode}";
                    _context.TenantWalletHistories.Update(tx);

                    tenant.ExtraUsers = count;
                    tenant.ExtraUsersExpiryDate = tenant.LicenceExpiryDate;

                    _context.Tenants.Update(tenant);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { success = false, message = "Database error: " + ex.Message });
                }
            }

            return Ok(new { success = true, company = MapToSaCompany(tenant) });
        }

        [HttpPost("companies/{id}/reject-user-limits")]
        public async Task<IActionResult> RejectUserLimits(string id)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Company not found." });
            }

            var tx = await _context.TenantWalletHistories
                .FirstOrDefaultAsync(h => h.TenantId == id && h.ServiceType == "PendingLimitUpgrade");
            if (tx == null)
            {
                return BadRequest(new { success = false, message = "No pending limit upgrade request found for this company." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (string.Equals(tx.PaymentMode, "Wallet", StringComparison.OrdinalIgnoreCase) && tx.Debit > 0)
                    {
                        tenant.WalletBalance += tx.Debit;
                        
                        var refundEntry = new TenantWalletHistory
                        {
                            TenantId = tenant.Id,
                            TransactionDateTime = DateTime.Now,
                            Credit = tx.Debit,
                            Debit = 0,
                            PaymentMode = "Wallet",
                            ReferenceNo = $"RFD-{tx.ReferenceNo}",
                            ServiceType = "Wallet Recharge",
                            ServiceCharge = 0,
                            Remarks = $"Refund for rejected user limit upgrade request ({tx.Remarks})",
                            ClosingBalance = tenant.WalletBalance
                        };
                        _context.TenantWalletHistories.Add(refundEntry);
                    }

                    tx.ServiceType = "RejectedLimitUpgrade";
                    tx.Remarks = $"Rejected by SA: {tx.Remarks}";
                    _context.TenantWalletHistories.Update(tx);

                    _context.Tenants.Update(tenant);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { success = false, message = "Database error: " + ex.Message });
                }
            }

            return Ok(new { success = true, company = MapToSaCompany(tenant) });
        }

        [HttpPost("companies/{id}/rollback")]
        public async Task<IActionResult> PurchaseRollbackPackage(string id, [FromBody] RollbackRequest request)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            if (request == null)
            {
                return BadRequest(new { success = false, message = "Invalid request." });
            }

            var duration = request.DurationMonths;
            decimal price = 0;
            if (duration == 3) price = 1500;
            else if (duration == 6) price = 2800;
            else if (duration == 9) price = 4000;
            else if (duration == 12) price = 5000;
            else
            {
                return BadRequest(new { success = false, message = "Invalid duration. Choose 3, 6, 9 or 12 months." });
            }

            var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Company not found." });
            }

            if (tenant.WalletBalance < price)
            {
                return BadRequest(new { success = false, message = $"Insufficient wallet balance. Activating {duration}-month rollback requires INR {price}. Current balance: INR {tenant.WalletBalance}" });
            }

            tenant.WalletBalance -= price;
            tenant.RollbackDurationMonths = duration;
            tenant.RollbackExpiryDate = DateTime.Now.AddMonths(duration);

            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();

            await LogTenantWalletHistoryAsync(tenant.Id, price, isDebit: true, "RollbackSetup", $"Activated {duration}-Month Data Rollback Package", tenant.WalletBalance);

            return Ok(new { company = MapToSaCompany(tenant) });
        }

        [HttpPost("companies/{id}/module-access")]
        public async Task<IActionResult> UpdateModuleAccess(string id, [FromBody] ModuleAccessRequest request)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            if (request == null || request.AllowedModules == null)
            {
                return BadRequest(new { success = false, message = "Allowed modules list is required." });
            }

            var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Company not found." });
            }

            // Map SuperAdmin module names back to C# feature keys
            var featureKeys = new List<string> { "Master", "Master.ItemMaster", "Master.Ledger", "Master.Area", "Master.Staff", "Master.Settings", "GlobalSettings", "Wallet" }; // Always include basic features

            if (request.AllowedModules.Contains("Sales Module", StringComparer.OrdinalIgnoreCase))
            {
                featureKeys.AddRange(new[] { "Sales", "Sales.Create", "Sales.Index", "Sales.HoldList", "Sales.Order", "Sales.Return", "Sales.StockIssue" });
            }
            if (request.AllowedModules.Contains("Purchase Module", StringComparer.OrdinalIgnoreCase))
            {
                featureKeys.AddRange(new[] { "Purchase", "Purchase.Entry", "Purchase.Return", "Purchase.StockReceive", "Purchase.Challan", "PurchaseOrder", "PurchaseOrder.Manual", "PurchaseOrder.AI", "PaymentVoucher", "PaymentVoucher.Entry", "PaymentVoucher.Category", "ReceiveVoucher" });
            }
            if (request.AllowedModules.Contains("Inventory Module", StringComparer.OrdinalIgnoreCase))
            {
                featureKeys.AddRange(new[] { "Inventory", "Inventory.CurrentStock", "Inventory.NearExpiry", "Inventory.Expired", "Inventory.MinimumStock", "Inventory.DumpStock", "Inventory.FastMoving", "Inventory.SlowMoving", "Inventory.SupplierWise", "Inventory.CompanyItemWise", "Inventory.OverStock", "Inventory.BatchWise", "Inventory.StockSales" });
            }
            if (request.AllowedModules.Contains("Reports Module", StringComparer.OrdinalIgnoreCase))
            {
                featureKeys.AddRange(new[] { "Reports", "Reports.Sales", "Reports.SalesReturn", "Reports.StockIssue", "Reports.StockReceive", "Reports.Purchase", "Reports.PurchaseReturn", "GSTR", "GSTR.GSTR1", "GSTR.GSTR2", "GSTR.GSTR3" });
            }

            tenant.AllowedModulesJson = JsonConvert.SerializeObject(featureKeys.Distinct().ToList());
            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();

            return Ok(new { company = MapToSaCompany(tenant) });
        }

        // ================= PLANS & FEATURES CONFIGURATION API =================

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            var plans = await _context.SubscriptionPlans
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .ToListAsync();

            var result = plans.Select(p => new
            {
                id = p.Id,
                planName = p.PlanName,
                monthlyPrice = p.MonthlyPrice,
                yearlyPrice = p.YearlyPrice,
                dailyCustomerLimit = p.DailyCustomerLimit,
                maxDesktopLogins = p.MaxDesktopLogins,
                maxMobileLogins = p.MaxMobileLogins,
                isActive = p.IsActive,
                features = p.PlanFeatures.Where(pf => pf.Feature != null).Select(pf => pf.FeatureId).ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpPost("plans")]
        public async Task<IActionResult> CreatePlan([FromBody] PlanPayload payload)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });
            if (payload == null || string.IsNullOrEmpty(payload.PlanName)) return BadRequest("Invalid plan details.");

            var plan = new SubscriptionPlan
            {
                PlanName = payload.PlanName,
                MonthlyPrice = payload.MonthlyPrice,
                YearlyPrice = payload.YearlyPrice,
                DailyCustomerLimit = payload.DailyCustomerLimit,
                MaxDesktopLogins = payload.MaxDesktopLogins,
                MaxMobileLogins = payload.MaxMobileLogins,
                IsActive = payload.IsActive
            };

            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            if (payload.SelectedFeatures != null && payload.SelectedFeatures.Any())
            {
                foreach (var featId in payload.SelectedFeatures)
                {
                    _context.PlanFeatures.Add(new PlanFeature { PlanId = plan.Id, FeatureId = featId });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true, planId = plan.Id });
        }

        [HttpPut("plans/{id}")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] PlanPayload payload)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });
            if (payload == null || string.IsNullOrEmpty(payload.PlanName)) return BadRequest("Invalid plan details.");

            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan == null) return NotFound("Plan not found.");

            plan.PlanName = payload.PlanName;
            plan.MonthlyPrice = payload.MonthlyPrice;
            plan.YearlyPrice = payload.YearlyPrice;
            plan.DailyCustomerLimit = payload.DailyCustomerLimit;
            plan.MaxDesktopLogins = payload.MaxDesktopLogins;
            plan.MaxMobileLogins = payload.MaxMobileLogins;
            plan.IsActive = payload.IsActive;

            _context.SubscriptionPlans.Update(plan);

            var existing = _context.PlanFeatures.Where(pf => pf.PlanId == id);
            _context.PlanFeatures.RemoveRange(existing);

            if (payload.SelectedFeatures != null && payload.SelectedFeatures.Any())
            {
                foreach (var featId in payload.SelectedFeatures)
                {
                    _context.PlanFeatures.Add(new PlanFeature { PlanId = plan.Id, FeatureId = featId });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("plans/{id}")]
        public async Task<IActionResult> DeletePlan(int id)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan == null) return NotFound("Plan not found.");

            var existing = _context.PlanFeatures.Where(pf => pf.PlanId == id);
            _context.PlanFeatures.RemoveRange(existing);

            _context.SubscriptionPlans.Remove(plan);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet("features")]
        public async Task<IActionResult> GetFeatures()
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            var list = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();
            var parents = list.Where(f => f.ParentFeatureId == null).Select(p => new
            {
                id = p.Id,
                featureKey = p.FeatureKey,
                displayName = p.DisplayName,
                children = list.Where(c => c.ParentFeatureId == p.Id).Select(c => new
                {
                    id = c.Id,
                    featureKey = c.FeatureKey,
                    displayName = c.DisplayName
                }).ToList()
            }).ToList();

            return Ok(parents);
        }

        // ================= PRIVATE HELPERS & MAPS =================

        private object MapToSaCompany(Tenant tenant)
        {
            var allowedModules = new List<string>();
            if (!string.IsNullOrEmpty(tenant.AllowedModulesJson))
            {
                try
                {
                    var rawModules = JsonConvert.DeserializeObject<List<string>>(tenant.AllowedModulesJson) ?? new List<string>();
                    
                    // If the list already contains SA module names, preserve them
                    foreach (var m in rawModules)
                    {
                        if (m == "Sales Module" || m == "Purchase Module" || m == "Inventory Module" || m == "Reports Module")
                        {
                            if (!allowedModules.Contains(m)) allowedModules.Add(m);
                        }
                    }

                    // Map C# feature keys to SA module names
                    if (rawModules.Any(m => m.StartsWith("Sales", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!allowedModules.Contains("Sales Module")) allowedModules.Add("Sales Module");
                    }
                    if (rawModules.Any(m => m.StartsWith("Purchase", StringComparison.OrdinalIgnoreCase) || 
                                            m.StartsWith("PaymentVoucher", StringComparison.OrdinalIgnoreCase) || 
                                            m.StartsWith("ReceiveVoucher", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!allowedModules.Contains("Purchase Module")) allowedModules.Add("Purchase Module");
                    }
                    if (rawModules.Any(m => m.StartsWith("Inventory", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!allowedModules.Contains("Inventory Module")) allowedModules.Add("Inventory Module");
                    }
                    if (rawModules.Any(m => m.StartsWith("Reports", StringComparison.OrdinalIgnoreCase) || 
                                            m.StartsWith("GSTR", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!allowedModules.Contains("Reports Module")) allowedModules.Add("Reports Module");
                    }
                }
                catch { }
            }

            var defaultModules = new List<string> { "Sales Module", "Purchase Module", "Inventory Module", "Reports Module" };
            if (allowedModules.Count == 0)
            {
                allowedModules = defaultModules;
            }

            var planName = tenant.SubscriptionPlan != null ? tenant.SubscriptionPlan.PlanName : "Smart Plan";
            var planPrice = tenant.SubscriptionPlan != null ? tenant.SubscriptionPlan.MonthlyPrice : 300;

            return new
            {
                id = tenant.Id,
                projectId = "easybill",
                projectName = "EasyBill",
                status = tenant.Status ?? (tenant.IsWalletActive ? "Active" : "Inactive"),
                createdAt = DateTime.UtcNow.AddDays(-30).ToString("o"),
                updatedAt = DateTime.UtcNow.ToString("o"),
                companyCode = tenant.BranchCode ?? tenant.TenantCode ?? "EB-" + tenant.Id.Substring(0, 4).ToUpper(),
                companyName = tenant.Name,
                primaryContactName = tenant.ContactPerson ?? "Admin",
                primaryEmail = tenant.Email,
                city = tenant.Location ?? "Gujarat",
                state = tenant.State != null ? tenant.State.Name : "Gujarat",
                walletBalance = tenant.WalletBalance,
                subscriptionPlan = planName,
                auditLocked = tenant.IsAuditLocked,
                supportPriority = tenant.SupportTier ?? "Standard",
                fields = new
                {
                    tenantCode = tenant.BranchCode ?? tenant.TenantCode ?? "EB-" + tenant.Id.Substring(0, 4).ToUpper(),
                    legalName = tenant.Name,
                    displayName = tenant.Name,
                    companyType = tenant.CompanyType.ToString(),
                    registrationNumber = tenant.IFSSAINo ?? "N/A",
                    onboardingDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"),
                    primaryContactName = tenant.ContactPerson ?? "Admin",
                    primaryEmail = tenant.Email,
                    primaryPhone = tenant.MobileNo ?? tenant.Phone ?? "",
                    supportEmail = tenant.Email,
                    supportPhone = tenant.MobileNo ?? tenant.Phone ?? "",
                    accountManager = "System Admin",
                    addressLine1 = tenant.Address1 ?? "",
                    addressLine2 = tenant.Address2 ?? "",
                    city = tenant.Location ?? "",
                    state = tenant.State != null ? tenant.State.Name : "Gujarat",
                    country = tenant.Country != null ? tenant.Country.Name : "India",
                    postalCode = tenant.PinCode ?? "",
                    notes = tenant.Description ?? "",
                    gstNo = tenant.GstNo ?? "",
                    billingModel = tenant.BillingModel ?? "Store Wise",
                    inventoryMode = tenant.InventoryMode ?? "Multi Warehouse",
                    outletCount = tenant.OutletCount > 0 ? tenant.OutletCount : 1,
                    supportTier = tenant.SupportTier ?? "Standard",
                    compType = tenant.CompanyType.ToString(),
                    stateCode = tenant.StateCode?.ToString() ?? "",
                    businessType = tenant.BusinessType.ToString(),
                    calendar = tenant.CalanderType.ToString(),
                    finYearFrom = tenant.YearFrom?.ToString("yyyy-MM-dd") ?? "",
                    finYearTo = tenant.YearTo?.ToString("yyyy-MM-dd") ?? "",
                    taxType = tenant.TaxType.ToString(),
                    fssaiNo = tenant.IFSSAINo ?? "",
                    drugLic = tenant.DrugLicNo ?? "",
                    licExp = tenant.LicenceExpiryDate?.ToString("yyyy-MM-dd") ?? "",
                    branchCode = tenant.BranchCode ?? ""
                },
                subscription = new
                {
                    planName = planName,
                    status = "Active",
                    billingCycle = "Monthly",
                    amount = planPrice,
                    nextBillingDate = DateTime.UtcNow.AddDays(15).ToString("yyyy-MM-dd"),
                    autoRenew = true,
                    includedUsers = (tenant.SubscriptionPlan?.MaxDesktopLogins ?? 0) + tenant.ExtraUsers,
                    rollbackPackage = tenant.RollbackDurationMonths > 0 ? new
                    {
                        durationMonths = tenant.RollbackDurationMonths,
                        expiryDate = tenant.RollbackExpiryDate?.ToString("yyyy-MM-dd") ?? "",
                        active = tenant.RollbackExpiryDate > DateTime.Now
                    } : null
                },
                wallet = new
                {
                    availableBalance = tenant.WalletBalance,
                    creditLimit = 25000,
                    minimumOperationalBalance = 10000,
                    currencyCode = "INR",
                    allowNegativeBalance = tenant.AllowNegativeBalance,
                    autoRechargeEnabled = false,
                    autoRechargeThreshold = 0,
                    autoRechargeAmount = 0,
                    lastTopUpOn = DateTime.UtcNow.AddDays(-5).ToString("o"),
                    lastChargeOn = DateTime.UtcNow.AddDays(-1).ToString("o"),
                    extraUsers = tenant.ExtraUsers,
                    extraUsersExpiryDate = tenant.ExtraUsersExpiryDate?.ToString("yyyy-MM-dd") ?? "",
                    pendingUpgradeDemand = GetPendingUpgradeDemand(tenant.Id),
                    pendingSaUpgrade = GetPendingSaUpgrade(tenant.Id),
                    transactions = GetTenantTransactions(tenant.Id)
                },
                permissions = new
                {
                    allowedModules = allowedModules,
                    canManageBilling = true,
                    allowSupportImpersonation = true,
                    allowReportExports = true,
                    enforceMfa = false
                },
                audit = new[] {
                    new {
                        id = Guid.NewGuid().ToString(),
                        timestamp = DateTime.UtcNow.ToString("o"),
                        actor = "SuperAdmin",
                        action = "System Checked",
                        detail = "Accessed via SA Control Plane"
                    }
                }
            };
        }

        private object GetTenantTransactions(string tenantId)
        {
            var list = _context.TenantWalletHistories
                .Where(x => x.TenantId == tenantId)
                .OrderByDescending(x => x.TransactionDateTime)
                .Take(50)
                .ToList();

            return list.Select(tx => new
            {
                id = tx.Id.ToString(),
                timestamp = tx.TransactionDateTime.ToString("o"),
                type = tx.Credit > 0 ? "Credit" : "Debit",
                amount = tx.Credit > 0 ? tx.Credit : tx.Debit,
                description = tx.Remarks ?? "N/A"
            }).ToList();
        }

        private async Task LogTenantWalletHistoryAsync(string tenantId, decimal amount, bool isDebit, string serviceType, string remarks, decimal closingBalance)
        {
            var historyRepo = _unitOfWork.GetRepository<TenantWalletHistory>();
            historyRepo.Add(new TenantWalletHistory
            {
                TenantId = tenantId,
                TransactionDateTime = DateTime.Now,
                Credit = isDebit ? 0m : amount,
                Debit = isDebit ? amount : 0m,
                PaymentMode = "Manual",
                ReferenceNo = "SA-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                GatewayTransactionId = null,
                ReferenceSaleId = null,
                ServiceType = serviceType,
                ServiceCharge = 0m,
                Remarks = remarks,
                ClosingBalance = closingBalance
            });

            using var transaction = historyRepo.BeginTransaction();
            await historyRepo.SaveChangesAsync();
            transaction.Commit();
        }

        [HttpPost("companies/{id}/policies")]
        public async Task<IActionResult> UpdatePolicies(string id, [FromBody] PoliciesRequest request)
        {
            if (!IsAuthorized()) return Unauthorized(new { success = false, message = "Unauthorized key." });

            if (request == null)
            {
                return BadRequest(new { success = false, message = "Invalid request." });
            }

            var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Company not found." });
            }

            if (request.LoginEnabled.HasValue)
            {
                tenant.IsWalletActive = request.LoginEnabled.Value;
                tenant.Status = request.LoginEnabled.Value ? "Active" : "Suspended";
            }

            if (request.AuditLocked.HasValue)
            {
                tenant.IsAuditLocked = request.AuditLocked.Value;
            }

            if (request.AllowNegativeBalance.HasValue)
            {
                tenant.AllowNegativeBalance = request.AllowNegativeBalance.Value;
            }

            if (!string.IsNullOrEmpty(request.SupportPriority))
            {
                tenant.SupportTier = request.SupportPriority;
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                tenant.Status = request.Status;
                if (request.Status == "Active")
                {
                    tenant.IsWalletActive = true;
                }
                else
                {
                    tenant.IsWalletActive = false;
                }
            }

            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();

            return Ok(new { company = MapToSaCompany(tenant) });
        }

        private decimal GetPriceForExtraUsers(int count)
        {
            if (count <= 0) return 0;
            
            // Standard bundle prices:
            if (count == 5) return 500;
            if (count == 10) return 900;
            if (count == 15) return 1300;
            if (count == 20) return 1600;
            
            // Multiple of 5 pricing combination
            int twenties = count / 20;
            int remainder = count % 20;
            
            decimal price = twenties * 1600;
            price += remainder switch
            {
                5 => 500,
                10 => 900,
                15 => 1300,
                _ => (remainder / 5) * 500
            };
            
            return price;
        }

        private int ParseExtraUsersFromRemarks(string? remarks)
        {
            if (string.IsNullOrEmpty(remarks)) return 0;

            // 1. "Purchased X Extra Users"
            var m1 = System.Text.RegularExpressions.Regex.Match(remarks, @"Purchased\s+(\d+)\s+Extra\s+Users", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m1.Success) return int.Parse(m1.Groups[1].Value);

            // 2. "Adjusted Extra Users [count] to X"
            var m2 = System.Text.RegularExpressions.Regex.Match(remarks, @"Adjusted\s+Extra\s+Users(?:\s+count)?\s+to\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m2.Success) return int.Parse(m2.Groups[1].Value);

            // 3. "to X (+..." or "to X (No Charge)" or "to X" in "extra users from Y to X"
            var m3 = System.Text.RegularExpressions.Regex.Match(remarks, @"extra\s+users\s+from\s+\d+\s+to\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m3.Success) return int.Parse(m3.Groups[1].Value);

            return 0;
        }

        private async Task<int> GetMaxPaidExtraUsersInActivePeriodAsync(string tenantId, DateTime expiryDate)
        {
            DateTime periodStart = expiryDate.AddMonths(-1);
            var txs = await _context.TenantWalletHistories
                .Where(h => h.TenantId == tenantId && h.ServiceType == "LimitUpgrade" && h.TransactionDateTime >= periodStart)
                .ToListAsync();
            
            int maxPaid = 0;
            foreach (var tx in txs)
            {
                int countFromRemarks = ParseExtraUsersFromRemarks(tx.Remarks);
                if (countFromRemarks > maxPaid)
                {
                    maxPaid = countFromRemarks;
                }
            }
            return maxPaid;
        }

        private object? GetPendingUpgradeDemand(string tenantId)
        {
            var tx = _context.TenantWalletHistories
                .FirstOrDefault(h => h.TenantId == tenantId && h.ServiceType == "PendingLimitUpgrade");
            if (tx == null) return null;

            int count = ParseExtraUsersFromRemarks(tx.Remarks);
            return new
            {
                id = tx.Id,
                count = count,
                cost = tx.Debit,
                remarks = tx.Remarks
            };
        }

        private object? GetPendingSaUpgrade(string tenantId)
        {
            var tx = _context.TenantWalletHistories
                .FirstOrDefault(h => h.TenantId == tenantId && h.ServiceType == "PendingSaUpgrade");
            if (tx == null) return null;

            int count = ParseExtraUsersFromRemarks(tx.Remarks);
            return new
            {
                id = tx.Id,
                count = count,
                cost = tx.Debit,
                remarks = tx.Remarks
            };
        }
        [HttpPost("merge-to-ho")]
        public async Task<IActionResult> MergeToHeadOfficeAsync([FromBody] MergeTenantsRequest request)
        {
            if (!IsAuthorized()) return Unauthorized("Invalid API Key");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var hoTenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == request.HoTenantId);
            if (hoTenant == null || !hoTenant.IsHeadOffice)
                return BadRequest("Invalid Head Office Tenant. Make sure the target is marked as Head Office or Standalone.");

            var branches = await _context.Tenants.Where(t => request.BranchTenantIds.Contains(t.Id)).ToListAsync();
            if (!branches.Any())
                return BadRequest("No valid branch tenants found.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Update Branches
                foreach (var branch in branches)
                {
                    branch.ParentTenantId = hoTenant.Id;
                    branch.CompanyType = AOne.Utility.Enums.CompanyType.Branch;
                    branch.IsHeadOffice = false;
                }

                // 2. Consolidate CategoryMaster
                var branchCategoryMasters = await _context.CategoryMasters
                    .Where(c => request.BranchTenantIds.Contains(c.TenantId))
                    .ToListAsync();
                
                var newCategories = new List<CategoryMaster>();
                foreach (var group in branchCategoryMasters.GroupBy(c => c.CategoryName.Trim().ToLower()))
                {
                    var cat = group.First();
                    var hoCat = new CategoryMaster
                    {
                        CategoryName = cat.CategoryName,
                        TenantId = hoTenant.Id
                    };
                    newCategories.Add(hoCat);
                    // Since there's no IsActive, we can't soft delete. We'll skip modifying the old branch categories for now.
                }
                if (newCategories.Any()) await _context.CategoryMasters.AddRangeAsync(newCategories);

                // 3. Consolidate ItemMaster
                var branchItems = await _context.ItemMasters
                    .Where(i => request.BranchTenantIds.Contains(i.TenantId) && i.IsActive)
                    .ToListAsync();

                var newItems = new List<ItemMaster>();
                foreach (var group in branchItems.GroupBy(i => string.IsNullOrWhiteSpace(i.Barcode) ? i.Name.Trim().ToLower() : i.Barcode.Trim()))
                {
                    var item = group.First();
                    var hoItem = new ItemMaster
                    {
                        Name = item.Name,
                        Code = item.Code,
                        Barcode = item.Barcode,
                        Unit1 = item.Unit1,
                        Unit2 = item.Unit2,
                        Packing = item.Packing,
                        Mrp = item.Mrp,
                        SalesRate1 = item.SalesRate1,
                        SalesRate2 = item.SalesRate2,
                        MinimumQty = item.MinimumQty,
                        MaximumQty = item.MaximumQty,
                        IsActive = true,
                        TenantId = hoTenant.Id
                    };
                    newItems.Add(hoItem);

                    foreach (var oldItem in group)
                    {
                        oldItem.IsActive = false;
                    }
                }
                
                if (newItems.Any()) await _context.ItemMasters.AddRangeAsync(newItems);
                
                await _context.SaveChangesAsync();

                // Post-Save ID Mapping for ParentItemId
                foreach(var hoItem in newItems)
                {
                    var matchingOldItems = branchItems.Where(i => 
                        (string.IsNullOrWhiteSpace(i.Barcode) ? i.Name.Trim().ToLower() : i.Barcode.Trim()) == 
                        (string.IsNullOrWhiteSpace(hoItem.Barcode) ? hoItem.Name.Trim().ToLower() : hoItem.Barcode.Trim())
                    );
                    foreach(var oldItem in matchingOldItems)
                    {
                        oldItem.ParentItemId = hoItem.Id;
                    }
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { Message = "Tenants successfully merged to Head Office. Consolidated " + newItems.Count + " unique items." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal server error during consolidation: {ex.Message}");
            }
        }
    }
}
