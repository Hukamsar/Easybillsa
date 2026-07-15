using AOne.Models;
using AOne.Models.Entity;
using AOne.Utility.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class Contra : BaseEntity, IMayHaveTenant
    {
        [Key]
        public int Id { get; set; }
        public required string VouncherNo { get; set; }
        public DateTime? Date { get; set; }
        public ContraCategory? Category { get; set; }
        public CashAndBank? CashAndBank { get; set; }
        public int? BankId { get; set; }
        [ForeignKey("BankId")]
        public Bank? Bank { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? Attachments { get; set; }
        public string? TenantId { get; set; }
        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }
    }
}
