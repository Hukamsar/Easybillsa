using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AOne.Models.ViewModels
{
    public static class LoginTypes
    {
        public const string EmailPassword = "EmailPassword";
        public const string MobilePin = "MobilePin";
        public const string MobileOtp = "MobileOtp";
    }

    public class LoginViewModels : IValidatableObject
    {
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public string? MobileNumber { get; set; }

        [DataType(DataType.Password)]
        public string? Pin { get; set; }

        public string? Otp { get; set; }

        public string LoginType { get; set; } = LoginTypes.EmailPassword;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var mode = string.IsNullOrWhiteSpace(LoginType)
                ? LoginTypes.EmailPassword
                : LoginType.Trim();

            if (mode.Equals(LoginTypes.EmailPassword, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(Email))
                    yield return new ValidationResult("Email is required.", new[] { nameof(Email) });
                else if (!new EmailAddressAttribute().IsValid(Email))
                    yield return new ValidationResult("Enter a valid email address.", new[] { nameof(Email) });

                if (string.IsNullOrWhiteSpace(Password))
                    yield return new ValidationResult("Password is required.", new[] { nameof(Password) });

                yield break;
            }

            if (mode.Equals(LoginTypes.MobilePin, StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidMobile(MobileNumber))
                    yield return new ValidationResult("Enter a valid 10-digit mobile number.", new[] { nameof(MobileNumber) });

                if (string.IsNullOrWhiteSpace(Pin))
                    yield return new ValidationResult("PIN is required.", new[] { nameof(Pin) });

                yield break;
            }

            if (mode.Equals(LoginTypes.MobileOtp, StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidMobile(MobileNumber))
                    yield return new ValidationResult("Enter a valid 10-digit mobile number.", new[] { nameof(MobileNumber) });

                if (string.IsNullOrWhiteSpace(Otp))
                    yield return new ValidationResult("OTP is required.", new[] { nameof(Otp) });

                yield break;
            }

            yield return new ValidationResult("Invalid login mode selected.", new[] { nameof(LoginType) });
        }

        private static bool IsValidMobile(string? mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber))
                return false;

            var digits = Regex.Replace(mobileNumber, "[^0-9]", string.Empty);
            if (digits.Length > 10)
                digits = digits.Substring(digits.Length - 10);

            return Regex.IsMatch(digits, "^[6-9][0-9]{9}$");
        }
    }
}
