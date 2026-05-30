using AOne.Models;
using AOne.Models.Entity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class PurchaseChallanItem : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int PurchaseChallanId { get; set; }
        [ForeignKey(nameof(PurchaseChallanId))]
        public PurchaseChallan? PurchaseChallan { get; set; }
        public string? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
        public string? Batch { get; set; }
        public int ItemId { get; set; }
        [ForeignKey(nameof(ItemId))]
        public ItemMaster? ItemMasters { get; set; }
        public int Qty { get; set; }
        public int FreeQty { get; set; }
        public string? Unit { get; set; }
        public decimal Rate { get; set; }
        public int? HsnId { get; set; }
        [ForeignKey(nameof(HsnId))]
        public Hsn? Hsns { get; set; }
        public decimal Gst { get; set; }
        public decimal GstAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal DiscountAmt { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalAmt { get; set; }
        public decimal BatchWiseCose { get; set; }
        public decimal Mrp { get; set; }
        public decimal salserateA { get; set; }
        public decimal salserateB { get; set; }
        public string? Barcode { get; set; }
        public decimal CGst { get; set; }
        public decimal SGst { get; set; }
        public decimal CGstAmount { get; set; }
        public decimal SGstAmount { get; set; }
        public int ConvertedQty { get; set; }
    }
}
