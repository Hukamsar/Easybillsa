using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models;
using AOne.Models.Entity;

namespace EasyBill.Models.Entity
{
    public class Batch : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string BatchNo { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal Mrp { get; set; }
        public decimal PurchaseRate {  get; set; }
        public decimal SalesRateA { get; set; }
        public decimal SalesRateB { get; set; }
        public int Qty { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
