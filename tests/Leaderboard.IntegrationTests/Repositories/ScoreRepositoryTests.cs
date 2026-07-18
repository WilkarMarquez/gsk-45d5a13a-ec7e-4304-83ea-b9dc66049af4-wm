using FluentAssertions;
using Leaderboard.Domain.Entities;
using Leaderboard.Infrastructure.Repositories;
using Leaderboard.IntegrationTests.Common;

namespace Leaderboard.IntegrationTests.Repositories;

public class ScoreRepositoryTests : IntegrationTestBase
{
    public ScoreRepositoryTests(PostgresContainerFixture fixture) : base(fixture) { }

    private static User CreateUser(string externalId, string name) =>
        new() { Id = Guid.NewGuid(), ExternalId = externalId, Name = name, CreatedAt = DateTime.UtcNow };

    [Fact]
    public async Task GetLeaderboardAsync_ShouldSumScoresPerUser_AndOrderDescending()
    {
        // Arrange
        var userA = CreateUser("ext-a", "Alice");
        var userB = CreateUser("ext-b", "Bob");

        await using var seedContext = CreateContext();
        seedContext.Users.AddRange(userA, userB);
        seedContext.ScoreEvents.AddRange(
            new ScoreEvent { Id = Guid.NewGuid(), UserId = userA.Id, Score = 100, EventTimestamp = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new ScoreEvent { Id = Guid.NewGuid(), UserId = userA.Id, Score = 50, EventTimestamp = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new ScoreEvent { Id = Guid.NewGuid(), UserId = userB.Id, Score = 300, EventTimestamp = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        );
        await seedContext.SaveChangesAsync();

        await using var context = CreateContext();
        var repository = new ScoreRepository(context);

        // Act
        var result = await repository.GetLeaderboardAsync(DateTime.UtcNow.AddDays(-1), 10, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.First().ExternalUserId.Should().Be("ext-b"); // 300 > 150, va primero
        result.First().Score.Should().Be(300);
        result.Last().ExternalUserId.Should().Be("ext-a");
        result.Last().Score.Should().Be(150); // 100 + 50 sumados correctamente
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldExcludeScoresOlderThanFromDate()
    {
        // Arrange
        var user = CreateUser("ext-old", "OldUser");

        await using var seedContext = CreateContext();
        seedContext.Users.Add(user);
        seedContext.ScoreEvents.Add(new ScoreEvent
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Score = 999,
            EventTimestamp = DateTime.UtcNow.AddDays(-30), // fuera de la ventana
            CreatedAt = DateTime.UtcNow
        });
        await seedContext.SaveChangesAsync();

        await using var context = CreateContext();
        var repository = new ScoreRepository(context);

        // Act: ventana de solo 7 días
        var result = await repository.GetLeaderboardAsync(DateTime.UtcNow.AddDays(-7), 10, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldRespectTopLimit()
    {
        // Arrange: 5 usuarios, pedimos top 2
        await using var seedContext = CreateContext();
        for (int i = 0; i < 5; i++)
        {
            var user = CreateUser($"ext-{i}", $"User{i}");
            seedContext.Users.Add(user);
            seedContext.ScoreEvents.Add(new ScoreEvent
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Score = (i + 1) * 100, // 100, 200, 300, 400, 500
                EventTimestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }
        await seedContext.SaveChangesAsync();

        await using var context = CreateContext();
        var repository = new ScoreRepository(context);

        // Act
        var result = await repository.GetLeaderboardAsync(DateTime.UtcNow.AddDays(-1), 2, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.First().Score.Should().Be(500); // el más alto
        result.Last().Score.Should().Be(400);
    }

    [Fact]
    public async Task GetLeaderboardAsync_WhenNoScoreEvents_ShouldReturnEmpty()
    {
        await using var context = CreateContext();
        var repository = new ScoreRepository(context);

        var result = await repository.GetLeaderboardAsync(DateTime.UtcNow.AddDays(-7), 10, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldIncludeEventsExactlyAtBoundary()
    {
        // Verifica el operador >= en el filtro de fecha
        var user = CreateUser("ext-boundary", "BoundaryUser");
        var boundaryDate = DateTime.UtcNow.AddDays(-7);

        await using var seedContext = CreateContext();
        seedContext.Users.Add(user);
        seedContext.ScoreEvents.Add(new ScoreEvent
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Score = 42,
            EventTimestamp = boundaryDate,
            CreatedAt = DateTime.UtcNow
        });
        await seedContext.SaveChangesAsync();

        await using var context = CreateContext();
        var repository = new ScoreRepository(context);

        var result = await repository.GetLeaderboardAsync(boundaryDate, 10, CancellationToken.None);

        result.Should().ContainSingle(x => x.ExternalUserId == "ext-boundary");
    }
}