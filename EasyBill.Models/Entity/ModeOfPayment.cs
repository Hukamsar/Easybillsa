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
    public class ModeOfPayment : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        public modeofpayment? PaymentType { get; set; }

        public int? BankId { get; set; }
        [ForeignKey("BankId")]
        public Bank? Bank { get; set; }
    }
}
