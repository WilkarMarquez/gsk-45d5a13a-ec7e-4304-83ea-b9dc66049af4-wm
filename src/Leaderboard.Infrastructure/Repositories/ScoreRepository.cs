using Leaderboard.Application.IRepositories;
using Leaderboard.Domain.Entities;
using Leaderboard.Domain.Models;
using Leaderboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Infrastructure.Repositories;

public sealed class ScoreRepository : IScoreRepository
{
    private readonly LeaderboardDbContext _context;

    public ScoreRepository(
        LeaderboardDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ScoreEvent scoreEvent,
        CancellationToken cancellationToken)
    {
        await _context.ScoreEvents.AddAsync(
            scoreEvent,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<LeaderboardEntry>> GetLeaderboardAsync(
        DateTime from,
        int top,
        CancellationToken cancellationToken)
    {
        return await _context.ScoreEvents
            .AsNoTracking()
            .Where(x => x.EventTimestamp >= from)
            .GroupBy(x => new
            {
                x.User.ExternalId,
                x.User.Name
            })
            .Select(g => new LeaderboardEntry
            {
                ExternalUserId = g.Key.ExternalId,
                Name = g.Key.Name,
                Score = g.Sum(x => x.Score)
            })
            .OrderByDescending(x => x.Score)
            .Take(top)
            .ToListAsync(cancellationToken);
    }
}