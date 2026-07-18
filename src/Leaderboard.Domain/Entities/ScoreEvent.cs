namespace Leaderboard.Domain.Entities;

public class ScoreEvent
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public int Score { get; set; }

    public DateTime EventTimestamp { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}