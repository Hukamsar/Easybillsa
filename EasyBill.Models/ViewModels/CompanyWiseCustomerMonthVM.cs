using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class CompanyWiseCustomerMonthVM
    {
        public string CustomerName { get; set; }
        public Dictionary<int, decimal> MonthAmounts { get; set; } = new();
    }

}
