using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class BatchVM
    {
        public int Id { get; set; }
        public string BatchNo { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal Mrp { get; set; }
        public decimal PurchaseRate { get; set; }
        public decimal SalesRateA { get; set; }
        public decimal SalesRateB { get; set; }
        public int Qty { get; set; }
    }
}
