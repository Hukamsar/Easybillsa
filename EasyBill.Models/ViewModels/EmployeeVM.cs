using AOne.Utility.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class EmployeeVM
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Designation name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Designation name must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).+$", ErrorMessage = "Designation name cannot start or end with spaces.")]
        public string Name { get; set; }
        public Initials? Initials { get; set; }
        public int? DepartMentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? DesignationId { get; set; }
        public string? DesignationName { get; set; }


        // Email Validation
        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Enter a valid email address")]
        public string? Email { get; set; }

        // Phone Number (India)
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[6-9][0-9]{9}$", ErrorMessage = "Enter a valid 10-digit mobile number (numbers only)")]
        public string? Phone { get; set; }


        public bool RequiredCreditional { get; set; }
        public string? RoleId { get; set; }
        public List<SelectListItem> RoleList { get; set; }
        public int? AccountGroupId { get; set; }
    }
}
