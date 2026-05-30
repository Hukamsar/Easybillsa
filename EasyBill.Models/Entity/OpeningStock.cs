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
    public class OpeningStock : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public int? ItemId { get; set; }
        [ForeignKey("ItemId")]
        public ItemMaster? Master { get; set; }
        public string? Batch {  get; set; }
        public DateTime? Expirydate { get; set; }
        public decimal Mrp {  get; set; }
        public decimal RateA {  get; set; }
        public decimal RateB { get; set; }
        public decimal Qty {  get; set; }
        public string? TenantId { get; set; }
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }
    }
}
