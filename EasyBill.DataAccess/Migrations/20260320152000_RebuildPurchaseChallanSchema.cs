using AOne.DataAccess.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260320152000_RebuildPurchaseChallanSchema")]
    public partial class RebuildPurchaseChallanSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[PurchaseChallans]', N'U') IS NOT NULL
                   AND COL_LENGTH('dbo.PurchaseChallans', 'BillNo') IS NULL
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM sys.foreign_keys
                        WHERE name = 'FK_SalsePaymentDetails_PurchaseChallans_PurchaseChallanId'
                    )
                    BEGIN
                        ALTER TABLE [dbo].[SalsePaymentDetails]
                        DROP CONSTRAINT [FK_SalsePaymentDetails_PurchaseChallans_PurchaseChallanId];
                    END;

                    IF OBJECT_ID(N'[dbo].[PurchaseChallanItems]', N'U') IS NOT NULL
                       AND EXISTS (
                           SELECT 1
                           FROM sys.foreign_keys
                           WHERE name = 'FK_PurchaseChallanItems_PurchaseChallans_PurchaseChallanId'
                       )
                    BEGIN
                        ALTER TABLE [dbo].[PurchaseChallanItems]
                        DROP CONSTRAINT [FK_PurchaseChallanItems_PurchaseChallans_PurchaseChallanId];
                    END;

                    IF OBJECT_ID(N'[dbo].[PurchaseChallanItems_Legacy_20260320]', N'U') IS NULL
                       AND OBJECT_ID(N'[dbo].[PurchaseChallanItems]', N'U') IS NOT NULL
                    BEGIN
                        EXEC sp_rename N'dbo.PurchaseChallanItems', N'PurchaseChallanItems_Legacy_20260320';
                    END;

                    IF OBJECT_ID(N'[dbo].[PurchaseChallans_Legacy_20260320]', N'U') IS NULL
                    BEGIN
                        EXEC sp_rename N'dbo.PurchaseChallans', N'PurchaseChallans_Legacy_20260320';
                    END;
                END;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[PurchaseChallans]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PurchaseChallans]
                    (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [SupplierId] int NULL,
                        [BillNo] nvarchar(max) NOT NULL,
                        [BillDate] datetime2 NULL,
                        [PartyBillNo] nvarchar(max) NOT NULL,
                        [PartyBillDate] datetime2 NULL,
                        [TenantId] nvarchar(450) NULL,
                        [TotalGstAmt] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_TotalGstAmt] DEFAULT (0),
                        [Totaldiscount] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_Totaldiscount] DEFAULT (0),
                        [TotalPayable] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_TotalPayable] DEFAULT (0),
                        [discountPercent] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_discountPercent] DEFAULT (0),
                        [discountAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_discountAmount] DEFAULT (0),
                        [Total] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_Total] DEFAULT (0),
                        [PaymentAmt] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_PaymentAmt] DEFAULT (0),
                        [PaymentStatus] nvarchar(max) NULL,
                        [RoundOffAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_RoundOffAmount] DEFAULT (0),
                        [billingType] nvarchar(max) NULL,
                        [PaymentType] nvarchar(max) NULL,
                        [PurchaseType] nvarchar(max) NULL,
                        [TotalCGstAmt] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_TotalCGstAmt] DEFAULT (0),
                        [TotalSGstAmt] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_TotalSGstAmt] DEFAULT (0),
                        [PaidAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_PaidAmount] DEFAULT (0),
                        [ReturnAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_ReturnAmount] DEFAULT (0),
                        [Balance] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallans_Balance] DEFAULT (0),
                        [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_PurchaseChallans_Status] DEFAULT (N'Pending'),
                        [ConvertedPurchaseId] int NULL,
                        [Created] datetime2 NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [LastModified] datetime2 NULL,
                        [LastModifiedBy] nvarchar(max) NULL,
                        [Deleted] datetime2 NULL,
                        [DeletedBy] nvarchar(max) NULL,
                        CONSTRAINT [PK_PurchaseChallans] PRIMARY KEY ([Id])
                    );
                END;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[PurchaseChallanItems]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PurchaseChallanItems]
                    (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [PurchaseChallanId] int NOT NULL,
                        [TenantId] nvarchar(450) NULL,
                        [Batch] nvarchar(max) NULL,
                        [ItemId] int NOT NULL,
                        [Qty] int NOT NULL CONSTRAINT [DF_PurchaseChallanItems_Qty] DEFAULT (0),
                        [FreeQty] int NOT NULL CONSTRAINT [DF_PurchaseChallanItems_FreeQty] DEFAULT (0),
                        [Unit] nvarchar(max) NULL,
                        [Rate] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_Rate] DEFAULT (0),
                        [HsnId] int NULL,
                        [Gst] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_Gst] DEFAULT (0),
                        [GstAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_GstAmount] DEFAULT (0),
                        [Discount] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_Discount] DEFAULT (0),
                        [DiscountAmt] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_DiscountAmt] DEFAULT (0),
                        [ExpiryDate] datetime2 NULL,
                        [Amount] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_Amount] DEFAULT (0),
                        [TotalAmt] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_TotalAmt] DEFAULT (0),
                        [BatchWiseCose] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_BatchWiseCose] DEFAULT (0),
                        [Mrp] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_Mrp] DEFAULT (0),
                        [salserateA] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_salserateA] DEFAULT (0),
                        [salserateB] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_salserateB] DEFAULT (0),
                        [Barcode] nvarchar(max) NULL,
                        [CGst] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_CGst] DEFAULT (0),
                        [SGst] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_SGst] DEFAULT (0),
                        [CGstAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_CGstAmount] DEFAULT (0),
                        [SGstAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_PurchaseChallanItems_SGstAmount] DEFAULT (0),
                        [ConvertedQty] int NOT NULL CONSTRAINT [DF_PurchaseChallanItems_ConvertedQty] DEFAULT (0),
                        [Created] datetime2 NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        [LastModified] datetime2 NULL,
                        [LastModifiedBy] nvarchar(max) NULL,
                        [Deleted] datetime2 NULL,
                        [DeletedBy] nvarchar(max) NULL,
                        CONSTRAINT [PK_PurchaseChallanItems] PRIMARY KEY ([Id])
                    );
                END;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[PurchaseChallans_Legacy_20260320]', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM [dbo].[PurchaseChallans])
                BEGIN
                    SET IDENTITY_INSERT [dbo].[PurchaseChallans] ON;

                    INSERT INTO [dbo].[PurchaseChallans]
                    (
                        [Id], [SupplierId], [BillNo], [BillDate], [PartyBillNo], [PartyBillDate], [TenantId],
                        [TotalGstAmt], [Totaldiscount], [TotalPayable], [discountPercent], [discountAmount],
                        [Total], [PaymentAmt], [PaymentStatus], [RoundOffAmount], [billingType], [PaymentType],
                        [PurchaseType], [TotalCGstAmt], [TotalSGstAmt], [PaidAmount], [ReturnAmount], [Balance],
                        [Status], [ConvertedPurchaseId], [Created], [CreatedBy], [LastModified], [LastModifiedBy],
                        [Deleted], [DeletedBy]
                    )
                    SELECT
                        [Id],
                        [SupplierId],
                        [ChallanNo],
                        [ChallanDate],
                        ISNULL([SupplierChallanNo], N''),
                        [ChallanDate],
                        [TenantId],
                        [GstAmount],
                        [DiscountAmount],
                        [NetAmount],
                        0,
                        [DiscountAmount],
                        [GrossAmount],
                        0,
                        N'Pending',
                        0,
                        NULL,
                        NULL,
                        NULL,
                        0,
                        0,
                        0,
                        0,
                        [NetAmount],
                        ISNULL(NULLIF([Status], N''), N'Pending'),
                        NULL,
                        [Created],
                        [CreatedBy],
                        [LastModified],
                        [LastModifiedBy],
                        [Deleted],
                        [DeletedBy]
                    FROM [dbo].[PurchaseChallans_Legacy_20260320];

                    SET IDENTITY_INSERT [dbo].[PurchaseChallans] OFF;
                END;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[PurchaseChallanItems_Legacy_20260320]', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM [dbo].[PurchaseChallanItems])
                BEGIN
                    SET IDENTITY_INSERT [dbo].[PurchaseChallanItems] ON;

                    INSERT INTO [dbo].[PurchaseChallanItems]
                    (
                        [Id], [PurchaseChallanId], [TenantId], [Batch], [ItemId], [Qty], [FreeQty], [Unit],
                        [Rate], [HsnId], [Gst], [GstAmount], [Discount], [DiscountAmt], [ExpiryDate], [Amount],
                        [TotalAmt], [BatchWiseCose], [Mrp], [salserateA], [salserateB], [Barcode], [CGst], [SGst],
                        [CGstAmount], [SGstAmount], [ConvertedQty], [Created], [CreatedBy], [LastModified],
                        [LastModifiedBy], [Deleted], [DeletedBy]
                    )
                    SELECT
                        [Id],
                        [PurchaseChallanId],
                        [TenantId],
                        NULLIF(LTRIM(RTRIM([Description])), N''),
                        [ItemId],
                        ISNULL(TRY_CONVERT(int, ROUND([Qty], 0)), 0),
                        0,
                        NULL,
                        [Rate],
                        NULL,
                        [GstPercent],
                        [GstAmount],
                        [DiscountPercent],
                        [DiscountAmount],
                        NULL,
                        [Amount],
                        [Amount],
                        [Rate],
                        0,
                        0,
                        0,
                        NULL,
                        0,
                        0,
                        0,
                        0,
                        ISNULL(TRY_CONVERT(int, ROUND([ConvertedQty], 0)), 0),
                        [Created],
                        [CreatedBy],
                        [LastModified],
                        [LastModifiedBy],
                        [Deleted],
                        [DeletedBy]
                    FROM [dbo].[PurchaseChallanItems_Legacy_20260320];

                    SET IDENTITY_INSERT [dbo].[PurchaseChallanItems] OFF;
                END;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_PurchaseChallans_ConvertedPurchaseId'
                      AND object_id = OBJECT_ID(N'[dbo].[PurchaseChallans]')
                )
                BEGIN
                    CREATE INDEX [IX_PurchaseChallans_ConvertedPurchaseId]
                    ON [dbo].[PurchaseChallans] ([ConvertedPurchaseId]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_PurchaseChallans_SupplierId'
                      AND object_id = OBJECT_ID(N'[dbo].[PurchaseChallans]')
                )
                BEGIN
                    CREATE INDEX [IX_PurchaseChallans_SupplierId]
                    ON [dbo].[PurchaseChallans] ([SupplierId]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_PurchaseChallans_TenantId'
                      AND object_id = OBJECT_ID(N'[dbo].[PurchaseChallans]')
                )
                BEGIN
                    CREATE INDEX [IX_PurchaseChallans_TenantId]
                    ON [dbo].[PurchaseChallans] ([TenantId]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_PurchaseChallans_Purchases_ConvertedPurchaseId'
                )
                BEGIN
                    ALTER TABLE [dbo].[PurchaseChallans]
                    ADD CONSTRAINT [FK_PurchaseChallans_Purchases_ConvertedPurchaseId]
                    FOREIGN KEY ([ConvertedPurchaseId]) REFERENCES [dbo].[Purchases] ([Id]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_PurchaseChallans_Suppliers_SupplierId'
                )
                BEGIN
                    ALTER TABLE [dbo].[PurchaseChallans]
                    ADD CONSTRAINT [FK_PurchaseChallans_Suppliers_SupplierId]
                    FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers] ([Id]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_PurchaseChallans_Tenants_TenantId'
                )
                BEGIN
                    ALTER TABLE [dbo].[PurchaseChallans]
                    ADD CONSTRAINT [FK_PurchaseChallans_Tenants_TenantId]
                    FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants] ([Id]);
                END;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_PurchaseChallanItems_HsnId'
                      AND object_id = OBJECT_ID(N'[dbo].[PurchaseChallanItems]')
                )
                BEGIN
                    CREATE INDEX [IX_PurchaseChallanItems_HsnId]
                    ON [dbo].[PurchaseChallanItems] ([HsnId]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_PurchaseChallanItems_ItemId'
                      AND object_id = OBJECT_ID(N'[dbo].[PurchaseChallanItems]')
                )
                BEGIN
                    CREATE INDEX [IX_PurchaseChallanItems_ItemId]
                    ON [dbo].[PurchaseChallanItems] ([ItemId]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_PurchaseChallanItems_PurchaseChallanId'
                      AND object_id = OBJECT_ID(N'[dbo].[PurchaseChallanItems]')
                )
                BEGIN
                    CREATE INDEX [IX_PurchaseChallanItems_PurchaseChallanId]
                    ON [dbo].[PurchaseChallanItems] ([PurchaseChallanId]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_PurchaseChallanItems_TenantId'
                      AND object_id = OBJECT_ID(N'[dbo].[PurchaseChallanItems]')
                )
                BEGIN
                    CREATE INDEX [IX_PurchaseChallanItems_TenantId]
                    ON [dbo].[PurchaseChallanItems] ([TenantId]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_PurchaseChallanItems_Hsns_HsnId'
                )
                BEGIN
                    ALTER TABLE [dbo].[PurchaseChallanItems]
                    ADD CONSTRAINT [FK_PurchaseChallanItems_Hsns_HsnId]
                    FOREIGN KEY ([HsnId]) REFERENCES [dbo].[Hsns] ([Id]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_PurchaseChallanItems_ItemMasters_ItemId'
                )
                BEGIN
                    ALTER TABLE [dbo].[PurchaseChallanItems]
                    ADD CONSTRAINT [FK_PurchaseChallanItems_ItemMasters_ItemId]
                    FOREIGN KEY ([ItemId]) REFERENCES [dbo].[ItemMasters] ([Id]) ON DELETE CASCADE;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_PurchaseChallanItems_PurchaseChallans_PurchaseChallanId'
                )
                BEGIN
                    ALTER TABLE [dbo].[PurchaseChallanItems]
                    ADD CONSTRAINT [FK_PurchaseChallanItems_PurchaseChallans_PurchaseChallanId]
                    FOREIGN KEY ([PurchaseChallanId]) REFERENCES [dbo].[PurchaseChallans] ([Id]) ON DELETE CASCADE;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_PurchaseChallanItems_Tenants_TenantId'
                )
                BEGIN
                    ALTER TABLE [dbo].[PurchaseChallanItems]
                    ADD CONSTRAINT [FK_PurchaseChallanItems_Tenants_TenantId]
                    FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants] ([Id]);
                END;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.SalsePaymentDetails', 'PurchaseChallanId') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[SalsePaymentDetails]
                    ADD [PurchaseChallanId] int NULL;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_SalsePaymentDetails_PurchaseChallanId'
                      AND object_id = OBJECT_ID(N'[dbo].[SalsePaymentDetails]')
                )
                BEGIN
                    CREATE INDEX [IX_SalsePaymentDetails_PurchaseChallanId]
                    ON [dbo].[SalsePaymentDetails] ([PurchaseChallanId]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_SalsePaymentDetails_PurchaseChallans_PurchaseChallanId'
                )
                BEGIN
                    ALTER TABLE [dbo].[SalsePaymentDetails]
                    ADD CONSTRAINT [FK_SalsePaymentDetails_PurchaseChallans_PurchaseChallanId]
                    FOREIGN KEY ([PurchaseChallanId]) REFERENCES [dbo].[PurchaseChallans] ([Id]);
                END;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[PurchaseChallanItems_Legacy_20260320]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[PurchaseChallanItems_Legacy_20260320];
                END;

                IF OBJECT_ID(N'[dbo].[PurchaseChallans_Legacy_20260320]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[PurchaseChallans_Legacy_20260320];
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
