using EasyBill.Models.Model;
using EasyBill.Models.ViewModels;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AOneWeb.Service.AuthService
{
    public class AuthService
    {
        private readonly IConfiguration _config;
        public AuthService(IConfiguration config)
        {
            _config = config;
        }

        public AuthResponse GenerateJwtToken(ApplicationUsers user, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("S0M3RAN0MS3CR3T!1!MAG1C!1!3CR3T!1!MAG1C!1!3CR3T!1!MAG1C!1!3CR3T!1!MAG1C!1!"); // Secure this key

            var claims = new List<Claim>
             {
                 new Claim(ClaimTypes.NameIdentifier, user.Id),
                 new Claim(ClaimTypes.Name, user.UserName ?? ""),
                 new Claim(ClaimTypes.Email, user.Email ?? ""),
                 new Claim(ClaimTypes.Role, role),
                 new Claim("TenantId", user.TenantId ?? ""),
                 new Claim("TenantName", user.TenantName ?? ""),
                 new Claim("PhoneNumber", user.PhoneNumber ?? "")
             }; 
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(2), // Token expiry time
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            }; 
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            return new AuthResponse
            {
                Token = jwtToken,
                TokenExpiry = tokenDescriptor.Expires.Value
            };
        }

        public AuthResponse GenerateCustomerToken(CustomerResponse customer)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("S0M3RAN0MS3CR3T!1!MAG1C!1!3CR3T!1!MAG1C!1!3CR3T!1!MAG1C!1!3CR3T!1!MAG1C!1!");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, customer.IdentityUserId ?? ""),
                new Claim("CustomerId", customer.Id.ToString()),
                new Claim("CustomerName", customer.FullName ?? ""),
                new Claim("PhoneNumber", customer.MobileNumber),
                new Claim(ClaimTypes.Role, "Customer"),
                new Claim("IsMobileVerified", customer.IsMobileVerified.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),


                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            return new AuthResponse
            {
                Token = jwtToken,
                TokenExpiry = tokenDescriptor.Expires.Value
            };
        }



    }
}
