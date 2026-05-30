using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class ApplicationUserVM
    {
        public string Id { get; set; }
        public int? EmployeeId { get; set; } 
        public string? EmployeeName { get; set; }
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? MobileNo { get; set; }
        public string? Email {  get; set; }
        public string? Role { get; set; }
    }
}
