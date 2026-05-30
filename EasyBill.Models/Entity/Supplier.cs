using AOne.Models;
using AOne.Models.Entity;
using AOne.Utility.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class Supplier:BaseEntity,IMayHaveTenant
    {
        [Key]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string? Address { get; set; } 
        public string? Area { get; set; }
        public string? PinCode { get; set; }
        public string? PhoneNO { get; set; }
        public string? Email { get; set; }
        [StringLength(15)]
        public string? GstNO { get; set; }
        public string? ManufacturingLicNO { get; set; }
        public string? DrugLicNO { get; set; }
        public string? IFSSAINo { get; set; }
        public GSTType Type { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        public int? AccountGroupId { get; set; }
        [ForeignKey("AccountGroupId")]
        public AccountGroup? AccountGroup { get; set; }
        public int? CountryId { get; set; }
        [ForeignKey("CountryId")]
        public Country? Country { get; set; }
        public int? StateId {  get; set; }
        [ForeignKey("StateId")]
        public State? State { get; set; }
        public int? CityId { get; set; }
        [ForeignKey("CityId")]
        public City? City { get; set; }
        public int? CurrencyId { get; set; }
        [ForeignKey("CurrencyId")]
        public Currency? Currency { get; set; }
        public int? ParentSupplierId { get; set; }
        [ForeignKey("ParentSupplierId")]
        public Supplier? ParentSupplier { get; set; }
        public decimal Balance { get; set; }
        public string? AccountNo { get; set; }
        public string? RTGSNo { get; set; }
        public string? IFSCCode {  get; set; }
        public string? Branch {  get; set; }
        public string? MICRNo { get; set; } 
        public int PaymentDays {  get; set; }

    }
}
