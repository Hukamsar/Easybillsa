using System.ComponentModel.DataAnnotations.Schema;
using AOne.Models;
using AOne.Models.Entity;

namespace EasyBill.Models.Entity
{
    public class CustomerAddress : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public string? TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public string Title { get; set; } = "Home"; // e.g., Home, Office, Other
        public string AddressLine { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Pincode { get; set; }

        // Geo-Location
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}
