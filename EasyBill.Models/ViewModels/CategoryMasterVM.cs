using AOne.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class CategoryMasterVM
    {
        [ValidateNever]
        public IEnumerable<CategoryMaster>? Categories { get; set; }
        public int Id { get; set; }
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Category name must be between 2 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).{3,150}$", ErrorMessage = "Category name cannot start or end with spaces.")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } 
    }
}
