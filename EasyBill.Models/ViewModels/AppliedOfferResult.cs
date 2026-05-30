using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class AppliedOfferResult
    {
        //public decimal Discount { get; set; } = 0;
        //public List<SalesItemVM> FreeItems { get; set; } = new();
        //public List<string> AppliedOffers { get; set; } = new();
        public decimal TotalDiscount { get; set; } = 0;
        public List<OfferVM> Offers { get; set; } = new();
        public List<SalesItemVM> FreeItems { get; set; } = new();
    }
}
