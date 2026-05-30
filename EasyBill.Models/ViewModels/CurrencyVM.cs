using AOne.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class CurrencyVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Currency code is required.")]
        [StringLength(10, MinimumLength = 2, ErrorMessage = "Currency code must be between 2 and 10 characters.")]
        [Display(Name = "CurrencyCode")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Currency symbol is required.")]
        [StringLength(5, ErrorMessage = "Symbol cannot exceed 5 characters.")]
        public string Symbol { get; set; }

        [Required(ErrorMessage = "Currency name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Currency name must be between 2 and 100 characters.")]
        [Display(Name = "Currency Name")]
        public string Name { get; set; }

        [StringLength(100, ErrorMessage = "Sub name cannot exceed 100 characters.")]
        public string SubName { get; set; }
    }
}
