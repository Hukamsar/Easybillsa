using AOne.Models;
using AOne.Models.Entity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class Bank : BaseEntity, IMayHaveTenant
    {
        [Key]
        public int Id { get; set; }

        public string BankName { get; set; } = string.Empty;

        public int? AccountGroupId { get; set; }
        [ForeignKey("AccountGroupId")]
        public AccountGroup? AccountGroup { get; set; }

        public string? Branch { get; set; }
        public string? City { get; set; }
        public string? AccountNo { get; set; }
        public string? IFSCCode { get; set; }
        public string? SwiftNo { get; set; }
        public decimal OpeningBalance { get; set; }

        public string? TenantId { get; set; }
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }
    }
}
