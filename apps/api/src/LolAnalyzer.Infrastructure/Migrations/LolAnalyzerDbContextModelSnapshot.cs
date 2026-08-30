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

        modelBuilder.Entity("LolAnalyzer.Domain.Entities.MatchParticipant", entity =>
        {
            entity.HasOne("LolAnalyzer.Domain.Entities.Match", "Match").WithMany("Participants").HasForeignKey("MatchId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            entity.HasOne("LolAnalyzer.Domain.Entities.Player", "Player").WithMany("MatchParticipants").HasForeignKey("PlayerId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
    }
}
