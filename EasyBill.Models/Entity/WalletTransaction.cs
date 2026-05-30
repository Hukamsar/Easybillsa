using AOne.Models;
using AOne.Models.Entity;
using AOne.Utility.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class WalletTransaction : BaseEntity, IMayHaveTenant, IMasterEntity
    {
        public int Id { get; set; }

        public int WalletMasterId { get; set; }

        [ForeignKey(nameof(WalletMasterId))]
        public WalletMaster? WalletMaster { get; set; }

        public DateTime TransactionDateTime { get; set; } = DateTime.Now;

        public decimal Credit { get; set; }

        public decimal Debit { get; set; }

        public WalletTransactionType TransactionType { get; set; } = WalletTransactionType.ManualCredit;

        [MaxLength(40)]
        public string? PaymentMode { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(80)]
        public string? ReferenceBillNo { get; set; }

        public int? ReferenceSaleId { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(150)]
        public string? UserName { get; set; }

        [MaxLength(600)]
        public string? Remarks { get; set; }

        public decimal ClosingBalance { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public Tenant? Tenant { get; set; }

        public string? TenantId { get; set; }
    }
}
