using AOne.Models;
using AOne.Models.Entity;
using AOne.Utility.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class Employee : BaseEntity, IMayHaveTenant, IMasterEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int? DepartMentId { get; set; }
        [ForeignKey("DepartMentId")]
        public Department? Departments { get; set; }

        public int? DesignationId { get; set; }
        [ForeignKey("DesignationId")]
        public Designation? Designation { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool RequiredCreditional { get; set; }
        public string? TenantId { get; set; }
        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }
        public Initials? Initials { get; set; }
        public int? AccountGroupId { get; set; }
        [ForeignKey("AccountGroupId")]
        public AccountGroup? AccountGroup { get; set; }

        public bool IsHoAdmin { get; set; } = false;
    }
}
