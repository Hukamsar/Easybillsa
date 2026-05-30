using AOne.DataAccess.Data;
using AOne.DataAccess.Repository.IRepository;
using AOne.Models.Entity;
using AOne.Utility.Enums;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.StoredProcedures;
using EasyBill.Models.Entity;
using EasyBill.Models.Model.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq.Expressions;

namespace EasyBill.DataAccess.Repository
{
    public class PurchaseRepository : StoredProcedureRepositoryBase, IPurchaseRepository
    {
        private readonly IStockService _stockService;
        private readonly IUnitOfWork _unitofwork;
        public PurchaseRepository(
            IUnitOfWork unitofwork,
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ITenantAccessor tenantAccessor,
            IStockService stockService)
            : base(dbContext, httpContextAccessor, tenantAccessor)
        {
            _unitofwork = unitofwork;
            _stockService = stockService;
        }

        #region Create Purchase

        /// <summary>
        /// Creates a new purchase record and strictly synchronizes inventory using the latest pricing data.
        /// </summary>
        public async Task<Purchase> Create(Purchase model)
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                model.Id = await SavePurchaseAsync(model);

                foreach (var item in model.PurchaseItems ?? new List<PurchaseItem>())
                {
                    if (item.SourcePurchaseChallanId == null || item.SourcePurchaseChallanId <= 0)
                    {
                        // Passing the new parameters added by TL to the Stock Service
                        await _stockService.UpdateStock(
                            item.ItemId,
                            item.Batch ?? string.Empty,
                            item.Qty + item.FreeQty,
                            item.ExpiryDate,
                            item.Mrp,
                            item.salserateA,
                            item.salserateB,
                            item.Rate,
                            item.Barcode);
                    }
                }

