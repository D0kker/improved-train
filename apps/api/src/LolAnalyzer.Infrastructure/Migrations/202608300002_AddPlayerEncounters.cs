using LolAnalyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LolAnalyzer.Infrastructure.Migrations;

[DbContext(typeof(LolAnalyzerDbContext))]
[Migration("202608300002_AddPlayerEncounters")]
public partial class AddPlayerEncounters : Migration
{
    private static readonly string[] PlayerEncounterRankingColumns = ["owner_player_id", "total_matches"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "player_encounters",
            columns: table => new
            {
                owner_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                other_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                total_matches = table.Column<int>(type: "integer", nullable: false),
                same_team_matches = table.Column<int>(type: "integer", nullable: false),
                enemy_team_matches = table.Column<int>(type: "integer", nullable: false),
                wins_together = table.Column<int>(type: "integer", nullable: false),
                losses_together = table.Column<int>(type: "integer", nullable: false),
                wins_against = table.Column<int>(type: "integer", nullable: false),
                losses_against = table.Column<int>(type: "integer", nullable: false),
                first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_player_encounters", item => new { item.owner_player_id, item.other_player_id });
                table.CheckConstraint(
                    "ck_player_encounters_distinct_players",
                    "owner_player_id <> other_player_id");
                table.ForeignKey(
                    name: "fk_player_encounters_players_other_player_id",
                    column: item => item.other_player_id,
                    principalTable: "players",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_player_encounters_players_owner_player_id",
                    column: item => item.owner_player_id,
                    principalTable: "players",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_player_encounters_other_player_id",
            table: "player_encounters",
            column: "other_player_id");
        migrationBuilder.CreateIndex(
            name: "ix_player_encounters_owner_player_id_total_matches",
            table: "player_encounters",
            columns: PlayerEncounterRankingColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "player_encounters");
    }
}
