using Eventum.Events.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventum.Events.Infrastructure.Migrations;

[DbContext(typeof(EventsDbContext))]
[Migration("20260704000200_EventsInitialCreate")]
public partial class EventsInitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "events",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                total_seats = table.Column<int>(type: "integer", nullable: false),
                available_seats = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_events", x => x.id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "events");
    }
}
