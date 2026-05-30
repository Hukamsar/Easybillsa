using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class PurchaseReturnVM 
    {
        public PurchaseReturnVM()
        {
            PurchaseReturnItemVMs = new List<PurchaseReturnItemVM>();
        }
        public int Id { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public string PartyBillNo { get; set; }
        public DateTime? PartyBillDate { get; set; }
        public int Reason { get; set; }
        public decimal Total { get; set; }
        public decimal TotalGstAmt { get; set; }
        public decimal Totaldiscount { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal discountPercent { get; set; } = 0.0M;
        public decimal discountAmount { get; set; }
        public List<PurchaseReturnItemVM> PurchaseReturnItemVMs { get; set; }
        [Display(Name = "Round Off")]
        public decimal RoundOffAmount { get; set; }
        public decimal TotalCessAmt { get; set; }
    }
}
