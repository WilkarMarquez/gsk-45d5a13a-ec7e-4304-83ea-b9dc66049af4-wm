using FluentAssertions;
using Leaderboard.Application.IConfigurations;
using Leaderboard.Application.IRepositories;
using Leaderboard.Application.Leaderboard.Queries.GetLeaderboard;
using Leaderboard.Domain.Models;
using NSubstitute;

namespace Leaderboard.UnitTests.Queries;

public class GetLeaderboardQueryHandlerTests
{
    private readonly IScoreRepository _scoreRepository = Substitute.For<IScoreRepository>();
    private readonly ILeaderboardConfiguration _leaderboardConfiguration = Substitute.For<ILeaderboardConfiguration>();
    private readonly GetLeaderboardQueryHandler _sut;

    public GetLeaderboardQueryHandlerTests()
    {
        _leaderboardConfiguration.WindowDays.Returns(-7);
        _sut = new GetLeaderboardQueryHandler(_scoreRepository, _leaderboardConfiguration);
    }

    // ---------- Mapeo y ranking ----------

    [Fact]
    public async Task Handle_ShouldAssignRanksStartingAtOne_InOrderReturnedByRepository()
    {
        // Arrange
        var repositoryResult = new List<LeaderboardEntry>
        {
            new() { ExternalUserId = "ext-1", Name = "Alice", Score = 500 },
            new() { ExternalUserId = "ext-2", Name = "Bob", Score = 400 },
            new() { ExternalUserId = "ext-3", Name = "Carol", Score = 300 }
        };

        _scoreRepository.GetLeaderboardAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(repositoryResult);

        var query = new GetLeaderboardQuery(10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.ElementAt(0).Should().BeEquivalentTo(new LeaderboardItemResponse(1, "ext-1", "Alice", 500));
        result.ElementAt(1).Should().BeEquivalentTo(new LeaderboardItemResponse(2, "ext-2", "Bob", 400));
        result.ElementAt(2).Should().BeEquivalentTo(new LeaderboardItemResponse(3, "ext-3", "Carol", 300));
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        _scoreRepository.GetLeaderboardAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaderboardEntry>());

        var query = new GetLeaderboardQuery(10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    // ---------- Parámetros pasados al repositorio ----------

    [Fact]
    public async Task Handle_ShouldPassTopValueFromQuery_ToRepository()
    {
        // Arrange
        _scoreRepository.GetLeaderboardAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaderboardEntry>());

        var query = new GetLeaderboardQuery(25);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _scoreRepository.Received(1).GetLeaderboardAsync(
            Arg.Any<DateTime>(),
            25,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldComputeFromDate_UsingConfiguredWindowDays()
    {
        // Arrange
        _leaderboardConfiguration.WindowDays.Returns(-14);
        _scoreRepository.GetLeaderboardAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaderboardEntry>());

        var expectedFrom = DateTime.UtcNow.AddDays(-14);
        var query = new GetLeaderboardQuery(10);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert: comparamos con tolerancia porque UtcNow se evalúa en instantes distintos
        await _scoreRepository.Received(1).GetLeaderboardAsync(
            Arg.Is<DateTime>(d => Math.Abs((d - expectedFrom).TotalSeconds) < 2),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken_ToRepository()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _scoreRepository.GetLeaderboardAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<LeaderboardEntry>());

        var query = new GetLeaderboardQuery(10);

        // Act
        await _sut.Handle(query, cts.Token);

        // Assert
        await _scoreRepository.Received(1).GetLeaderboardAsync(
            Arg.Any<DateTime>(),
            Arg.Any<int>(),
            cts.Token);
    }

    [Fact]
    public async Task Handle_ShouldMapLargeScoreValues_Correctly()
    {
        // Arrange
        var repositoryResult = new List<LeaderboardEntry>
    {
        new() { ExternalUserId = "ext-1", Name = "Alice", Score = long.MaxValue }
    };

        _scoreRepository.GetLeaderboardAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(repositoryResult);

        var query = new GetLeaderboardQuery(10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Single().Score.Should().Be(long.MaxValue);
    }
}