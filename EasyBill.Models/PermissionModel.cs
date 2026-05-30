using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Models
{
    public class PermissionModel
    { 
        public string? Description { get; set; }
        public string? Group { get; set; }
        public string? ClaimType { get; set; }
        public string? ClaimValue { get; set; }
        public bool Assigned { get; set; }

        public string? RoleId { get; set; }
    }
}
