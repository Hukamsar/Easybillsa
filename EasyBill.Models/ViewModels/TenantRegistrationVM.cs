using AOne.Models.Entity;
using AOne.Utility.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class TenantRegistrationVM
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [MaxLength(156)]
        [Display(Name = "Company Name")]
        public string? Name { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public  string Email { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Location { get; set; }
        [Display(Name = "Country")]
        public int? CountryId { get; set; }
        public string? Country { get; set; }
        [Display(Name = "State")]
        public int? StateId { get; set; }
        public string? State { get; set; }
        [Display(Name = "City")]
        public int? CityId { get; set; }
        public string? City { get; set; }
        public string? PinCode { get; set; }
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? MobileNo { get; set; }
        public string? GstNo { get; set; }
        [Display(Name = "State Code")]
        public GstStateCode? StateCode { get; set; }
        public CompanyType? CompanyType { get; set; }
        public string? Branch { get; set; }
        public string? Logo { get; set; }
        public string? Description { get; set; }

        public string? IFSSAINo { get; set; }
        public string? DrugLicNo { get; set; }
        public DateTime? LicenceExpiryDate { get; set; }
        //Other Tabs
        public string? Jurisdiction { get; set; }
        public WorkingStyle? WorkingStyle { get; set; }

        // Company Details
        public string? BranchCode { get; set; }
        public BusinessType? BusinessType { get; set; }
        public CalenderType? CalenderType { get; set; }
        public DateTime? YearFrom { get; set; }
        public DateTime? YearTo { get; set; }
        public TaxType? TaxType { get; set; }
        public IFormFile? UploadAttachement { get; set; }

        [Display(Name = "Subscription Plan")]
        public int? SubscriptionPlanId { get; set; }
        public List<string>? SelectedFeatures { get; set; }
    }
}
