using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesLikeFieldsToStockIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockIssues_Customers_CustomerId",
                table: "StockIssues");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "StockIssues",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "StockIssues",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetCollection",
                table: "StockIssues",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "StockIssues",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "StockIssues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnAmount",
                table: "StockIssues",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RoundOffAmount",
                table: "StockIssues",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TaxCalculation",
                table: "StockIssues",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCessAmount",
                table: "StockIssues",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "billingType",
                table: "StockIssues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CGst",
                table: "StockIssueItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cess",
                table: "StockIssueItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HsnCode",
                table: "StockIssueItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HsnId",
                table: "StockIssueItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IGst",
                table: "StockIssueItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSoldInTablets",
                table: "StockIssueItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SGst",
                table: "StockIssueItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StripRate",
                table: "StockIssueItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StockIssueId",
                table: "SalsePaymentDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockIssueItems_HsnId",
                table: "StockIssueItems",
                column: "HsnId");

            migrationBuilder.CreateIndex(
                name: "IX_SalsePaymentDetails_StockIssueId",
                table: "SalsePaymentDetails",
                column: "StockIssueId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalsePaymentDetails_StockIssues_StockIssueId",
                table: "SalsePaymentDetails",
                column: "StockIssueId",
                principalTable: "StockIssues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockIssueItems_Hsns_HsnId",
                table: "StockIssueItems",
                column: "HsnId",
                principalTable: "Hsns",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockIssues_Customers_CustomerId",
                table: "StockIssues",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalsePaymentDetails_StockIssues_StockIssueId",
                table: "SalsePaymentDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockIssueItems_Hsns_HsnId",
                table: "StockIssueItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockIssues_Customers_CustomerId",
                table: "StockIssues");

            migrationBuilder.DropIndex(
                name: "IX_StockIssueItems_HsnId",
                table: "StockIssueItems");

            migrationBuilder.DropIndex(
                name: "IX_SalsePaymentDetails_StockIssueId",
                table: "SalsePaymentDetails");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "NetCollection",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "ReturnAmount",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "RoundOffAmount",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "TaxCalculation",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "TotalCessAmount",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "billingType",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "CGst",
                table: "StockIssueItems");

            migrationBuilder.DropColumn(
                name: "Cess",
                table: "StockIssueItems");

            migrationBuilder.DropColumn(
                name: "HsnCode",
                table: "StockIssueItems");

            migrationBuilder.DropColumn(
                name: "HsnId",
                table: "StockIssueItems");

            migrationBuilder.DropColumn(
                name: "IGst",
                table: "StockIssueItems");

            migrationBuilder.DropColumn(
                name: "IsSoldInTablets",
                table: "StockIssueItems");

            migrationBuilder.DropColumn(
                name: "SGst",
                table: "StockIssueItems");

            migrationBuilder.DropColumn(
                name: "StripRate",
                table: "StockIssueItems");

            migrationBuilder.DropColumn(
                name: "StockIssueId",
                table: "SalsePaymentDetails");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "StockIssues",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StockIssues_Customers_CustomerId",
                table: "StockIssues",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
