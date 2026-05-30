using AOne.Models;
using AOne.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class OfferItem : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; } 
        public int OfferId { get; set; }
        [ForeignKey(nameof(OfferId))]
        public Offer Offer { get; set; } 
        public int ItemId { get; set; }
        [ForeignKey(nameof(ItemId))]
        public ItemMaster ItemMaster { get; set; }
        public int? FreeItemId { get; set; }  
        [ForeignKey(nameof(FreeItemId))]
        public ItemMaster? FreeItem {  get; set; }
        public int BuyQty { get; set; }
        public int FreeQty { get; set; }
        public decimal fixedPrice {  get; set; }
        public string? ComboGroupId { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
