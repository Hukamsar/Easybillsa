using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addcustomerIdinreeivevoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceiveVouchers_Suppliers_VenderId",
                table: "ReceiveVouchers");

            migrationBuilder.RenameColumn(
                name: "VenderId",
                table: "ReceiveVouchers",
                newName: "CustomerId");

            migrationBuilder.RenameIndex(
                name: "IX_ReceiveVouchers_VenderId",
                table: "ReceiveVouchers",
                newName: "IX_ReceiveVouchers_CustomerId");

            migrationBuilder.AlterColumn<int>(
                name: "Party",
                table: "ReceiveVouchers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiveVouchers_Customers_CustomerId",
                table: "ReceiveVouchers",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceiveVouchers_Customers_CustomerId",
                table: "ReceiveVouchers");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "ReceiveVouchers",
                newName: "VenderId");

            migrationBuilder.RenameIndex(
                name: "IX_ReceiveVouchers_CustomerId",
                table: "ReceiveVouchers",
                newName: "IX_ReceiveVouchers_VenderId");

            migrationBuilder.AlterColumn<int>(
                name: "Party",
                table: "ReceiveVouchers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiveVouchers_Suppliers_VenderId",
                table: "ReceiveVouchers",
                column: "VenderId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }
    }
}
