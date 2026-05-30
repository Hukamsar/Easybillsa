using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addinvoicethemesetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceThemeSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaperSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TextColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FontFamily = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaseFontSize = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Margins = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Orientation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoSize = table.Column<int>(type: "int", nullable: true),
                    LogoPosition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WatermarkPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WatermarkOpacity = table.Column<int>(type: "int", nullable: true),
                    WatermarkSize = table.Column<int>(type: "int", nullable: true),
                    SignaturePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatoryLabel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowHSN = table.Column<bool>(type: "bit", nullable: true),
                    ShowGSTPercent = table.Column<bool>(type: "bit", nullable: true),
                    ShowGSTAmount = table.Column<bool>(type: "bit", nullable: true),
                    ShowTaxSummaryTable = table.Column<bool>(type: "bit", nullable: true),
                    ShowBusinessGSTIN = table.Column<bool>(type: "bit", nullable: true),
                    ShowItemDiscount = table.Column<bool>(type: "bit", nullable: true),
                    IsZebraStriped = table.Column<bool>(type: "bit", nullable: true),
                    ShowAmountInWords = table.Column<bool>(type: "bit", nullable: true),
                    BankDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermsAndConditions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowPaymentQR = table.Column<bool>(type: "bit", nullable: true),
                    UPIDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThermalPaperSize = table.Column<int>(type: "int", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceThemeSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceThemeSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceThemeSettings_TenantId",
                table: "InvoiceThemeSettings",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceThemeSettings");
        }
    }
}
