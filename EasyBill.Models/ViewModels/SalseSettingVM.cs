using AOne.Models.Entity;
using AOne.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Utility.Enums;

namespace EasyBill.Models.ViewModels
{
    public class SalseSettingVM
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } 
        public bool DoctorRequired { get; set; }
        public bool ItemBarCodeBase { get; set; }
        public string? ItemConversion { get; set; }
        public bool RateRoundUp { get; set; }
        public bool AllowNegative { get; set; }

        public int ExpiryAllowedDays { get; set; }
        public bool IsNotConfigured { get; set; }
        public string? PrintType { get; set; }
        public int? ThermalPaperSize { get; set; }
        public SalesTax SalesTax { get; set; }
        public bool showdiscount { get; set; }
    }
}
