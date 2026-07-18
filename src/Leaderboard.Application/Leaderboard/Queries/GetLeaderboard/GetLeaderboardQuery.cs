using MediatR;

namespace Leaderboard.Application.Leaderboard.Queries.GetLeaderboard;

public record GetLeaderboardQuery(int Top = 10)
    : IRequest<IReadOnlyCollection<LeaderboardItemResponse>>;