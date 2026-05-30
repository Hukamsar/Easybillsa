using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AOne.Models.ViewModels;

namespace AOne.DataAccess.ProfileService
{
    public interface IProfileService
    {
        Task Set(ClaimsPrincipal principal);
        //Task Update(UserProfile profile);
        UserProfile Profile { get; }

        //event Action? OnChange;
        //Task UpdateUserProfileAsync(UserProfile profile);
    }
}
