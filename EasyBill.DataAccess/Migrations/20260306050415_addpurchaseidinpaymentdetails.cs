using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addpurchaseidinpaymentdetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PurchaseId",
                table: "SalsePaymentDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockReturnId",
                table: "SalsePaymentDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalsePaymentDetails_PurchaseId",
                table: "SalsePaymentDetails",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SalsePaymentDetails_StockReturnId",
                table: "SalsePaymentDetails",
                column: "StockReturnId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalsePaymentDetails_Purchases_PurchaseId",
                table: "SalsePaymentDetails",
                column: "PurchaseId",
                principalTable: "Purchases",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SalsePaymentDetails_StockReturns_StockReturnId",
                table: "SalsePaymentDetails",
                column: "StockReturnId",
                principalTable: "StockReturns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalsePaymentDetails_Purchases_PurchaseId",
                table: "SalsePaymentDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SalsePaymentDetails_StockReturns_StockReturnId",
                table: "SalsePaymentDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalsePaymentDetails_PurchaseId",
                table: "SalsePaymentDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalsePaymentDetails_StockReturnId",
                table: "SalsePaymentDetails");

            migrationBuilder.DropColumn(
                name: "PurchaseId",
                table: "SalsePaymentDetails");

            migrationBuilder.DropColumn(
                name: "StockReturnId",
                table: "SalsePaymentDetails");
        }
    }
}
