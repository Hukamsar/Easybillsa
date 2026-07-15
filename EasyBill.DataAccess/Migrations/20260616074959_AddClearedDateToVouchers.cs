using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddClearedDateToVouchers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DrCr",
                table: "Banks");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClearedDate",
                table: "ReceiveVouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClearedDate",
                table: "PaymentVoucher",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClearedDate",
                table: "ReceiveVouchers");

            migrationBuilder.DropColumn(
                name: "ClearedDate",
                table: "PaymentVoucher");

            migrationBuilder.AddColumn<string>(
                name: "DrCr",
                table: "Banks",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
