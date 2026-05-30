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
    public class SupplierAdvance : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }

        public int SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }
        public decimal AdvanceAmount { get; set; }

        public DateTime? Date { get; set; }

        public string? Remarks { get; set; }
        public string? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
