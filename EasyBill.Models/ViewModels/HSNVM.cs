using AOne.Utility.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class HSNVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "HSN Code is required")]
        [RegularExpression(@"^\d{4,8}$", ErrorMessage = "HSN Code must be 4–8 digits")]
        public string HsnCode { get; set; }

        [Range(0, 100, ErrorMessage = "Invalid SGST value")]
        public decimal? SGST { get; set; }

        [Range(0, 100, ErrorMessage = "Invalid CGST value")]
        public decimal? CGST { get; set; }

        [Range(0, 100, ErrorMessage = "Invalid IGST value")]
        public decimal? IGST { get; set; }

        public decimal? Cess { get; set; }

        [Required(ErrorMessage = "HSN Type is required")]
        public HsnType? HsnType { get; set; }
    }
}
