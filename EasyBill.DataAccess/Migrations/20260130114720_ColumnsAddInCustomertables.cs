using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ColumnsAddInCustomertables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountGroupId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GSTNo",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GSTType",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StateCode",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AccountGroupId",
                table: "Customers",
                column: "AccountGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_AccountGroups_AccountGroupId",
                table: "Customers",
                column: "AccountGroupId",
                principalTable: "AccountGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_AccountGroups_AccountGroupId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_AccountGroupId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "AccountGroupId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "GSTNo",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "GSTType",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "StateCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Customers");
        }
    }
}
