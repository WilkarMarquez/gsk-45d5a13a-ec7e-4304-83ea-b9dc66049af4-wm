namespace Leaderboard.Application.Leaderboard.Queries.GetLeaderboard;

public record LeaderboardItemResponse
(
    int Position,
    string UserId,
    string UserName,
    long Score
);