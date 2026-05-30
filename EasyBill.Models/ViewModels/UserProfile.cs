using AOne.Utility.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Models.ViewModels
{
    public class UserProfile
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string? Avatar { get; set; }  // ✅ Store Avatar if available
        public string? PhoneNumber { get; set; }
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }
        public BusinessType? BusinessType { get; set; }
    }
}
