using AOne.DataAccess.Data;
using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.StoredProcedures;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.Common;

namespace EasyBill.DataAccess.Repository
{
    public class StockService : StoredProcedureRepositoryBase, IStockService
    {
        public StockService(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ITenantAccessor tenantAccessor)
            : base(dbContext, httpContextAccessor, tenantAccessor)
        {
        }

        #region Fetch All Stock

        /// <summary>
        /// Retrieves the complete, unfiltered inventory of current stock across all items.
        /// Applies tenant-level data isolation to ensure users only retrieve authorized stock records.
        /// </summary>
        /// <returns>A comprehensive list of CurrentStock entities.</returns>
        public async Task<IList<CurrentStock>> GetAll()
        {
            return await WithStoredProcedureCommandAsync("dbo.usp_CurrentStock_GetAll", async command =>
            {
                AddFilterParameters(command);

                var stocks = new List<CurrentStock>();
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    stocks.Add(MapCurrentStock(reader));
                }

                return (IList<CurrentStock>)stocks;
            });
        }

        #endregion

        #region Update Stock (Delta Application with Rates & Barcode)

        /// <summary>
        /// Applies a delta to the stock quantity and synchronizes pricing rates (Sales/Purchase) and barcode.
        /// Implements the latest business logic to overwrite existing rates with the most recent purchase data.
        /// </summary>
        public async Task UpdateStock(
            int itemId,
            string batch,
            decimal qtyChange,
            DateTime? expiry,
            decimal mrp,
            decimal salesRateA,
            decimal? salesRateB,
            decimal? purchaseRate,
            string? barcode,
            bool forceUpdate = false)
        {
            batch = NormalizeBatch(batch);

            await WithStoredProcedureCommandAsync("dbo.usp_CurrentStock_ApplyDelta", async command =>
            {
                AddParameter(command, "@ItemId", itemId, DbType.Int32);
                AddParameter(command, "@Batch", batch);
                AddParameter(command, "@QtyChange", qtyChange, DbType.Decimal);
                AddParameter(command, "@ExpiryDate", expiry, DbType.DateTime2);
                AddParameter(command, "@Mrp", mrp, DbType.Decimal);

                // New parameters added by TL
                AddParameter(command, "@SalesRateA", salesRateA, DbType.Decimal);
                AddParameter(command, "@SalesRateB", salesRateB, DbType.Decimal);
                AddParameter(command, "@PurchaseRate", purchaseRate, DbType.Decimal);
                AddParameter(command, "@Barcode", barcode);

                AddParameter(command, "@ForceUpdate", forceUpdate, DbType.Boolean);
                AddFilterParameters(command);
                AddParameter(command, "@Now", DateTime.Now, DbType.DateTime2);

                await command.ExecuteNonQueryAsync();
            });
        }

        #endregion
        #region Get Specific Stock

