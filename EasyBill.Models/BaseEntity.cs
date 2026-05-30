using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Models
{
    public class BaseEntity
    {
        public DateTime? Created { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModified { get; set; }
        public string? LastModifiedBy { get; set; } 
        public DateTime? Deleted { get; set; }
        public string? DeletedBy { get; set; } 
    }
}
