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
    public class GeneratePO : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int? SupplierType { get; set; } // 1 = Last, 2 = Best
        public int? SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }
        public int? CompanyId { get; set; }
        [ForeignKey("CompanyId")]
        public Company? Company { get; set; } 
        public int BasedOn { get; set; }
        // 1 = Sales, 2 = SalesOrder, 3 = MinQty, 4 = ZeroStock

        public int Days { get; set; }
        public decimal Multiplier { get; set; } 
        public bool LessPO { get; set; } 
        public List<GeneratePOItem> Items { get; set; } = new();
        public string? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
