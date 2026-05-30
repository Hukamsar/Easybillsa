using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models;
using AOne.Models.Entity;

namespace EasyBill.Models.Entity
{
    public class HoldSales : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }

        public string HoldToken { get; set; } = null!; // H001, H002
        public string? billingType { get; set; }
        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public string? CustomerName { get; set; }
        public string? MobileNo { get; set; }
        public string? Address { get; set; }

        public DateTime HoldDate { get; set; }

        public decimal Total { get; set; }
        public decimal TotalGstAmt { get; set; }
        public decimal Totaldiscount { get; set; }
        public decimal discountPercent { get; set; }
        public decimal discountAmount { get; set; }
        public decimal TotalPayable { get; set; }

        public int? PharmacyDoctorId { get; set; }
        [ForeignKey(nameof(PharmacyDoctorId))]
        public PharmacyDoctor? PharmacyDoctor { get; set; }
        public string? DoctorMobileNumber { get; set; }
        public string? DoctorRegNumber { get; set; }

        public int? OfferId { get; set; }

        public IList<HoldSalesItem> HoldSalesItems { get; set; } = new List<HoldSalesItem>();

        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }

}
