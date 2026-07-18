using Leaderboard.Application.IRepositories;
using Leaderboard.Domain.Entities;
using Leaderboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly LeaderboardDbContext _context;

    public UserRepository(LeaderboardDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ExternalId == externalId,
                cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }
}