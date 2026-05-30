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
    }
}
