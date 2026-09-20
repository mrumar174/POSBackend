using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Entities.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountTypeToPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "Purchases",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "Purchases",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "PurchaseDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountValue",
                table: "PurchaseDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "PurchaseDetails");

            migrationBuilder.DropColumn(
                name: "DiscountValue",
                table: "PurchaseDetails");
        }
    }
}
