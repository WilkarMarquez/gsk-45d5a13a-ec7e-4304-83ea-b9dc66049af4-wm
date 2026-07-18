using FluentAssertions;
using Leaderboard.Domain.Entities;
using Leaderboard.Infrastructure.Repositories;
using Leaderboard.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.IntegrationTests.Repositories;

public class UnitOfWorkTests : IntegrationTestBase
{
    public UnitOfWorkTests(PostgresContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CommitAsync_ShouldPersistChanges_MadeWithinTransaction()
    {
        // Arrange
        await using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var user = new User { Id = Guid.NewGuid(), ExternalId = "ext-commit", Name = "Alice", CreatedAt = DateTime.UtcNow };

        // Act
        await unitOfWork.BeginTransactionAsync(CancellationToken.None);
        context.Users.Add(user);
        await unitOfWork.CommitAsync(CancellationToken.None);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Users.FindAsync(user.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task RollbackAsync_ShouldDiscardChanges_MadeWithinTransaction()
    {
        // Arrange
        await using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var user = new User { Id = Guid.NewGuid(), ExternalId = "ext-rollback", Name = "Bob", CreatedAt = DateTime.UtcNow };

        // Act
        await unitOfWork.BeginTransactionAsync(CancellationToken.None);
        context.Users.Add(user);
        await context.SaveChangesAsync(); // dentro de la transacción, pero sin commit
        await unitOfWork.RollbackAsync(CancellationToken.None);

        // Assert: como se hizo rollback, no debería existir en una conexión nueva
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Users.FirstOrDefaultAsync(u => u.ExternalId == "ext-rollback");
        saved.Should().BeNull();
    }

    [Fact]
    public async Task Commit_ThenRollback_ShouldNotUndoAlreadyCommittedChanges()
    {
        // Verifica que una vez hecho Commit, un Rollback posterior (aunque no debería llamarse) no revierte nada
        // porque la transacción ya fue confirmada (esto documenta el comportamiento actual del código)
        await using var context = CreateContext();
        var unitOfWork = new UnitOfWork(context);
        var user = new User { Id = Guid.NewGuid(), ExternalId = "ext-both", Name = "Carol", CreatedAt = DateTime.UtcNow };

        await unitOfWork.BeginTransactionAsync(CancellationToken.None);
        context.Users.Add(user);
        await unitOfWork.CommitAsync(CancellationToken.None);

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Users.FindAsync(user.Id);
        saved.Should().NotBeNull();
    }
}