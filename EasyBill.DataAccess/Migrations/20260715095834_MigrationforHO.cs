using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyBill.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MigrationforHO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanManageOwnPO",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageOwnStock",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHeadOffice",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ParentTenantId",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPendingTransfer",
                table: "StockReceives",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SourceStockIssueId",
                table: "StockReceives",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferFromTenantId",
                table: "StockReceives",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReceived",
                table: "StockIssues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TransferStatus",
                table: "StockIssues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferToTenantId",
                table: "StockIssues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Zone",
                table: "States",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryType",
                table: "PurchaseOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderType",
                table: "PurchaseOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentPurchaseOrderId",
                table: "PurchaseOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetTenantId",
                table: "PurchaseOrders",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowStatus",
                table: "PurchaseOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuyQty",
                table: "Offers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FreeQty",
                table: "Offers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHoAdmin",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Zone",
                table: "Cities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedBranches",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BranchItemMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ItemMasterId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchItemMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchItemMappings_ItemMasters_ItemMasterId",
                        column: x => x.ItemMasterId,
                        principalTable: "ItemMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BranchItemMappings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTransferLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FromTenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToTenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToDepartmentId = table.Column<int>(type: "int", nullable: true),
                    ToDesignationId = table.Column<int>(type: "int", nullable: true),
                    TransferReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsReadByDestination = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTransferLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTransferLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OfferStoreMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfferId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferStoreMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferStoreMappings_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TargetTenantId",
                table: "PurchaseOrders",
                column: "TargetTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchItemMappings_ItemMasterId",
                table: "BranchItemMappings",
                column: "ItemMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchItemMappings_TenantId",
                table: "BranchItemMappings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTransferLogs_EmployeeId",
                table: "EmployeeTransferLogs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferStoreMappings_OfferId",
                table: "OfferStoreMappings",
                column: "OfferId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Tenants_TargetTenantId",
                table: "PurchaseOrders",
                column: "TargetTenantId",
                principalTable: "Tenants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Tenants_TargetTenantId",
                table: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "BranchItemMappings");

            migrationBuilder.DropTable(
                name: "EmployeeTransferLogs");

            migrationBuilder.DropTable(
                name: "OfferStoreMappings");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TargetTenantId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CanManageOwnPO",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CanManageOwnStock",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsHeadOffice",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ParentTenantId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsPendingTransfer",
                table: "StockReceives");

            migrationBuilder.DropColumn(
                name: "SourceStockIssueId",
                table: "StockReceives");

            migrationBuilder.DropColumn(
                name: "TransferFromTenantId",
                table: "StockReceives");

            migrationBuilder.DropColumn(
                name: "IsReceived",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "TransferStatus",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "TransferToTenantId",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "Zone",
                table: "States");

            migrationBuilder.DropColumn(
                name: "DeliveryType",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ParentPurchaseOrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TargetTenantId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "BuyQty",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "FreeQty",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "IsHoAdmin",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Zone",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "AllowedBranches",
                table: "AspNetUsers");
        }
    }
}
