using EasyBill.Models.Entity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AOne.Models.Entity
{
    public class ItemImage : BaseEntity, IMayHaveTenant
    { 
        public int Id { get; set; }

        // 🔗 FK to ItemMaster
        public int ItemMasterId { get; set; }

        [ForeignKey(nameof(ItemMasterId))]
        public ItemMaster ItemMaster { get; set; } = null!;

        // 📌 Only path stored in DB
        [Required]
        [MaxLength(300)]
        public string ImagePath { get; set; } = null!;

        // 🔐 Duplicate image prevention
        [Required]
        [MaxLength(64)]
        public string ImageHash { get; set; } = null!;

        // ⭐ Main image
        public bool IsPrimary { get; set; } = false;

        // ↕️ Image order
        public int SortOrder { get; set; } = 0;

        // 🗑️ Soft delete
        public bool IsDeleted { get; set; } = false;

        // 🌍 Tenant support
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}