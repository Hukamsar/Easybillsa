using System;
using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class BankBookDetailVM
    {
        public DateTime Date { get; set; }
        public string VoucherNo { get; set; } = string.Empty;
        public string Particular { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Bank { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
        public string PaymentModeRef { get; set; } = string.Empty;
    }

    public class BankBookVM
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<BankBookDetailVM> Details { get; set; } = new List<BankBookDetailVM>();
    }
}
