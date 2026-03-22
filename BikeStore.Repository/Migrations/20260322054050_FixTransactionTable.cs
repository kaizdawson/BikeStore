using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeStore.Repository.Migrations
{
    /// <inheritdoc />
    public partial class FixTransactionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Transactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Transactions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountName",
                table: "Transactions",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountName",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Transactions");
        }
    }
}
