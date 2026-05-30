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
    public class CityVM
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "City name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "City name must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).{3,150}$", ErrorMessage = "City name cannot start or end with spaces.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Please select a State")]
        public int StateId { get; set; }
        [Required(ErrorMessage = "Please select a country")]
        public int CountryId { get; set; }
        [ValidateNever]
        public IEnumerable<City> City { get; set; }
    }
}
