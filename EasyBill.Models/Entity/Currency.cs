using AOne.Models;
using AOne.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class Currency : BaseEntity, IMayHaveTenant
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string SubName { get; set;   }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
