using LolAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LolAnalyzer.Infrastructure.Persistence;

public sealed class LolAnalyzerDbContext(DbContextOptions<LolAnalyzerDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();

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
