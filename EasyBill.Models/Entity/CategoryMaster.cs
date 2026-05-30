using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Models.Entity
{
    public class CategoryMaster : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string CategoryName {  get; set; } 
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
