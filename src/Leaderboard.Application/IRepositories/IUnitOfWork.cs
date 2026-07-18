namespace Leaderboard.Application.IRepositories;

public interface IUnitOfWork
{
    Task BeginTransactionAsync(
        CancellationToken cancellationToken);

    Task CommitAsync(
        CancellationToken cancellationToken);

    Task RollbackAsync(
        CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}