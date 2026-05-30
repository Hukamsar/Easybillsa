using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class SubCategoryVM
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "SubCategory name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "SubCategory name must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).{3,150}$", ErrorMessage = "SubCategory name cannot start or end with spaces.")]
        public string Name { get; set; }
        public int CategoryId { get; set; } 
        public string? CategoryName { get; set; }
    }
}
