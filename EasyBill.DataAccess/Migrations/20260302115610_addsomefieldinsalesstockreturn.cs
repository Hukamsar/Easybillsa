using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addsomefieldinsalesstockreturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NetCollection",
                table: "StockReturns",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "StockReturns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RoundOffAmount",
                table: "StockReturns",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "billingType",
                table: "StockReturns",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetCollection",
                table: "StockReturns");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "StockReturns");

            migrationBuilder.DropColumn(
                name: "RoundOffAmount",
                table: "StockReturns");

            migrationBuilder.DropColumn(
                name: "billingType",
                table: "StockReturns");
        }
    }
}
