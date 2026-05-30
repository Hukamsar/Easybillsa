using AOne.Models;
using AOne.Models.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class TenantWalletHistory : BaseEntity, IMayHaveTenant, IMasterEntity
    {
        public int Id { get; set; }

        public DateTime TransactionDateTime { get; set; } = DateTime.Now;

        public decimal Credit { get; set; }

        public decimal Debit { get; set; }

        [MaxLength(40)]
        public string? PaymentMode { get; set; }

        [MaxLength(100)]
        public string? ReferenceNo { get; set; }

        [MaxLength(100)]
        public string? GatewayTransactionId { get; set; }

        public int? ReferenceSaleId { get; set; }

        [MaxLength(80)]
        public string? ServiceType { get; set; }

        public decimal ServiceCharge { get; set; }

        [MaxLength(600)]
        public string? Remarks { get; set; }

        public decimal ClosingBalance { get; set; }

        public string? TenantId { get; set; }

        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }
    }
}
