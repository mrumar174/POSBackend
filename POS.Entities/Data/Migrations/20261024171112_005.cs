using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Entities.Data.Migrations
{
    /// <inheritdoc />
    public partial class _005 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductBarcodes_TenantId_Barcode",
                table: "ProductBarcodes");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcodes_TenantId_Barcode",
                table: "ProductBarcodes",
                columns: new[] { "TenantId", "Barcode" },
                unique: true,
                filter: "[DeletedOn] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductBarcodes_TenantId_Barcode",
                table: "ProductBarcodes");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBarcodes_TenantId_Barcode",
                table: "ProductBarcodes",
                columns: new[] { "TenantId", "Barcode" },
                unique: true);
        }
    }
}
