using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class CustomerWiseSalesDetailsVM
    {
        public string BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public string CustomerName { get; set; }
        public decimal BillValue { get; set; }
    }

}
