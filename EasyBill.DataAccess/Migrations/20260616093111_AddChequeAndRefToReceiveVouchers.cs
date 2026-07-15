using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddChequeAndRefToReceiveVouchers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ChequeDate",
                table: "ReceiveVouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChequeNo",
                table: "ReceiveVouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefNo",
                table: "ReceiveVouchers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChequeDate",
                table: "ReceiveVouchers");

            migrationBuilder.DropColumn(
                name: "ChequeNo",
                table: "ReceiveVouchers");

            migrationBuilder.DropColumn(
                name: "RefNo",
                table: "ReceiveVouchers");
        }
    }
}
