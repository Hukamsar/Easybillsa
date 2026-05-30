using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models;
using AOne.Models.Entity;

namespace EasyBill.Models.Entity
{
    public class Division : BaseEntity, IMayHaveTenant
    {
        public int Id {  get; set; }
        public string Name { get; set; }
        public int CompanyId {  get; set; }
        [ForeignKey(nameof(CompanyId))]
        public Company Company { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
