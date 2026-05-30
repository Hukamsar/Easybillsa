using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class AccountGroupVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; } 
        public bool IsActive { get; set; }
        [ValidateNever]
        public IEnumerable<AccountGroup> AccountGroups { get; set; }
    }
}
