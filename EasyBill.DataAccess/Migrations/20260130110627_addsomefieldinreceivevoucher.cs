using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addsomefieldinreceivevoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "ReceiveVouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "ReceiveVouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Party",
                table: "PaymentVoucher",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiveVouchers_EmployeeId",
                table: "ReceiveVouchers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiveVouchers_SupplierId",
                table: "ReceiveVouchers",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiveVouchers_Employees_EmployeeId",
                table: "ReceiveVouchers",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiveVouchers_Suppliers_SupplierId",
                table: "ReceiveVouchers",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceiveVouchers_Employees_EmployeeId",
                table: "ReceiveVouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiveVouchers_Suppliers_SupplierId",
                table: "ReceiveVouchers");

            migrationBuilder.DropIndex(
                name: "IX_ReceiveVouchers_EmployeeId",
                table: "ReceiveVouchers");

            migrationBuilder.DropIndex(
                name: "IX_ReceiveVouchers_SupplierId",
                table: "ReceiveVouchers");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "ReceiveVouchers");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "ReceiveVouchers");

            migrationBuilder.DropColumn(
                name: "Party",
                table: "PaymentVoucher");
        }
    }
}
