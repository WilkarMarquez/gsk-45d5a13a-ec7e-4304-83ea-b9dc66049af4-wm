using Leaderboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Infrastructure.Persistence;

public class LeaderboardDbContext : DbContext
{
    public LeaderboardDbContext(DbContextOptions<LeaderboardDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<ScoreEvent> ScoreEvents => Set<ScoreEvent>();

    public DbSet<UserAggregate> UserAggregates => Set<UserAggregate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.ExternalId)
                .IsUnique();

            entity.Property(x => x.ExternalId)
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Entity<ScoreEvent>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.UserId);

            entity.HasIndex(x => x.EventTimestamp);

            entity.HasIndex(x => new
            {
                x.UserId,
                x.EventTimestamp
            });

            entity.HasOne(x => x.User)
                .WithMany(x => x.ScoreEvents)
                .HasForeignKey(x => x.UserId);
        });

        builder.Entity<UserAggregate>(entity =>
        {
            entity.HasKey(x => x.UserId);

            entity.HasOne(x => x.User)
                .WithOne(x => x.Aggregate)
                .HasForeignKey<UserAggregate>(x => x.UserId);

            entity.HasIndex(x => x.TotalScore);
        });
    }
}