using AOne.Models.Entity;
using AOne.Utility.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class ItemMasterVM
    {
        public int Id { get; set; }


        [Required(ErrorMessage = "Name is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).+$", ErrorMessage = "Name cannot start or end with spaces.")]
        public string Name { get; set; }


        [Required(ErrorMessage = "Code is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).+$", ErrorMessage = "Code cannot start or end with spaces.")]
        public string Code { get; set; }
        public string? Barcode { get; set; }
        public string? Unit { get; set; }
        public string? Packing { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please select Category.")]
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please select Sub Category.")]
        public int? SubCategoryId { get; set; }
        public string? SubCategoryname { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please select Division.")]
        public int? DivisionId { get; set; }
        public string? DivisionName { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please select a valid HSN.")]
        public int? HsnId { get; set; }
        public string? HsnCode { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Please select Company.")]
        public int? CompanyId { get; set; }
        public string? Companyname { get; set; }
        public decimal Mrp { get; set; }
        public decimal SalesRate1 { get; set; }
        public decimal SalesRate2 { get; set; }
        public int MinimumQty { get; set; }
        public int MaximumQty { get; set; }
        public int ShelfLife { get; set; }
        public string? ShelfLifeUnit { get; set; }

        [Range(0, 100, ErrorMessage = "Maximum Discount 0 se 100 ke beech hona chahiye")]
        public decimal MaximumDiscount { get; set; }
        public bool DecemalAllowed { get; set; }
        public int Conversion { get; set; }
        public decimal SGST { get; set; }
        public decimal CGST { get; set; }
        public decimal IGST { get; set; }
        public decimal Cess { get; set; }
        public string? UploadImage { get; set; }
        public IFormFile? Photo { get; set; }

        //[Required(ErrorMessage = "Please select Tax Status.")]
        public TaxStatus? Local { get; set; }

        //[Required(ErrorMessage = "Please select Central Tax Status.")]
        public TaxStatus? Central { get; set; }
        public bool? IsActive { get; set; }

        public bool Narcotics { get; set; }
        public bool ScheduleH { get; set; }
        public bool ScheduleH1 { get; set; }
        public string? Salt { get; set; }

        public List<IFormFile>? Images { get; set; }
        public int PrimaryIndex { get; set; }
        public List<ItemImageVM> ExistingImages { get; set; } = new();
        public int? PrimaryImageId { get; set; }
        public List<int> DeletedImageIds { get; set; } = new();
        public string? ItemType { get; set; } // Standard, Bulk, Repacked
        public int? ParentItemId { get; set; }
        public decimal? ConversionFactor { get; set; }
    }
}
