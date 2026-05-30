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
    public class Optical : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public decimal Sphere {  get; set; }
        public decimal Cylinder {  get; set; }
        public decimal Axis {  get; set; }
        public decimal Prism {  get; set; }
        public decimal Add {  get; set; }
        public string Rx { get; set; }
        public int SalesOrderId { get; set; }
        [ForeignKey(nameof(SalesOrderId))]
        public SalesOrder? SalesOrder { get; set; }
        public int ItemMasterId { get; set; }
        [ForeignKey(nameof(ItemMasterId))]
        public ItemMaster? ItemMaster { get; set; }
        public string? TenantId { get; set; }
        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }
       
    }
}
