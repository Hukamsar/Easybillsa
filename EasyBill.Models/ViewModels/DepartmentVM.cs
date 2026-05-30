using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class DepartmentVM
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Department name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Department name must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).+$", ErrorMessage = "Department name cannot start or end with spaces.")]
        public string Name { get; set; }
        [ValidateNever]
        public List<DepartmentVM> Departments { get; set; }
    }
}
