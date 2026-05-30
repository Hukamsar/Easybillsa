using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class CustomerWiseSalesSummaryVM
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalSales { get; set; }
    }

}
