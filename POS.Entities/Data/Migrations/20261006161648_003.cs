using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Entities.Data.Migrations
{
    /// <inheritdoc />
    public partial class _003 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualCash",
                table: "DailyCashClosings");

            migrationBuilder.DropColumn(
                name: "Difference",
                table: "DailyCashClosings");

            migrationBuilder.DropColumn(
                name: "ExpectedCash",
                table: "DailyCashClosings");

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "DailyCashClosings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ClosingBalance",
                table: "DailyCashClosings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalExpenses",
                table: "DailyCashClosings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSales",
                table: "DailyCashClosings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_ShopId_TransactionDate",
                table: "CashTransactions",
                columns: new[] { "ShopId", "TransactionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashTransactions_ShopId_TransactionDate",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "ClosingBalance",
                table: "DailyCashClosings");

            migrationBuilder.DropColumn(
                name: "TotalExpenses",
                table: "DailyCashClosings");

            migrationBuilder.DropColumn(
                name: "TotalSales",
                table: "DailyCashClosings");

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "DailyCashClosings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualCash",
                table: "DailyCashClosings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Difference",
                table: "DailyCashClosings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedCash",
                table: "DailyCashClosings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
