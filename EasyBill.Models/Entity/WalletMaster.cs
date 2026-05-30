using AOne.Models;
using AOne.Models.Entity;
using AOne.Utility.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class WalletMaster : BaseEntity, IMayHaveTenant, IMasterEntity
    {
        public int Id { get; set; }

        [MaxLength(40)]
        public string WalletID { get; set; } = string.Empty;

        public WalletOwnerType WalletType { get; set; } = WalletOwnerType.CustomerWallet;

        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public int? DeliveryBoyId { get; set; }

        [ForeignKey(nameof(DeliveryBoyId))]
        public Employee? DeliveryBoy { get; set; }

        [MaxLength(160)]
        public string? DeliveryBoyName { get; set; }

        [MaxLength(20)]
        public string? DeliveryBoyMobileNo { get; set; }

        public decimal CurrentBalance { get; set; }

        public WalletStatus Status { get; set; } = WalletStatus.Active;

        public decimal? PerBillUsageLimit { get; set; }

        public bool AutoDeductInBilling { get; set; } = true;

        public bool IsExpiryEnabled { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public Tenant? Tenant { get; set; }

        public string? TenantId { get; set; }

        public IList<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
    }
}
