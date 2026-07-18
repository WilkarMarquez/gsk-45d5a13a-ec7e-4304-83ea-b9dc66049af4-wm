namespace Leaderboard.Domain.Entities;

public class UserAggregate
{
    public Guid UserId { get; set; }

    public long TotalScore { get; set; }

    public DateTime LastUpdated { get; set; }

    public User User { get; set; } = null!;
}