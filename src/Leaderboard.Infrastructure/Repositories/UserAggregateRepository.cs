using Leaderboard.Application.IRepositories;
using Leaderboard.Domain.Entities;
using Leaderboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Infrastructure.Repositories;

public sealed class UserAggregateRepository : IUserAggregateRepository
{
    private readonly LeaderboardDbContext _context;

    public UserAggregateRepository(
        LeaderboardDbContext context)
    {
        _context = context;
    }

    public async Task<UserAggregate?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.UserAggregates
            .FirstOrDefaultAsync(
                x => x.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(
        UserAggregate aggregate,
        CancellationToken cancellationToken)
    {
        await _context.UserAggregates.AddAsync(
            aggregate,
            cancellationToken);
    }

    public Task UpdateAsync(
        UserAggregate aggregate,
        CancellationToken cancellationToken)
    {
        _context.UserAggregates.Update(aggregate);

        return Task.CompletedTask;
    }
}