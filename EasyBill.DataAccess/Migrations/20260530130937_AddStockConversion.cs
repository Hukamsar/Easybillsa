using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddStockConversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<bool>(
                name: "AllowNegativeBalance",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AllowedModulesJson",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingModel",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExtraUsers",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InventoryMode",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAuditLocked",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OutletCount",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RollbackDurationMonths",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RollbackExpiryDate",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionPlanId",
                table: "Tenants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportTier",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantCode",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);


            migrationBuilder.AddColumn<decimal>(
                name: "ConversionFactor",
                table: "ItemMasters",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "ItemMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentItemId",
                table: "ItemMasters",
                type: "int",
                nullable: true);


            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ParentFeatureId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Features_Features_ParentFeatureId",
                        column: x => x.ParentFeatureId,
                        principalTable: "Features",
                        principalColumn: "Id");
                });


            migrationBuilder.CreateTable(
                name: "StockConversions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BulkItemId = table.Column<int>(type: "int", nullable: false),
                    BulkQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RetailItemId = table.Column<int>(type: "int", nullable: false),
                    RetailQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WastageQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockConversions_ItemMasters_BulkItemId",
                        column: x => x.BulkItemId,
                        principalTable: "ItemMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockConversions_ItemMasters_RetailItemId",
                        column: x => x.RetailItemId,
                        principalTable: "ItemMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockConversions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });


            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YearlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyCustomerLimit = table.Column<int>(type: "int", nullable: false),
                    MaxDesktopLogins = table.Column<int>(type: "int", nullable: false),
                    MaxMobileLogins = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanFeatures",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    FeatureId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanFeatures", x => new { x.PlanId, x.FeatureId });
                    table.ForeignKey(
                        name: "FK_PlanFeatures_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanFeatures_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_SubscriptionPlanId",
                table: "Tenants",
                column: "SubscriptionPlanId");


            migrationBuilder.CreateIndex(
                name: "IX_ItemMasters_ParentItemId",
                table: "ItemMasters",
                column: "ParentItemId");


            migrationBuilder.CreateIndex(
                name: "IX_Features_ParentFeatureId",
                table: "Features",
                column: "ParentFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanFeatures_FeatureId",
                table: "PlanFeatures",
                column: "FeatureId");


            migrationBuilder.CreateIndex(
                name: "IX_StockConversions_BulkItemId",
                table: "StockConversions",
                column: "BulkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockConversions_RetailItemId",
                table: "StockConversions",
                column: "RetailItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockConversions_TenantId",
                table: "StockConversions",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemMasters_ItemMasters_ParentItemId",
                table: "ItemMasters",
                column: "ParentItemId",
                principalTable: "ItemMasters",
                principalColumn: "Id");


            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_SubscriptionPlans_SubscriptionPlanId",
                table: "Tenants",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemMasters_ItemMasters_ParentItemId",
                table: "ItemMasters");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_SubscriptionPlans_SubscriptionPlanId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "PlanFeatures");

            migrationBuilder.DropTable(
                name: "StockConversions");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_SubscriptionPlanId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_ItemMasters_ParentItemId",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "AllowNegativeBalance",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AllowedModulesJson",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "BillingModel",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ExtraUsers",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "InventoryMode",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsAuditLocked",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "OutletCount",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RollbackDurationMonths",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RollbackExpiryDate",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SupportTier",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TenantCode",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ConversionFactor",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "ItemMasters");

            migrationBuilder.DropColumn(
                name: "ParentItemId",
                table: "ItemMasters");
        }
    }
}
