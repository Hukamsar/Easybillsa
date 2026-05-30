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
    public class PointTransaction : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; }
        public int EarnedPoints { get; set; }
        public int RedeemedPoints { get; set; }

        public decimal SaleAmount { get; set; }
        public int? SaleId { get; set; }
        [ForeignKey(nameof(SaleId))]
        public Sales? Sales { get; set; }
        public DateTime TransactionDate { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
