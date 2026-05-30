using AOne.Utility.Enums;

namespace EasyBill.Models.ViewModels
{
    public class SalseSettingApiRequestVM
    {
        public string? ApplicationUserId { get; set; }
        public bool DoctorRequired { get; set; }
        public bool ItemBarCodeBase { get; set; }
        public string? ItemConversion { get; set; }
        public bool RateRoundUp { get; set; }
        public bool AllowNegative { get; set; }
        public int ExpiryAllowedDays { get; set; }
        public string? PrintType { get; set; }
        public int? ThermalPaperSize { get; set; }

        // API contract: 0 = Inclusive, 1 = Exclusive
        public int? GstType { get; set; }
    }

    public class SalseSettingApiResponseVM
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public bool DoctorRequired { get; set; }
        public bool ItemBarCodeBase { get; set; }
        public string? ItemConversion { get; set; }
        public bool RateRoundUp { get; set; }
        public bool AllowNegative { get; set; }
        public int ExpiryAllowedDays { get; set; }
        public string? PrintType { get; set; }
        public int? ThermalPaperSize { get; set; }

        // API contract: 0 = Inclusive, 1 = Exclusive
        public int GstType { get; set; }
        public string GstTypeName { get; set; } = string.Empty;
        public bool IsNotConfigured { get; set; }
        public bool showdiscount { get; set; }
        public SalesTax SalesTax { get; set; }
    }
}
