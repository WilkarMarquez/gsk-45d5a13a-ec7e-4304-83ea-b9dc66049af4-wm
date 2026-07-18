using Leaderboard.Domain.Entities;

namespace Leaderboard.Application.IRepositories;

public interface IUserAggregateRepository
{
    Task<UserAggregate?> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(UserAggregate aggregate, CancellationToken cancellationToken);

    Task UpdateAsync(UserAggregate aggregate, CancellationToken cancellationToken);
}