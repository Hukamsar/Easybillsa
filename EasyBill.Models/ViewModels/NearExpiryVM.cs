using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class NearExpiryVM
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string CategoryName { get; set; }
        public string Unit { get; set; }
        public string BatchNo { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal RemainingQty { get; set; }
        public int DaysToExpire { get; set; }
        public decimal Stocks { get; set; }
    }
}
