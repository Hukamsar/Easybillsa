using AOne.Models;
using AOne.Models.Entity;
using AOne.Utility.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class PurchaseSetting : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string? ApplicationUserId { get; set; }
        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUsers? ApplicationUsers { get; set; }
        public int MinPurchaseExpiryDays { get; set; }

        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        public PurchaseTax SalesTax { get; set; }
        public bool Expense { get; set; }
    }
}
