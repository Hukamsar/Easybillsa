using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addcolumnInStockreturntenentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "StockReturns",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "StockReturnItems",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockReturns_TenantId",
                table: "StockReturns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReturnItems_TenantId",
                table: "StockReturnItems",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockReturnItems_Tenants_TenantId",
                table: "StockReturnItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockReturns_Tenants_TenantId",
                table: "StockReturns",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockReturnItems_Tenants_TenantId",
                table: "StockReturnItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockReturns_Tenants_TenantId",
                table: "StockReturns");

            migrationBuilder.DropIndex(
                name: "IX_StockReturns_TenantId",
                table: "StockReturns");

            migrationBuilder.DropIndex(
                name: "IX_StockReturnItems_TenantId",
                table: "StockReturnItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StockReturns");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "StockReturnItems");
        }
    }
}
