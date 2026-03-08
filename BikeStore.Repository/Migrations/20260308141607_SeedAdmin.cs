using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeStore.Repository.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvtUrl", "CreatedAt", "Email", "FirebaseUID", "FullName", "IsDeleted", "Password", "PhoneNumber", "Role", "Status", "UpdatedAt", "WalletBalance" },
                values: new object[] { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), null, new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@bikestore.com", null, "System Admin", false, "100000.SmNwViw55hJgT+ezXV5aouQ==.2o3dJJDfDKlm/9WO9o7L9CkC1nY7tY2Qm3kF+JjL5o=", "0000000000", 1, 1, new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        }
    }
}
