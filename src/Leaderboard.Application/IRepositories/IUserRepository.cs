using Leaderboard.Domain.Entities;

namespace Leaderboard.Application.IRepositories;

public interface IUserRepository
{
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}