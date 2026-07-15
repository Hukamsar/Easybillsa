using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Models
{
    public class ApplicationUsers : IdentityUser
    {
        public int? EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }
        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }
        
        // Multi-Branch Access (JSON array of allowed TenantIds)
        public string? AllowedBranches { get; set; }
        
        [NotMapped]
        public string? SelectedTenantIdForLogin { get; set; }

    }
}
