using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationAndVoucherFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedPurchaseIds",
                table: "ReceiveVouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedSalesIds",
                table: "ReceiveVouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedPurchaseIds",
                table: "PaymentVoucher",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedSalesIds",
                table: "PaymentVoucher",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedPurchaseIds",
                table: "ReceiveVouchers");

            migrationBuilder.DropColumn(
                name: "SelectedSalesIds",
                table: "ReceiveVouchers");

            migrationBuilder.DropColumn(
                name: "SelectedPurchaseIds",
                table: "PaymentVoucher");

            migrationBuilder.DropColumn(
                name: "SelectedSalesIds",
                table: "PaymentVoucher");
        }
    }
}
