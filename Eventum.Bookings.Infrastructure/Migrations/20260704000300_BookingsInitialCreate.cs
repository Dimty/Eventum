using Eventum.Bookings.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventum.Bookings.Infrastructure.Migrations;

[DbContext(typeof(BookingsDbContext))]
[Migration("20260704000300_BookingsInitialCreate")]
public partial class BookingsInitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "bookings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                seats = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_bookings", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_bookings_event_id",
            table: "bookings",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "ix_bookings_user_id",
            table: "bookings",
            column: "user_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "bookings");
    }
}
