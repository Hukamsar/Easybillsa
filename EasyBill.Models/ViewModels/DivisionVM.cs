using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class DivisionVM
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Division name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Division name must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).{3,150}$", ErrorMessage = "Division name cannot start or end with spaces.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please select a Company")]
        public int CompanyId { get; set; }
        public string? Companyname {  get; set; }
    }
}
