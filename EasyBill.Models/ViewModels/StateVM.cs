using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class StateVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "State name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "State name must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).{3,150}$", ErrorMessage = "State name cannot start or end with spaces.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please select a country")]
        public int CountryId { get; set; }

        public string? Zone { get; set; }

        [ValidateNever]
        public IEnumerable<State> States { get; set; }
    }
}