                await transaction.CommitAsync();
                return model;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Purchase create failed.", ex);
            }
        }

        #endregion
        #region Create Purchase With Explicit Stock List
        public async Task<Purchase> CreateWithStockAsync(Purchase model, List<Stock> stocks)
        {
            // Initialize a database transaction to ensure atomicity across SP execution and EF Core operations
            await using var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                // Execute the stored procedure to persist the Purchase, Items, and Payment details
                // The SP handles the complex bulk operations and returns the newly generated primary key
                model.Id = await SavePurchaseAsync(model);

                // Check if an explicit list of Stock entities has been provided for insertion
                if (stocks != null && stocks.Count > 0)
                {
                    // Utilize Entity Framework Core's bulk add feature to track the new stock entities
                    DbContext.Set<Stock>().AddRange(stocks);

                    // Persist the stock changes to the database
                    await DbContext.SaveChangesAsync();
                }

                // Commit the transaction only if both the SP and EF Core operations succeed
                await transaction.CommitAsync();
                return model;
            }
            catch (Exception ex)
            {
                // Revert all changes (both from SP and EF Core) to maintain data integrity
                await transaction.RollbackAsync();
                throw new Exception("CreateWithStockAsync failed.", ex);
            }
        }
        #endregion

        #region Fetch Operations

        /// <summary>
        /// Retrieves a complete list of purchases including their associated items and payment details.
        /// Applies tenant and user-level data isolation filters automatically.
        /// </summary>
        /// <returns>A list of fully populated Purchase entity graphs.</returns>
        public async Task<IList<Purchase>> GetAll()
        {
            // Execute the stored procedure using the centralized command execution wrapper
            // The wrapper safely handles database connection lifecycle (Open/Dispose)
            return await WithStoredProcedureCommandAsync("dbo.usp_Purchase_GetAll", async command =>
            {
                // Inject contextual security parameters (e.g., TenantId, UserId, Role-based filters)
                AddFilterParameters(command);

                // Execute the reader and map multiple active result sets (MARS) 
                // into a hierarchical Object Graph (Purchase -> Items, Payments)
                return await ReadPurchaseGraphAsync(command);
            });
        }

        /// <summary>
        /// Retrieves a single purchase record along with its hierarchical dependencies based on the primary key.
        /// </summary>
        /// <param name="Id">The primary key of the Purchase entity.</param>
        /// <returns>A fully populated Purchase entity if found and authorized; otherwise, null.</returns>
        public async Task<Purchase?> GetById(int? Id)
        {
            // Delegate the command configuration and execution to the base wrapper
            return await WithStoredProcedureCommandAsync("dbo.usp_Purchase_GetById", async command =>
            {
                // Bind the exact primary key parameter expected by the stored procedure
                AddParameter(command, "@Id", Id, DbType.Int32);

                // Enforce data isolation logic to ensure the user is authorized to view this specific record
                AddFilterParameters(command);

                // Rehydrate the object graph from the relational data streams
                var purchases = await ReadPurchaseGraphAsync(command);

                // Extract the single requested entity from the mapped collection, or return default (null)
                return purchases.FirstOrDefault();
            });
        }

        #endregion

        #region Get Purchases By Supplier ID

        /// <summary>
        /// Retrieves a lightweight list of purchase records associated with a specific supplier.
        /// Note: This method only maps the root Purchase entity for performance optimization, 
        /// omitting the hierarchical generation of items and payment details.
        /// </summary>
        /// <param name="supplierId">The unique identifier of the supplier.</param>
        /// <returns>A collection of Purchase entities linked to the specified supplier.</returns>
        public async Task<IList<Purchase>> GetBySupplierId(int supplierId)
        {
            return await WithStoredProcedureCommandAsync("dbo.usp_Purchase_GetBySupplierId", async command =>
            {
                // Bind the specific supplier parameter
                AddParameter(command, "@SupplierId", supplierId, DbType.Int32);

                // Apply tenant and contextual security filters
                AddFilterParameters(command);

                var purchases = new List<Purchase>();
                await using var reader = await command.ExecuteReaderAsync();

                // Iterate through the flat result set and map only the parent Purchase schema
                while (await reader.ReadAsync())
                {
                    purchases.Add(MapPurchase(reader));
                }

                return (IList<Purchase>)purchases;
            });
        }

        #endregion

        public async Task<IList<Purchase>> GetPendingBillsBySupplierId(int? supplierid)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Purchase>();
                IList<Purchase> results = await repository.Query().Where(x => x.SupplierId == supplierid && (x.TotalPayable - x.PaymentAmt) > 0).OrderBy(x => x.BillDate).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Update Purchase

        /// <summary>
        /// Updates an existing Purchase record and its associated hierarchical data (Items, Payments).
        /// Delegates the core UPSERT (Update/Insert) logic to the underlying SavePurchaseAsync method.
        /// </summary>
        /// <param name="model">The modified Purchase entity to be persisted.</param>
        /// <returns>The updated Purchase entity.</returns>
        public async Task<Purchase> Update(Purchase model)
        {
            // Delegate to the centralized saving mechanism which relies on SQL TVPs to handle modifications
            await SavePurchaseAsync(model);
            return model;
        }

        #endregion

        #region Delete Purchase

        /// <summary>
        /// Initiates a soft-delete operation for a specific Purchase record.
        /// Updates the 'Deleted' and 'DeletedBy' audit fields in the database instead of a hard removal.
        /// </summary>
        /// <param name="model">The Purchase entity intended for deletion.</param>
        /// <returns>A task representing the asynchronous asynchronous deletion operation.</returns>
        public async Task Delete(Purchase model)
        {
            await WithStoredProcedureCommandAsync("dbo.usp_Purchase_Delete", async command =>
            {
                // Pass the primary key of the record to be soft-deleted
                AddParameter(command, "@Id", model.Id, DbType.Int32);

                // Apply contextual filters to ensure the user has permission to delete this record
                AddFilterParameters(command);

                // Pass the current timestamp for the 'Deleted' audit column
                AddParameter(command, "@Now", DateTime.Now, DbType.DateTime2);

                // Execute the command without expecting a result set back
                await command.ExecuteNonQueryAsync();
            });
        }

        #endregion

        #region Item Purchase History Retrieval

        /// <summary>
        /// Retrieves the general purchase history for a specific item across all suppliers.
        /// This is typically used to display previous purchase rates, discounts, and batches for pricing reference.
        /// Delegates the execution to the centralized 'GetPurchaseHistoryAsync' helper.
        /// </summary>
        /// <param name="itemId">The unique identifier of the item.</param>
        /// <returns>A list of data transfer objects (DTOs) containing historical purchase details for the item.</returns>
        public async Task<List<ItemPurchaseHistoryDto>> GetItemSupplierPurchaseInfoByItemIdAsync(int itemId)
        {
            // Call the centralized history fetcher, passing the specific Stored Procedure name
            return await GetPurchaseHistoryAsync("dbo.usp_Purchase_GetItemSupplierPurchaseInfo", command =>
            {
                // Bind the required ItemId parameter dynamically via the lambda action
                AddParameter(command, "@ItemId", itemId, DbType.Int32);
            });
        }

        /// <summary>
        /// Retrieves the purchase history for a specific item, strictly filtered by a specific supplier.
        /// This is useful for analyzing vendor-specific pricing trends and previous discounts.
        /// </summary>
        /// <param name="itemId">The unique identifier of the item.</param>
        /// <param name="supplierId">The unique identifier of the specific supplier.</param>
        /// <returns>A list of data transfer objects (DTOs) containing vendor-specific historical purchase details.</returns>
        public async Task<List<ItemPurchaseHistoryDto>> GetItemPurchaseHistory(int itemId, int supplierId)
        {
            // Call the centralized history fetcher, passing the specific Stored Procedure name
            return await GetPurchaseHistoryAsync("dbo.usp_Purchase_GetItemHistory", command =>
            {
                // Bind both ItemId and SupplierId parameters to strictly filter the historical data
                AddParameter(command, "@ItemId", itemId, DbType.Int32);
                AddParameter(command, "@SupplierId", supplierId, DbType.Int32);
            });
        }

        #endregion

        #region Core Save Engine (UPSERT)

        /// <summary>
        /// The core, centralized engine responsible for persisting a Purchase entity to the database.
        /// Executes an UPSERT (Update or Insert) operation via the 'usp_Purchase_Save' stored procedure.
        /// Efficiently handles bulk operations by mapping child collections (Items, Payments) to SQL Server Table-Valued Parameters (TVPs).
        /// </summary>
        /// <param name="model">The fully populated Purchase entity to be saved.</param>
        /// <returns>The newly generated or existing primary key (Id) of the Purchase record.</returns>
        private async Task<int> SavePurchaseAsync(Purchase model)
        {
            // Delegate the execution to the base wrapper which manages the connection and transaction lifecycle
            return await WithStoredProcedureCommandAsync("dbo.usp_Purchase_Save", async command =>
            {
                // 1. Core Identifiers & UPSERT Signal
                // Passing null for Id signals the Stored Procedure to perform an INSERT. Otherwise, it performs an UPDATE.
                AddParameter(command, "@Id", model.Id > 0 ? model.Id : null, DbType.Int32);
                AddParameter(command, "@SupplierId", model.SupplierId, DbType.Int32);

                // 2. Header Information
                AddParameter(command, "@BillNo", model.BillNo);
                AddParameter(command, "@BillDate", model.BillDate, DbType.DateTime2);
                AddParameter(command, "@PartyBillNo", model.PartyBillNo);
                AddParameter(command, "@PartyBillDate", model.PartyBillDate, DbType.DateTime2);

                // 3. Financial & Tax Totals
                AddParameter(command, "@Total", model.Total, DbType.Decimal);
                AddParameter(command, "@TotalGstAmt", model.TotalGstAmt, DbType.Decimal);
                AddParameter(command, "@Totaldiscount", model.Totaldiscount, DbType.Decimal);
                AddParameter(command, "@TotalPayable", model.TotalPayable, DbType.Decimal);
                AddParameter(command, "@discountPercent", model.discountPercent, DbType.Decimal);
                AddParameter(command, "@discountAmount", model.discountAmount, DbType.Decimal);
                AddParameter(command, "@RoundOffAmount", model.RoundOffAmount, DbType.Decimal);
                AddParameter(command, "@TotalCGstAmt", model.TotalCGstAmt, DbType.Decimal);
                AddParameter(command, "@TotalSGstAmt", model.TotalSGstAmt, DbType.Decimal);
                AddParameter(command, "@TotalCessAmt", model.TotalCessAmt, DbType.Decimal);

                // 4. Payment Tracking Information
                AddParameter(command, "@PaymentAmt", model.PaymentAmt, DbType.Decimal);
                AddParameter(command, "@PaymentStatus", model.PaymentStatus);
                AddParameter(command, "@PaidAmount", model.PaidAmount, DbType.Decimal);
                AddParameter(command, "@ReturnAmount", model.ReturnAmount, DbType.Decimal);
                AddParameter(command, "@Balance", model.Balance, DbType.Decimal);

                // 5. Categorization Flags
                AddParameter(command, "@billingType", model.billingType);
                AddParameter(command, "@PaymentType", model.PaymentType);
                AddParameter(command, "@PurchaseType", model.PurchaseType);
                AddParameter(command, "@PurchaseOrderId", model.PurchaseOrderId, DbType.Int32);

                // 6. Table-Valued Parameters (TVPs) for Bulk Operations
                // Transforms the C# generic lists into SQL Server DataTables using the custom TableTypeMapper
                AddStructuredParameter(command, "@PurchaseItems", "dbo.PurchaseItemTvp", PurchaseModuleTableTypeMapper.CreatePurchaseItemTable(model.PurchaseItems));
                AddStructuredParameter(command, "@PaymentDetails", "dbo.PurchasePaymentDetailTvp", PurchaseModuleTableTypeMapper.CreatePurchasePaymentDetailTable(model.PaymentDetails));

                // 7. Security, Auditing, and Context Isolation
                AddFilterParameters(command);
                AddParameter(command, "@Now", DateTime.Now, DbType.DateTime2);

                // Execute the command. ExecuteScalarAsync is utilized because the SP explicitly returns the resultant Primary Key.
                var result = await command.ExecuteScalarAsync();

                // Ensure culture-invariant conversion to prevent formatting bugs on different server locales
                return Convert.ToInt32(result, CultureInfo.InvariantCulture);
            });
        }

        #endregion

        #region Centralized History Fetcher (DRY Helper)

        /// <summary>
        /// A centralized, highly reusable helper method to execute stored procedures that return item purchase history.
        /// Employs the DRY (Don't Repeat Yourself) principle by encapsulating the ADO.NET DataReader logic, 
        /// dynamic parameter binding via delegates, and manual DTO mapping into a single execution pipeline.
        /// </summary>
        /// <param name="storedProcedureName">The exact name of the SQL Stored Procedure to execute.</param>
        /// <param name="parameterBuilder">A delegate action that injects context-specific parameters into the DbCommand.</param>
        /// <returns>A strongly-typed, lightweight list of ItemPurchaseHistoryDto objects.</returns>
        private async Task<List<ItemPurchaseHistoryDto>> GetPurchaseHistoryAsync(string storedProcedureName, Action<DbCommand> parameterBuilder)
        {
            // Delegate command lifecycle management to the base repository wrapper
            return await WithStoredProcedureCommandAsync(storedProcedureName, async command =>
            {
                // 1. Dynamic Parameter Injection
                // Invoke the caller's delegate to bind specific parameters (e.g., ItemId, SupplierId)
                parameterBuilder(command);

                // 2. Contextual Security
                // Automatically append tenant/user isolation parameters to the command
                AddFilterParameters(command);

                var history = new List<ItemPurchaseHistoryDto>();

                // 3. High-Performance Data Streaming
                // Use an asynchronous forward-only stream to read data with minimal memory overhead
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    // 4. Manual DTO Mapping
                    // Extract and cast database columns directly into a lightweight Data Transfer Object
                    history.Add(new ItemPurchaseHistoryDto
                    {
                        Supplier = reader.ReadNullableString("Supplier") ?? string.Empty,
                        BillNo = reader.ReadNullableString("BillNo") ?? string.Empty,
                        BillDate = reader.ReadNullableDateTime("BillDate"),
                        Qty = reader.ReadInt32("Qty"),
                        FreeQty = reader.ReadInt32("FreeQty"),
                        Batch = reader.ReadNullableString("Batch") ?? string.Empty,
                        Mrp = reader.ReadDecimal("Mrp"),
                        Rate = reader.ReadDecimal("Rate"),
                        ExpiryDate = reader.ReadNullableDateTime("ExpiryDate"),
                        TotalDiscount = reader.ReadDecimal("TotalDiscount"),
                        BatchWiseCost = reader.ReadDecimal("BatchWiseCost")
                    });
                }

                return history;
            });
        }

        #endregion

        #region Rehydrate Object Graph (MARS)

        /// <summary>
        /// Constructs a complete hierarchical Object Graph (Purchase -> PurchaseItems, PaymentDetails) 
        /// from a forward-only database stream using Multiple Active Result Sets (MARS).
        /// Eliminates the N+1 query problem and Cartesian explosion by processing flat result sets sequentially 
        /// and linking child entities to their parents in-memory using an O(1) Dictionary lookup.
        /// </summary>
        /// <param name="command">The executed DbCommand containing the resultant data stream.</param>
        /// <returns>A fully populated collection of Purchase entities with their respective nested collections.</returns>
        private static async Task<List<Purchase>> ReadPurchaseGraphAsync(DbCommand command)
        {
            var purchases = new List<Purchase>();

            // An Identity Map (Dictionary) to provide O(1) lookup speed when associating child entities with their parents
            var lookup = new Dictionary<int, Purchase>();

            // Open an asynchronous, forward-only stream to read the data
            await using var reader = await command.ExecuteReaderAsync();

            // Result Set 1: Read Parent Entities (Purchases)
            while (await reader.ReadAsync())
            {
                var purchase = MapPurchase(reader);
                purchases.Add(purchase);

                // Cache the parent entity in the dictionary using its primary key
                lookup[purchase.Id] = purchase;
            }

            // Result Set 2: Read and Link Child Entities (PurchaseItems)
            // Move the reader cursor to the next SELECT statement outputted by the Stored Procedure
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var purchaseId = reader.ReadInt32("PurchaseId");

                    // Instantly locate the parent entity in memory. If not found, skip to prevent orphaned records.
                    if (!lookup.TryGetValue(purchaseId, out var purchase))
                    {
                        continue;
                    }

                    // Map the child item and append it directly to the parent's navigation collection
                    purchase.PurchaseItems.Add(MapPurchaseItem(reader, purchase));
                }
            }

            // Result Set 3: Read and Link Child Entities (PaymentDetails)
            // Move the reader cursor to the final SELECT statement outputted by the Stored Procedure
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var purchaseId = reader.ReadNullableInt32("PurchaseId");

                    // Ensure the foreign key exists and the corresponding parent entity is available in the lookup
                    if (!purchaseId.HasValue || !lookup.TryGetValue(purchaseId.Value, out var purchase))
                    {
                        continue;
                    }

                    // Map the payment detail and append it directly to the parent's navigation collection
                    purchase.PaymentDetails!.Add(MapPaymentDetail(reader, purchaseId.Value));
                }
            }

            return purchases;
        }

        #endregion

        #region Entity Mapping

        /// <summary>
        /// Manually maps a single database record from a DbDataReader to a Purchase entity.
        /// Bypasses ORM reflection overhead for maximum performance and instantiates navigation properties 
        /// (Supplier) if the joined data is present in the current row.
        /// </summary>
        /// <param name="reader">The active data reader pointing to the current record.</param>
        /// <returns>A fully hydrated Purchase entity.</returns>
        private static Purchase MapPurchase(DbDataReader reader)
        {
            var purchase = new Purchase
            {
                Id = reader.ReadInt32("Id"),
                SupplierId = reader.ReadNullableInt32("SupplierId"),
                BillNo = reader.ReadNullableString("BillNo") ?? string.Empty,
                BillDate = reader.ReadNullableDateTime("BillDate"),
                PartyBillNo = reader.ReadNullableString("PartyBillNo") ?? string.Empty,
                PartyBillDate = reader.ReadNullableDateTime("PartyBillDate"),
                Total = reader.ReadDecimal("Total"),
                TotalGstAmt = reader.ReadDecimal("TotalGstAmt"),
                Totaldiscount = reader.ReadDecimal("Totaldiscount"),
                TotalPayable = reader.ReadDecimal("TotalPayable"),
                discountPercent = reader.ReadDecimal("discountPercent"),
                discountAmount = reader.ReadDecimal("discountAmount"),
                PaymentAmt = reader.ReadDecimal("PaymentAmt"),
                PaymentStatus = reader.ReadNullableString("PaymentStatus"),
                RoundOffAmount = reader.ReadDecimal("RoundOffAmount"),
                billingType = reader.ReadNullableString("billingType"),
                PaymentType = reader.ReadNullableString("PaymentType"),
                PurchaseType = reader.ReadNullableString("PurchaseType"),
                TotalCGstAmt = reader.ReadDecimal("TotalCGstAmt"),
                TotalSGstAmt = reader.ReadDecimal("TotalSGstAmt"),
                TotalCessAmt = reader.ReadDecimal("TotalCessAmt"),
                PaidAmount = reader.ReadDecimal("PaidAmount"),
                ReturnAmount = reader.ReadDecimal("ReturnAmount"),
                Balance = reader.ReadDecimal("Balance"),
                PurchaseOrderId = reader.ReadNullableInt32("PurchaseOrderId"),
                TenantId = reader.ReadNullableString("TenantId"),
                Created = reader.ReadNullableDateTime("Created"),
                CreatedBy = reader.ReadNullableString("CreatedBy"),
                LastModified = reader.ReadNullableDateTime("LastModified"),
                LastModifiedBy = reader.ReadNullableString("LastModifiedBy"),
                Deleted = reader.ReadNullableDateTime("Deleted"),
                DeletedBy = reader.ReadNullableString("DeletedBy"),
                PurchaseItems = new List<PurchaseItem>(),
                PaymentDetails = new List<SalsePaymentDetails>()
            };

            var supplierName = reader.ReadNullableString("SupplierName");

            if (purchase.SupplierId.HasValue || !string.IsNullOrWhiteSpace(supplierName))
            {
                purchase.Suppliers = new Supplier
                {
                    Id = purchase.SupplierId ?? 0,
                    FirstName = supplierName ?? string.Empty
                };
            }

            return purchase;
        }

        #endregion

        #region Entity Mapping - Purchase Item

        /// <summary>
        /// Maps a single row from the data reader to a PurchaseItem entity.
        /// Also establishes the bidirectional relationship with its parent Purchase entity 
        /// and eagerly loads the associated ItemMaster details to prevent N+1 query issues on the UI.
        /// </summary>
        /// <param name="reader">The data reader containing the result set.</param>
        /// <param name="purchase">The parent Purchase entity to bind this item to.</param>
        /// <returns>A populated PurchaseItem entity.</returns>
        private static PurchaseItem MapPurchaseItem(DbDataReader reader, Purchase purchase)
        {
            var item = new PurchaseItem
            {
                Id = reader.ReadInt32("Id"),
                PurchaseId = reader.ReadInt32("PurchaseId"),

                // Establishes the parent-child relationship in memory for EF Core tracking
                Purchases = purchase,

                ItemId = reader.ReadInt32("ItemId"),
                Batch = reader.ReadNullableString("Batch"),
                ExpiryDate = reader.ReadNullableDateTime("ExpiryDate"),
                Mrp = reader.ReadDecimal("Mrp"),
                Qty = reader.ReadInt32("Qty"),
                FreeQty = reader.ReadInt32("FreeQty"),
                Unit = reader.ReadNullableString("Unit"),
                Rate = reader.ReadDecimal("Rate"),
                HsnId = reader.ReadNullableInt32("HsnId"),
                Gst = reader.ReadDecimal("Gst"),
                GstAmount = reader.ReadDecimal("GstAmount"),
                Discount = reader.ReadDecimal("Discount"),
                DiscountAmt = reader.ReadDecimal("DiscountAmt"),
                Amount = reader.ReadDecimal("Amount"),
                TotalAmt = reader.ReadDecimal("TotalAmt"),
                BatchWiseCose = reader.ReadDecimal("BatchWiseCose"),
                salserateA = reader.ReadDecimal("salserateA"),
                salserateB = reader.ReadDecimal("salserateB"),
                Barcode = reader.ReadNullableString("Barcode"),
                CGst = reader.ReadDecimal("CGst"),
                SGst = reader.ReadDecimal("SGst"),
                CGstAmount = reader.ReadDecimal("CGstAmount"),
                SGstAmount = reader.ReadDecimal("SGstAmount"),
                Cess = reader.ReadDecimal("Cess"),
                SourcePurchaseChallanId = reader.ReadNullableInt32("SourcePurchaseChallanId"),
                TenantId = reader.ReadNullableString("TenantId"),
                Created = reader.ReadNullableDateTime("Created"),
                CreatedBy = reader.ReadNullableString("CreatedBy"),
                LastModified = reader.ReadNullableDateTime("LastModified"),
                LastModifiedBy = reader.ReadNullableString("LastModifiedBy"),
                Deleted = reader.ReadNullableDateTime("Deleted"),
                DeletedBy = reader.ReadNullableString("DeletedBy")
            };

            // Eagerly instantiate the linked ItemMaster to populate UI grids without secondary DB calls
            item.ItemMasters = new ItemMaster
            {
                Id = item.ItemId,
                Name = reader.ReadNullableString("ItemName") ?? string.Empty,
                Conversion = reader.ReadInt32("ItemConversion"),
                Packing = reader.ReadNullableString("ItemPacking"),
                CompanyId = reader.ReadNullableInt32("ItemCompanyId")
            };

            return item;
        }

        #endregion

        #region Entity Mapping - Payment Details

        /// <summary>
        /// Maps a single row from the active data reader to a SalsePaymentDetails entity.
        /// Explicitly binds the child payment record to its parent Purchase entity using the provided foreign key 
        /// to ensure data integrity within the object graph.
        /// </summary>
        /// <param name="reader">The data reader containing the current payment row.</param>
        /// <param name="purchaseId">The primary key of the parent Purchase entity.</param>
        /// <returns>A fully hydrated SalsePaymentDetails entity.</returns>
        private static SalsePaymentDetails MapPaymentDetail(DbDataReader reader, int purchaseId)
        {
            var paymentModeId = reader.ReadInt32("PaymentModeId");
            var paymentModeName = reader.ReadNullableString("ModeOfPaymentName");
            var paymentModeType = reader.ReadNullableInt32("ModeOfPaymentType");

            return new SalsePaymentDetails
            {
                Id = reader.ReadInt32("Id"),

                // Explicitly assign the foreign key from the parent context
                PurchaseId = purchaseId,

                PaymentModeId = paymentModeId,
                ModeOfPayment = new ModeOfPayment
                {
                    Id = paymentModeId,
                    Name = paymentModeName ?? string.Empty,
                    Description = reader.ReadNullableString("ModeOfPaymentDescription"),
                    TenantId = reader.ReadNullableString("ModeOfPaymentTenantId"),
                    PaymentType = paymentModeType.HasValue ? (modeofpayment?)paymentModeType.Value : null,
                    Created = reader.ReadNullableDateTime("ModeOfPaymentCreated"),
                    CreatedBy = reader.ReadNullableString("ModeOfPaymentCreatedBy"),
                    LastModified = reader.ReadNullableDateTime("ModeOfPaymentLastModified"),
                    LastModifiedBy = reader.ReadNullableString("ModeOfPaymentLastModifiedBy"),
                    Deleted = reader.ReadNullableDateTime("ModeOfPaymentDeleted"),
                    DeletedBy = reader.ReadNullableString("ModeOfPaymentDeletedBy")
                },
                Amount = reader.ReadDecimal("Amount"),
                ReferenceNo = reader.ReadNullableString("ReferenceNo"),
                Description = reader.ReadNullableString("Description"),
                Date = reader.ReadNullableDateTime("Date"),
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

        public async Task<bool> IsBillNoDuplicateAsync(string billNo, int id = 0)
        {
            return await WithStoredProcedureCommandAsync("dbo.usp_Purchase_IsBillNoDuplicate", async command =>
            {
                // Parameters
                AddParameter(command, "@BillNo", billNo);
                AddParameter(command, "@Id", id, DbType.Int32);

                // Tenant filter (IMPORTANT)
                AddFilterParameters(command);

                var result = await command.ExecuteScalarAsync();

                return result != null && Convert.ToBoolean(result);
            });
        }
    }

}
