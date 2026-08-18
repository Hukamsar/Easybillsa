using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSubCategoryToOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubCategoryId",
                table: "Offers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_SubCategoryId",
                table: "Offers",
                column: "SubCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_SubCategories_SubCategoryId",
                table: "Offers",
                column: "SubCategoryId",
                principalTable: "SubCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offers_SubCategories_SubCategoryId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_SubCategoryId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "SubCategoryId",
                table: "Offers");
        }
    }
}
