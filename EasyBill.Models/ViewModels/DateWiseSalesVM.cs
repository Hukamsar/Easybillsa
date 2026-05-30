using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class DateWiseSalesVM
    {
        public DateTime Date { get; set; }
        public int NoOfBills { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal GstAmount { get; set; }
        public decimal TotalValue { get; set; }
        public decimal RoundOffAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal RoundOff { get; set; }
    }

}
