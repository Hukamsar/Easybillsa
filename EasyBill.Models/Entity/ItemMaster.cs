using AOne.Utility.Enums;
using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace AOne.Models.Entity
{
    public class ItemMaster : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Barcode {  get; set; }
        public string? Unit1 { get; set; }
        public string? Unit2 { get; set; }
        public string? Packing { get; set; }
        public int? CategoryId {  get; set; }
        [ForeignKey(nameof(CategoryId))]
        public CategoryMaster? Category { get; set; }
        public int? DivisionId { get; set; }
        [ForeignKey(nameof(DivisionId))]
        public Division? Division { get; set; }
        public int? HsnId { get; set; }
        [ForeignKey(nameof(HsnId))]
        public Hsn? Hsn { get; set; }
        public decimal Mrp {  get; set; }
        public decimal SalesRate1 { get; set; }
        public decimal SalesRate2 { get; set; }
        public int MinimumQty { get; set; }
        public int MaximumQty { get; set; }
        public int ShelfLife { get; set; }
        public string? ShelfLifeUnit { get; set; }
        public decimal MaximumDiscount { get; set; }
        public bool DecemalAllowed {  get; set; }
        public int Conversion { get; set; }
        public int? SubCategoryId { get; set; }
        [ForeignKey(nameof(SubCategoryId))]
        public SubCategory? SubCategory { get; set; }
        public int? CompanyId { get; set; }
        [ForeignKey(nameof(CompanyId))]
        public Company? Company { get; set; }
        public Tenant? Tenant { get; set; }
        public  string? TenantId { get; set; }
        public string? UploadImage { get; set; }
        public TaxStatus? Local { get; set; }
        public TaxStatus? Central { get; set; }
        public bool IsActive { get; set; } = true;

        public bool Narcotics { get; set; }
        public bool ScheduleH { get; set; }
        public bool ScheduleH1 { get; set; }
        public string? Salt { get; set; }
        public string? ItemType { get; set; } // Standard, Bulk, Repacked
        public int? ParentItemId { get; set; }
        [ForeignKey(nameof(ParentItemId))]
        public ItemMaster? ParentItem { get; set; }
        public decimal? ConversionFactor { get; set; }
        public ICollection<ItemImage> ItemImages { get; set; } = new List<ItemImage>();
    }
}
