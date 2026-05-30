using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class BillwiseProfitVM
    {
        public int Id { get; set; }
        public string? BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public string? CustomerName { get; set; }
        public string? MobileNo { get; set; }

        // Sales
        public decimal TotalSalesValue { get; set; }   // Qty * Rate
        public decimal TotalGstAmt { get; set; }
        public decimal BillAmount { get; set; }         // TotalPayable
        public decimal Discount { get; set; }

        // Cost
        public decimal TotalCostValue { get; set; }     // Qty * CostRate

        // Profit
        public decimal GrossProfit { get; set; }        // Sales - Cost
        public decimal ProfitPct { get; set; }          // %

        // Counts
        public int TotalItems { get; set; }
        public decimal TotalQty { get; set; }

        // GST Mode
        public decimal TaxableAmount => TotalSalesValue - TotalGstAmt;
    }
}
