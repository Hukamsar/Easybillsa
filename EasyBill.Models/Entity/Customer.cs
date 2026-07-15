using AOne.Models;
using AOne.Models.Entity;
using AOne.Utility.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class Customer : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Address { get; set; }
        public string? Address2 { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? PinCode { get; set; }
        public string? PhoneNo {  get; set; }
        public string? Email { get; set; }

        // Geo-Location
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Accounting
        public int? AccountGroupId { get; set; }
        [ForeignKey("AccountGroupId")]
        public AccountGroup? AccountGroup { get; set; }

        // GST
        public string? GSTNo { get; set; }
        public GstStateCode? StateCode { get; set; }
        public GSTType GSTType { get; set; } = GSTType.UnRegistered;

        // Business
        public CustomerCategory? Category { get; set; }
        public CustomerStatus Status { get; set; } = CustomerStatus.Active;

        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        public int PaymentDays {  get; set; }

        public string? LastOtp { get; set; }

        public DateTime? OtpExpiresAt { get; set; }

        public bool IsMobileVerified { get; set; } = false;

        public DateTime? LastLogin { get; set; }

        public ICollection<CustomerAddress>? Addresses { get; set; }
    }
}
