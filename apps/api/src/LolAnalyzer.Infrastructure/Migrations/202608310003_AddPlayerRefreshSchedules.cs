using LolAnalyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LolAnalyzer.Infrastructure.Migrations;

[DbContext(typeof(LolAnalyzerDbContext))]
[Migration("202608310003_AddPlayerRefreshSchedules")]
public partial class AddPlayerRefreshSchedules : Migration
{
    private static readonly string[] DueColumns = ["enabled", "next_run_at"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "player_refresh_schedules",
            columns: table => new
            {
                puuid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                requested_count = table.Column<int>(type: "integer", nullable: false),
                interval_minutes = table.Column<int>(type: "integer", nullable: false),
                enabled = table.Column<bool>(type: "boolean", nullable: false),
                next_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_enqueued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_player_refresh_schedules", item => item.puuid);
                table.CheckConstraint(
                    "ck_player_refresh_schedules_interval",
                    "interval_minutes >= 15 AND interval_minutes <= 10080");
                table.CheckConstraint(
                    "ck_player_refresh_schedules_requested_count",
                    "requested_count >= 1 AND requested_count <= 200");
            });

        migrationBuilder.CreateIndex(
            name: "ix_player_refresh_schedules_enabled_next_run_at",
            table: "player_refresh_schedules",
            columns: DueColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "player_refresh_schedules");
    }
}
