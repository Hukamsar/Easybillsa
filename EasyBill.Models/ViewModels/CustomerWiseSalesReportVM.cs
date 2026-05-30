using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class CustomerWiseSalesReportVM
    {
        public string ReportType { get; set; } // Summary / Details

        public List<CustomerWiseSalesSummaryVM> Summary { get; set; }
        public List<CustomerWiseSalesDetailsVM> Details { get; set; }
    }

}
