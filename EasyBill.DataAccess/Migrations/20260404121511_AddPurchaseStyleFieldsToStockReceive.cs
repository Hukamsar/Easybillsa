using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseStyleFieldsToStockReceive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StockReceives_Customers_CustomerId')
BEGIN
    ALTER TABLE [StockReceives] DROP CONSTRAINT [FK_StockReceives_Customers_CustomerId];
END");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[StockReceives]')
      AND name = N'CustomerId'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE [StockReceives] ALTER COLUMN [CustomerId] int NULL;
END");

            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'Balance') IS NULL ALTER TABLE [StockReceives] ADD [Balance] decimal(18,2) NOT NULL DEFAULT 0.0;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'PaidAmount') IS NULL ALTER TABLE [StockReceives] ADD [PaidAmount] decimal(18,2) NOT NULL DEFAULT 0.0;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'PaymentType') IS NULL ALTER TABLE [StockReceives] ADD [PaymentType] nvarchar(max) NULL;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'ReturnAmount') IS NULL ALTER TABLE [StockReceives] ADD [ReturnAmount] decimal(18,2) NOT NULL DEFAULT 0.0;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'RoundOffAmount') IS NULL ALTER TABLE [StockReceives] ADD [RoundOffAmount] decimal(18,2) NOT NULL DEFAULT 0.0;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'SupplierId') IS NULL ALTER TABLE [StockReceives] ADD [SupplierId] int NULL;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'TaxCalculation') IS NULL ALTER TABLE [StockReceives] ADD [TaxCalculation] nvarchar(max) NULL;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'TotalCessAmount') IS NULL ALTER TABLE [StockReceives] ADD [TotalCessAmount] decimal(18,2) NOT NULL DEFAULT 0.0;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceiveItems', 'Cess') IS NULL ALTER TABLE [StockReceiveItems] ADD [Cess] decimal(18,2) NOT NULL DEFAULT 0.0;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.SalsePaymentDetails', 'StockReceiveId') IS NULL ALTER TABLE [SalsePaymentDetails] ADD [StockReceiveId] int NULL;");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.StockReceives', 'SupplierId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockReceives_SupplierId' AND object_id = OBJECT_ID(N'[StockReceives]'))
BEGIN
    CREATE INDEX [IX_StockReceives_SupplierId] ON [StockReceives] ([SupplierId]);
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.SalsePaymentDetails', 'StockReceiveId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalsePaymentDetails_StockReceiveId' AND object_id = OBJECT_ID(N'[SalsePaymentDetails]'))
BEGIN
    CREATE INDEX [IX_SalsePaymentDetails_StockReceiveId] ON [SalsePaymentDetails] ([StockReceiveId]);
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.SalsePaymentDetails', 'StockReceiveId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SalsePaymentDetails_StockReceives_StockReceiveId')
BEGIN
    ALTER TABLE [SalsePaymentDetails]
    ADD CONSTRAINT [FK_SalsePaymentDetails_StockReceives_StockReceiveId]
    FOREIGN KEY ([StockReceiveId]) REFERENCES [StockReceives] ([Id]);
END");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[StockReceives]') AND name = N'CustomerId')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StockReceives_Customers_CustomerId')
BEGIN
    ALTER TABLE [StockReceives]
    ADD CONSTRAINT [FK_StockReceives_Customers_CustomerId]
    FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]);
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.StockReceives', 'SupplierId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StockReceives_Suppliers_SupplierId')
BEGIN
    ALTER TABLE [StockReceives]
    ADD CONSTRAINT [FK_StockReceives_Suppliers_SupplierId]
    FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SalsePaymentDetails_StockReceives_StockReceiveId') ALTER TABLE [SalsePaymentDetails] DROP CONSTRAINT [FK_SalsePaymentDetails_StockReceives_StockReceiveId];");
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StockReceives_Suppliers_SupplierId') ALTER TABLE [StockReceives] DROP CONSTRAINT [FK_StockReceives_Suppliers_SupplierId];");
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_StockReceives_Customers_CustomerId') ALTER TABLE [StockReceives] DROP CONSTRAINT [FK_StockReceives_Customers_CustomerId];");
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_StockReceives_SupplierId' AND object_id = OBJECT_ID(N'[StockReceives]')) DROP INDEX [IX_StockReceives_SupplierId] ON [StockReceives];");
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SalsePaymentDetails_StockReceiveId' AND object_id = OBJECT_ID(N'[SalsePaymentDetails]')) DROP INDEX [IX_SalsePaymentDetails_StockReceiveId] ON [SalsePaymentDetails];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'Balance') IS NOT NULL ALTER TABLE [StockReceives] DROP COLUMN [Balance];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'PaidAmount') IS NOT NULL ALTER TABLE [StockReceives] DROP COLUMN [PaidAmount];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'PaymentType') IS NOT NULL ALTER TABLE [StockReceives] DROP COLUMN [PaymentType];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'ReturnAmount') IS NOT NULL ALTER TABLE [StockReceives] DROP COLUMN [ReturnAmount];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'RoundOffAmount') IS NOT NULL ALTER TABLE [StockReceives] DROP COLUMN [RoundOffAmount];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'SupplierId') IS NOT NULL ALTER TABLE [StockReceives] DROP COLUMN [SupplierId];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'TaxCalculation') IS NOT NULL ALTER TABLE [StockReceives] DROP COLUMN [TaxCalculation];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'TotalCessAmount') IS NOT NULL ALTER TABLE [StockReceives] DROP COLUMN [TotalCessAmount];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceiveItems', 'Cess') IS NOT NULL ALTER TABLE [StockReceiveItems] DROP COLUMN [Cess];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.SalsePaymentDetails', 'StockReceiveId') IS NOT NULL ALTER TABLE [SalsePaymentDetails] DROP COLUMN [StockReceiveId];");
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[StockReceives]')
      AND name = N'CustomerId'
      AND is_nullable = 1
)
AND NOT EXISTS (SELECT 1 FROM [StockReceives] WHERE [CustomerId] IS NULL)
BEGIN
    ALTER TABLE [StockReceives] ALTER COLUMN [CustomerId] int NOT NULL;
    ALTER TABLE [StockReceives]
    ADD CONSTRAINT [FK_StockReceives_Customers_CustomerId]
    FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE;
END");
        }
    }
}
