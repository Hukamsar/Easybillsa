using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class CreateCustomerRequest
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        //[Required]
        [MaxLength(250)]
        public string? Address { get; set; }

        [Required]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid mobile number")]
        public string MobileNumber { get; set; }
    }
}
