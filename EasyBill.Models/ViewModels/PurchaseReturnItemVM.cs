using AOne.Utility.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class PurchaseReturnItemVM
    {
        public int Id { get; set; }
        public int PurchaseId { get; set; }
        public string Purchases { get; set; }
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? Batch { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int FreeQty { get; set; }
        public int Qty { get; set; }
        public string? Unit { get; set; }
        public decimal Rate { get; set; }
        public int HsnId { get; set; }
        public decimal Gst { get; set; }
        public decimal GstAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal DiscountAmt { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalAmt { get; set; }
        public decimal BatchWiseCose { get; set; }
        public decimal Mrp { get; set; }
        public decimal salserateA { get; set; }
        public decimal salserateB { get; set; }
        //public Purchasereturnreason? Reason { get; set; }
        public decimal Cess { get; set; }
    }
}
