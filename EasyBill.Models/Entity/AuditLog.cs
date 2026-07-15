using AOne.Models;
using AOne.Models.Entity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class AuditLog : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUsers? ApplicationUsers { get; set; }
        public string? Username { get; set; }
        public string? Action { get; set; }
        public string? ControllerName { get; set; }
        public string? ActionName { get; set; }
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string? TenantId { get; set; }
        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }
    }
}
