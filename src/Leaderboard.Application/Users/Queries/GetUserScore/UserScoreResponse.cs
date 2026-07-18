namespace Leaderboard.Application.Users.Queries.GetUserScore;

public record UserScoreResponse
(
    string UserId,
    string Name,
    long TotalScore
);