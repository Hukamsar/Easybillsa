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
    public class Offer : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string OfferName { get; set; }
        public OfferType? OfferType { get; set; } // "Flat", "Percent", "BuyXGetY"
        public Applicable? Applicable { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MinAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsActive { get; set; } 
        public ICollection<OfferItem> OfferItems { get; set; }
        public int? CompanyId {  get; set; }
        [ForeignKey(nameof(CompanyId))]
        public Company? Company { get; set; }
        public int? CategoryId {  get; set; }
        [ForeignKey(nameof(CategoryId))]
        public CategoryMaster? CategoryMaster { get; set; }
        public int? ItemId { get; set; }
        [ForeignKey(nameof(ItemId))]
        public ItemMaster? ItemMaster { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
