using AOne.Models;
using AOne.Models.Entity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class EmployeeTransferLog : BaseEntity
    {
        public int Id { get; set; }
        public DateTime TransferDate { get; set; }
        public int EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }
        public string? FromTenantId { get; set; }
        public string? ToTenantId { get; set; }
        
        // NEW FIELDS
        public int? ToDepartmentId { get; set; }
        public int? ToDesignationId { get; set; }
        
        public string? TransferReason { get; set; }
        public bool IsReadByDestination { get; set; } = false;
    }
}