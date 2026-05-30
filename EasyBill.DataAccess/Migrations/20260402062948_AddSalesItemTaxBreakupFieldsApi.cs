using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesItemTaxBreakupFieldsApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CGst",
                table: "SalesItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HsnCode",
                table: "SalesItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HsnId",
                table: "SalesItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IGst",
                table: "SalesItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SGst",
                table: "SalesItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesItems_HsnId",
                table: "SalesItems",
                column: "HsnId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesItems_Hsns_HsnId",
                table: "SalesItems",
                column: "HsnId",
                principalTable: "Hsns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesItems_Hsns_HsnId",
                table: "SalesItems");

            migrationBuilder.DropIndex(
                name: "IX_SalesItems_HsnId",
                table: "SalesItems");

            migrationBuilder.DropColumn(
                name: "CGst",
                table: "SalesItems");

            migrationBuilder.DropColumn(
                name: "HsnCode",
                table: "SalesItems");

            migrationBuilder.DropColumn(
                name: "HsnId",
                table: "SalesItems");

            migrationBuilder.DropColumn(
                name: "IGst",
                table: "SalesItems");

            migrationBuilder.DropColumn(
                name: "SGst",
                table: "SalesItems");
        }
    }
}
