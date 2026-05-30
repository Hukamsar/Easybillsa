using EasyBill.Models.Entity;
using System.Data;

namespace EasyBill.DataAccess.StoredProcedures
{
    public static class PurchaseModuleTableTypeMapper
    {
        public static DataTable CreatePurchaseItemTable(IEnumerable<PurchaseItem>? items)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("ItemId", typeof(int));
            table.Columns.Add("Batch", typeof(string));
            table.Columns.Add("ExpiryDate", typeof(DateTime));
            table.Columns.Add("Mrp", typeof(decimal));
            table.Columns.Add("Qty", typeof(int));
            table.Columns.Add("FreeQty", typeof(int));
            table.Columns.Add("Unit", typeof(string));
            table.Columns.Add("Rate", typeof(decimal));
            table.Columns.Add("HsnId", typeof(int));
            table.Columns.Add("Gst", typeof(decimal));
            table.Columns.Add("GstAmount", typeof(decimal));
            table.Columns.Add("Discount", typeof(decimal));
            table.Columns.Add("DiscountAmt", typeof(decimal));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("TotalAmt", typeof(decimal));
            table.Columns.Add("BatchWiseCose", typeof(decimal));
            table.Columns.Add("salserateA", typeof(decimal));
            table.Columns.Add("salserateB", typeof(decimal));
            table.Columns.Add("Barcode", typeof(string));
            table.Columns.Add("CGst", typeof(decimal));
            table.Columns.Add("SGst", typeof(decimal));
            table.Columns.Add("CGstAmount", typeof(decimal));
            table.Columns.Add("SGstAmount", typeof(decimal));
            table.Columns.Add("Cess", typeof(decimal));
            table.Columns.Add("SourcePurchaseChallanId", typeof(int));

            foreach (var item in items ?? Enumerable.Empty<PurchaseItem>())
            {
                var row = table.NewRow();
                row["Id"] = item.Id > 0 ? item.Id : DBNull.Value;
                row["ItemId"] = item.ItemId;
                row["Batch"] = string.IsNullOrWhiteSpace(item.Batch) ? DBNull.Value : item.Batch.Trim();
                row["ExpiryDate"] = item.ExpiryDate.HasValue ? item.ExpiryDate.Value : DBNull.Value;
                row["Mrp"] = item.Mrp;
                row["Qty"] = item.Qty;
                row["FreeQty"] = item.FreeQty;
                row["Unit"] = string.IsNullOrWhiteSpace(item.Unit) ? DBNull.Value : item.Unit.Trim();
                row["Rate"] = item.Rate;
                row["HsnId"] = item.HsnId.HasValue ? item.HsnId.Value : DBNull.Value;
                row["Gst"] = item.Gst;
                row["GstAmount"] = item.GstAmount;
                row["Discount"] = item.Discount;
                row["DiscountAmt"] = item.DiscountAmt;
                row["Amount"] = item.Amount;
                row["TotalAmt"] = item.TotalAmt;
                row["BatchWiseCose"] = item.BatchWiseCose;
                row["salserateA"] = item.salserateA;
                row["salserateB"] = item.salserateB;
                row["Barcode"] = string.IsNullOrWhiteSpace(item.Barcode) ? DBNull.Value : item.Barcode.Trim();
                row["CGst"] = item.CGst;
                row["SGst"] = item.SGst;
                row["CGstAmount"] = item.CGstAmount;
                row["SGstAmount"] = item.SGstAmount;
                row["Cess"] = item.Cess;
                row["SourcePurchaseChallanId"] = item.SourcePurchaseChallanId.HasValue ? item.SourcePurchaseChallanId.Value : DBNull.Value;
                table.Rows.Add(row);
            }

            return table;
        }

        public static DataTable CreatePurchasePaymentDetailTable(IEnumerable<SalsePaymentDetails>? payments)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Date", typeof(DateTime));
            table.Columns.Add("PaymentModeId", typeof(int));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("ReferenceNo", typeof(string));
            table.Columns.Add("Description", typeof(string));

            foreach (var payment in payments ?? Enumerable.Empty<SalsePaymentDetails>())
            {
                var row = table.NewRow();
                row["Id"] = payment.Id > 0 ? payment.Id : DBNull.Value;
                row["Date"] = payment.Date.HasValue ? payment.Date.Value : DBNull.Value;
                row["PaymentModeId"] = payment.PaymentModeId;
                row["Amount"] = payment.Amount;
                row["ReferenceNo"] = string.IsNullOrWhiteSpace(payment.ReferenceNo) ? DBNull.Value : payment.ReferenceNo.Trim();
                row["Description"] = string.IsNullOrWhiteSpace(payment.Description) ? DBNull.Value : payment.Description.Trim();
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
