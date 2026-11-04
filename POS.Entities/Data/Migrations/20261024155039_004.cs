using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Entities.Data.Migrations
{
    /// <inheritdoc />
    public partial class _004 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "StockTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "SaleDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "PurchaseDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpiryAlertDays",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrackBatch",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TrackExpiry",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ProductBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BatchNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ManufactureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddedBy = table.Column<int>(type: "int", nullable: true),
                    AddedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ShopId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductBatches_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_BatchId",
                table: "StockTransactions",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_BatchId",
                table: "SaleDetails",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseDetails_BatchId",
                table: "PurchaseDetails",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_ExpiryDate",
                table: "ProductBatches",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_ProductId",
                table: "ProductBatches",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_ShopId_ProductId_BatchNo_ExpiryDate",
                table: "ProductBatches",
                columns: new[] { "ShopId", "ProductId", "BatchNo", "ExpiryDate" },
                unique: true,
                filter: "[BatchNo] IS NOT NULL AND [ExpiryDate] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseDetails_ProductBatches_BatchId",
                table: "PurchaseDetails",
                column: "BatchId",
                principalTable: "ProductBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleDetails_ProductBatches_BatchId",
                table: "SaleDetails",
                column: "BatchId",
                principalTable: "ProductBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_ProductBatches_BatchId",
                table: "StockTransactions",
                column: "BatchId",
                principalTable: "ProductBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseDetails_ProductBatches_BatchId",
                table: "PurchaseDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleDetails_ProductBatches_BatchId",
                table: "SaleDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_ProductBatches_BatchId",
                table: "StockTransactions");

            migrationBuilder.DropTable(
                name: "ProductBatches");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_BatchId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_BatchId",
                table: "SaleDetails");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseDetails_BatchId",
                table: "PurchaseDetails");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "SaleDetails");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "PurchaseDetails");

            migrationBuilder.DropColumn(
                name: "ExpiryAlertDays",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TrackBatch",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TrackExpiry",
                table: "Products");
        }
    }
}
