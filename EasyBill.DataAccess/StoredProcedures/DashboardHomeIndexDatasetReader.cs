using AOne.Models.Entity;
using AOne.Utility.Enums;
using EasyBill.Models.Entity;
using System.Data.Common;
using System.Globalization;

namespace EasyBill.DataAccess.StoredProcedures
{
    internal static class DashboardHomeIndexDatasetReader
    {
        public sealed class Payload
        {
            public List<PurchaseChallan> PurchaseChallans { get; } = new();
            public List<PaymentVoucher> Payments { get; } = new();
            public List<PurchaseReturn> PurchaseReturns { get; } = new();
            public List<StockIssue> StockIssues { get; } = new();
            public List<StockReturn> StockReturnsFiltered { get; } = new();
            public List<StockReturn> StockReturnsAll { get; } = new();
            public List<StockReceive> StockReceives { get; } = new();
            public List<CategoryMaster> Categories { get; } = new();
            public List<Company> Companies { get; } = new();
        }

        public static async Task<Payload> ReadAsync(DbDataReader r, CancellationToken cancellationToken)
        {
            var p = new Payload();
            var challanById = new Dictionary<int, PurchaseChallan>();
            while (await r.ReadAsync(cancellationToken))
            {
                var c = MapPurchaseChallanHeader(r);
                challanById[c.Id] = c;
                p.PurchaseChallans.Add(c);
            }

            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var it = MapPurchaseChallanItem(r);
                if (challanById.TryGetValue(it.PurchaseChallanId, out var ch))
                {
                    ch.PurchaseChallanItems.Add(it);
                }
            }

            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var spd = MapSalsePaymentDetail(r);
                if (spd.PurchaseChallanId is int pcid && challanById.TryGetValue(pcid, out var ch))
                {
                    ch.PaymentDetails ??= new List<SalsePaymentDetails>();
                    ch.PaymentDetails.Add(spd);
                }
            }

            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                p.Payments.Add(MapPaymentVoucher(r));
            }

            var prById = new Dictionary<int, PurchaseReturn>();
            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var pr = MapPurchaseReturnHeader(r);
                prById[pr.Id] = pr;
                p.PurchaseReturns.Add(pr);
            }

            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var pri = MapPurchaseReturnItem(r);
                if (prById.TryGetValue(pri.PurchaseReturnId, out var pr))
                {
                    pr.PurchaseReturnItems.Add(pri);
                }
            }

            var siById = new Dictionary<int, StockIssue>();
            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var si = MapStockIssueHeader(r);
                siById[si.Id] = si;
                p.StockIssues.Add(si);
            }

            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var ii = MapStockIssueItem(r);
                if (siById.TryGetValue(ii.StockIssueId, out var si))
                {
                    si.StockIssuesItems ??= new List<StockIssueItem>();
                    si.StockIssuesItems.Add(ii);
                }
            }

            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var spd = MapSalsePaymentDetail(r);
                if (spd.StockIssueId is int siid && siById.TryGetValue(siid, out var si))
                {
                    si.SalsePaymentDetails ??= new List<SalsePaymentDetails>();
                    si.SalsePaymentDetails.Add(spd);
                }
            }

            var srfById = new Dictionary<int, StockReturn>();
            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var sr = MapStockReturnHeader(r);
                srfById[sr.Id] = sr;
                p.StockReturnsFiltered.Add(sr);
            }

            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var ri = MapStockReturnItem(r);
                if (srfById.TryGetValue(ri.StockReturnId, out var sr))
                {
                    sr.StockReturnItems ??= new List<StockReturnItem>();
                    sr.StockReturnItems.Add(ri);
                }
            }

            var sraById = new Dictionary<int, StockReturn>();
            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var sr = MapStockReturnHeader(r);
                sraById[sr.Id] = sr;
                p.StockReturnsAll.Add(sr);
            }

            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var ri = MapStockReturnItem(r);
                if (sraById.TryGetValue(ri.StockReturnId, out var sr))
                {
                    sr.StockReturnItems ??= new List<StockReturnItem>();
                    sr.StockReturnItems.Add(ri);
                }
            }

            var recvById = new Dictionary<int, StockReceive>();
            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var rx = MapStockReceiveHeader(r);
                recvById[rx.Id] = rx;
                p.StockReceives.Add(rx);
            }

            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var ri = MapStockReceiveItem(r);
                if (recvById.TryGetValue(ri.StockReceiveId, out var rx))
                {
                    rx.StockReceiveItems ??= new List<StockReceiveItem>();
                    rx.StockReceiveItems.Add(ri);
                }
            }

            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                p.Categories.Add(MapCategoryMaster(r));
            }

            await r.NextResultAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                p.Companies.Add(MapCompany(r));
            }

            return p;
        }

        private static PurchaseChallan MapPurchaseChallanHeader(DbDataReader r) => new()
        {
            Id = r.ReadInt32("Id"),
            SupplierId = r.ReadNullableInt32("SupplierId"),
            BillNo = r.ReadNullableString("BillNo") ?? string.Empty,
            BillDate = r.ReadNullableDateTime("BillDate"),
            PartyBillNo = r.ReadNullableString("PartyBillNo") ?? string.Empty,
            PartyBillDate = r.ReadNullableDateTime("PartyBillDate"),
            TenantId = r.ReadNullableString("TenantId"),
            TotalGstAmt = r.ReadDecimal("TotalGstAmt"),
            Totaldiscount = r.ReadDecimal("Totaldiscount"),
            TotalPayable = r.ReadDecimal("TotalPayable"),
            discountPercent = r.ReadDecimal("discountPercent"),
            discountAmount = r.ReadDecimal("discountAmount"),
            Total = r.ReadDecimal("Total"),
            PaymentAmt = r.ReadDecimal("PaymentAmt"),
            PaymentStatus = r.ReadNullableString("PaymentStatus"),
            RoundOffAmount = r.ReadDecimal("RoundOffAmount"),
            billingType = r.ReadNullableString("billingType"),
            PaymentType = r.ReadNullableString("PaymentType"),
            PurchaseType = r.ReadNullableString("PurchaseType"),
            TotalCGstAmt = r.ReadDecimal("TotalCGstAmt"),
            TotalSGstAmt = r.ReadDecimal("TotalSGstAmt"),
            PaidAmount = r.ReadDecimal("PaidAmount"),
            ReturnAmount = r.ReadDecimal("ReturnAmount"),
            Balance = r.ReadDecimal("Balance"),
            Status = r.ReadNullableString("Status") ?? string.Empty,
            ConvertedPurchaseId = r.ReadNullableInt32("ConvertedPurchaseId"),
            Created = r.ReadNullableDateTime("Created"),
            CreatedBy = r.ReadNullableString("CreatedBy"),
            LastModified = r.ReadNullableDateTime("LastModified"),
            LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
            Deleted = r.ReadNullableDateTime("Deleted"),
            DeletedBy = r.ReadNullableString("DeletedBy"),
            PurchaseChallanItems = new List<PurchaseChallanItem>(),
            PaymentDetails = new List<SalsePaymentDetails>(),
            Suppliers = MapSupplierDenorm(r, "SupplierFirstName", "SupplierPhoneNO")
        };

        private static Supplier? MapSupplierDenorm(DbDataReader r, string fn, string ph)
        {
            var sid = r.ReadNullableInt32("SupplierId");
            if (!sid.HasValue || sid.Value == 0)
            {
                return null;
            }

            return new Supplier
            {
                Id = sid.Value,
                FirstName = r.ReadNullableString(fn) ?? string.Empty,
                PhoneNO = r.ReadNullableString(ph)
            };
        }

        private static PurchaseChallanItem MapPurchaseChallanItem(DbDataReader r) => new()
        {
            Id = r.ReadInt32("Id"),
            PurchaseChallanId = r.ReadInt32("PurchaseChallanId"),
            TenantId = r.ReadNullableString("TenantId"),
            Batch = r.ReadNullableString("Batch"),
            ItemId = r.ReadInt32("ItemId"),
            Qty = r.ReadDecimal("Qty"),
            FreeQty = r.ReadDecimal("FreeQty"),
            Unit = r.ReadNullableString("Unit"),
            Rate = r.ReadDecimal("Rate"),
            HsnId = r.ReadNullableInt32("HsnId"),
            Gst = r.ReadDecimal("Gst"),
            GstAmount = r.ReadDecimal("GstAmount"),
            Discount = r.ReadDecimal("Discount"),
            DiscountAmt = r.ReadDecimal("DiscountAmt"),
            ExpiryDate = r.ReadNullableDateTime("ExpiryDate"),
            Amount = r.ReadDecimal("Amount"),
            TotalAmt = r.ReadDecimal("TotalAmt"),
            BatchWiseCose = r.ReadDecimal("BatchWiseCose"),
            Mrp = r.ReadDecimal("Mrp"),
            salserateA = r.ReadDecimal("salserateA"),
            salserateB = r.ReadDecimal("salserateB"),
            Barcode = r.ReadNullableString("Barcode"),
            CGst = r.ReadDecimal("CGst"),
            SGst = r.ReadDecimal("SGst"),
            CGstAmount = r.ReadDecimal("CGstAmount"),
            SGstAmount = r.ReadDecimal("SGstAmount"),
            ConvertedQty = r.ReadInt32("ConvertedQty"),
            Created = r.ReadNullableDateTime("Created"),
            CreatedBy = r.ReadNullableString("CreatedBy"),
            LastModified = r.ReadNullableDateTime("LastModified"),
            LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
            Deleted = r.ReadNullableDateTime("Deleted"),
            DeletedBy = r.ReadNullableString("DeletedBy")
        };

        private static SalsePaymentDetails MapSalsePaymentDetail(DbDataReader r)
        {
            var pmId = r.ReadInt32("PaymentModeId");
            return new SalsePaymentDetails
            {
                Id = r.ReadInt32("Id"),
                PaymentModeId = pmId,
                ModeOfPayment = new ModeOfPayment { Id = pmId, Name = r.ReadNullableString("ModeOfPaymentName") ?? string.Empty },
                Amount = r.ReadDecimal("Amount"),
                ReferenceNo = r.ReadNullableString("ReferenceNo"),
                Description = r.ReadNullableString("Description"),
                CustomerId = r.ReadNullableInt32("CustomerId"),
                SalseId = r.ReadNullableInt32("SalseId"),
                TenantId = r.ReadNullableString("TenantId"),
                SalesOrderId = r.ReadNullableInt32("SalesOrderId"),
                Date = r.ReadNullableDateTime("Date"),
                StockReturnId = r.ReadNullableInt32("StockReturnId"),
                StockIssueId = r.ReadNullableInt32("StockIssueId"),
                PurchaseId = r.ReadNullableInt32("PurchaseId"),
                PurchaseChallanId = r.ReadNullableInt32("PurchaseChallanId"),
                Created = r.ReadNullableDateTime("Created"),
                CreatedBy = r.ReadNullableString("CreatedBy"),
                LastModified = r.ReadNullableDateTime("LastModified"),
                LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
                Deleted = r.ReadNullableDateTime("Deleted"),
                DeletedBy = r.ReadNullableString("DeletedBy")
            };
        }

        private static PaymentVoucher MapPaymentVoucher(DbDataReader r)
        {
            var mopId = r.ReadNullableInt32("PaymentModeId");
            var supId = r.ReadNullableInt32("SupplierId");
            var catId = r.ReadNullableInt32("VoucherCategoryId");
            return new PaymentVoucher
            {
                Id = r.ReadInt32("Id"),
                VouncherNo = r.ReadNullableString("VouncherNo") ?? string.Empty,
                Date = r.ReadNullableDateTime("Date"),
                SupplierId = supId,
                VoucherCategoryId = catId,
                Amount = r.ReadDecimal("Amount"),
                GST = r.ReadDecimal("GST"),
                GSTAmount = r.ReadDecimal("GSTAmount"),
                NetAmount = r.ReadDecimal("NetAmount"),
                Description = r.ReadNullableString("Description"),
                Attachments = r.ReadNullableString("Attachments"),
                ChequeNo = r.ReadNullableString("ChequeNo"),
                ChequeDate = r.ReadNullableDateTime("ChequeDate"),
                RefNo = r.ReadNullableString("RefNo"),
                CustomerId = r.ReadNullableInt32("CustomerId"),
                EmployeeId = r.ReadNullableInt32("EmployeeId"),
                Party = (AOne.Utility.Enums.Party?)r.ReadNullableInt32("Party"),
                PaymentModeId = mopId,
                ModeOfPayment = mopId is int mid && mid != 0
                    ? new ModeOfPayment { Id = mid, Name = r.ReadNullableString("ModeOfPaymentName") ?? string.Empty }
                    : null,
                Suppliers = supId is int s && s != 0
                    ? new Supplier { Id = s, FirstName = r.ReadNullableString("SupplierFirstName") ?? string.Empty, PhoneNO = r.ReadNullableString("SupplierPhoneNO") }
                    : null,
                paymentVouchercategory = catId is int c && c != 0
                    ? new PaymentVoucherCategory { Id = c, Name = r.ReadNullableString("VoucherCategoryName") ?? string.Empty }
                    : null,
                Created = r.ReadNullableDateTime("Created"),
                CreatedBy = r.ReadNullableString("CreatedBy"),
                LastModified = r.ReadNullableDateTime("LastModified"),
                LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
                Deleted = r.ReadNullableDateTime("Deleted"),
                DeletedBy = r.ReadNullableString("DeletedBy")
            };
        }

        private static PurchaseReturn MapPurchaseReturnHeader(DbDataReader r)
        {
            var reason = r.ReadNullableInt32("Reason");
            return new PurchaseReturn
            {
                Id = r.ReadInt32("Id"),
                SupplierId = r.ReadNullableInt32("SupplierId"),
                BillNo = r.ReadNullableString("BillNo") ?? string.Empty,
                BillDate = r.ReadNullableDateTime("BillDate"),
                PartyBillNo = r.ReadNullableString("PartyBillNo") ?? string.Empty,
                PartyBillDate = r.ReadNullableDateTime("PartyBillDate"),
                TenantId = r.ReadNullableString("TenantId"),
                TotalGstAmt = r.ReadDecimal("TotalGstAmt"),
                Totaldiscount = r.ReadDecimal("Totaldiscount"),
                TotalPayable = r.ReadDecimal("TotalPayable"),
                discountPercent = r.ReadDecimal("discountPercent"),
                discountAmount = r.ReadDecimal("discountAmount"),
                Total = r.ReadDecimal("Total"),
                RoundOffAmount = r.ReadDecimal("RoundOffAmount"),
                TotalCessAmt = r.ReadDecimal("TotalCessAmt"),
                Reason = reason.HasValue ? (Purchasereturnreason?)reason.Value : null,
                Created = r.ReadNullableDateTime("Created"),
                CreatedBy = r.ReadNullableString("CreatedBy"),
                LastModified = r.ReadNullableDateTime("LastModified"),
                LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
                Deleted = r.ReadNullableDateTime("Deleted"),
                DeletedBy = r.ReadNullableString("DeletedBy"),
                PurchaseReturnItems = new List<PurchaseReturnItem>(),
                Suppliers = MapSupplierDenorm(r, "SupplierFirstName", "SupplierPhoneNO")
            };
        }

        private static PurchaseReturnItem MapPurchaseReturnItem(DbDataReader r) => new()
        {
            Id = r.ReadInt32("Id"),
            PurchaseReturnId = r.ReadInt32("PurchaseReturnId"),
            Batch = r.ReadNullableString("Batch"),
            ItemId = r.ReadInt32("ItemId"),
            Qty = r.ReadInt32("Qty"),
            FreeQty = r.ReadInt32("FreeQty"),
            Unit = r.ReadNullableString("Unit"),
            Rate = r.ReadDecimal("Rate"),
            HsnId = r.ReadInt32("HsnId"),
            Gst = r.ReadDecimal("Gst"),
            GstAmount = r.ReadDecimal("GstAmount"),
            Discount = r.ReadDecimal("Discount"),
            DiscountAmt = r.ReadDecimal("DiscountAmt"),
            ExpiryDate = r.ReadNullableDateTime("ExpiryDate"),
            Amount = r.ReadDecimal("Amount"),
            TotalAmt = r.ReadDecimal("TotalAmt"),
            TenantId = r.ReadNullableString("TenantId"),
            BatchWiseCose = r.ReadDecimal("BatchWiseCose"),
            Mrp = r.ReadDecimal("Mrp"),
            salserateA = r.ReadDecimal("salserateA"),
            salserateB = r.ReadDecimal("salserateB"),
            Cess = r.ReadDecimal("Cess"),
            Created = r.ReadNullableDateTime("Created"),
            CreatedBy = r.ReadNullableString("CreatedBy"),
            LastModified = r.ReadNullableDateTime("LastModified"),
            LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
            Deleted = r.ReadNullableDateTime("Deleted"),
            DeletedBy = r.ReadNullableString("DeletedBy")
        };

        private static StockIssue MapStockIssueHeader(DbDataReader r) => new()
        {
            Id = r.ReadInt32("Id"),
            CustomerId = r.ReadNullableInt32("CustomerId"),
            billingType = r.ReadNullableString("billingType"),
            PaymentType = r.ReadNullableString("PaymentType"),
            ChallanNo = r.ReadNullableString("ChallanNo"),
            ChallanDate = r.ReadNullableDateTime("ChallanDate"),
            MobileNo = r.ReadNullableString("MobileNo"),
            Address = r.ReadNullableString("Address"),
            Total = r.ReadDecimal("Total"),
            TotalGstAmt = r.ReadDecimal("TotalGstAmt"),
            TotalPayable = r.ReadDecimal("TotalPayable"),
            TenantId = r.ReadNullableString("TenantId"),
            PharmacyDoctorId = r.ReadNullableInt32("PharmacyDoctorId"),
            DoctorMobileNumber = r.ReadNullableString("DoctorMobileNumber"),
            DoctorRegNumber = r.ReadNullableString("DoctorRegNumber"),
            Totaldiscount = r.ReadDecimal("Totaldiscount"),
            discountPercent = r.ReadDecimal("discountPercent"),
            discountAmount = r.ReadDecimal("discountAmount"),
            PaidAmount = ReadNullableDecimal(r, "PaidAmount"),
            ReturnAmount = ReadNullableDecimal(r, "ReturnAmount"),
            Balance = ReadNullableDecimal(r, "Balance"),
            NetCollection = ReadNullableDecimal(r, "NetCollection"),
            RoundOffAmount = ReadNullableDecimal(r, "RoundOffAmount"),
            TotalCessAmount = ReadNullableDecimal(r, "TotalCessAmount"),
            TaxCalculation = r.ReadNullableString("TaxCalculation"),
            Created = r.ReadNullableDateTime("Created"),
            CreatedBy = r.ReadNullableString("CreatedBy"),
            LastModified = r.ReadNullableDateTime("LastModified"),
            LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
            Deleted = r.ReadNullableDateTime("Deleted"),
            DeletedBy = r.ReadNullableString("DeletedBy"),
            StockIssuesItems = new List<StockIssueItem>(),
            SalsePaymentDetails = new List<SalsePaymentDetails>(),
            Customers = MapCustomerDenorm(r)
        };

        private static Customer? MapCustomerDenorm(DbDataReader r)
        {
            var cid = r.ReadNullableInt32("CustomerId");
            if (!cid.HasValue || cid.Value == 0)
            {
                return null;
            }

            return new Customer
            {
                Id = cid.Value,
                Name = r.ReadNullableString("CustomerName") ?? string.Empty,
                PhoneNo = r.ReadNullableString("CustomerPhoneNo")
            };
        }

        private static StockIssueItem MapStockIssueItem(DbDataReader r) => new()
        {
            Id = r.ReadInt32("Id"),
            StockIssueId = r.ReadInt32("StockIssueId"),
            ItemMasterId = r.ReadInt32("ItemMasterId"),
            HsnId = r.ReadNullableInt32("HsnId"),
            HsnCode = r.ReadNullableString("HsnCode"),
            PurchaseItemId = r.ReadNullableInt32("PurchaseItemId"),
            TenantId = r.ReadNullableString("TenantId"),
            Batch = r.ReadNullableString("Batch"),
            Qty = r.ReadDecimal("Qty"),
            Rate = r.ReadDecimal("Rate"),
            Gst = r.ReadDecimal("Gst"),
            IGst = ReadNullableDecimal(r, "IGst"),
            CGst = ReadNullableDecimal(r, "CGst"),
            SGst = ReadNullableDecimal(r, "SGst"),
            Discount = r.ReadDecimal("Discount"),
            Amount = r.ReadDecimal("Amount"),
            Expirydate = r.ReadNullableDateTime("Expirydate"),
            Mrp = r.ReadDecimal("Mrp"),
            IsSoldInTablets = ReadBit(r, "IsSoldInTablets"),
            StripRate = ReadNullableDecimal(r, "StripRate"),
            Cess = ReadNullableDecimal(r, "Cess"),
            Created = r.ReadNullableDateTime("Created"),
            CreatedBy = r.ReadNullableString("CreatedBy"),
            LastModified = r.ReadNullableDateTime("LastModified"),
            LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
            Deleted = r.ReadNullableDateTime("Deleted"),
            DeletedBy = r.ReadNullableString("DeletedBy")
        };

        private static StockReturn MapStockReturnHeader(DbDataReader r) => new()
        {
            Id = r.ReadInt32("Id"),
            CustomerId = r.ReadNullableInt32("CustomerId"),
            ChallanNo = r.ReadNullableString("ChallanNo"),
            ChallanDate = r.ReadNullableDateTime("ChallanDate"),
            MobileNo = r.ReadNullableString("MobileNo"),
            Address = r.ReadNullableString("Address"),
            Total = r.ReadDecimal("Total"),
            TotalGstAmt = r.ReadDecimal("TotalGstAmt"),
            TotalPayable = r.ReadDecimal("TotalPayable"),
            PharmacyDoctorId = r.ReadNullableInt32("PharmacyDoctorId"),
            DoctorMobileNumber = r.ReadNullableString("DoctorMobileNumber"),
            DoctorRegNumber = r.ReadNullableString("DoctorRegNumber"),
            Totaldiscount = r.ReadDecimal("Totaldiscount"),
            discountPercent = r.ReadDecimal("discountPercent"),
            discountAmount = r.ReadDecimal("discountAmount"),
            TenantId = r.ReadNullableString("TenantId"),
            billingType = r.ReadNullableString("billingType"),
            PaymentType = r.ReadNullableString("PaymentType"),
            NetCollection = r.ReadDecimal("NetCollection"),
            RoundOffAmount = r.ReadDecimal("RoundOffAmount"),
            TotalCessAmt = r.ReadDecimal("TotalCessAmt"),
            Created = r.ReadNullableDateTime("Created"),
            CreatedBy = r.ReadNullableString("CreatedBy"),
            LastModified = r.ReadNullableDateTime("LastModified"),
            LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
            Deleted = r.ReadNullableDateTime("Deleted"),
            DeletedBy = r.ReadNullableString("DeletedBy"),
            StockReturnItems = new List<StockReturnItem>(),
            Customers = MapCustomerDenorm(r)
        };

        private static StockReturnItem MapStockReturnItem(DbDataReader r) => new()
        {
            Id = r.ReadInt32("Id"),
            StockReturnId = r.ReadInt32("StockReturnId"),
            ItemMasterId = r.ReadInt32("ItemMasterId"),
            Batch = r.ReadNullableString("Batch"),
            Qty = r.ReadDecimal("Qty"),
            Rate = r.ReadDecimal("Rate"),
            Gst = r.ReadDecimal("Gst"),
            Cess = r.ReadDecimal("Cess"),
            Discount = r.ReadDecimal("Discount"),
            Amount = r.ReadDecimal("Amount"),
            Expirydate = r.ReadNullableDateTime("Expirydate"),
            Mrp = r.ReadDecimal("Mrp"),
            PurchaseItemId = r.ReadNullableInt32("PurchaseItemId"),
            StripRate = r.ReadDecimal("StripRate"),
            Reason = r.ReadNullableString("Reason"),
            TenantId = r.ReadNullableString("TenantId"),
            Created = r.ReadNullableDateTime("Created"),
            CreatedBy = r.ReadNullableString("CreatedBy"),
            LastModified = r.ReadNullableDateTime("LastModified"),
            LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
            Deleted = r.ReadNullableDateTime("Deleted"),
            DeletedBy = r.ReadNullableString("DeletedBy")
        };

        private static StockReceive MapStockReceiveHeader(DbDataReader r) => new()
        {
            Id = r.ReadInt32("Id"),
            CustomerId = r.ReadInt32("CustomerId"),
            ChallanNo = r.ReadNullableString("ChallanNo"),
            ChallanDate = r.ReadNullableDateTime("ChallanDate"),
            MobileNo = r.ReadNullableString("MobileNo"),
            Address = r.ReadNullableString("Address"),
            Total = r.ReadDecimal("Total"),
            TotalGstAmt = r.ReadDecimal("TotalGstAmt"),
            TotalPayable = r.ReadDecimal("TotalPayable"),
            TenantId = r.ReadNullableString("TenantId"),
            PharmacyDoctorId = r.ReadNullableInt32("PharmacyDoctorId"),
            DoctorMobileNumber = r.ReadNullableString("DoctorMobileNumber"),
            DoctorRegNumber = r.ReadNullableString("DoctorRegNumber"),
            Totaldiscount = r.ReadDecimal("Totaldiscount"),
            discountPercent = r.ReadDecimal("discountPercent"),
            discountAmount = r.ReadDecimal("discountAmount"),
            Created = r.ReadNullableDateTime("Created"),
            CreatedBy = r.ReadNullableString("CreatedBy"),
            LastModified = r.ReadNullableDateTime("LastModified"),
            LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
            Deleted = r.ReadNullableDateTime("Deleted"),
            DeletedBy = r.ReadNullableString("DeletedBy"),
            StockReceiveItems = new List<StockReceiveItem>(),
            Customers = MapCustomerDenorm(r)
        };

        private static StockReceiveItem MapStockReceiveItem(DbDataReader r) => new()
        {
            Id = r.ReadInt32("Id"),
            StockReceiveId = r.ReadInt32("StockReceiveId"),
            ItemMasterId = r.ReadInt32("ItemMasterId"),
            Batch = r.ReadNullableString("Batch"),
            Qty = r.ReadDecimal("Qty"),
            Rate = r.ReadDecimal("Rate"),
            Gst = r.ReadDecimal("Gst"),
            Discount = r.ReadDecimal("Discount"),
            Amount = r.ReadDecimal("Amount"),
            Expirydate = r.ReadNullableDateTime("Expirydate"),
            Mrp = r.ReadDecimal("Mrp"),
            PurchaseItemId = r.ReadNullableInt32("PurchaseItemId"),
            TenantId = r.ReadNullableString("TenantId"),
            Created = r.ReadNullableDateTime("Created"),
            CreatedBy = r.ReadNullableString("CreatedBy"),
            LastModified = r.ReadNullableDateTime("LastModified"),
            LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
            Deleted = r.ReadNullableDateTime("Deleted"),
            DeletedBy = r.ReadNullableString("DeletedBy")
        };

        private static CategoryMaster MapCategoryMaster(DbDataReader r) => new()
        {
            Id = r.ReadInt32("Id"),
            CategoryName = r.ReadNullableString("CategoryName") ?? string.Empty,
            TenantId = r.ReadNullableString("TenantId"),
            Created = r.ReadNullableDateTime("Created"),
            CreatedBy = r.ReadNullableString("CreatedBy"),
            LastModified = r.ReadNullableDateTime("LastModified"),
            LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
            Deleted = r.ReadNullableDateTime("Deleted"),
            DeletedBy = r.ReadNullableString("DeletedBy")
        };

        private static Company MapCompany(DbDataReader r) => new()
        {
            Id = r.ReadInt32("Id"),
            Name = r.ReadNullableString("Name") ?? string.Empty,
            TenantId = r.ReadNullableString("TenantId"),
            Created = r.ReadNullableDateTime("Created"),
            CreatedBy = r.ReadNullableString("CreatedBy"),
            LastModified = r.ReadNullableDateTime("LastModified"),
            LastModifiedBy = r.ReadNullableString("LastModifiedBy"),
            Deleted = r.ReadNullableDateTime("Deleted"),
            DeletedBy = r.ReadNullableString("DeletedBy")
        };

        private static decimal? ReadNullableDecimal(DbDataReader r, string col)
        {
            for (var i = 0; i < r.FieldCount; i++)
            {
                if (string.Equals(r.GetName(i), col, StringComparison.OrdinalIgnoreCase))
                {
                    return r.IsDBNull(i) ? null : Convert.ToDecimal(r.GetValue(i), CultureInfo.InvariantCulture);
                }
            }

            return null;
        }

        private static bool ReadBit(DbDataReader r, string col)
        {
            for (var i = 0; i < r.FieldCount; i++)
            {
                if (string.Equals(r.GetName(i), col, StringComparison.OrdinalIgnoreCase))
                {
                    return !r.IsDBNull(i) && Convert.ToBoolean(r.GetValue(i), CultureInfo.InvariantCulture);
                }
            }

            return false;
        }
    }
}
