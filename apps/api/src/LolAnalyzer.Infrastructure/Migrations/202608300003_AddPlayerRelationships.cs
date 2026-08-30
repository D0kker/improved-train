using LolAnalyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LolAnalyzer.Infrastructure.Migrations;

[DbContext(typeof(LolAnalyzerDbContext))]
[Migration("202608300003_AddPlayerRelationships")]
public partial class AddPlayerRelationships : Migration
{
    private static readonly string[] PlayerARankingColumns = ["player_a_id", "relationship_score"];
    private static readonly string[] PlayerBRankingColumns = ["player_b_id", "relationship_score"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "player_relationships",
            columns: table => new
            {
                player_a_id = table.Column<Guid>(type: "uuid", nullable: false),
                player_b_id = table.Column<Guid>(type: "uuid", nullable: false),
                matches_together = table.Column<int>(type: "integer", nullable: false),
                same_team_matches = table.Column<int>(type: "integer", nullable: false),
                opposite_team_matches = table.Column<int>(type: "integer", nullable: false),
                recent_matches_together = table.Column<int>(type: "integer", nullable: false),
                consecutive_matches = table.Column<int>(type: "integer", nullable: false),
                first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                relationship_score = table.Column<int>(type: "integer", nullable: false),
                relationship_confidence = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_player_relationships", item => new { item.player_a_id, item.player_b_id });
                table.CheckConstraint("ck_player_relationships_canonical_pair", "player_a_id < player_b_id");
                table.CheckConstraint(
                    "ck_player_relationships_match_totals",
                    "matches_together = same_team_matches + opposite_team_matches");
                table.CheckConstraint(
                    "ck_player_relationships_nonnegative_counts",
                    "matches_together >= 0 AND same_team_matches >= 0 AND opposite_team_matches >= 0 AND recent_matches_together >= 0 AND consecutive_matches >= 0");
                table.CheckConstraint(
                    "ck_player_relationships_score_range",
                    "relationship_score >= 0 AND relationship_score <= 100");
                table.ForeignKey(
                    name: "fk_player_relationships_players_player_a_id",
                    column: item => item.player_a_id,
                    principalTable: "players",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_player_relationships_players_player_b_id",
                    column: item => item.player_b_id,
                    principalTable: "players",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_player_relationships_player_a_id_relationship_score",
            table: "player_relationships",
            columns: PlayerARankingColumns);
        migrationBuilder.CreateIndex(
            name: "ix_player_relationships_player_b_id_relationship_score",
            table: "player_relationships",
            columns: PlayerBRankingColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "player_relationships");
    }
}
