using AOne.Utility.Enums;
using System.ComponentModel.DataAnnotations;

namespace EasyBill.Models.ViewModels
{
    public class CompanyRegistrationRequest
    {
        [Required(ErrorMessage = "Company name is required.")]
        [MaxLength(156)]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address1 is required.")]
        public string Address1 { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address2 is required.")]
        public string Address2 { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person is required.")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required.")]
        public string MobileNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public BusinessType BusinessType { get; set; }
    }

    public class CompleteProfileSetupRequest
    {
        [DataType(DataType.Password)]
        public string? Pin { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }

    public class ProfileSetupStatusViewModel
    {
        public bool HasPin { get; set; }
        public bool HasEmail { get; set; }
        public bool HasPassword { get; set; }
        public bool IsProfileSetupComplete => HasPin && HasEmail && HasPassword;
    }
}
