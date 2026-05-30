using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addsomefieldintenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaymentGatewayActive",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentGatewayKey",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentGatewayProvider",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentGatewaySecret",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "Tenants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WhatsAppMessageCharge",
                table: "Tenants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaymentGatewayActive",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PaymentGatewayKey",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PaymentGatewayProvider",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PaymentGatewaySecret",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "WhatsAppMessageCharge",
                table: "Tenants");
        }
    }
}