        /// <summary>
        /// Retrieves an exact stock record matching the specified item, normalized batch, expiry, and MRP parameters.
        /// Useful for validating available quantities before processing a sales transaction.
        /// </summary>
        /// <param name="itemId">The unique identifier of the item.</param>
        /// <param name="batch">The associated batch number (will be normalized).</param>
        /// <param name="expiry">The expiration date.</param>
        /// <param name="mrp">The specific Maximum Retail Price of the stock layer.</param>
        /// <returns>The matching CurrentStock entity, or null if no such stock layer exists.</returns>
        public async Task<CurrentStock?> GetStock(int itemId, string batch, DateTime? expiry, decimal mrp)
        {
            batch = NormalizeBatch(batch);

            return await WithStoredProcedureCommandAsync("dbo.usp_CurrentStock_Get", async command =>
            {
                AddParameter(command, "@ItemId", itemId, DbType.Int32);
                AddParameter(command, "@Batch", batch);
                AddParameter(command, "@ExpiryDate", expiry, DbType.DateTime2);
                AddParameter(command, "@Mrp", mrp, DbType.Decimal);

                AddFilterParameters(command);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapCurrentStock(reader);
                }

                return null;
            });
        }

        #endregion

        #region Overwrite Stock (Admin Correction)

        /// <summary>
        /// Forcibly overwrites an existing stock record with absolute values instead of applying a delta.
        /// Typically used for physical inventory reconciliation or administrative stock adjustments.
        /// </summary>
        /// <param name="stockId">The primary key of the current stock record to overwrite.</param>
        /// <param name="batch">The new or existing batch string (will be normalized).</param>
        /// <param name="expiry">The adjusted expiration date.</param>
        /// <param name="mrp">The adjusted MRP.</param>
        /// <param name="qty">The absolute physical quantity to set in the database.</param>
        /// <param name="purchaseRate">The adjusted purchase rate.</param>
        public async Task OverwriteStock(
            int stockId,
            string batch,
            DateTime? expiry,
            decimal mrp,
            decimal qty,
            decimal purchaseRate,
            decimal? salesRateA = null,
            decimal? salesRateB = null,
            string? barcode = null) // MERGED FROM TL
        {
            batch = NormalizeBatch(batch);

            await WithStoredProcedureCommandAsync("dbo.usp_CurrentStock_Overwrite", async command =>
            {
                AddParameter(command, "@Id", stockId, DbType.Int32);
                AddParameter(command, "@Batch", batch);
                AddParameter(command, "@ExpiryDate", expiry, DbType.DateTime2);
                AddParameter(command, "@Mrp", mrp, DbType.Decimal);
                AddParameter(command, "@Qty", qty, DbType.Decimal);
                AddParameter(command, "@PurchaseRate", purchaseRate, DbType.Decimal);
                AddParameter(command, "@SalesRateA", salesRateA, DbType.Decimal);
                AddParameter(command, "@SalesRateB", salesRateB, DbType.Decimal);
                AddParameter(command, "@Barcode", barcode);

                AddFilterParameters(command);
                AddParameter(command, "@Now", DateTime.Now, DbType.DateTime2);

                await command.ExecuteNonQueryAsync();
            });
        }

        #endregion

        #region Helpers & Data Mapping

        /// <summary>
        /// Normalizes batch strings by trimming whitespace and converting to uppercase.
        /// Ensures consistent data grouping and prevents duplicate stock layers due to case sensitivity.
        /// </summary>
        private static string? NormalizeBatch(string? batch)
        {
            return string.IsNullOrWhiteSpace(batch) ? null : batch.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Maps a raw database reader row into a strongly-typed CurrentStock entity.
        /// Bypasses ORM reflection overhead for rapid data hydration.
        /// </summary>
        private static CurrentStock MapCurrentStock(DbDataReader reader)
        {
            return new CurrentStock
            {
                Id = reader.ReadInt32("Id"),
                ItemId = reader.ReadInt32("ItemId"),
                Batch = reader.ReadNullableString("Batch"),
                ExpiryDate = reader.ReadNullableDateTime("ExpiryDate"),
                Qty = reader.ReadDecimal("Qty"),
                PurchaseRate = reader.ReadDecimal("PurchaseRate"),
                SalesRateA = reader.ReadDecimal("SalesRateA"),
                SalesRateB = reader.ReadDecimal("SalesRateB"),
                Barcode = reader.ReadNullableString("Barcode"),
                Mrp = reader.ReadDecimal("Mrp"),
                TenantId = reader.ReadNullableString("TenantId"),
                Created = reader.ReadNullableDateTime("Created"),
                CreatedBy = reader.ReadNullableString("CreatedBy"),
                LastModified = reader.ReadNullableDateTime("LastModified"),
                LastModifiedBy = reader.ReadNullableString("LastModifiedBy"),
                Deleted = reader.ReadNullableDateTime("Deleted"),
                DeletedBy = reader.ReadNullableString("DeletedBy")
            };
        }

        #endregion
    }
}



//using AOne.DataAccess.Data;
//using AOne.DataAccess.Repository.IRepository;
//using EasyBill.DataAccess.Repository.IRepository;
//using EasyBill.Models.Entity;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace EasyBill.DataAccess.Repository
//{
//    public class StockService : IStockService
//    {
//        private readonly IUnitOfWork _unitofwork;

