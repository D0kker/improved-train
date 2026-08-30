using System;
using System.Text.Json;
using LolAnalyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LolAnalyzer.Infrastructure.Migrations;

[DbContext(typeof(LolAnalyzerDbContext))]
[Migration("202608300001_InitialCreate")]
public partial class InitialCreate : Migration
{
    private static readonly string[] MatchParticipantIdentityColumns = ["match_id", "player_id"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "matches",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                riot_match_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                queue_id = table.Column<int>(type: "integer", nullable: true),
                game_creation = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                game_start_timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                game_end_timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                game_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                raw_data = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("pk_matches", item => item.id));

        migrationBuilder.CreateTable(
            name: "players",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                puuid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                game_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                tag_line = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                platform_region = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("pk_players", item => item.id));

        migrationBuilder.CreateTable(
            name: "match_participants",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                match_id = table.Column<Guid>(type: "uuid", nullable: false),
                player_id = table.Column<Guid>(type: "uuid", nullable: false),
                team_id = table.Column<int>(type: "integer", nullable: false),
                participant_id = table.Column<int>(type: "integer", nullable: false),
                champion_id = table.Column<int>(type: "integer", nullable: false),
                champion_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                team_position = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                individual_position = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                kills = table.Column<int>(type: "integer", nullable: false),
                deaths = table.Column<int>(type: "integer", nullable: false),
                assists = table.Column<int>(type: "integer", nullable: false),
                win = table.Column<bool>(type: "boolean", nullable: false),
                gold_earned = table.Column<int>(type: "integer", nullable: false),
                total_damage_dealt_to_champions = table.Column<int>(type: "integer", nullable: false),
                vision_score = table.Column<int>(type: "integer", nullable: false),
                cs = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_match_participants", item => item.id);
                table.ForeignKey(
                    name: "fk_match_participants_matches_match_id",
                    column: item => item.match_id,
                    principalTable: "matches",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_match_participants_players_player_id",
                    column: item => item.player_id,
                    principalTable: "players",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "ix_match_participants_match_id", table: "match_participants", column: "match_id");
        migrationBuilder.CreateIndex(
            name: "ix_match_participants_match_id_player_id",
            table: "match_participants",
            columns: MatchParticipantIdentityColumns,
            unique: true);
        migrationBuilder.CreateIndex(name: "ix_match_participants_player_id", table: "match_participants", column: "player_id");
        migrationBuilder.CreateIndex(name: "ix_matches_riot_match_id", table: "matches", column: "riot_match_id", unique: true);
        migrationBuilder.CreateIndex(name: "ix_players_puuid", table: "players", column: "puuid", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "match_participants");
        migrationBuilder.DropTable(name: "matches");
        migrationBuilder.DropTable(name: "players");
    }
}
