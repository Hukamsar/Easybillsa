using AOne.Models.Entity;
using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class TermConditionsVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        [StringLength(300, MinimumLength = 3, ErrorMessage = "This field must be between 3 and 300 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).+$", ErrorMessage = "This field cannot start or end with spaces.")]
        public string Name { get; set; }
        public IEnumerable<TermsConditions> TermsConditions { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
