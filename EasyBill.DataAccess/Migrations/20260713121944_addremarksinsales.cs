using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addremarksinsales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Saless",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Saless");
        }
    }
}
