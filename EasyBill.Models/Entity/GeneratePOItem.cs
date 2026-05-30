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
    public class GeneratePOItem : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int? GeneratePOId { get; set; }
        [ForeignKey("GeneratePOId")]
        public GeneratePO? GeneratePO { get; set; }
        public int ItemId { get; set; }
        [ForeignKey("ItemId")]
        public ItemMaster? ItemMaster { get; set; }  
        public string? Packing { get; set; } 
        public decimal SalesQty { get; set; }
        public decimal SalesOrderQty { get; set; }

        public decimal CurrentQty { get; set; }
        public decimal MinQty { get; set; }

        public decimal ReorderQty { get; set; }

       // public int SupplierId { get; set; }
        public decimal LastRate { get; set; }

        public string? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
