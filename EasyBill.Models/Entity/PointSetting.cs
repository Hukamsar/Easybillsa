using AOne.Models;
using AOne.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class PointSetting : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; } 
        public decimal MinAmountToEarn { get; set; }  
        public decimal EarnPerAmount { get; set; }  
        public decimal PointValueInRs { get; set; }  
        public bool AllowRedemption { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
