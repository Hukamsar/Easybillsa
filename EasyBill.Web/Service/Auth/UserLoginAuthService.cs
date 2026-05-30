using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using AOne.DataAccess.Repository.IRepository;
using AOne.Models;
using AOne.Models.Entity;
using AOne.Utility;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EasyBill.UI.Service.Auth
{
    public class UserLoginAuthService
    {
        private const string PinTokenProvider = "EasyBillAuth";
        private const string PinTokenName = "MobilePinHash";
        private const string PlaceholderEmailDomain = "pending.easybill.local";

        private readonly UserManager<ApplicationUsers> _userManager;
        private readonly ITenantRepository _tenantRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordHasher<ApplicationUsers> _pinHasher = new();

        public UserLoginAuthService(
            UserManager<ApplicationUsers> userManager,
            ITenantRepository tenantRepository,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _tenantRepository = tenantRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserLoginAuthResult> RegisterCompanyAsync(CompanyRegistrationRequest request)
        {
            if (request == null)
                return UserLoginAuthResult.Fail("Invalid request.");

            var normalizedMobile = NormalizeMobileNumber(request.MobileNumber);
            if (!IsValidMobileNumber(normalizedMobile))
                return UserLoginAuthResult.Fail("Enter a valid 10-digit mobile number.");

            var email = request.Email?.Trim() ?? string.Empty;
            if (!new EmailAddressAttribute().IsValid(email))
                return UserLoginAuthResult.Fail("Enter a valid email address.");

            var companyName = request.CompanyName?.Trim() ?? string.Empty;
            var address1 = request.Address1?.Trim() ?? string.Empty;
            var address2 = request.Address2?.Trim() ?? string.Empty;
            var cityName = request.City?.Trim() ?? string.Empty;
            var contactPerson = request.ContactPerson?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(companyName))
                return UserLoginAuthResult.Fail("Company name is required.");
            if (string.IsNullOrWhiteSpace(address1))
                return UserLoginAuthResult.Fail("Address1 is required.");
            if (string.IsNullOrWhiteSpace(address2))
                return UserLoginAuthResult.Fail("Address2 is required.");
            if (string.IsNullOrWhiteSpace(cityName))
                return UserLoginAuthResult.Fail("City is required.");
            if (string.IsNullOrWhiteSpace(contactPerson))
                return UserLoginAuthResult.Fail("Contact person is required.");
            if (!Enum.IsDefined(typeof(AOne.Utility.Enums.BusinessType), request.BusinessType) || (int)request.BusinessType <= 0)
                return UserLoginAuthResult.Fail("Business type is required.");

            var existingByMobile = await _userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == normalizedMobile);
            if (existingByMobile != null)
                return UserLoginAuthResult.Fail("Mobile number already registered. Please login.");

            var existingByEmail = await _userManager.FindByEmailAsync(email);
            if (existingByEmail != null)
                return UserLoginAuthResult.Fail("Email already registered. Please login.");

            if (await _tenantRepository.ExistMobile(normalizedMobile))
                return UserLoginAuthResult.Fail("Mobile number already registered for another company.");

            if (await _tenantRepository.ExistEmail(email))
                return UserLoginAuthResult.Fail("Email already registered for another company.");

            var cityRepo = _unitOfWork.GetRepository<City>();
            var city = await cityRepo.Query()
                .FirstOrDefaultAsync(x => x.Name != null && x.Name.ToLower() == cityName.ToLower());

            var tenant = new Tenant
            {
                Name = companyName,
                Email = email,
                Address1 = address1,
                Address2 = address2,
                Location = cityName,
                CityId = city?.Id,
                StateId = city?.StateId,
                CountryId = city?.CountryId,
                ContactPerson = contactPerson,
                Phone = normalizedMobile,
                MobileNo = normalizedMobile,
                BusinessType = request.BusinessType,
                Description = "Pending profile setup"
            };

            var tenantRepo = _unitOfWork.GetRepository<Tenant>();
            tenantRepo.Add(tenant);
            using (var transaction = tenantRepo.BeginTransaction())
            {
                await tenantRepo.SaveChangesAsync();
                transaction.Commit();
            }

            var newUser = new ApplicationUsers
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = normalizedMobile,
                PhoneNumberConfirmed = true,
                TenantId = tenant.Id,
                TenantName = tenant.Name
            };

            // Password is intentionally skipped at registration. User will set it from profile after mobile login.
            var createUserResult = await _userManager.CreateAsync(newUser);
            if (!createUserResult.Succeeded)
            {
                await TryDeleteTenantAsync(tenant);

                return UserLoginAuthResult.Fail(GetErrorMessage(createUserResult, "Unable to register company."));
            }

            var addRoleResult = await _userManager.AddToRoleAsync(newUser, RoleName.Admin);
            if (!addRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                await TryDeleteTenantAsync(tenant);

                return UserLoginAuthResult.Fail(GetErrorMessage(addRoleResult, "Unable to assign role."));
            }

            return new UserLoginAuthResult
            {
                Success = true,
                Message = "Company registered successfully.",
                User = newUser,
                TenantId = tenant.Id,
                SetupStatus = await GetProfileSetupStatusAsync(newUser)
            };
        }

        public async Task<UserLoginAuthResult> SendLoginOtpAsync(string? mobileNumber)
        {
            var normalizedMobile = NormalizeMobileNumber(mobileNumber);
            if (!IsValidMobileNumber(normalizedMobile))
                return UserLoginAuthResult.Fail("Enter a valid 10-digit mobile number.");

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == normalizedMobile);
            if (user == null)
                return UserLoginAuthResult.Fail("Mobile number is not registered. Please register first.");

            var otpCode = new Random().Next(1000, 9999).ToString("D4");
            var otpRecord = new CustomerOtp
            {
                MobileNumber = normalizedMobile,
                OtpCode = otpCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Created = DateTime.UtcNow,
                IsUsed = false
            };

            var otpRepo = _unitOfWork.GetRepository<CustomerOtp>();
            otpRepo.Add(otpRecord);
            using (var transaction = otpRepo.BeginTransaction())
            {
                await otpRepo.SaveChangesAsync();
                transaction.Commit();
            }

            return new UserLoginAuthResult
            {
                Success = true,
                Message = "OTP sent successfully.",
                OtpCode = otpCode
            };
        }

        public async Task<UserLoginAuthResult> VerifyLoginOtpAsync(string? mobileNumber, string? otp)
        {
            var normalizedMobile = NormalizeMobileNumber(mobileNumber);
            if (!IsValidMobileNumber(normalizedMobile))
                return UserLoginAuthResult.Fail("Enter a valid 10-digit mobile number.");

            if (string.IsNullOrWhiteSpace(otp))
                return UserLoginAuthResult.Fail("OTP is required.");

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == normalizedMobile);
            if (user == null)
                return UserLoginAuthResult.Fail("Mobile number is not registered. Please register first.");

            var otpRepo = _unitOfWork.GetRepository<CustomerOtp>();
            var otpRecord = await otpRepo.Query()
                .Where(x => x.MobileNumber == normalizedMobile && x.OtpCode == otp.Trim() && !x.IsUsed)
                .OrderByDescending(x => x.Created)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
                return UserLoginAuthResult.Fail("OTP is invalid or already used.");

            if (otpRecord.ExpiresAt < DateTime.UtcNow)
                return UserLoginAuthResult.Fail("OTP has expired.");

            otpRecord.IsUsed = true;
            otpRepo.Update(otpRecord);
            using (var transaction = otpRepo.BeginTransaction())
            {
                await otpRepo.SaveChangesAsync();
                transaction.Commit();
            }

            return new UserLoginAuthResult
            {
                Success = true,
                Message = "OTP verified successfully.",
                User = user,
                SetupStatus = await GetProfileSetupStatusAsync(user)
            };
        }

        public async Task<UserLoginAuthResult> VerifyMobilePinAsync(string? mobileNumber, string? pin)
        {
            var normalizedMobile = NormalizeMobileNumber(mobileNumber);
            if (!IsValidMobileNumber(normalizedMobile))
                return UserLoginAuthResult.Fail("Enter a valid 10-digit mobile number.");

            if (string.IsNullOrWhiteSpace(pin))
                return UserLoginAuthResult.Fail("PIN is required.");

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == normalizedMobile);
            if (user == null)
                return UserLoginAuthResult.Fail("Mobile number is not registered. Please register first.");

            var pinHash = await _userManager.GetAuthenticationTokenAsync(user, PinTokenProvider, PinTokenName);
            if (string.IsNullOrWhiteSpace(pinHash))
                return UserLoginAuthResult.Fail("PIN is not set. Please login with OTP and set PIN from profile.");

            var verifyResult = _pinHasher.VerifyHashedPassword(user, pinHash, pin.Trim());
            if (verifyResult == PasswordVerificationResult.Failed)
                return UserLoginAuthResult.Fail("Invalid PIN.");

            return new UserLoginAuthResult
            {
                Success = true,
                Message = "PIN verified successfully.",
                User = user,
                SetupStatus = await GetProfileSetupStatusAsync(user)
            };
        }

        public async Task<UserLoginAuthResult> SetProfileCredentialsAsync(ApplicationUsers user, CompleteProfileSetupRequest request)
        {
            if (user == null)
                return UserLoginAuthResult.Fail("User not found.");

            if (request == null)
                return UserLoginAuthResult.Fail("Invalid request.");

            var hasInput =
                !string.IsNullOrWhiteSpace(request.Pin) ||
                !string.IsNullOrWhiteSpace(request.Email) ||
                !string.IsNullOrWhiteSpace(request.Password);

            if (!hasInput)
                return UserLoginAuthResult.Fail("Please provide PIN, Email or Password.");

            var emailUpdated = false;
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var email = request.Email.Trim();
                if (!new EmailAddressAttribute().IsValid(email))
                    return UserLoginAuthResult.Fail("Enter a valid email address.");

                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null && existingUser.Id != user.Id)
                    return UserLoginAuthResult.Fail("Email is already in use.");

                user.Email = email;
                user.NormalizedEmail = _userManager.NormalizeEmail(email);

                if (string.IsNullOrWhiteSpace(user.UserName) ||
                    string.Equals(user.UserName, user.PhoneNumber, StringComparison.OrdinalIgnoreCase) ||
                    IsPlaceholderEmail(user.UserName))
                {
                    user.UserName = email;
                    user.NormalizedUserName = _userManager.NormalizeName(email);
                }

                emailUpdated = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var hasPassword = await _userManager.HasPasswordAsync(user);
                if (hasPassword)
                    return UserLoginAuthResult.Fail("Password already set. Use existing change-password flow.");

                var addPasswordResult = await _userManager.AddPasswordAsync(user, request.Password.Trim());
                if (!addPasswordResult.Succeeded)
                    return UserLoginAuthResult.Fail(GetErrorMessage(addPasswordResult, "Unable to set password."));
            }

            if (!string.IsNullOrWhiteSpace(request.Pin))
            {
                var pin = request.Pin.Trim();
                if (!IsValidPin(pin))
                    return UserLoginAuthResult.Fail("PIN must be 4 to 6 digits.");

                var pinHash = _pinHasher.HashPassword(user, pin);
                var setPinResult = await _userManager.SetAuthenticationTokenAsync(user, PinTokenProvider, PinTokenName, pinHash);
                if (!setPinResult.Succeeded)
                    return UserLoginAuthResult.Fail(GetErrorMessage(setPinResult, "Unable to set PIN."));
            }

            if (emailUpdated)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return UserLoginAuthResult.Fail(GetErrorMessage(updateResult, "Unable to update profile email."));

                if (!string.IsNullOrWhiteSpace(user.TenantId))
                {
                    var tenant = await _tenantRepository.GetById(user.TenantId);
                    if (tenant != null &&
                        !string.IsNullOrWhiteSpace(user.Email) &&
                        !string.Equals(tenant.Email, user.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        tenant.Email = user.Email;
                        await _tenantRepository.Update(tenant);
                    }
                }
            }

            return new UserLoginAuthResult
            {
                Success = true,
                Message = "Profile security updated successfully.",
                User = user,
                SetupStatus = await GetProfileSetupStatusAsync(user)
            };
        }

        public async Task<ProfileSetupStatusViewModel> GetProfileSetupStatusAsync(ApplicationUsers user)
        {
            if (user == null)
                return new ProfileSetupStatusViewModel();

            var pinHash = await _userManager.GetAuthenticationTokenAsync(user, PinTokenProvider, PinTokenName);
            var hasPassword = await _userManager.HasPasswordAsync(user);

            return new ProfileSetupStatusViewModel
            {
                HasPin = !string.IsNullOrWhiteSpace(pinHash),
                HasPassword = hasPassword,
                HasEmail = HasUsableEmail(user.Email)
            };
        }

        public static string NormalizeMobileNumber(string? mobileNumber)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber))
                return string.Empty;

            var digits = Regex.Replace(mobileNumber, "[^0-9]", string.Empty);
            if (digits.Length > 10)
                digits = digits.Substring(digits.Length - 10);

            return digits;
        }

        private static bool IsValidMobileNumber(string mobileNumber)
            => Regex.IsMatch(mobileNumber, "^[6-9][0-9]{9}$");

        private static bool IsValidPin(string pin)
            => Regex.IsMatch(pin, "^[0-9]{4,6}$");

        private async Task TryDeleteTenantAsync(Tenant tenant)
        {
            try
            {
                var tenantRepo = _unitOfWork.GetRepository<Tenant>();
                tenantRepo.Delete(tenant);
                using (var transaction = tenantRepo.BeginTransaction())
                {
                    await tenantRepo.SaveChangesAsync();
                    transaction.Commit();
                }
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        private static bool HasUsableEmail(string? email)
            => !string.IsNullOrWhiteSpace(email);

        private static bool IsPlaceholderEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return email.Trim().EndsWith($"@{PlaceholderEmailDomain}", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetErrorMessage(IdentityResult result, string fallbackMessage)
        {
            return result.Errors.FirstOrDefault()?.Description ?? fallbackMessage;
        }
    }

    public class UserLoginAuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ApplicationUsers? User { get; set; }
        public string? OtpCode { get; set; }
        public string? TenantId { get; set; }
        public ProfileSetupStatusViewModel? SetupStatus { get; set; }

        public static UserLoginAuthResult Fail(string message)
        {
            return new UserLoginAuthResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
