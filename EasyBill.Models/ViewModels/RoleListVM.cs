using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class RoleListVM
    {
        public string RoleName { get; set; }
        public List<IdentityRole> ExistingRoles { get; set; } = new List<IdentityRole>();
    }
}
