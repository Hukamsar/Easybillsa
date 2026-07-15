using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addexpenseinpurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Expense",
                table: "PurchaseSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Expense",
                table: "Purchases",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Purchases",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Expense",
                table: "PurchaseSettings");

            migrationBuilder.DropColumn(
                name: "Expense",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Purchases");
        }
    }
}
