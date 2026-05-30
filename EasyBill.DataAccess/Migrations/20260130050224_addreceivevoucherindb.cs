using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addreceivevoucherindb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiveVouchers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VouncherNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Party = table.Column<int>(type: "int", nullable: false),
                    VenderId = table.Column<int>(type: "int", nullable: true),
                    PaymentCategoryId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GST = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GSTAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attachments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PaymentModeId = table.Column<int>(type: "int", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiveVouchers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiveVouchers_ModeOfPayments_PaymentModeId",
                        column: x => x.PaymentModeId,
                        principalTable: "ModeOfPayments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReceiveVouchers_PaymentVoucherCategories_PaymentCategoryId",
                        column: x => x.PaymentCategoryId,
                        principalTable: "PaymentVoucherCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReceiveVouchers_Suppliers_VenderId",
                        column: x => x.VenderId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReceiveVouchers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiveVouchers_PaymentCategoryId",
                table: "ReceiveVouchers",
                column: "PaymentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiveVouchers_PaymentModeId",
                table: "ReceiveVouchers",
                column: "PaymentModeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiveVouchers_TenantId",
                table: "ReceiveVouchers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiveVouchers_VenderId",
                table: "ReceiveVouchers",
                column: "VenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiveVouchers");
        }
    }
}
