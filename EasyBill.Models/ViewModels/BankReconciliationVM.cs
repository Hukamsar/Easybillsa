using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class BankReconciliationVM
    {
        public int? BankId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchQuery { get; set; }
        
        public List<BankReconciliationItemVM> Transactions { get; set; } = new List<BankReconciliationItemVM>();
        public List<SelectListItem> Banks { get; set; } = new List<SelectListItem>();
    }

    public class BankReconciliationItemVM
    {
        public int Id { get; set; }
        public string VoucherType { get; set; } // "Payment" or "Receive"
        public string VoucherNo { get; set; }
        public DateTime Date { get; set; }
        public string PartyName { get; set; }
        public string PaymentModeName { get; set; }
        public string? ChequeNo { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? RefNo { get; set; }
        public decimal Amount { get; set; }
        public DateTime? ClearedDate { get; set; }
        public bool IsCleared => ClearedDate.HasValue;
    }

    public class ReconciliationUpdateVM
    {
        public int Id { get; set; }
        public string VoucherType { get; set; } // "Payment" or "Receive"
        public DateTime? ClearedDate { get; set; }
    }
}
