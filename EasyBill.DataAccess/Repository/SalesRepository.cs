using AOne.DataAccess.Repository.IRepository;
using AOne.DataAccess.Data;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.StoredProcedures;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace EasyBill.DataAccess.Repository
{
    public class SalesRepository : StoredProcedureRepositoryBase, ISalesRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public SalesRepository(
            IUnitOfWork unitofwork,
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ITenantAccessor tenantAccessor)
            : base(dbContext, httpContextAccessor, tenantAccessor)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<Sales>> GetAll()
        {
            return await WithStoredProcedureCommandAsync("dbo.usp_Sales_GetAll", async command =>
            {
                AddFilterParameters(command);
                return (IList<Sales>)await ReadSalesGraphAsync(command);
            });
        }

        public async Task<IList<Sales>> GetAllGSTR1Hsn()
        {
            return await GetAll();
        }

        public async Task<IList<Sales>> GetByCustomerId(int? customerId)
        {
            if (!customerId.HasValue)
            {
                return new List<Sales>();
            }

            return await WithStoredProcedureCommandAsync("dbo.usp_Sales_GetByCustomerId", async command =>
            {
                AddParameter(command, "@CustomerId", customerId.Value, DbType.Int32);
                AddFilterParameters(command);
                return (IList<Sales>)await ReadSalesGraphAsync(command);
            });
        }

        public async Task<Sales> Create(Sales model)
        {
            model.Id = await SaveSalesAsync(model);
            return model;
        }

        public async Task<Sales> GetById(int? Id)
        {
            if (!Id.HasValue)
            {
                return null!;
            }

            return await WithStoredProcedureCommandAsync("dbo.usp_Sales_GetById", async command =>
            {
                AddParameter(command, "@Id", Id.Value, DbType.Int32);
                AddFilterParameters(command);
                var sales = await ReadSalesGraphAsync(command);
                return sales.FirstOrDefault()!;
            });
        }

        public async Task<Sales> Update(Sales model)
        {
            await SaveSalesAsync(model);
            return model;
        }

        public async Task Delete(Sales model)
        {
            await WithStoredProcedureCommandAsync("dbo.usp_Sales_Delete", async command =>
            {
                AddParameter(command, "@Id", model.Id, DbType.Int32);
                AddParameter(command, "@TenantId", GetCurrentTenantId());
                AddParameter(command, "@UserId", GetCurrentUserId());
                AddParameter(command, "@Now", DateTime.Now, DbType.DateTime2);

                await command.ExecuteNonQueryAsync();
            });
        }
    

        public async Task<IList<Sales>> GetPendingBillsByCustomerId(int? customerid)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Sales>();
                IList<Sales> results = await repository.Query().Where(x => x.CustomerId == customerid && x.Balance > 0).OrderBy(x => x.BillDate).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IList<HoldSalesListVM>> GetAllHoldSales()
        {
            try
            {
                var repo = _unitofwork.GetRepository<HoldSales>();

                var result = await repo.Query()
                    .AsNoTracking() // 🔥 performance + safety
                    .Select(h => new HoldSalesListVM
                    {
                        Id = h.Id,
                        HoldToken = h.HoldToken,
                        HoldDate = h.HoldDate,
                        CustomerName = h.CustomerName,
                        MobileNo = h.MobileNo,
                        Address = h.Address,
                        ItemCount = h.HoldSalesItems.Count()
                    })
                    .OrderByDescending(x => x.HoldDate)
                    .ToListAsync();

                return result;
            }
            catch
            {
                throw;
            }
        }
        public async Task<HoldSales> GetHoldSalesById(int id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<HoldSales>();

                var result = await repository.Query()
                    .Include(h => h.HoldSalesItems)
                        .ThenInclude(i => i.ItemMaster)
                    .Include(h => h.PharmacyDoctor)
                    .FirstOrDefaultAsync(h => h.Id == id);

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<HoldSales> CreateHold(HoldSales hold)
        {
            try
            {
                var repository = _unitofwork.GetRepository<HoldSales>();
                repository.Add(hold);
                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }
                return hold;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task UpdateHold(HoldSales model)
        {
            var repo = _unitofwork.GetRepository<HoldSales>();
            repo.Update(model);
            await _unitofwork.SaveAsync();
        }

        public async Task DeleteHoldSales(int holdId)
        {
            try
            {
                var holdRepo = _unitofwork.GetRepository<HoldSales>();
                var itemRepo = _unitofwork.GetRepository<HoldSalesItem>();

                // 🔹 Child items HARD DELETE
                itemRepo.DeleteWhere(x => x.HoldSalesId == holdId);

                // 🔹 Parent HARD DELETE
                var hold = await holdRepo.Query()
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == holdId);

                if (hold != null)
                {
                    holdRepo.Delete(hold);
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<IList<Sales>> GetAllGSTR1Data()
        {
            return await GetAll();
        }


        // B2C Large Invoices (> 2.5 Lakh) with filters
        public async Task<IList<Sales>> GetB2CLargeInvoices(string tenantId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Sales>();

                var query = repository.Query()
                    .Include(s => s.Customers)
                    .Include(s => s.SalesItems)
                        .ThenInclude(si => si.ItemMaster)
                    .Where(s =>
                        // B2C Customer Check - GST No must be NULL or Empty
                         //(s.Customers.GSTNo == null
                         //   || s.Customers.GSTNo == ""
                         //   || s.Customers.GSTNo.Trim().Length == 0) &&
                         // Large Invoice - Greater than 2.5 lakh
                         s.TotalPayable > 250000);

                // Apply date filters
                if (startDate.HasValue)
                    query = query.Where(s => s.BillDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.BillDate <= endDate.Value);

                IList<Sales> results = await query.ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // B2C Small Invoices (<= 2.5 Lakh) with filters
        public async Task<IList<Sales>> GetB2CSmallInvoices(string tenantId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Sales>();

                var query = repository.Query()
                    .Include(s => s.Customers)
                    .Include(s => s.SalesItems)
                        .ThenInclude(si => si.ItemMaster)
                    .Where(s => 
                            //(s.Customers.GSTNo == null
                            //|| s.Customers.GSTNo == ""
                            //|| s.Customers.GSTNo.Trim().Length == 0) &&
                         // Small Invoice - Less than or equal to 2.5 lakh
                         s.TotalPayable <= 250000);

                // Apply date filters
                if (startDate.HasValue)
                    query = query.Where(s => s.BillDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.BillDate <= endDate.Value);

                IList<Sales> results = await query.ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<int> SaveSalesAsync(Sales model)
        {
            return await WithStoredProcedureCommandAsync("dbo.usp_Sales_Save", async command =>
            {
                AddParameter(command, "@Id", model.Id > 0 ? model.Id : null, DbType.Int32);
                AddParameter(command, "@CustomerId", model.CustomerId, DbType.Int32);
                AddParameter(command, "@billingType", model.billingType);
                AddParameter(command, "@PaymentType", model.PaymentType);
                AddParameter(command, "@BillNo", model.BillNo);
                AddParameter(command, "@BillDate", model.BillDate, DbType.DateTime2);
                AddParameter(command, "@MobileNo", model.MobileNo);
                AddParameter(command, "@Address", model.Address);
                AddParameter(command, "@Total", model.Total, DbType.Decimal);
                AddParameter(command, "@TotalGstAmt", model.TotalGstAmt, DbType.Decimal);
                AddParameter(command, "@TotalPayable", model.TotalPayable, DbType.Decimal);
                AddParameter(command, "@PharmacyDoctorId", model.PharmacyDoctorId, DbType.Int32);
                AddParameter(command, "@DoctorMobileNumber", model.DoctorMobileNumber);
                AddParameter(command, "@DoctorRegNumber", model.DoctorRegNumber);
                AddParameter(command, "@Totaldiscount", model.Totaldiscount, DbType.Decimal);
                AddParameter(command, "@discountPercent", model.discountPercent, DbType.Decimal);
                AddParameter(command, "@discountAmount", model.discountAmount, DbType.Decimal);
                AddParameter(command, "@PaidAmount", model.PaidAmount, DbType.Decimal);
                AddParameter(command, "@ReturnAmount", model.ReturnAmount, DbType.Decimal);
                AddParameter(command, "@OfferId", model.OfferId, DbType.Int32);
                AddParameter(command, "@SalesOrderId", model.SalesOrderId, DbType.Int32);
                AddParameter(command, "@Balance", model.Balance, DbType.Decimal);
                AddParameter(command, "@NetCollection", model.NetCollection, DbType.Decimal);
                AddParameter(command, "@RoundOffAmount", model.RoundOffAmount, DbType.Decimal);
                AddParameter(command, "@TotalCessAmount", model.TotalCessAmount, DbType.Decimal);
                AddParameter(command, "@PaymentStatus", model.PaymentStatus);

                AddStructuredParameter(command, "@SalesItems", "dbo.SalesItemTvp", SalesModuleTableTypeMapper.CreateSalesItemTable(model.SalesItems));
                AddStructuredParameter(command, "@PaymentDetails", "dbo.SalesPaymentDetailTvp", SalesModuleTableTypeMapper.CreateSalesPaymentDetailTable(model.SalsePaymentDetails));

                AddParameter(command, "@TenantId", GetCurrentTenantId());
                AddParameter(command, "@UserId", GetCurrentUserId());
                AddParameter(command, "@Now", DateTime.Now, DbType.DateTime2);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result, CultureInfo.InvariantCulture);
            });
        }

        private static async Task<List<Sales>> ReadSalesGraphAsync(DbCommand command)
        {
            var salesList = new List<Sales>();
            var lookup = new Dictionary<int, Sales>();

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var sale = MapSale(reader);
                salesList.Add(sale);
                lookup[sale.Id] = sale;
            }

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var salesId = reader.ReadInt32("SalesId");
                    if (!lookup.TryGetValue(salesId, out var sale))
                    {
                        continue;
                    }

                    sale.SalesItems.Add(MapSalesItem(reader, sale));
                }
            }

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var salesId = reader.ReadNullableInt32("SalseId");
                    if (!salesId.HasValue || !lookup.TryGetValue(salesId.Value, out var sale))
                    {
                        continue;
                    }

                    sale.SalsePaymentDetails.Add(MapSalesPaymentDetail(reader, sale));
                }
            }

            return salesList;
        }

        private static Sales MapSale(DbDataReader reader)
        {
            var sale = new Sales
            {
                Id = reader.ReadInt32("Id"),
                CustomerId = reader.ReadNullableInt32("CustomerId"),
                billingType = reader.ReadNullableString("billingType"),
                PaymentType = reader.ReadNullableString("PaymentType"),
                BillNo = reader.ReadNullableString("BillNo"),
                BillDate = reader.ReadNullableDateTime("BillDate"),
                MobileNo = reader.ReadNullableString("MobileNo"),
                Address = reader.ReadNullableString("Address"),
                Total = reader.ReadDecimal("Total"),
                TotalGstAmt = reader.ReadDecimal("TotalGstAmt"),
                TotalPayable = reader.ReadDecimal("TotalPayable"),
                PharmacyDoctorId = reader.ReadNullableInt32("PharmacyDoctorId"),
                DoctorMobileNumber = reader.ReadNullableString("DoctorMobileNumber"),
                DoctorRegNumber = reader.ReadNullableString("DoctorRegNumber"),
                Totaldiscount = reader.ReadDecimal("Totaldiscount"),
                discountPercent = reader.ReadDecimal("discountPercent"),
                discountAmount = reader.ReadDecimal("discountAmount"),
                PaidAmount = reader.ReadDecimal("PaidAmount"),
                ReturnAmount = reader.ReadDecimal("ReturnAmount"),
                OfferId = reader.ReadNullableInt32("OfferId"),
                SalesOrderId = reader.ReadNullableInt32("SalesOrderId"),
                Balance = reader.ReadDecimal("Balance"),
                NetCollection = reader.ReadDecimal("NetCollection"),
                RoundOffAmount = reader.ReadDecimal("RoundOffAmount"),
                TotalCessAmount = reader.ReadDecimal("TotalCessAmount"),
                PaymentStatus = reader.ReadNullableString("PaymentStatus"),
                TenantId = reader.ReadNullableString("TenantId"),
                Created = reader.ReadNullableDateTime("Created"),
                CreatedBy = reader.ReadNullableString("CreatedBy"),
                LastModified = reader.ReadNullableDateTime("LastModified"),
                LastModifiedBy = reader.ReadNullableString("LastModifiedBy"),
                Deleted = reader.ReadNullableDateTime("Deleted"),
                DeletedBy = reader.ReadNullableString("DeletedBy"),
                SalesItems = new List<SalesItem>(),
                SalsePaymentDetails = new List<SalsePaymentDetails>()
            };

            var customerName = reader.ReadNullableString("CustomerName");
            if (sale.CustomerId.HasValue || !string.IsNullOrWhiteSpace(customerName))
            {
                sale.Customers = new Customer
                {
                    Id = sale.CustomerId ?? 0,
                    Name = customerName ?? string.Empty,
                    PhoneNo = reader.ReadNullableString("CustomerPhoneNo"),
                    GSTNo = reader.ReadNullableString("CustomerGSTNo"),
                    Address = reader.ReadNullableString("CustomerAddress"),
                    Email = reader.ReadNullableString("CustomerEmail")
                };
            }

            var doctorName = reader.ReadNullableString("PharmacyDoctorName");
            if (sale.PharmacyDoctorId.HasValue || !string.IsNullOrWhiteSpace(doctorName))
            {
                sale.PharmacyDoctor = new PharmacyDoctor
                {
                    Id = sale.PharmacyDoctorId ?? 0,
                    Name = doctorName ?? string.Empty,
                    PhoneNo = reader.ReadNullableString("PharmacyDoctorPhoneNo") ?? string.Empty,
                    RegistrationNo = reader.ReadNullableString("PharmacyDoctorRegistrationNo")
                };
            }

            return sale;
        }

        private static SalesItem MapSalesItem(DbDataReader reader, Sales sale)
        {
            var salesItem = new SalesItem
            {
                Id = reader.ReadInt32("Id"),
                SalesId = reader.ReadInt32("SalesId"),
                Sales = sale,
                ItemMasterId = reader.ReadInt32("ItemMasterId"),
                PurchaseItemId = reader.ReadNullableInt32("PurchaseItemId"),
                Batch = reader.ReadNullableString("Batch"),
                Qty = reader.ReadDecimal("Qty"),
                Rate = reader.ReadDecimal("Rate"),
                Gst = reader.ReadDecimal("Gst"),
                Discount = reader.ReadDecimal("Discount"),
                Amount = reader.ReadDecimal("Amount"),
                Expirydate = reader.ReadNullableDateTime("Expirydate"),
                Mrp = reader.ReadDecimal("Mrp"),
                IsSoldInTablets = ReadBoolean(reader, "IsSoldInTablets"),
                TenantId = reader.ReadNullableString("TenantId"),
                StripRate = reader.ReadDecimal("StripRate"),
                Cess = reader.ReadDecimal("Cess"),
                Created = reader.ReadNullableDateTime("Created"),
                CreatedBy = reader.ReadNullableString("CreatedBy"),
                LastModified = reader.ReadNullableDateTime("LastModified"),
                LastModifiedBy = reader.ReadNullableString("LastModifiedBy"),
                Deleted = reader.ReadNullableDateTime("Deleted"),
                DeletedBy = reader.ReadNullableString("DeletedBy")
            };

            salesItem.ItemMaster = new ItemMaster
            {
                Id = salesItem.ItemMasterId,
                Name = reader.ReadNullableString("ItemName") ?? string.Empty,
                Code = reader.ReadNullableString("ItemCode") ?? string.Empty,
                Barcode = reader.ReadNullableString("ItemBarcode"),
                Conversion = reader.ReadInt32("ItemConversion"),
                Packing = reader.ReadNullableString("ItemPacking"),
                IsActive = ReadBoolean(reader, "ItemIsActive"),
                MaximumDiscount = reader.ReadDecimal("ItemMaximumDiscount"),
                MinimumQty = reader.ReadInt32("ItemMinimumQty"),
                CompanyId = reader.ReadNullableInt32("ItemCompanyId"),
                HsnId = reader.ReadNullableInt32("ItemHsnId"),
                Narcotics = ReadBoolean(reader, "ItemNarcotics"),
                ScheduleH = ReadBoolean(reader, "ItemScheduleH"),
                ScheduleH1 = ReadBoolean(reader, "ItemScheduleH1"),
            };

            if (salesItem.ItemMaster.CompanyId.HasValue || !string.IsNullOrWhiteSpace(reader.ReadNullableString("ItemCompanyName")))
            {
                salesItem.ItemMaster.Company = new Company
                {
                    Id = salesItem.ItemMaster.CompanyId ?? 0,
                    Name = reader.ReadNullableString("ItemCompanyName") ?? string.Empty
                };
            }

            if (salesItem.ItemMaster.HsnId.HasValue || !string.IsNullOrWhiteSpace(reader.ReadNullableString("ItemHsnCode")))
            {
                salesItem.ItemMaster.Hsn = new Hsn
                {
                    Id = salesItem.ItemMaster.HsnId ?? 0,
                    HsnCode = reader.ReadNullableString("ItemHsnCode") ?? string.Empty,
                    IGST = reader.ReadDecimal("ItemHsnIGST"),
                    Cess = reader.ReadDecimal("ItemHsnCess"),
                    CGST = reader.ReadDecimal("ItemHsnCGST"),
                    SGST = reader.ReadDecimal("ItemHsnSGST")
                };
            }

            return salesItem;
        }

        private static SalsePaymentDetails MapSalesPaymentDetail(DbDataReader reader, Sales sale)
        {
            var paymentDetail = new SalsePaymentDetails
            {
                Id = reader.ReadInt32("Id"),
                SalseId = reader.ReadNullableInt32("SalseId"),
                Sales = sale,
                PaymentModeId = reader.ReadInt32("PaymentModeId"),
                Amount = reader.ReadDecimal("Amount"),
                ReferenceNo = reader.ReadNullableString("ReferenceNo"),
                Description = reader.ReadNullableString("Description"),
                CustomerId = reader.ReadNullableInt32("CustomerId"),
                SalesOrderId = reader.ReadNullableInt32("SalesOrderId"),
                Date = reader.ReadNullableDateTime("Date"),
                TenantId = reader.ReadNullableString("TenantId"),
                Created = reader.ReadNullableDateTime("Created"),
                CreatedBy = reader.ReadNullableString("CreatedBy"),
                LastModified = reader.ReadNullableDateTime("LastModified"),
                LastModifiedBy = reader.ReadNullableString("LastModifiedBy"),
                Deleted = reader.ReadNullableDateTime("Deleted"),
                DeletedBy = reader.ReadNullableString("DeletedBy")
            };

            paymentDetail.ModeOfPayment = new ModeOfPayment
            {
                Id = paymentDetail.PaymentModeId,
                Name = reader.ReadNullableString("PaymentModeName") ?? string.Empty,
                Description = reader.ReadNullableString("PaymentModeDescription")
            };

            return paymentDetail;
        }

        private static bool ReadBoolean(DbDataReader reader, string columnName)
        {
            if (!reader.HasColumn(columnName))
            {
                return false;
            }

            var value = reader[columnName];
            return value != DBNull.Value && Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }
    }
}
