using AOne.Models;
using AOne.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    #region Invoice Theme Configuration Model (Nullable Architecture)
    /// <summary>
    /// Professional Normalized Model for Invoice Settings.
    /// Fully adapted to the Nullable Architecture to match the 'Sales' entity pattern.
    /// Engineered for Enterprise Billing Systems (A4, A5, Thermal support).
    /// </summary>
    public class InvoiceThemeSetting : BaseEntity, IMayHaveTenant
    {
        [Key]
        public int Id { get; set; }

        #region Multi-Tenancy
        public string? TenantId { get; set; }

        [ValidateNever]
        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }
        #endregion

        // Identifiers
        [Required]
        public string ApplicationUserId { get; set; }

        [Required]
        public string PaperSize { get; set; } // e.g., "A4", "A5", "Thermal"

        // BTR ADDITION: To identify the active default layout for the user
        public bool IsDefault { get; set; } = false;

        #region Layout & Branding
        // BTR ADDITION: For Layout Styles
        public string? TemplateName { get; set; } // e.g., "Classic", "Modern"

        public string? PrimaryColor { get; set; }
        public string? TextColor { get; set; }
        public string? FontFamily { get; set; }

        // BTR ADDITION: Font scaling
        public int? BaseFontSize { get; set; }

        public string? Title { get; set; }
        public string? Margins { get; set; } // "Narrow", "Normal", "Wide"
        public string? Orientation { get; set; } // "Portrait", "Landscape"

        // Persistent Paths for Assets
        public string? LogoPath { get; set; }
        public int? LogoSize { get; set; }
        public string? LogoPosition { get; set; } // "Left", "Center", "Right"

        public string? WatermarkPath { get; set; }
        public int? WatermarkOpacity { get; set; }
        public int? WatermarkSize { get; set; }

        public string? SignaturePath { get; set; }
        public string? SignatoryLabel { get; set; }
        #endregion

        #region GST & Compliance Toggles (Nullable Booleans)
        public bool? ShowHSN { get; set; }
        public bool? ShowGSTPercent { get; set; }
        public bool? ShowGSTAmount { get; set; }
        public bool? ShowTaxSummaryTable { get; set; }
        public bool? ShowBusinessGSTIN { get; set; }
        public bool? ShowItemDiscount { get; set; }
        public bool? IsZebraStriped { get; set; }
        public bool? ShowAmountInWords { get; set; }
        #endregion

        #region Payment & Footer Details
        public string? BankDetails { get; set; }
        public string? TermsAndConditions { get; set; }
        public bool? ShowPaymentQR { get; set; }
        public string? UPIDetails { get; set; }
        #endregion

        public DateTime? UpdatedAt { get; set; }
        public int? ThermalPaperSize { get; set; }
    }
    #endregion
}