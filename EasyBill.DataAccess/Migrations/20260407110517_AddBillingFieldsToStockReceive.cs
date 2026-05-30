using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingFieldsToStockReceive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'PartyBillNo') IS NULL ALTER TABLE [StockReceives] ADD [PartyBillNo] nvarchar(max) NULL;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'PartyBillDate') IS NULL ALTER TABLE [StockReceives] ADD [PartyBillDate] datetime2 NULL;");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'billingType') IS NULL ALTER TABLE [StockReceives] ADD [billingType] nvarchar(max) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'billingType') IS NOT NULL ALTER TABLE [StockReceives] DROP COLUMN [billingType];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'PartyBillDate') IS NOT NULL ALTER TABLE [StockReceives] DROP COLUMN [PartyBillDate];");
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.StockReceives', 'PartyBillNo') IS NOT NULL ALTER TABLE [StockReceives] DROP COLUMN [PartyBillNo];");
        }
    }
}
