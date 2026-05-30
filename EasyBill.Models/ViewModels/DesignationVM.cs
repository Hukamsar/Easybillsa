using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class DesignationVM
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Designation name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Designation name must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).+$", ErrorMessage = "Designation name cannot start or end with spaces.")]
        public string Name { get; set; }
        [ValidateNever]
        public IEnumerable<Designation> Designations { get; set; }
    }
}
