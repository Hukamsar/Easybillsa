using AOne.Utility.Enums;
using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class OfferVM
    {
        public OfferVM()
        {
            OfferItems = new List<OfferItemVM>();
        }
        public int Id { get; set; }

        [Required(ErrorMessage = "OfferName is required.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "OfferName must be between 3 and 150 characters.")]
        [RegularExpression(@"^(?!\s)(?!.*\s$).+$", ErrorMessage = "OfferName cannot start or end with spaces.")]
        public string OfferName { get; set; }
        public OfferType? OfferType { get; set; } // "Flat", "Percent", "BuyXGetY"
        public Applicable? Applicable { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MinAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsActive { get; set; }
        public int? CompanyId { get; set; }
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public int? ItemId { get; set; }
        public int? BuyQty { get; set; }
        public int? FreeQty { get; set; }
        public List<OfferItemVM> OfferItems { get; set; }
    }
}
