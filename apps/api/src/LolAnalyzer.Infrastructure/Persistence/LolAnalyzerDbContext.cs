using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class LolAnalyzerDbContext(DbContextOptions<LolAnalyzerDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();

    public DbSet<PlayerEncounter> PlayerEncounters => Set<PlayerEncounter>();

    public DbSet<PlayerRelationship> PlayerRelationships => Set<PlayerRelationship>();

    public DbSet<AnalysisJob> AnalysisJobs => Set<AnalysisJob>();

    public DbSet<PlayerRefreshSchedule> PlayerRefreshSchedules => Set<PlayerRefreshSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.ToTable("players");
            entity.HasKey(player => player.Id);
            entity.Property(player => player.Puuid).HasMaxLength(128).IsRequired();
            entity.Property(player => player.GameName).HasMaxLength(64).IsRequired();
            entity.Property(player => player.TagLine).HasMaxLength(16).IsRequired();
            entity.Property(player => player.PlatformRegion).HasMaxLength(16).IsRequired();
            entity.HasIndex(player => player.Puuid).IsUnique();
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.ToTable("matches");
            entity.HasKey(match => match.Id);
            entity.Property(match => match.RiotMatchId).HasMaxLength(64).IsRequired();
            entity.Property(match => match.GameVersion).HasMaxLength(32);
            entity.Property(match => match.RawData).HasColumnType("jsonb");
            entity.HasIndex(match => match.RiotMatchId).IsUnique();
        });

        modelBuilder.Entity<MatchParticipant>(entity =>
        {
            entity.ToTable("match_participants");
            entity.HasKey(participant => participant.Id);
            entity.Property(participant => participant.ChampionName).HasMaxLength(64).IsRequired();
            entity.Property(participant => participant.TeamPosition).HasMaxLength(32);
            entity.Property(participant => participant.IndividualPosition).HasMaxLength(32);
            entity.HasIndex(participant => new { participant.MatchId, participant.PlayerId }).IsUnique();
            entity.HasIndex(participant => participant.MatchId);
            entity.HasIndex(participant => participant.PlayerId);
            entity.HasOne(participant => participant.Match)
                .WithMany(match => match.Participants)
                .HasForeignKey(participant => participant.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(participant => participant.Player)
                .WithMany(player => player.MatchParticipants)
                .HasForeignKey(participant => participant.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlayerEncounter>(entity =>
        {
            entity.ToTable("player_encounters", table => table.HasCheckConstraint(
                "ck_player_encounters_distinct_players",
                "owner_player_id <> other_player_id"));
            entity.HasKey(encounter => new { encounter.OwnerPlayerId, encounter.OtherPlayerId });
            entity.HasIndex(encounter => new { encounter.OwnerPlayerId, encounter.TotalMatches });
            entity.HasOne(encounter => encounter.OwnerPlayer)
                .WithMany()
                .HasForeignKey(encounter => encounter.OwnerPlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(encounter => encounter.OtherPlayer)
                .WithMany()
                .HasForeignKey(encounter => encounter.OtherPlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlayerRelationship>(entity =>
        {
            entity.ToTable("player_relationships", table =>
            {
                table.HasCheckConstraint("ck_player_relationships_canonical_pair", "player_a_id < player_b_id");
                table.HasCheckConstraint(
                    "ck_player_relationships_match_totals",
                    "matches_together = same_team_matches + opposite_team_matches");
                table.HasCheckConstraint(
                    "ck_player_relationships_nonnegative_counts",
                    "matches_together >= 0 AND same_team_matches >= 0 AND opposite_team_matches >= 0 AND recent_matches_together >= 0 AND consecutive_matches >= 0");
                table.HasCheckConstraint(
                    "ck_player_relationships_score_range",
                    "relationship_score >= 0 AND relationship_score <= 100");
            });
            entity.HasKey(relationship => new { relationship.PlayerAId, relationship.PlayerBId });
            entity.Property(relationship => relationship.RelationshipConfidence).HasMaxLength(16).IsRequired();
            entity.HasIndex(relationship => new { relationship.PlayerAId, relationship.RelationshipScore });
            entity.HasIndex(relationship => new { relationship.PlayerBId, relationship.RelationshipScore });
            entity.HasOne(relationship => relationship.PlayerA)
                .WithMany()
                .HasForeignKey(relationship => relationship.PlayerAId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(relationship => relationship.PlayerB)
                .WithMany()
                .HasForeignKey(relationship => relationship.PlayerBId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AnalysisJob>(entity =>
        {
            entity.ToTable("analysis_jobs", table =>
            {
                table.HasCheckConstraint(
                    "ck_analysis_jobs_requested_count",
                    "requested_count >= 1 AND requested_count <= 200");
                table.HasCheckConstraint(
                    "ck_analysis_jobs_progress",
                    "matches_processed >= 0 AND matches_processed <= requested_count");
            });
            entity.HasKey(job => job.Id);
            entity.Property(job => job.Puuid).HasMaxLength(128).IsRequired();
            entity.Property(job => job.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(job => job.ErrorCode).HasMaxLength(64);
            entity.HasIndex(job => new { job.Status, job.CreatedAt });
            entity.HasIndex(job => new { job.Puuid, job.CreatedAt });
            entity.HasIndex(job => new { job.Puuid, job.RequestedCount })
                .IsUnique()
                .HasDatabaseName("ux_analysis_jobs_active_request")
                .HasFilter("status IN ('Queued', 'Running')");
        });

        modelBuilder.Entity<PlayerRefreshSchedule>(entity =>
        {
            entity.ToTable("player_refresh_schedules", table =>
            {
                table.HasCheckConstraint(
                    "ck_player_refresh_schedules_requested_count",
                    "requested_count >= 1 AND requested_count <= 200");
                table.HasCheckConstraint(
                    "ck_player_refresh_schedules_interval",
                    "interval_minutes >= 15 AND interval_minutes <= 10080");
            });
            entity.HasKey(schedule => schedule.Puuid);
            entity.Property(schedule => schedule.Puuid).HasMaxLength(128).IsRequired();
            entity.HasIndex(schedule => new { schedule.Enabled, schedule.NextRunAt });
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
