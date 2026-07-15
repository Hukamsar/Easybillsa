using AOne.Models.Entity;
using AOne.Utility.Enums;
using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class ItemMasterListVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? Unit1 { get; set; }
        public string? Unit2 { get; set; }
        public string? Packing { get; set; }

        public int? CategoryId { get; set; }
        public CategoryMaster? Category { get; set; }

        public int? DivisionId { get; set; }
        public Division? Division { get; set; }

        public int? HsnId { get; set; }
        public Hsn? Hsn { get; set; }

        public decimal Mrp { get; set; }
        public decimal SalesRate1 { get; set; }
        public decimal SalesRate2 { get; set; }

        public int MinimumQty { get; set; }
        public int MaximumQty { get; set; }

        public int ShelfLife { get; set; }
        public string? ShelfLifeUnit { get; set; }

        public decimal MaximumDiscount { get; set; }
        public bool DecemalAllowed { get; set; }

        public int Conversion { get; set; }

        public int? SubCategoryId { get; set; }
        public SubCategory? SubCategory { get; set; }

        public int? CompanyId { get; set; }
        public Company? Company { get; set; }

        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }

        public string? UploadImage { get; set; }

        public TaxStatus? Local { get; set; }
        public TaxStatus? Central { get; set; }

        public bool IsActive { get; set; }

        public bool Narcotics { get; set; }
        public bool ScheduleH { get; set; }
        public bool ScheduleH1 { get; set; }

        public string? Salt { get; set; }

        public string? ItemType { get; set; }

        public int? ParentItemId { get; set; }

        public ItemMaster? ParentItem { get; set; }

        public decimal? ConversionFactor { get; set; }

        public List<ItemImageVM> ItemImages { get; set; } = new();
    }

    
}
