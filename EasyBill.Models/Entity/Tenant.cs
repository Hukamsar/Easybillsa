using AOne.Utility.Enums;
using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Models.Entity
{
    public class Tenant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [MaxLength(156)]
        public string? Name { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public required string Email { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Location { get; set; }
        public int? CountryId { get; set; }
        [ForeignKey(nameof(CountryId))]
        public Country? Country { get; set; }
        public int? StateId { get; set; }
        [ForeignKey(nameof(StateId))]
        public State? State { get; set; }
        public int? CityId { get; set; }
        [ForeignKey(nameof(CityId))]
        public City? City { get; set; }
        public string? PinCode { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? MobileNo { get; set; }
        public string? GstNo { get; set; }
        public GstStateCode? StateCode { get; set; }
        public CompanyType CompanyType { get; set; }
        public string? Branch { get; set; }
        public string? Logo { get; set; }
        public string? Description { get; set; }
  
        public string? IFSSAINo { get; set; }
        public string? DrugLicNo { get; set; } 
        public DateTime? LicenceExpiryDate { get; set; }

        //Other Tabs
        public string? Jurisdiction { get; set; }
        public WorkingStyle WorkingStyle { get; set; }


        // Company Details
        public string? BranchCode { get; set; }
        public BusinessType BusinessType { get; set; }
        public CalenderType CalanderType { get; set; }
        public DateTime? YearFrom { get; set; }
        public DateTime? YearTo { get; set; }
        public TaxType TaxType { get; set; }

        // Wallet and gateway settings
        public decimal WalletBalance { get; set; }
        public bool IsWalletActive { get; set; } = true;
        public decimal WhatsAppMessageCharge { get; set; }
        public decimal SmsMessageCharge { get; set; }
        public decimal EmailMessageCharge { get; set; }
        public bool IsSmsChargeActive { get; set; } 
        public bool IsEmailChargeActive { get; set; } 
        public bool IsWhatsAppChargeActive { get; set; }
        public string? PaymentGatewayProvider { get; set; }
        public string? PaymentGatewayKey { get; set; }
        public string? PaymentGatewaySecret { get; set; }
        public bool IsPaymentGatewayActive { get; set; }
        [NotMapped]
        public string FullAddress
        {
            get
            {
                var parts = new List<string>();

                if (!string.IsNullOrWhiteSpace(Address1)) parts.Add(Address1);
                if (!string.IsNullOrWhiteSpace(Address2)) parts.Add(Address2);
                if (!string.IsNullOrWhiteSpace(Location)) parts.Add(Location);
                if (City != null) parts.Add(City.Name);
                if (State != null) parts.Add(State.Name);
                if (Country != null) parts.Add(Country.Name);
                if (!string.IsNullOrWhiteSpace(PinCode)) parts.Add("Pin: " + PinCode);

                return string.Join(", ", parts);
            }
        }

        // SuperAdmin Control Plane Properties
        public string? AllowedModulesJson { get; set; }
        public int ExtraUsers { get; set; }
        public int RollbackDurationMonths { get; set; }
        public DateTime? RollbackExpiryDate { get; set; }
        public string? BillingModel { get; set; }
        public string? InventoryMode { get; set; }
        public int OutletCount { get; set; }
        public string? SupportTier { get; set; }
        public string? TenantCode { get; set; }
        public bool IsAuditLocked { get; set; }
        public bool AllowNegativeBalance { get; set; }
        public string? Status { get; set; } = "Active";
        public int? SubscriptionPlanId { get; set; }
        [ForeignKey(nameof(SubscriptionPlanId))]
        public SubscriptionPlan? SubscriptionPlan { get; set; }
    }
}

