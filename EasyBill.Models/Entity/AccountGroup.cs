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
    public class AccountGroup : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId {  get; set; }
        [ForeignKey("ParentId")]
        public AccountGroup? ParentGroup { get; set; }
        public bool IsActive {  get; set; }
        public string? TenantId { get; set; }
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }
    }
}
