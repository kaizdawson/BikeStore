using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeStore.Repository.Migrations
{
    /// <inheritdoc />
    public partial class fixDbForReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Transactions_TransactionId",
                table: "Reviews");

            migrationBuilder.RenameColumn(
                name: "TransactionId",
                table: "Reviews",
                newName: "OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_Reviews_TransactionId",
                table: "Reviews",
                newName: "IX_Reviews_OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Orders_OrderId",
                table: "Reviews",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Orders_OrderId",
                table: "Reviews");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "Reviews",
                newName: "TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_Reviews_OrderId",
                table: "Reviews",
                newName: "IX_Reviews_TransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Transactions_TransactionId",
                table: "Reviews",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
