using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToPaymentVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "PaymentVoucher",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVoucher_TenantId",
                table: "PaymentVoucher",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentVoucher_Tenants_TenantId",
                table: "PaymentVoucher",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentVoucher_Tenants_TenantId",
                table: "PaymentVoucher");

            migrationBuilder.DropIndex(
                name: "IX_PaymentVoucher_TenantId",
                table: "PaymentVoucher");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PaymentVoucher");
        }
    }
}
