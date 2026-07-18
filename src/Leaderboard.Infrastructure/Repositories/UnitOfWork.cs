using Leaderboard.Application.IRepositories;
using Leaderboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Leaderboard.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly LeaderboardDbContext _context;

    private IDbContextTransaction? _transaction;

    public UnitOfWork(
        LeaderboardDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken)
    {
        _transaction =
            await _context.Database
                .BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(
        CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);

        if (_transaction != null)
            await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(
        CancellationToken cancellationToken)
    {
        if (_transaction != null)
            await _transaction.RollbackAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}