//        public StockService(IUnitOfWork unitofwork)
//        {
//            _unitofwork = unitofwork;
//        }
//        public async Task<IList<CurrentStock>> GetAll()
//        {
//            try
//            {
//                var repository = _unitofwork.GetRepository<CurrentStock>();
//                IList<CurrentStock> results = await repository.Query().ToListAsync();
//                return results;
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//        }
//        //public async Task UpdateStock(
//        //    int itemId,
//        //    string batch,
//        //    decimal qtyChange,
//        //    DateTime? expiry,
//        //    decimal mrp,
//        //    decimal purchaseRate)
//        //{
//        //    var repository = _unitofwork.GetRepository<CurrentStock>();

//        //    var stock = await repository.Query()
//        //        .FirstOrDefaultAsync(x =>
//        //            x.ItemId == itemId &&
//        //            x.Batch == batch && x.Mrp == mrp && x.ExpiryDate == expiry);

//        //    if (stock == null)
//        //    {
//        //        stock = new CurrentStock
//        //        {
//        //            ItemId = itemId,
//        //            Batch = batch,
//        //            ExpiryDate = expiry,
//        //            Qty = 0,
//        //            Mrp = mrp,
//        //            PurchaseRate = purchaseRate,
//        //            //SalesRateA = salesrateA,
//        //            //SalesRateB = salesRateB,
//        //            //Barcode = Barcode
//        //        };

//        //        repository.Add(stock);
//        //    }

//        //    stock.Qty += qtyChange;
//        //    stock.Qty = Math.Round(stock.Qty, 2);
//        //    //if (stock.Qty < 0)
//        //    //    throw new Exception($"Negative stock not allowed for ItemId: {itemId}, Batch: {batch}");
//        //}

//        public async Task UpdateStock(
//            int itemId,
//            string batch,
//            decimal qtyChange,
//            DateTime? expiry,
//            decimal mrp,
//            decimal purchaseRate,
//            bool forceUpdate = false)
//        {
//            var repository = _unitofwork.GetRepository<CurrentStock>();

//            batch = batch?.Trim().ToUpper();

//            // 🔍 find ANY stock of this item (old one)
//            var stock = await repository.Query()
//                .FirstOrDefaultAsync(x =>
//                    x.ItemId == itemId &&
//                    x.Batch == batch &&
//                    x.Mrp == mrp &&
//                    x.ExpiryDate == expiry
//                );

//            // ✅ FORCE UPDATE MODE
//            if (forceUpdate)
//            {
//                if (stock != null)
//                {
//                    stock.Qty += qtyChange;
//                    stock.PurchaseRate = purchaseRate; // update rate also
//                    stock.Qty = Math.Round(stock.Qty, 2);
//                    return;
//                }
//            }

//            // 🔁 NORMAL FLOW (create if not exists)
//            if (stock == null)
//            {
//                stock = new CurrentStock
//                {
//                    ItemId = itemId,
//                    Batch = batch,
//                    ExpiryDate = expiry,
//                    Qty = 0,
//                    Mrp = mrp,
//                    PurchaseRate = purchaseRate
//                };

//                repository.Add(stock);
//            }

//            stock.Qty += qtyChange;
//            stock.Qty = Math.Round(stock.Qty, 2);
//        }
//        public async Task<CurrentStock?> GetStock(
//            int itemId,
//            string batch,
//            DateTime? expiry,
//            decimal mrp)
//        {
//            var repository = _unitofwork.GetRepository<CurrentStock>();

//            batch = batch?.Trim().ToUpper();

//            var stock = await repository.Query()
//                .FirstOrDefaultAsync(x =>
//                    x.ItemId == itemId &&
//                    x.Batch == batch &&
//                    x.ExpiryDate == expiry &&
//                    x.Mrp == mrp
//                );

//            return stock;
//        }
//    }
//}
