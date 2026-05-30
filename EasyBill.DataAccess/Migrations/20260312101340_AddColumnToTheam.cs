using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnToTheam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.RenameColumn(
            //    name: "ConfigsJson",
            //    table: "InvoiceThemeSettings",
            //    newName: "PaperSize");

            //migrationBuilder.AlterColumn<DateTime>(
            //    name: "UpdatedAt",
            //    table: "InvoiceThemeSettings",
            //    type: "datetime2",
            //    nullable: true,
            //    oldClrType: typeof(DateTime),
            //    oldType: "datetime2");

            //migrationBuilder.AddColumn<string>(
            //    name: "BankDetails",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "BaseFontSize",
            //    table: "InvoiceThemeSettings",
            //    type: "int",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "FontFamily",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsDefault",
            //    table: "InvoiceThemeSettings",
            //    type: "bit",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsZebraStriped",
            //    table: "InvoiceThemeSettings",
            //    type: "bit",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "LogoPath",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "LogoPosition",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "LogoSize",
            //    table: "InvoiceThemeSettings",
            //    type: "int",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "Margins",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "Orientation",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "PrimaryColor",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<bool>(
            //    name: "ShowAmountInWords",
            //    table: "InvoiceThemeSettings",
            //    type: "bit",
            //    nullable: true);

            //migrationBuilder.AddColumn<bool>(
            //    name: "ShowBusinessGSTIN",
            //    table: "InvoiceThemeSettings",
            //    type: "bit",
            //    nullable: true);

            //migrationBuilder.AddColumn<bool>(
            //    name: "ShowGSTAmount",
            //    table: "InvoiceThemeSettings",
            //    type: "bit",
            //    nullable: true);

            //migrationBuilder.AddColumn<bool>(
            //    name: "ShowGSTPercent",
            //    table: "InvoiceThemeSettings",
            //    type: "bit",
            //    nullable: true);

            //migrationBuilder.AddColumn<bool>(
            //    name: "ShowHSN",
            //    table: "InvoiceThemeSettings",
            //    type: "bit",
            //    nullable: true);

            //migrationBuilder.AddColumn<bool>(
            //    name: "ShowItemDiscount",
            //    table: "InvoiceThemeSettings",
            //    type: "bit",
            //    nullable: true);

            //migrationBuilder.AddColumn<bool>(
            //    name: "ShowPaymentQR",
            //    table: "InvoiceThemeSettings",
            //    type: "bit",
            //    nullable: true);

            //migrationBuilder.AddColumn<bool>(
            //    name: "ShowTaxSummaryTable",
            //    table: "InvoiceThemeSettings",
            //    type: "bit",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "SignatoryLabel",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "SignaturePath",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "TemplateName",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "TenantId",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(450)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "TermsAndConditions",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "TextColor",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThermalPaperSize",
                table: "InvoiceThemeSettings",
                type: "int",
                nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "Title",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "UPIDetails",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "WatermarkOpacity",
            //    table: "InvoiceThemeSettings",
            //    type: "int",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "WatermarkPath",
            //    table: "InvoiceThemeSettings",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "WatermarkSize",
            //    table: "InvoiceThemeSettings",
            //    type: "int",
            //    nullable: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_InvoiceThemeSettings_TenantId",
            //    table: "InvoiceThemeSettings",
            //    column: "TenantId");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_InvoiceThemeSettings_Tenants_TenantId",
            //    table: "InvoiceThemeSettings",
            //    column: "TenantId",
            //    principalTable: "Tenants",
            //    principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceThemeSettings_Tenants_TenantId",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceThemeSettings_TenantId",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "BankDetails",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "BaseFontSize",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "FontFamily",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "IsZebraStriped",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "LogoPath",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "LogoPosition",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "LogoSize",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "Margins",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "Orientation",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "ShowAmountInWords",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "ShowBusinessGSTIN",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "ShowGSTAmount",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "ShowGSTPercent",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "ShowHSN",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "ShowItemDiscount",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "ShowPaymentQR",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "ShowTaxSummaryTable",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "SignatoryLabel",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "SignaturePath",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "TemplateName",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "TermsAndConditions",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "TextColor",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "ThermalPaperSize",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "UPIDetails",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "WatermarkOpacity",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "WatermarkPath",
                table: "InvoiceThemeSettings");

            migrationBuilder.DropColumn(
                name: "WatermarkSize",
                table: "InvoiceThemeSettings");

            migrationBuilder.RenameColumn(
                name: "PaperSize",
                table: "InvoiceThemeSettings",
                newName: "ConfigsJson");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "InvoiceThemeSettings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
