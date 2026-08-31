using System;
using System.Text.Json;
using LolAnalyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LolAnalyzer.Infrastructure.Migrations;

[DbContext(typeof(LolAnalyzerDbContext))]
public partial class LolAnalyzerDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");
        modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("LolAnalyzer.Domain.Entities.AnalysisJob", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            entity.Property<DateTimeOffset?>("CompletedAt").HasColumnType("timestamp with time zone").HasColumnName("completed_at");
            entity.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            entity.Property<string>("ErrorCode").HasMaxLength(64).HasColumnType("character varying(64)").HasColumnName("error_code");
            entity.Property<int>("MatchesProcessed").HasColumnType("integer").HasColumnName("matches_processed");
            entity.Property<string>("Puuid").IsRequired().HasMaxLength(128).HasColumnType("character varying(128)").HasColumnName("puuid");
            entity.Property<int>("RequestedCount").HasColumnType("integer").HasColumnName("requested_count");
            entity.Property<DateTimeOffset?>("StartedAt").HasColumnType("timestamp with time zone").HasColumnName("started_at");
            entity.Property<LolAnalyzer.Domain.Entities.AnalysisJobStatus>("Status").HasMaxLength(16).HasColumnType("character varying(16)").HasColumnName("status");
            entity.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone").HasColumnName("updated_at");
            entity.HasKey("Id");
            entity.HasIndex("Puuid", "CreatedAt");
            entity.HasIndex("Status", "CreatedAt");
            entity.ToTable("analysis_jobs", table =>
            {
                table.HasCheckConstraint("ck_analysis_jobs_progress", "matches_processed >= 0 AND matches_processed <= requested_count");
                table.HasCheckConstraint("ck_analysis_jobs_requested_count", "requested_count >= 1 AND requested_count <= 200");
            });
        });

        modelBuilder.Entity("LolAnalyzer.Domain.Entities.Match", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            entity.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            entity.Property<int?>("GameDurationSeconds").HasColumnType("integer").HasColumnName("game_duration_seconds");
            entity.Property<DateTimeOffset?>("GameCreation").HasColumnType("timestamp with time zone").HasColumnName("game_creation");
            entity.Property<DateTimeOffset?>("GameEndTimestamp").HasColumnType("timestamp with time zone").HasColumnName("game_end_timestamp");
            entity.Property<DateTimeOffset?>("GameStartTimestamp").HasColumnType("timestamp with time zone").HasColumnName("game_start_timestamp");
            entity.Property<string>("GameVersion").HasMaxLength(32).HasColumnType("character varying(32)").HasColumnName("game_version");
            entity.Property<int?>("QueueId").HasColumnType("integer").HasColumnName("queue_id");
            entity.Property<JsonDocument>("RawData").HasColumnType("jsonb").HasColumnName("raw_data");
            entity.Property<string>("RiotMatchId").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)").HasColumnName("riot_match_id");
            entity.HasKey("Id");
            entity.HasIndex("RiotMatchId").IsUnique();
            entity.ToTable("matches", (string)null);
        });

        modelBuilder.Entity("LolAnalyzer.Domain.Entities.Player", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            entity.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            entity.Property<string>("GameName").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)").HasColumnName("game_name");
            entity.Property<DateTimeOffset>("LastSeenAt").HasColumnType("timestamp with time zone").HasColumnName("last_seen_at");
            entity.Property<string>("PlatformRegion").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)").HasColumnName("platform_region");
            entity.Property<string>("Puuid").IsRequired().HasMaxLength(128).HasColumnType("character varying(128)").HasColumnName("puuid");
            entity.Property<string>("TagLine").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)").HasColumnName("tag_line");
            entity.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone").HasColumnName("updated_at");
            entity.HasKey("Id");
            entity.HasIndex("Puuid").IsUnique();
            entity.ToTable("players", (string)null);
        });

        modelBuilder.Entity("LolAnalyzer.Domain.Entities.MatchParticipant", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            entity.Property<int>("Assists").HasColumnType("integer").HasColumnName("assists");
            entity.Property<int>("ChampionId").HasColumnType("integer").HasColumnName("champion_id");
            entity.Property<string>("ChampionName").IsRequired().HasMaxLength(64).HasColumnType("character varying(64)").HasColumnName("champion_name");
            entity.Property<int>("Cs").HasColumnType("integer").HasColumnName("cs");
            entity.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            entity.Property<int>("Deaths").HasColumnType("integer").HasColumnName("deaths");
            entity.Property<int>("GoldEarned").HasColumnType("integer").HasColumnName("gold_earned");
            entity.Property<string>("IndividualPosition").HasMaxLength(32).HasColumnType("character varying(32)").HasColumnName("individual_position");
            entity.Property<int>("Kills").HasColumnType("integer").HasColumnName("kills");
            entity.Property<Guid>("MatchId").HasColumnType("uuid").HasColumnName("match_id");
            entity.Property<int>("ParticipantId").HasColumnType("integer").HasColumnName("participant_id");
            entity.Property<Guid>("PlayerId").HasColumnType("uuid").HasColumnName("player_id");
            entity.Property<int>("TeamId").HasColumnType("integer").HasColumnName("team_id");
            entity.Property<string>("TeamPosition").HasMaxLength(32).HasColumnType("character varying(32)").HasColumnName("team_position");
            entity.Property<int>("TotalDamageDealtToChampions").HasColumnType("integer").HasColumnName("total_damage_dealt_to_champions");
            entity.Property<int>("VisionScore").HasColumnType("integer").HasColumnName("vision_score");
            entity.Property<bool>("Win").HasColumnType("boolean").HasColumnName("win");
            entity.HasKey("Id");
            entity.HasIndex("MatchId");
            entity.HasIndex("MatchId", "PlayerId").IsUnique();
            entity.HasIndex("PlayerId");
            entity.ToTable("match_participants", (string)null);
        });

        modelBuilder.Entity("LolAnalyzer.Domain.Entities.PlayerEncounter", entity =>
        {
            entity.Property<Guid>("OwnerPlayerId").HasColumnType("uuid").HasColumnName("owner_player_id");
            entity.Property<Guid>("OtherPlayerId").HasColumnType("uuid").HasColumnName("other_player_id");
            entity.Property<int>("EnemyTeamMatches").HasColumnType("integer").HasColumnName("enemy_team_matches");
            entity.Property<DateTimeOffset>("FirstSeenAt").HasColumnType("timestamp with time zone").HasColumnName("first_seen_at");
            entity.Property<DateTimeOffset>("LastSeenAt").HasColumnType("timestamp with time zone").HasColumnName("last_seen_at");
            entity.Property<int>("LossesAgainst").HasColumnType("integer").HasColumnName("losses_against");
            entity.Property<int>("LossesTogether").HasColumnType("integer").HasColumnName("losses_together");
            entity.Property<int>("SameTeamMatches").HasColumnType("integer").HasColumnName("same_team_matches");
            entity.Property<int>("TotalMatches").HasColumnType("integer").HasColumnName("total_matches");
            entity.Property<int>("WinsAgainst").HasColumnType("integer").HasColumnName("wins_against");
            entity.Property<int>("WinsTogether").HasColumnType("integer").HasColumnName("wins_together");
            entity.HasKey("OwnerPlayerId", "OtherPlayerId");
            entity.HasIndex("OtherPlayerId");
            entity.HasIndex("OwnerPlayerId", "TotalMatches");
            entity.ToTable("player_encounters", table => table.HasCheckConstraint(
                "ck_player_encounters_distinct_players",
                "owner_player_id <> other_player_id"));
        });

        modelBuilder.Entity("LolAnalyzer.Domain.Entities.PlayerRelationship", entity =>
        {
            entity.Property<Guid>("PlayerAId").HasColumnType("uuid").HasColumnName("player_a_id");
            entity.Property<Guid>("PlayerBId").HasColumnType("uuid").HasColumnName("player_b_id");
            entity.Property<int>("ConsecutiveMatches").HasColumnType("integer").HasColumnName("consecutive_matches");
            entity.Property<DateTimeOffset>("FirstSeenAt").HasColumnType("timestamp with time zone").HasColumnName("first_seen_at");
            entity.Property<DateTimeOffset>("LastSeenAt").HasColumnType("timestamp with time zone").HasColumnName("last_seen_at");
            entity.Property<int>("MatchesTogether").HasColumnType("integer").HasColumnName("matches_together");
            entity.Property<int>("OppositeTeamMatches").HasColumnType("integer").HasColumnName("opposite_team_matches");
            entity.Property<int>("RecentMatchesTogether").HasColumnType("integer").HasColumnName("recent_matches_together");
            entity.Property<string>("RelationshipConfidence").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)").HasColumnName("relationship_confidence");
            entity.Property<int>("RelationshipScore").HasColumnType("integer").HasColumnName("relationship_score");
            entity.Property<int>("SameTeamMatches").HasColumnType("integer").HasColumnName("same_team_matches");
            entity.HasKey("PlayerAId", "PlayerBId");
            entity.HasIndex("PlayerAId", "RelationshipScore");
            entity.HasIndex("PlayerBId", "RelationshipScore");
            entity.ToTable("player_relationships", table =>
            {
                table.HasCheckConstraint("ck_player_relationships_canonical_pair", "player_a_id < player_b_id");
                table.HasCheckConstraint("ck_player_relationships_match_totals", "matches_together = same_team_matches + opposite_team_matches");
                table.HasCheckConstraint("ck_player_relationships_nonnegative_counts", "matches_together >= 0 AND same_team_matches >= 0 AND opposite_team_matches >= 0 AND recent_matches_together >= 0 AND consecutive_matches >= 0");
                table.HasCheckConstraint("ck_player_relationships_score_range", "relationship_score >= 0 AND relationship_score <= 100");
            });
        });

        modelBuilder.Entity("LolAnalyzer.Domain.Entities.MatchParticipant", entity =>
        {
            entity.HasOne("LolAnalyzer.Domain.Entities.Match", "Match").WithMany("Participants").HasForeignKey("MatchId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            entity.HasOne("LolAnalyzer.Domain.Entities.Player", "Player").WithMany("MatchParticipants").HasForeignKey("PlayerId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });

        modelBuilder.Entity("LolAnalyzer.Domain.Entities.PlayerEncounter", entity =>
        {
            entity.HasOne("LolAnalyzer.Domain.Entities.Player", "OtherPlayer").WithMany().HasForeignKey("OtherPlayerId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.HasOne("LolAnalyzer.Domain.Entities.Player", "OwnerPlayer").WithMany().HasForeignKey("OwnerPlayerId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("LolAnalyzer.Domain.Entities.PlayerRelationship", entity =>
        {
            entity.HasOne("LolAnalyzer.Domain.Entities.Player", "PlayerA").WithMany().HasForeignKey("PlayerAId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.HasOne("LolAnalyzer.Domain.Entities.Player", "PlayerB").WithMany().HasForeignKey("PlayerBId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
    }
}
