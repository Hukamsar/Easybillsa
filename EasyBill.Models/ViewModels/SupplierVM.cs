using AOne.Models.Entity;
using AOne.Utility.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class SupplierVM
    {
        public int Id { get; set; }
         
        [Display(Name = "Party Name")]
        [Required(ErrorMessage = "Party Name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Party Name must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).+$", ErrorMessage = "Party Name cannot start or end with spaces.")]
        public string FirstName { get; set; }
         
        [Required(ErrorMessage = "Address is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Address must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).+$", ErrorMessage = "Address cannot start or end with spaces.")]
        public string Address { get; set; }  
        [Required(ErrorMessage = "Area is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Area must be between 3 and 50 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).+$", ErrorMessage = "Area cannot start or end with spaces.")]
        public string Area { get; set; }
         
        // PIN CODE (India – 6 digits)
        [Required(ErrorMessage = "Pin Code is required")]
        [RegularExpression(@"^[1-9][0-9]{5}$", ErrorMessage = "Enter a valid 6-digit Pin Code")]
        public string PinCode { get; set; }
         
        // Phone Number (India)
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[6-9][0-9]{9}$",ErrorMessage = "Enter a valid 10-digit mobile number (numbers only)")]
        public string PhoneNO { get; set; }
         
        // Email Validation
        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }
         
        [StringLength(15, MinimumLength = 15, ErrorMessage = "GST Number must be exactly 15 characters")]
        [RegularExpression(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$",
            ErrorMessage = "Invalid GST Number format")]
        public string? GstNO { get; set; } 
        [Display(Name = "MFR Lic NO")]
        public string? ManufacturingLicNO { get; set; }
        [Display(Name = "Drug Lic NO")]
        public string? DrugLicNO { get; set; }
        [Display(Name = "IFSSAI No")]
        public string? IFSSAINo { get; set; } 
        [Required(ErrorMessage = "Type is required")]
        public GSTType Type { get; set; }
        public int? AccountGroupId { get; set; }
        public string? AccountGroupName { get; set; }
        public int? CountryId { get; set; }
        public string? CountryName {  get; set; }
        public int? StateId { get; set; }
        public string? StateName {  get; set; }
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public int? CurrencyId { get; set; }
        public string? CurrencyName { get; set; }   
        public int? ParentSupplierId { get; set; }
        public decimal Balance { get; set; }
        public string? AccountNo { get; set; }
        public string? RTGSNo { get; set; }
        public string? IFSCCode { get; set; }
        public string? Branch { get; set; }
        public string? MICRNo { get; set; }
        public string? Unit { get; set; }
        public decimal Rate { get; set; }
        public decimal Mrp { get; set; }
        public int PaymentDays { get; set; }
    }
}
