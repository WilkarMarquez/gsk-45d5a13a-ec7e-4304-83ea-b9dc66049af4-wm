using FluentAssertions;
using Leaderboard.Domain.Entities;
using Leaderboard.Infrastructure.Repositories;
using Leaderboard.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;


namespace Leaderboard.IntegrationTests.Repositories;

public class UserAggregateRepositoryTests : IntegrationTestBase
{
    public UserAggregateRepositoryTests(PostgresContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task AddAsync_ShouldPersistAggregate()
    {
        await using var context = CreateContext();
        var repository = new UserAggregateRepository(context);
        var aggregate = new UserAggregate { UserId = Guid.NewGuid(), TotalScore = 100, LastUpdated = DateTime.UtcNow };

        await repository.AddAsync(aggregate, CancellationToken.None);
        await context.SaveChangesAsync();

        await using var verifyContext = CreateContext();
        var saved = await verifyContext.UserAggregates.FirstOrDefaultAsync(a => a.UserId == aggregate.UserId);
        saved.Should().NotBeNull();
        saved!.TotalScore.Should().Be(100);
    }

    [Fact]
    public async Task GetAsync_WhenExists_ShouldReturnAggregate()
    {
        var userId = Guid.NewGuid();
        await using var seedContext = CreateContext();
        seedContext.UserAggregates.Add(new UserAggregate { UserId = userId, TotalScore = 250, LastUpdated = DateTime.UtcNow });
        await seedContext.SaveChangesAsync();

        await using var context = CreateContext();
        var repository = new UserAggregateRepository(context);

        var result = await repository.GetAsync(userId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalScore.Should().Be(250);
    }

    [Fact]
    public async Task GetAsync_WhenNotExists_ShouldReturnNull()
    {
        await using var context = CreateContext();
        var repository = new UserAggregateRepository(context);

        var result = await repository.GetAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges_AfterSaveChanges()
    {
        // Arrange: seedeamos y luego actualizamos en un contexto NUEVO (simulando el flujo real del handler)
        var userId = Guid.NewGuid();
        await using var seedContext = CreateContext();
        seedContext.UserAggregates.Add(new UserAggregate { UserId = userId, TotalScore = 100, LastUpdated = DateTime.UtcNow.AddDays(-1) });
        await seedContext.SaveChangesAsync();

        await using var context = CreateContext();
        var repository = new UserAggregateRepository(context);
        var aggregate = await repository.GetAsync(userId, CancellationToken.None);
        aggregate!.TotalScore += 50;
        aggregate.LastUpdated = DateTime.UtcNow;

        // Act
        await repository.UpdateAsync(aggregate, CancellationToken.None);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var updated = await verifyContext.UserAggregates.FirstAsync(a => a.UserId == userId);
        updated.TotalScore.Should().Be(150);
    }
}