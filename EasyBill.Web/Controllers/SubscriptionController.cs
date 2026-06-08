using AOne.DataAccess.Data;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EasyBill.UI.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantRepository _tenantRepository;

        public SubscriptionController(
            ApplicationDbContext context,
            ITenantRepository tenantRepository)
        {
            _context = context;
            _tenantRepository = tenantRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetUpgradePlans()
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Json(new { success = false, message = "Tenant session not found." });
                }

                var tenant = await _tenantRepository.GetById(tenantId);
                if (tenant == null)
                {
                    return Json(new { success = false, message = "Tenant not found." });
                }

                decimal currentPlanPrice = 0;
                if (tenant.SubscriptionPlan != null)
                {
                    currentPlanPrice = tenant.SubscriptionPlan.MonthlyPrice;
                }

                // Get active plans higher than current plan price
                var plans = await _context.SubscriptionPlans
                    .Include(p => p.PlanFeatures)
                        .ThenInclude(pf => pf.Feature)
                    .Where(p => p.IsActive && p.MonthlyPrice > currentPlanPrice)
                    .OrderBy(p => p.MonthlyPrice)
                    .ToListAsync();

                var currentPlanInfo = tenant.SubscriptionPlan != null ? new
                {
                    id = tenant.SubscriptionPlan.Id,
                    planName = tenant.SubscriptionPlan.PlanName,
                    monthlyPrice = tenant.SubscriptionPlan.MonthlyPrice,
                    yearlyPrice = tenant.SubscriptionPlan.YearlyPrice
                } : null;

                var plansData = plans.Select(p => new
                {
                    id = p.Id,
                    planName = p.PlanName,
                    monthlyPrice = p.MonthlyPrice,
                    yearlyPrice = p.YearlyPrice,
                    dailyCustomerLimit = p.DailyCustomerLimit,
                    maxDesktopLogins = p.MaxDesktopLogins,
                    maxMobileLogins = p.MaxMobileLogins,
                    features = p.PlanFeatures?
                        .Where(pf => pf.Feature != null && pf.Feature.IsActive)
                        .Select(pf => pf.Feature.DisplayName)
                        .ToList() ?? new List<string>()
                }).ToList();

                return Json(new
                {
                    success = true,
                    walletBalance = tenant.WalletBalance,
                    currentPlan = currentPlanInfo,
                    plans = plansData
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to retrieve plans: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Upgrade(int planId, string billingCycle, string paymentMode = "Wallet", string? referenceNo = null)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Json(new { success = false, message = "Tenant session not found." });
                }

                var tenant = await _tenantRepository.GetById(tenantId);
                if (tenant == null)
                {
                    return Json(new { success = false, message = "Tenant not found." });
                }

                var targetPlan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
                if (targetPlan == null)
                {
                    return Json(new { success = false, message = "Selected plan not found or is inactive." });
                }

                decimal currentPlanPrice = 0;
                if (tenant.SubscriptionPlan != null)
                {
                    currentPlanPrice = tenant.SubscriptionPlan.MonthlyPrice;
                }

                if (targetPlan.MonthlyPrice <= currentPlanPrice)
                {
                    return Json(new { success = false, message = "You can only upgrade to a higher tier plan." });
                }

                decimal cost = string.Equals(billingCycle, "yearly", StringComparison.OrdinalIgnoreCase) 
                    ? targetPlan.YearlyPrice 
                    : targetPlan.MonthlyPrice;

                // Perform the upgrade inside a transaction
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var randomSuffix = new Random().Next(1000, 9999);
                        string finalRefNo = referenceNo ?? string.Empty;

                        if (string.Equals(paymentMode, "Wallet", StringComparison.OrdinalIgnoreCase))
                        {
                            if (tenant.WalletBalance < cost)
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = $"Insufficient wallet balance. Required: ₹{cost:F2}, Available: ₹{tenant.WalletBalance:F2}. Please recharge your wallet or choose a direct payment option."
                                });
                            }

                            // Deduct cost from wallet
                            tenant.WalletBalance -= cost;

                            if (string.IsNullOrEmpty(finalRefNo))
                            {
                                finalRefNo = $"UPG-WLT-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{randomSuffix}";
                            }

                            // Log history entry
                            var historyEntry = new TenantWalletHistory
                            {
                                TenantId = tenant.Id,
                                TransactionDateTime = DateTime.Now,
                                Credit = 0,
                                Debit = cost,
                                PaymentMode = "Wallet",
                                ReferenceNo = finalRefNo,
                                ServiceType = "Plan Upgrade",
                                ServiceCharge = 0,
                                Remarks = $"Upgraded to {targetPlan.PlanName} ({billingCycle}) via Wallet",
                                ClosingBalance = tenant.WalletBalance
                            };
                            _context.TenantWalletHistories.Add(historyEntry);
                        }
                        else
                        {
                            // Direct Gateway or Manual Payment
                            var isGateway = string.Equals(paymentMode, "Gateway", StringComparison.OrdinalIgnoreCase);
                            var payModeLabel = isGateway ? "Gateway" : "Manual";

                            if (string.IsNullOrEmpty(finalRefNo))
                            {
                                finalRefNo = isGateway 
                                    ? $"UPG-GWY-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{randomSuffix}"
                                    : $"UPG-MNL-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{randomSuffix}";
                            }

                            // Log Credit entry (simulating direct payment arrival)
                            var creditEntry = new TenantWalletHistory
                            {
                                TenantId = tenant.Id,
                                TransactionDateTime = DateTime.Now,
                                Credit = cost,
                                Debit = 0,
                                PaymentMode = payModeLabel,
                                ReferenceNo = finalRefNo,
                                ServiceType = "Plan Recharge",
                                ServiceCharge = 0,
                                Remarks = $"Direct recharge for upgrade to {targetPlan.PlanName} ({billingCycle})",
                                ClosingBalance = tenant.WalletBalance + cost
                            };
                            _context.TenantWalletHistories.Add(creditEntry);

                            // Log Debit entry (the upgrade purchase deduction)
                            var debitEntry = new TenantWalletHistory
                            {
                                TenantId = tenant.Id,
                                TransactionDateTime = DateTime.Now,
                                Credit = 0,
                                Debit = cost,
                                PaymentMode = "Wallet",
                                ReferenceNo = $"UPG-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{randomSuffix}",
                                ServiceType = "Plan Upgrade",
                                ServiceCharge = 0,
                                Remarks = $"Upgraded to {targetPlan.PlanName} ({billingCycle}) via {payModeLabel}",
                                ClosingBalance = tenant.WalletBalance
                            };
                            _context.TenantWalletHistories.Add(debitEntry);
                        }

                        // Update plan & clear overrides
                        tenant.SubscriptionPlanId = targetPlan.Id;
                        tenant.AllowedModulesJson = null;

                        _context.Tenants.Update(tenant);
                        await _context.SaveChangesAsync();
                        
                        transaction.Commit();

                        return Json(new { success = true, message = "Subscription upgraded successfully!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = $"An error occurred during upgrade database operation: {ex.Message}" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to process upgrade request: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingUpgradeStatus()
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Json(new { success = false, message = "Tenant session not found." });
                }

                var tenant = await _tenantRepository.GetById(tenantId);
                if (tenant == null)
                {
                    return Json(new { success = false, message = "Tenant not found." });
                }

                var pendingAdmin = await _context.TenantWalletHistories
                    .FirstOrDefaultAsync(h => h.TenantId == tenantId && h.ServiceType == "PendingLimitUpgrade");
                var pendingSa = await _context.TenantWalletHistories
                    .FirstOrDefaultAsync(h => h.TenantId == tenantId && h.ServiceType == "PendingSaUpgrade");

                int pendingAdminCount = pendingAdmin != null ? ParseExtraUsersFromRemarks(pendingAdmin.Remarks) : 0;
                int pendingSaCount = pendingSa != null ? ParseExtraUsersFromRemarks(pendingSa.Remarks) : 0;

                int remainingMonths = 0;
                if (tenant.LicenceExpiryDate.HasValue && tenant.LicenceExpiryDate.Value > DateTime.Now)
                {
                    double totalDays = (tenant.LicenceExpiryDate.Value - DateTime.Now).TotalDays;
                    remainingMonths = (int)Math.Max(1, Math.Round(totalDays / 30.4375));
                }

                return Json(new
                {
                    success = true,
                    pendingAdminRequest = pendingAdmin != null,
                    pendingSaUpgrade = pendingSa != null,
                    pendingAdminRequestCount = pendingAdminCount,
                    pendingSaUpgradeCount = pendingSaCount,
                    walletBalance = tenant.WalletBalance,
                    licenceExpiryDate = tenant.LicenceExpiryDate?.ToString("yyyy-MM-dd") ?? "",
                    remainingMonths = remainingMonths,
                    activeExtraUsers = tenant.ExtraUsers,
                    extraUsersExpiryDate = tenant.ExtraUsersExpiryDate?.ToString("yyyy-MM-dd") ?? ""
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RedeemSaUpgrade()
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Json(new { success = false, message = "Tenant session not found." });
                }

                var tenant = await _tenantRepository.GetById(tenantId);
                if (tenant == null)
                {
                    return Json(new { success = false, message = "Tenant not found." });
                }

                var tx = await _context.TenantWalletHistories
                    .FirstOrDefaultAsync(h => h.TenantId == tenantId && h.ServiceType == "PendingSaUpgrade");
                if (tx == null)
                {
                    return Json(new { success = false, message = "No pending upgrade from SuperAdmin found." });
                }

                int count = ParseExtraUsersFromRemarks(tx.Remarks);

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // Mark transaction as redeemed
                        tx.ServiceType = "LimitUpgrade";
                        tx.Remarks = $"Redeemed {count} Extra Users assigned by SA";
                        _context.TenantWalletHistories.Update(tx);

                        // Apply limits to tenant
                        tenant.ExtraUsers = count;
                        tenant.ExtraUsersExpiryDate = tenant.LicenceExpiryDate;

                        _context.Tenants.Update(tenant);
                        await _context.SaveChangesAsync();
                        transaction.Commit();

                        return Json(new { success = true, message = $"Successfully redeemed {count} extra user slots!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = $"An error occurred during redemption: {ex.Message}" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to process redemption: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PurchaseExtraUsers(int count, string paymentMode = "Wallet", string? referenceNo = null)
        {
            try
            {
                var tenantId = User.FindFirst("TenantId")?.Value;
                if (string.IsNullOrEmpty(tenantId))
                {
                    return Json(new { success = false, message = "Tenant session not found." });
                }

                var tenant = await _tenantRepository.GetById(tenantId);
                if (tenant == null)
                {
                    return Json(new { success = false, message = "Tenant not found." });
                }

                // Enforce guard clauses
                var hasPendingAdmin = await _context.TenantWalletHistories.AnyAsync(h => h.TenantId == tenant.Id && h.ServiceType == "PendingLimitUpgrade");
                if (hasPendingAdmin)
                {
                    return Json(new { success = false, message = "You already have a pending upgrade request awaiting SuperAdmin approval." });
                }

                var hasPendingSa = await _context.TenantWalletHistories.AnyAsync(h => h.TenantId == tenant.Id && h.ServiceType == "PendingSaUpgrade");
                if (hasPendingSa)
                {
                    return Json(new { success = false, message = "A pending upgrade from SuperAdmin is ready for you to redeem. Please redeem it first." });
                }

                // Verify license is active
                if (!tenant.LicenceExpiryDate.HasValue || tenant.LicenceExpiryDate.Value <= DateTime.Now)
                {
                    return Json(new { success = false, message = "Your company license is expired or not active. Please renew/activate your license before purchasing extra staff slots." });
                }

                double totalDays = (tenant.LicenceExpiryDate.Value - DateTime.Now).TotalDays;
                int remainingMonths = (int)Math.Max(1, Math.Round(totalDays / 30.4375));

                // Validate count
                if (count != 5 && count != 10 && count != 15 && count != 20)
                {
                    return Json(new { success = false, message = "Invalid user block count. Must be 5, 10, 15, or 20." });
                }

                // Check current active paid users
                bool currentActive = tenant.ExtraUsersExpiryDate.HasValue && tenant.ExtraUsersExpiryDate.Value > DateTime.Now;
                int maxPaidUsers = currentActive ? await GetMaxPaidExtraUsersInActivePeriodAsync(tenant.Id, tenant.ExtraUsersExpiryDate.Value) : 0;
                if (maxPaidUsers == 0 && currentActive)
                {
                    maxPaidUsers = tenant.ExtraUsers;
                }

                decimal monthlyCost = 0;

                if (count == maxPaidUsers)
                {
                    // Extending/Renewing the same bundle
                    monthlyCost = GetPriceForExtraUsers(count);
                }
                else if (count > maxPaidUsers)
                {
                    // Upgrading: Charge only the difference
                    monthlyCost = GetPriceForExtraUsers(count) - GetPriceForExtraUsers(maxPaidUsers);
                }
                else
                {
                    // Downgrading: Free (keeps existing expiry date)
                    monthlyCost = 0;
                }

                decimal cost = monthlyCost * remainingMonths;

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var randomSuffix = new Random().Next(1000, 9999);
                        string finalRefNo = referenceNo ?? string.Empty;

                        if (string.Equals(paymentMode, "Wallet", StringComparison.OrdinalIgnoreCase))
                        {
                            if (cost > 0 && tenant.WalletBalance < cost)
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = $"Insufficient wallet balance. Required: ₹{cost:F2}, Available: ₹{tenant.WalletBalance:F2}. Please recharge your wallet or choose a direct payment option."
                                });
                            }

                            // Deduct cost from wallet
                            if (cost > 0)
                            {
                                tenant.WalletBalance -= cost;
                            }

                            if (string.IsNullOrEmpty(finalRefNo))
                            {
                                finalRefNo = $"USR-WLT-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{randomSuffix}";
                            }

                            // Log history entry (even for 0 cost adjustment)
                            var historyEntry = new TenantWalletHistory
                            {
                                TenantId = tenant.Id,
                                TransactionDateTime = DateTime.Now,
                                Credit = 0,
                                Debit = cost,
                                PaymentMode = "Wallet",
                                ReferenceNo = finalRefNo,
                                ServiceType = "PendingLimitUpgrade",
                                ServiceCharge = 0,
                                Remarks = cost > 0 
                                    ? $"Purchased {count} Extra Users (Upgraded from {maxPaidUsers}) for {remainingMonths} months via Wallet"
                                    : $"Adjusted Extra Users count to {count} (Paid limit: {maxPaidUsers})",
                                ClosingBalance = tenant.WalletBalance
                             };
                             _context.TenantWalletHistories.Add(historyEntry);
                        }
                        else
                        {
                            // Direct Gateway or Manual Payment
                            var isGateway = string.Equals(paymentMode, "Gateway", StringComparison.OrdinalIgnoreCase);
                            var payModeLabel = isGateway ? "Gateway" : "Manual";

                            if (string.IsNullOrEmpty(finalRefNo))
                            {
                                finalRefNo = isGateway 
                                    ? $"USR-GWY-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{randomSuffix}"
                                    : $"USR-MNL-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{randomSuffix}";
                            }

                            if (cost > 0)
                            {
                                // Log Credit entry (simulating direct payment arrival)
                                var creditEntry = new TenantWalletHistory
                                {
                                    TenantId = tenant.Id,
                                    TransactionDateTime = DateTime.Now,
                                    Credit = cost,
                                    Debit = 0,
                                    PaymentMode = payModeLabel,
                                    ReferenceNo = finalRefNo,
                                    ServiceType = "Wallet Recharge",
                                    ServiceCharge = 0,
                                    Remarks = $"Direct recharge for {count} Extra Users upgrade ({remainingMonths} months)",
                                    ClosingBalance = tenant.WalletBalance + cost
                                };
                                _context.TenantWalletHistories.Add(creditEntry);

                                // Log Debit entry (the upgrade purchase deduction)
                                var debitEntry = new TenantWalletHistory
                                {
                                    TenantId = tenant.Id,
                                    TransactionDateTime = DateTime.Now,
                                    Credit = 0,
                                    Debit = cost,
                                    PaymentMode = "Wallet",
                                    ReferenceNo = $"USR-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{randomSuffix}",
                                    ServiceType = "PendingLimitUpgrade",
                                    ServiceCharge = 0,
                                    Remarks = $"Purchased {count} Extra Users (Upgraded from {maxPaidUsers}) for {remainingMonths} months via {payModeLabel}",
                                    ClosingBalance = tenant.WalletBalance
                                };
                                _context.TenantWalletHistories.Add(debitEntry);
                            }
                            else
                            {
                                // Log free adjustment transaction
                                var adjustmentEntry = new TenantWalletHistory
                                {
                                    TenantId = tenant.Id,
                                    TransactionDateTime = DateTime.Now,
                                    Credit = 0,
                                    Debit = 0,
                                    PaymentMode = payModeLabel,
                                    ReferenceNo = finalRefNo,
                                    ServiceType = "PendingLimitUpgrade",
                                    ServiceCharge = 0,
                                    Remarks = $"Adjusted Extra Users to {count} via {payModeLabel} (No Charge)",
                                    ClosingBalance = tenant.WalletBalance
                                };
                                _context.TenantWalletHistories.Add(adjustmentEntry);
                            }
                        }

                        // Save the wallet balance changes.
                        // Do NOT apply limits directly to the tenant's ExtraUsers/Expiry.
                        // These will be applied by SA upon approval.
                        _context.Tenants.Update(tenant);
                        await _context.SaveChangesAsync();

                        transaction.Commit();

                        return Json(new { success = true, message = $"Your request to purchase {count} extra user slots has been submitted for SuperAdmin approval." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = $"An error occurred during database operation: {ex.Message}" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to process purchase: {ex.Message}" });
            }
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
    }
}
