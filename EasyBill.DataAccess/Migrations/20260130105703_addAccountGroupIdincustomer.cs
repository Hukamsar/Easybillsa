using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addAccountGroupIdincustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "PaymentVoucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "PaymentVoucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountGroupId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVoucher_CustomerId",
                table: "PaymentVoucher",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVoucher_EmployeeId",
                table: "PaymentVoucher",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_AccountGroupId",
                table: "Employees",
                column: "AccountGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_AccountGroups_AccountGroupId",
                table: "Employees",
                column: "AccountGroupId",
                principalTable: "AccountGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentVoucher_Customers_CustomerId",
                table: "PaymentVoucher",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentVoucher_Employees_EmployeeId",
                table: "PaymentVoucher",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_AccountGroups_AccountGroupId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentVoucher_Customers_CustomerId",
                table: "PaymentVoucher");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentVoucher_Employees_EmployeeId",
                table: "PaymentVoucher");

            migrationBuilder.DropIndex(
                name: "IX_PaymentVoucher_CustomerId",
                table: "PaymentVoucher");

            migrationBuilder.DropIndex(
                name: "IX_PaymentVoucher_EmployeeId",
                table: "PaymentVoucher");

            migrationBuilder.DropIndex(
                name: "IX_Employees_AccountGroupId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "PaymentVoucher");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "PaymentVoucher");

            migrationBuilder.DropColumn(
                name: "AccountGroupId",
                table: "Employees");
        }
    }
}
