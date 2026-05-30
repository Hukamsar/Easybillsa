using EasyBill.Models.Entity;
using System.Data;

namespace EasyBill.DataAccess.StoredProcedures
{
    public static class SalesModuleTableTypeMapper
    {
        public static DataTable CreateSalesItemTable(IEnumerable<SalesItem>? items)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("ItemMasterId", typeof(int));
            table.Columns.Add("PurchaseItemId", typeof(int));
            table.Columns.Add("Batch", typeof(string));
            table.Columns.Add("ExpiryDate", typeof(DateTime));
            table.Columns.Add("Mrp", typeof(decimal));
            table.Columns.Add("Qty", typeof(decimal));
            table.Columns.Add("Rate", typeof(decimal));
            table.Columns.Add("StripRate", typeof(decimal));
            table.Columns.Add("Gst", typeof(decimal));
            table.Columns.Add("Cess", typeof(decimal));
            table.Columns.Add("Discount", typeof(decimal));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("IsSoldInTablets", typeof(bool));

            foreach (var item in items ?? Enumerable.Empty<SalesItem>())
            {
                var row = table.NewRow();
                row["Id"] = item.Id > 0 ? item.Id : DBNull.Value;
                row["ItemMasterId"] = item.ItemMasterId;
                row["PurchaseItemId"] = item.PurchaseItemId.HasValue ? item.PurchaseItemId.Value : DBNull.Value;
                row["Batch"] = string.IsNullOrWhiteSpace(item.Batch) ? DBNull.Value : item.Batch.Trim();
                row["ExpiryDate"] = item.Expirydate.HasValue ? item.Expirydate.Value : DBNull.Value;
                row["Mrp"] = item.Mrp;
                row["Qty"] = item.Qty;
                row["Rate"] = item.Rate;
                row["StripRate"] = item.StripRate;
                row["Gst"] = item.Gst;
                row["Cess"] = item.Cess;
                row["Discount"] = item.Discount;
                row["Amount"] = item.Amount;
                row["IsSoldInTablets"] = item.IsSoldInTablets;
                table.Rows.Add(row);
            }

            return table;
        }

        public static DataTable CreateSalesPaymentDetailTable(IEnumerable<SalsePaymentDetails>? payments)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Date", typeof(DateTime));
            table.Columns.Add("PaymentModeId", typeof(int));
            table.Columns.Add("Amount", typeof(decimal));
            table.Columns.Add("ReferenceNo", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("CustomerId", typeof(int));

            foreach (var payment in payments ?? Enumerable.Empty<SalsePaymentDetails>())
            {
                var row = table.NewRow();
                row["Id"] = payment.Id > 0 ? payment.Id : DBNull.Value;
                row["Date"] = payment.Date.HasValue ? payment.Date.Value : DBNull.Value;
                row["PaymentModeId"] = payment.PaymentModeId;
                row["Amount"] = payment.Amount;
                row["ReferenceNo"] = string.IsNullOrWhiteSpace(payment.ReferenceNo) ? DBNull.Value : payment.ReferenceNo.Trim();
                row["Description"] = string.IsNullOrWhiteSpace(payment.Description) ? DBNull.Value : payment.Description.Trim();
                row["CustomerId"] = payment.CustomerId.HasValue ? payment.CustomerId.Value : DBNull.Value;
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
