using AOne.Models;
using AOne.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class TermsConditions : BaseEntity, IMayHaveTenant
    {
        public int Id {  get; set; }
        public string Name {  get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
