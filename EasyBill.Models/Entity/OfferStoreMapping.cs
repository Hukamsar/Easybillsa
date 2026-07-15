using AOne.Models;
using AOne.Models.Entity;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class OfferStoreMapping : BaseEntity
    {
        public int Id { get; set; }

        public int OfferId { get; set; }
        [ForeignKey(nameof(OfferId))]
        public Offer? Offer { get; set; }

        public string? TenantId { get; set; }
    }
}