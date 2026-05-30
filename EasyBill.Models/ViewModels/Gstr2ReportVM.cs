using System;
using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class Gstr2ReportVM
    {
        public DateTime StartBillDate { get; set; }
        public DateTime EndBillDate { get; set; }
        public List<Gstr2SummaryVM> SummaryRows { get; set; } = new();
        public List<Gstr2DetailVM> DetailRows { get; set; } = new();
    }
}
