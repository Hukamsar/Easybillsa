using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AOne.Models.ViewModels
{
    public class RegisterViewModel
    {
        //[Required(ErrorMessage = "Employee is required.")]
        //public int EmployeeId { get; set; }
        //[ValidateNever]
        //public Employee Employee { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "The {0} must be at {2} and at max {1} character long. ")]
        [DataType(DataType.Password)]
        [Compare("ConfirmPassword", ErrorMessage = "Password does not match.")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }
        [ValidateNever]
        public List<SelectListItem> EmployeeList { get; set; }
        public string RoleId { get; set; }
        [ValidateNever]
        public List<SelectListItem> RoleList { get; set; }
    }
}
