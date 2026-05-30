using AOne.Utility.Enums;
using System.ComponentModel.DataAnnotations;

namespace EasyBill.Models.ViewModels
{
    public class CustomerVM
    {
        public int Id { get; set; }

        [Display(Name = "Customer Name")]
        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Customer Name must be between 3 and 50 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).+$", ErrorMessage = "Customer Name cannot start or end with spaces.")]
        public string Name { get; set; }

        public string? Address { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[6-9][0-9]{9}$", ErrorMessage = "Enter a valid 10-digit mobile number (numbers only)")]
        public string? PhoneNo { get; set; }

        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Enter a valid email address")]
        public string? Email { get; set; }

        [Display(Name = "Customer Group")]
        public int? AccountGroupId { get; set; }

        [Display(Name = "GST No")]
        [RegularExpression(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$",
            ErrorMessage = "Enter a valid 15-character GSTIN")]
        public string? GSTNo { get; set; }

        [Display(Name = "State Code")]
        public GstStateCode? StateCode { get; set; }

        [Display(Name = "GST Type")]
        public GSTType GSTType { get; set; } = GSTType.UnRegistered;

        [Display(Name = "Category")]
        public CustomerCategory? Category { get; set; }

        [Display(Name = "Status")]
        public CustomerStatus Status { get; set; } = CustomerStatus.Active;
        public int PaymentDays { get; set; }
    }
}