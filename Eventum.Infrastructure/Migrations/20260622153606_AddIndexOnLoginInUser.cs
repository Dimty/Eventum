using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventum.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexOnLoginInUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_login",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_users_login",
                table: "users",
                column: "login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_login",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_users_login",
                table: "users",
                column: "login");
        }
    }
}
