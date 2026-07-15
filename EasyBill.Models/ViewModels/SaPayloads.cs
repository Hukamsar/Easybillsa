using System;
using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class SaCompanyPayload
    {
        public SaCompanyFields? Fields { get; set; }
        public SaSubscriptionPayload? Subscription { get; set; }
    }

    public class SaSubscriptionPayload
    {
        public string? PlanName { get; set; }
        public string? Status { get; set; }
        public string? BillingCycle { get; set; }
        public decimal Amount { get; set; }
        public bool AutoRenew { get; set; }
    }

    public class SaCompanyFields
    {
        public string? TenantCode { get; set; }
        public string? LegalName { get; set; }
        public string? DisplayName { get; set; }
        public string? PrimaryContactName { get; set; }
        public string? PrimaryEmail { get; set; }
        public string? PrimaryPhone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? Notes { get; set; }
        public string? GstNo { get; set; }
        public string? BillingModel { get; set; }
        public string? InventoryMode { get; set; }
        public int OutletCount { get; set; }
        public string? SupportTier { get; set; }

        // Compliance and statutory properties
        public string? CompType { get; set; }
        public string? StateCode { get; set; }
        public string? BusinessType { get; set; }
        public string? Calendar { get; set; }
        public string? FinYearFrom { get; set; }
        public string? FinYearTo { get; set; }
        public string? TaxType { get; set; }
        public string? FssaiNo { get; set; }
        public string? DrugLic { get; set; }
        public string? LicExp { get; set; }
        public string? BranchCode { get; set; }
        public bool? IsHeadOffice { get; set; }
        public string? ParentTenantId { get; set; }
    }

    public class RechargeRequest
    {
        public decimal Amount { get; set; }
        public string? Remarks { get; set; }
    }

    public class UserLimitsRequest
    {
        public int ExtraUsersBlock { get; set; }
    }

    public class RollbackRequest
    {
        public int DurationMonths { get; set; }
    }

    public class ModuleAccessRequest
    {
        public List<string>? AllowedModules { get; set; }
    }

    public class PoliciesRequest
    {
        public bool? LoginEnabled { get; set; }
        public bool? AuditLocked { get; set; }
        public bool? AllowNegativeBalance { get; set; }
        public string? SupportPriority { get; set; }
        public string? Status { get; set; }
    }

    public class PlanPayload
    {
        public string? PlanName { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public int DailyCustomerLimit { get; set; }
        public int MaxDesktopLogins { get; set; }
        public int MaxMobileLogins { get; set; }
        public bool IsActive { get; set; }
        public List<int>? SelectedFeatures { get; set; }
    }
}
