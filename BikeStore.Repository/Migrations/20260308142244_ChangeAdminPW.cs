using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeStore.Repository.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAdminPW : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "Password",
                value: "100000.f30kAaLPHnWOkwS/xhR2cA==.mSeA7xDyGtUr393+a/H4ooLaNzHGXVCHHWgAsM3YrsY=");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "Password",
                value: "100000.SmNwViw55hJgT+ezXV5aouQ==.2o3dJJDfDKlm/9WO9o7L9CkC1nY7tY2Qm3kF+JjL5o=");
        }
    }
}
