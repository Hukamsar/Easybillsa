using AOne.DataAccess.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260320143000_FixPurchaseChallanPaymentLink")]
    public partial class FixPurchaseChallanPaymentLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.SalsePaymentDetails', 'PurchaseChallanId') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[SalsePaymentDetails] ADD [PurchaseChallanId] int NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_SalsePaymentDetails_PurchaseChallanId'
                      AND object_id = OBJECT_ID(N'[dbo].[SalsePaymentDetails]')
                )
                BEGIN
                    CREATE INDEX [IX_SalsePaymentDetails_PurchaseChallanId]
                    ON [dbo].[SalsePaymentDetails] ([PurchaseChallanId]);
                END
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_SalsePaymentDetails_PurchaseChallans_PurchaseChallanId'
                )
                BEGIN
                    ALTER TABLE [dbo].[SalsePaymentDetails]
                    ADD CONSTRAINT [FK_SalsePaymentDetails_PurchaseChallans_PurchaseChallanId]
                    FOREIGN KEY ([PurchaseChallanId]) REFERENCES [dbo].[PurchaseChallans] ([Id]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = 'FK_SalsePaymentDetails_PurchaseChallans_PurchaseChallanId'
                )
                BEGIN
                    ALTER TABLE [dbo].[SalsePaymentDetails]
                    DROP CONSTRAINT [FK_SalsePaymentDetails_PurchaseChallans_PurchaseChallanId];
                END
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_SalsePaymentDetails_PurchaseChallanId'
                      AND object_id = OBJECT_ID(N'[dbo].[SalsePaymentDetails]')
                )
                BEGIN
                    DROP INDEX [IX_SalsePaymentDetails_PurchaseChallanId]
                    ON [dbo].[SalsePaymentDetails];
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.SalsePaymentDetails', 'PurchaseChallanId') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[SalsePaymentDetails] DROP COLUMN [PurchaseChallanId];
                END
                """);
        }
    }
}
