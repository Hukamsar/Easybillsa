using AOne.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class InvoiceThemeSettingVM
    {
        public int Id { get; set; } 

        // =========================
        // IDENTIFIERS
        // =========================

        public string? ApplicationUserId { get; set; }

        [Required]
        public string? PaperSize { get; set; }

        public bool IsDefault { get; set; }

        // =========================
        // LAYOUT & BRANDING
        // =========================

        public string? TemplateName { get; set; }

        public string? PrimaryColor { get; set; }

        public string? TextColor { get; set; }

        public string? FontFamily { get; set; }

        public int? BaseFontSize { get; set; }

        public string? Title { get; set; }

        public string? Margins { get; set; }

        public string? Orientation { get; set; }

        // =========================
        // LOGO
        // =========================

        public string? LogoPath { get; set; }

        public int? LogoSize { get; set; }

        public string? LogoPosition { get; set; }

        // =========================
        // WATERMARK
        // =========================

        public string? WatermarkPath { get; set; }

        public int? WatermarkOpacity { get; set; }

        public int? WatermarkSize { get; set; }

        // =========================
        // SIGNATURE
        // =========================

        public string? SignaturePath { get; set; }

        public string? SignatoryLabel { get; set; }

        // =========================
        // GST SETTINGS
        // =========================

        public bool? ShowHSN { get; set; }

        public bool? ShowGSTPercent { get; set; }

        public bool? ShowGSTAmount { get; set; }

        public bool? ShowTaxSummaryTable { get; set; }

        public bool? ShowBusinessGSTIN { get; set; }

        public bool? ShowItemDiscount { get; set; }

        public bool? IsZebraStriped { get; set; }

        public bool? ShowAmountInWords { get; set; }

        // =========================
        // PAYMENT DETAILS
        // =========================

        public string? BankDetails { get; set; }

        public string? TermsAndConditions { get; set; }

        public bool? ShowPaymentQR { get; set; }

        public string? UPIDetails { get; set; }

        // =========================
        // OTHER
        // =========================

        public DateTime? UpdatedAt { get; set; }

        public int? ThermalPaperSize { get; set; }

        // =========================
        // EXTRA
        // =========================

        public bool IsNotConfigured { get; set; }
    }
}
