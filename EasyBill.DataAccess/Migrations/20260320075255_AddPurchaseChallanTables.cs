using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseChallanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseChallans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    BillNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PartyBillNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PartyBillDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TotalGstAmt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Totaldiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPayable = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    discountPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    discountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentAmt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoundOffAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    billingType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurchaseType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalCGstAmt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalSGstAmt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReturnAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConvertedPurchaseId = table.Column<int>(type: "int", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseChallans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseChallans_Purchases_ConvertedPurchaseId",
                        column: x => x.ConvertedPurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseChallans_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseChallans_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseChallanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseChallanId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Batch = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    FreeQty = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HsnId = table.Column<int>(type: "int", nullable: true),
                    Gst = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GstAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BatchWiseCose = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Mrp = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    salserateA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    salserateB = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CGst = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SGst = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CGstAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SGstAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ConvertedQty = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseChallanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseChallanItems_Hsns_HsnId",
                        column: x => x.HsnId,
                        principalTable: "Hsns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PurchaseChallanItems_ItemMasters_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseChallanItems_PurchaseChallans_PurchaseChallanId",
                        column: x => x.PurchaseChallanId,
                        principalTable: "PurchaseChallans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseChallanItems_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.AddColumn<int>(
                name: "PurchaseChallanId",
                table: "SalsePaymentDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseChallanItems_HsnId",
                table: "PurchaseChallanItems",
                column: "HsnId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseChallanItems_ItemId",
                table: "PurchaseChallanItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseChallanItems_PurchaseChallanId",
                table: "PurchaseChallanItems",
                column: "PurchaseChallanId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseChallanItems_TenantId",
                table: "PurchaseChallanItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseChallans_ConvertedPurchaseId",
                table: "PurchaseChallans",
                column: "ConvertedPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseChallans_SupplierId",
                table: "PurchaseChallans",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseChallans_TenantId",
                table: "PurchaseChallans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalsePaymentDetails_PurchaseChallanId",
                table: "SalsePaymentDetails",
                column: "PurchaseChallanId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalsePaymentDetails_PurchaseChallans_PurchaseChallanId",
                table: "SalsePaymentDetails",
                column: "PurchaseChallanId",
                principalTable: "PurchaseChallans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalsePaymentDetails_PurchaseChallans_PurchaseChallanId",
                table: "SalsePaymentDetails");

            migrationBuilder.DropTable(
                name: "PurchaseChallanItems");

            migrationBuilder.DropTable(
                name: "PurchaseChallans");

            migrationBuilder.DropIndex(
                name: "IX_SalsePaymentDetails_PurchaseChallanId",
                table: "SalsePaymentDetails");

            migrationBuilder.DropColumn(
                name: "PurchaseChallanId",
                table: "SalsePaymentDetails");
        }
    }
}
