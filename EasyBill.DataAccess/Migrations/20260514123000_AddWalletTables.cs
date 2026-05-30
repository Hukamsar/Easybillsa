using AOne.DataAccess.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260514123000_AddWalletTables")]
    public class AddWalletTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[WalletMasters]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WalletMasters]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [WalletID] NVARCHAR(40) NOT NULL,
        [WalletType] INT NOT NULL,
        [CustomerId] INT NULL,
        [DeliveryBoyId] INT NULL,
        [DeliveryBoyName] NVARCHAR(160) NULL,
        [DeliveryBoyMobileNo] NVARCHAR(20) NULL,
        [CurrentBalance] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_WalletMasters_CurrentBalance] DEFAULT ((0)),
        [Status] INT NOT NULL,
        [PerBillUsageLimit] DECIMAL(18,2) NULL,
        [AutoDeductInBilling] BIT NOT NULL CONSTRAINT [DF_WalletMasters_AutoDeductInBilling] DEFAULT ((1)),
        [IsExpiryEnabled] BIT NOT NULL CONSTRAINT [DF_WalletMasters_IsExpiryEnabled] DEFAULT ((0)),
        [ValidFrom] DATETIME2 NULL,
        [ValidTo] DATETIME2 NULL,
        [TenantId] NVARCHAR(450) NULL,
        [Created] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(MAX) NULL,
        [LastModified] DATETIME2 NULL,
        [LastModifiedBy] NVARCHAR(MAX) NULL,
        [Deleted] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_WalletMasters] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE UNIQUE INDEX [IX_WalletMasters_WalletID] ON [dbo].[WalletMasters]([WalletID]);
    CREATE INDEX [IX_WalletMasters_CustomerId] ON [dbo].[WalletMasters]([CustomerId]);
    CREATE INDEX [IX_WalletMasters_DeliveryBoyId] ON [dbo].[WalletMasters]([DeliveryBoyId]);
    CREATE INDEX [IX_WalletMasters_TenantId] ON [dbo].[WalletMasters]([TenantId]);

    ALTER TABLE [dbo].[WalletMasters] WITH CHECK
    ADD CONSTRAINT [FK_WalletMasters_Customers_CustomerId]
        FOREIGN KEY([CustomerId]) REFERENCES [dbo].[Customers]([Id]);

    ALTER TABLE [dbo].[WalletMasters] WITH CHECK
    ADD CONSTRAINT [FK_WalletMasters_Employees_DeliveryBoyId]
        FOREIGN KEY([DeliveryBoyId]) REFERENCES [dbo].[Employees]([Id]);

    ALTER TABLE [dbo].[WalletMasters] WITH CHECK
    ADD CONSTRAINT [FK_WalletMasters_Tenants_TenantId]
        FOREIGN KEY([TenantId]) REFERENCES [dbo].[Tenants]([Id]);
END

IF OBJECT_ID(N'[dbo].[WalletTransactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WalletTransactions]
    (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [WalletMasterId] INT NOT NULL,
        [TransactionDateTime] DATETIME2 NOT NULL,
        [Credit] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_WalletTransactions_Credit] DEFAULT ((0)),
        [Debit] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_WalletTransactions_Debit] DEFAULT ((0)),
        [TransactionType] INT NOT NULL,
        [PaymentMode] NVARCHAR(40) NULL,
        [ReferenceNumber] NVARCHAR(100) NULL,
        [ReferenceBillNo] NVARCHAR(80) NULL,
        [ReferenceSaleId] INT NULL,
        [UserId] NVARCHAR(450) NULL,
        [UserName] NVARCHAR(150) NULL,
        [Remarks] NVARCHAR(600) NULL,
        [ClosingBalance] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_WalletTransactions_ClosingBalance] DEFAULT ((0)),
        [ExpiryDate] DATETIME2 NULL,
        [TenantId] NVARCHAR(450) NULL,
        [Created] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(MAX) NULL,
        [LastModified] DATETIME2 NULL,
        [LastModifiedBy] NVARCHAR(MAX) NULL,
        [Deleted] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_WalletTransactions] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE INDEX [IX_WalletTransactions_WalletMasterId] ON [dbo].[WalletTransactions]([WalletMasterId]);
    CREATE INDEX [IX_WalletTransactions_TenantId] ON [dbo].[WalletTransactions]([TenantId]);
    CREATE INDEX [IX_WalletTransactions_WalletMasterId_TransactionDateTime]
        ON [dbo].[WalletTransactions]([WalletMasterId], [TransactionDateTime]);

    ALTER TABLE [dbo].[WalletTransactions] WITH CHECK
    ADD CONSTRAINT [FK_WalletTransactions_WalletMasters_WalletMasterId]
        FOREIGN KEY([WalletMasterId]) REFERENCES [dbo].[WalletMasters]([Id]) ON DELETE CASCADE;

    ALTER TABLE [dbo].[WalletTransactions] WITH CHECK
    ADD CONSTRAINT [FK_WalletTransactions_Tenants_TenantId]
        FOREIGN KEY([TenantId]) REFERENCES [dbo].[Tenants]([Id]);
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[WalletTransactions]', N'U') IS NOT NULL
    DROP TABLE [dbo].[WalletTransactions];

IF OBJECT_ID(N'[dbo].[WalletMasters]', N'U') IS NOT NULL
    DROP TABLE [dbo].[WalletMasters];");
        }
    }
}
