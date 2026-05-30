using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AOne.Models.ViewModels;

namespace AOne.DataAccess.ProfileService
{
    public class ProfileService : IProfileService
    {
        public UserProfile Profile { get; set; } = new UserProfile();

        public Task Set(ClaimsPrincipal user)
        {
            Profile = new UserProfile
            {
                UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,  // ✅ Fix UserId
                UserName = user.FindFirst(ClaimTypes.Name)?.Value,
                Email = user.FindFirst(ClaimTypes.Email)?.Value,
                Role = user.FindFirst(ClaimTypes.Role)?.Value,
                TenantId = user.FindFirst("TenantId")?.Value,
                PhoneNumber = user.FindFirst("PhoneNumber")?.Value,
                TenantName = user.FindFirst("TenantName")?.Value
            };
            return Task.CompletedTask;
        }

        //public Task Update(UserProfile profile)
        //{
        //    Profile = profile;
        //    OnChange?.Invoke();
        //    return Task.CompletedTask;
        //}

        //Task IProfileService.UpdateUserProfileAsync(UserProfile profile)
        //{
        //    if (profile == null)
        //    {
        //        throw new ArgumentNullException(nameof(profile), "UserProfile cannot be null.");
        //    }
        //    Update(profile);
        //    OnChange?.Invoke();
        //    return Task.CompletedTask;
        //    //return profile;
        //}
    }
}
