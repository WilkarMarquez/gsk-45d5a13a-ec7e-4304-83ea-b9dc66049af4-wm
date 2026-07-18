using FluentAssertions;
using Leaderboard.Application.IRepositories;
using Leaderboard.Application.Users.Queries.GetUserScore;
using Leaderboard.Domain.Entities;
using NSubstitute;

namespace Leaderboard.UnitTests.Queries;

public class GetUserScoreQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserAggregateRepository _userAggregateRepository = Substitute.For<IUserAggregateRepository>();
    private readonly GetUserScoreQueryHandler _sut;

    public GetUserScoreQueryHandlerTests()
    {
        _sut = new GetUserScoreQueryHandler(_userRepository, _userAggregateRepository);
    }

    // ---------- Usuario no existe ----------

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _userRepository.GetByExternalIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var query = new GetUserScoreQuery("ext-unknown");

        // Act
        var act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldNotQueryAggregate()
    {
        // Arrange
        _userRepository.GetByExternalIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var query = new GetUserScoreQuery("ext-unknown");

        // Act
        var act = async () => await _sut.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();

        // Assert
        await _userAggregateRepository.DidNotReceive()
            .GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ---------- Usuario existe, con agregado ----------

    [Fact]
    public async Task Handle_WhenUserExistsWithAggregate_ShouldReturnTotalScore()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = "ext-123",
            Name = "Alice",
            CreatedAt = DateTime.UtcNow
        };
        var aggregate = new UserAggregate
        {
            UserId = user.Id,
            TotalScore = 750,
            LastUpdated = DateTime.UtcNow
        };

        _userRepository.GetByExternalIdAsync(user.ExternalId, Arg.Any<CancellationToken>())
            .Returns(user);
        _userAggregateRepository.GetAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(aggregate);

        var query = new GetUserScoreQuery(user.ExternalId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(new UserScoreResponse(user.ExternalId, "Alice", 750));
    }

    // ---------- Usuario existe, sin agregado (nunca registró score) ----------

    [Fact]
    public async Task Handle_WhenUserExistsWithoutAggregate_ShouldReturnZeroScore()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = "ext-456",
            Name = "Bob",
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.GetByExternalIdAsync(user.ExternalId, Arg.Any<CancellationToken>())
            .Returns(user);
        _userAggregateRepository.GetAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((UserAggregate?)null);

        var query = new GetUserScoreQuery(user.ExternalId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(new UserScoreResponse(user.ExternalId, "Bob", 0));
    }

    // ---------- Verifica que se use el UserId externo, no el interno, en la respuesta ----------

    [Fact]
    public async Task Handle_ShouldReturnExternalUserId_NotInternalGuid()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = "ext-789",
            Name = "Carol",
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.GetByExternalIdAsync(user.ExternalId, Arg.Any<CancellationToken>())
            .Returns(user);
        _userAggregateRepository.GetAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((UserAggregate?)null);

        var query = new GetUserScoreQuery(user.ExternalId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.UserId.Should().Be("ext-789");
        result.UserId.Should().NotBe(user.Id.ToString());
    }

    // ---------- Verifica que se consulte el agregado usando el Id interno correcto ----------

    [Fact]
    public async Task Handle_ShouldQueryAggregate_UsingInternalUserId()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = "ext-999",
            Name = "Dave",
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.GetByExternalIdAsync(user.ExternalId, Arg.Any<CancellationToken>())
            .Returns(user);
        _userAggregateRepository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserAggregate?)null);

        var query = new GetUserScoreQuery(user.ExternalId);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _userAggregateRepository.Received(1).GetAsync(user.Id, Arg.Any<CancellationToken>());
    }
}