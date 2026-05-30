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
    public class SalseSetting : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUsers ApplicationUsers { get; set; }
        public bool DoctorRequired { get; set; }
        public bool ItemBarCodeBase { get; set; }
        public string? ItemConversion { get; set; }
        public bool RateRoundUp { get; set; }
        public bool AllowNegative { get; set; }
        public int ExpiryAllowedDays { get; set; }

        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        public string? PrintType { get; set; }
        public int? ThermalPaperSize { get; set; }
        public SalesTax SalesTax { get; set; }
        public bool showdiscount { get; set; }
    }
}
