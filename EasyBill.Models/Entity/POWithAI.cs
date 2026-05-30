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
    public class POWithAI : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string? SupplierType { get; set; } // Last / Best
        public int? SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }
        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public Company? Company { get; set; } 
        public string? BasedOn { get; set; } // Sales / SalesOrder / MinQty / ZeroStock
        public int Days { get; set; }
        public decimal Multiplier { get; set; } 
        public int CalculateDays { get; set; } 
        public bool LessPurchaseOrders { get; set; }
        public bool RoundOffMOQ { get; set; }
        public string? TenantId {  get; set; }
        public Tenant? Tenant { get; set; }
    }
}
