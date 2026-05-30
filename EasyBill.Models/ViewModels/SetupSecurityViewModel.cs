using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace EasyBill.Models.ViewModels
{
    public class SetupSecurityViewModel : IValidatableObject
    {
        [EmailAddress]
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Password and confirm password must match.")]
        public string? ConfirmPassword { get; set; }

        [DataType(DataType.Password)]
        public string? Pin { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(Pin), ErrorMessage = "PIN and confirm PIN must match.")]
        public string? ConfirmPin { get; set; }

        public bool HasPassword { get; set; }
        public bool HasPin { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!HasPassword && string.IsNullOrWhiteSpace(Password))
            {
                yield return new ValidationResult("Password is required.", new[] { nameof(Password) });
            }

            if (!HasPin && string.IsNullOrWhiteSpace(Pin))
            {
                yield return new ValidationResult("PIN is required.", new[] { nameof(Pin) });
            }

            if (!string.IsNullOrWhiteSpace(Pin) && !Regex.IsMatch(Pin.Trim(), "^[0-9]{4,6}$"))
            {
                yield return new ValidationResult("PIN must be 4 to 6 digits.", new[] { nameof(Pin) });
            }
        }
    }
}
