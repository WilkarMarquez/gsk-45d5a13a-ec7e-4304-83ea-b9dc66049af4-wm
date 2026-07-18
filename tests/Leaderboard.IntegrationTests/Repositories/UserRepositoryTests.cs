using FluentAssertions;
using Leaderboard.Domain.Entities;
using Leaderboard.Infrastructure.Repositories;
using Leaderboard.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.IntegrationTests.Repositories;

public class UserRepositoryTests : IntegrationTestBase
{
    public UserRepositoryTests(PostgresContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task AddAsync_ShouldPersistUser_AfterExplicitSaveChanges()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new UserRepository(context);
        var user = new User { Id = Guid.NewGuid(), ExternalId = "ext-1", Name = "Alice", CreatedAt = DateTime.UtcNow };

        // Act
        await repository.AddAsync(user, CancellationToken.None);
        await context.SaveChangesAsync(); // repo no lo hace, hay que hacerlo explícito

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Users.FindAsync(user.Id);
        saved.Should().NotBeNull();
        saved!.ExternalId.Should().Be("ext-1");
    }

    [Fact]
    public async Task AddAsync_WithoutSaveChanges_ShouldNotPersist()
    {
        // Arrange: verifica explícitamente que AddAsync por sí solo NO persiste
        await using var context = CreateContext();
        var repository = new UserRepository(context);
        var user = new User { Id = Guid.NewGuid(), ExternalId = "ext-2", Name = "Bob", CreatedAt = DateTime.UtcNow };

        // Act
        await repository.AddAsync(user, CancellationToken.None);
        // sin SaveChangesAsync

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Users.FirstOrDefaultAsync(u => u.ExternalId == "ext-2");
        saved.Should().BeNull();
    }

    [Fact]
    public async Task GetByExternalIdAsync_WhenExists_ShouldReturnUser()
    {
        await using var seedContext = CreateContext();
        var user = new User { Id = Guid.NewGuid(), ExternalId = "ext-3", Name = "Carol", CreatedAt = DateTime.UtcNow };
        seedContext.Users.Add(user);
        await seedContext.SaveChangesAsync();

        await using var context = CreateContext();
        var repository = new UserRepository(context);

        var result = await repository.GetByExternalIdAsync("ext-3", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Carol");
    }

    [Fact]
    public async Task GetByExternalIdAsync_WhenNotExists_ShouldReturnNull()
    {
        await using var context = CreateContext();
        var repository = new UserRepository(context);

        var result = await repository.GetByExternalIdAsync("nonexistent", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByExternalIdAsync_ShouldReturnUntrackedEntity()
    {
        // Verifica el AsNoTracking(): dos llamadas deben devolver instancias distintas
        await using var seedContext = CreateContext();
        var user = new User { Id = Guid.NewGuid(), ExternalId = "ext-4", Name = "Dave", CreatedAt = DateTime.UtcNow };
        seedContext.Users.Add(user);
        await seedContext.SaveChangesAsync();

        await using var context = CreateContext();
        var repository = new UserRepository(context);

        var first = await repository.GetByExternalIdAsync("ext-4", CancellationToken.None);
        var second = await repository.GetByExternalIdAsync("ext-4", CancellationToken.None);

        first.Should().NotBeSameAs(second);
    }
}