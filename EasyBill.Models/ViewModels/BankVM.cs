using System;
using System.ComponentModel.DataAnnotations;

namespace EasyBill.Models.ViewModels
{
    public class BankVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Bank Name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Bank Name must be between 3 and 150 characters.")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account Group is required.")]
        public int? AccountGroupId { get; set; }
        public string? AccountGroupName { get; set; }

        [Required(ErrorMessage = "Branch is required.")]
        [StringLength(100, ErrorMessage = "Branch name cannot exceed 100 characters.")]
        public string? Branch { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        [Required(ErrorMessage = "Account No is required.")]
        [StringLength(50, ErrorMessage = "Account number cannot exceed 50 characters.")]
        public string? AccountNo { get; set; }

        [Required(ErrorMessage = "IFSC Code is required.")]
        public string? IFSCCode { get; set; }

        [StringLength(11, ErrorMessage = "Swift No cannot exceed 11 characters.")]
        public string? SwiftNo { get; set; }

        [Required(ErrorMessage = "Opening Balance is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Opening Balance must be positive.")]
        public decimal OpeningBalance { get; set; }
    }
}
