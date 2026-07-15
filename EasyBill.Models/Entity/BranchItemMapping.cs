using AOne.Models;
using AOne.Models.Entity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class BranchItemMapping : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        
        public string? TenantId { get; set; }
        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }

        public int ItemMasterId { get; set; }
        [ForeignKey(nameof(ItemMasterId))]
        public ItemMaster? ItemMaster { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
