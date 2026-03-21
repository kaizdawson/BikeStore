using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeStore.Repository.Migrations
{
    /// <inheritdoc />
    public partial class nullableOrderidInTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrderId",
                table: "Transactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderId",
                table: "Transactions",
                column: "OrderId",
                unique: true,
                filter: "[OrderId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrderId",
                table: "Transactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderId",
                table: "Transactions",
                column: "OrderId",
                unique: true);
        }
    }
}
