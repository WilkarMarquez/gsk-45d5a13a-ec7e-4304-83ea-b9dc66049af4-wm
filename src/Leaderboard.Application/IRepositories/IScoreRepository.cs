using Leaderboard.Domain.Entities;
using Leaderboard.Domain.Models;

namespace Leaderboard.Application.IRepositories;

public interface IScoreRepository
{
    Task AddAsync(
        ScoreEvent scoreEvent,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LeaderboardEntry>> GetLeaderboardAsync(
        DateTime from,
        int top,
        CancellationToken cancellationToken);
}