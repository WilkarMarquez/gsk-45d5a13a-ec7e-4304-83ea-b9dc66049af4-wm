namespace Leaderboard.Domain.Models;

public class LeaderboardEntry
{
    public string ExternalUserId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public long Score { get; init; }
}