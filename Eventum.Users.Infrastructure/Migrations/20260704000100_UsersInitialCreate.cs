using Eventum.Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventum.Users.Infrastructure.Migrations;

[DbContext(typeof(UsersDbContext))]
[Migration("20260704000100_UsersInitialCreate")]
public partial class UsersInitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                login = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                password_hash = table.Column<string>(type: "text", nullable: false),
                role = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_users_login",
            table: "users",
            column: "login",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "users");
    }
}
