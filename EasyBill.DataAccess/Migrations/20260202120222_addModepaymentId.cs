using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addModepaymentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentModeId",
                table: "PaymentVoucher",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVoucher_PaymentModeId",
                table: "PaymentVoucher",
                column: "PaymentModeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentVoucher_ModeOfPayments_PaymentModeId",
                table: "PaymentVoucher",
                column: "PaymentModeId",
                principalTable: "ModeOfPayments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentVoucher_ModeOfPayments_PaymentModeId",
                table: "PaymentVoucher");

            migrationBuilder.DropIndex(
                name: "IX_PaymentVoucher_PaymentModeId",
                table: "PaymentVoucher");

            migrationBuilder.DropColumn(
                name: "PaymentModeId",
                table: "PaymentVoucher");
        }
    }
}
