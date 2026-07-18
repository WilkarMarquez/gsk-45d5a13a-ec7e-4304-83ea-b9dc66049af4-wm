using FluentAssertions;
using Leaderboard.Application.IRepositories;
using Leaderboard.Application.Scores.Commands.CreateScore;
using Leaderboard.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Leaderboard.UnitTests.Commands;

public class CreateScoreCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IScoreRepository _scoreRepository = Substitute.For<IScoreRepository>();
    private readonly IUserAggregateRepository _userAggregateRepository = Substitute.For<IUserAggregateRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateScoreCommandHandler _sut;

    public CreateScoreCommandHandlerTests()
    {
        _sut = new CreateScoreCommandHandler(
            _userRepository,
            _scoreRepository,
            _userAggregateRepository,
            _unitOfWork);
    }

    // ---------- Caso: usuario nuevo + agregado nuevo ----------

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldCreateUser()
    {
        // Arrange
        var command = new CreateScoreCommand("ext-123", 100, "John Doe",  DateTime.UtcNow);

        _userRepository.GetByExternalIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userAggregateRepository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserAggregate?)null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u =>
                u.ExternalId == command.UserId &&
                u.Name == command.Name &&
                u.Id != Guid.Empty),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldNotCreateNewUser()
    {
        // Arrange
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = "ext-123",
            Name = "John Doe",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var command = new CreateScoreCommand(existingUser.ExternalId, 100, "John Doe", DateTime.UtcNow);

        _userRepository.GetByExternalIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _userAggregateRepository.GetAsync(existingUser.Id, Arg.Any<CancellationToken>())
            .Returns((UserAggregate?)null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ---------- ScoreEvent siempre se crea ----------

    [Fact]
    public async Task Handle_ShouldAlwaysAddScoreEvent_LinkedToCorrectUser()
    {
        // Arrange
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = "ext-123",
            Name = "John Doe",
            CreatedAt = DateTime.UtcNow
        };
        var timestamp = DateTime.UtcNow.AddMinutes(-5);
        var command = new CreateScoreCommand(existingUser.ExternalId, 250, "John Doe", timestamp);

        _userRepository.GetByExternalIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _userAggregateRepository.GetAsync(existingUser.Id, Arg.Any<CancellationToken>())
            .Returns((UserAggregate?)null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _scoreRepository.Received(1).AddAsync(
            Arg.Is<ScoreEvent>(s =>
                s.UserId == existingUser.Id &&
                s.Score == command.Score &&
                s.EventTimestamp == timestamp &&
                s.Id != Guid.Empty),
            Arg.Any<CancellationToken>());
    }

    // ---------- Agregado: creación vs actualización ----------

    [Fact]
    public async Task Handle_WhenAggregateDoesNotExist_ShouldCreateAggregateWithScoreAsTotal()
    {
        // Arrange
        var existingUser = new User { Id = Guid.NewGuid(), ExternalId = "ext-123", Name = "John", CreatedAt = DateTime.UtcNow };
        var command = new CreateScoreCommand(existingUser.ExternalId, 150, "John", DateTime.UtcNow);

        _userRepository.GetByExternalIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _userAggregateRepository.GetAsync(existingUser.Id, Arg.Any<CancellationToken>())
            .Returns((UserAggregate?)null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _userAggregateRepository.Received(1).AddAsync(
            Arg.Is<UserAggregate>(a =>
                a.UserId == existingUser.Id &&
                a.TotalScore == command.Score),
            Arg.Any<CancellationToken>());

        await _userAggregateRepository.DidNotReceive().UpdateAsync(Arg.Any<UserAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAggregateExists_ShouldAccumulateTotalScore()
    {
        // Arrange
        var existingUser = new User { Id = Guid.NewGuid(), ExternalId = "ext-123", Name = "John", CreatedAt = DateTime.UtcNow };
        var existingAggregate = new UserAggregate
        {
            UserId = existingUser.Id,
            TotalScore = 500,
            LastUpdated = DateTime.UtcNow.AddDays(-1)
        };
        var command = new CreateScoreCommand(existingUser.ExternalId, 150, "John", DateTime.UtcNow);

        _userRepository.GetByExternalIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _userAggregateRepository.GetAsync(existingUser.Id, Arg.Any<CancellationToken>())
            .Returns(existingAggregate);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        existingAggregate.TotalScore.Should().Be(650); // 500 + 150

        await _userAggregateRepository.Received(1).UpdateAsync(
            Arg.Is<UserAggregate>(a => a.TotalScore == 650),
            Arg.Any<CancellationToken>());

        await _userAggregateRepository.DidNotReceive().AddAsync(Arg.Any<UserAggregate>(), Arg.Any<CancellationToken>());
    }

    // ---------- Transacción: happy path ----------

    [Fact]
    public async Task Handle_WhenSuccessful_ShouldBeginAndCommitTransaction()
    {
        // Arrange
        var command = new CreateScoreCommand("ext-123", 100, "John", DateTime.UtcNow);

        _userRepository.GetByExternalIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userAggregateRepository.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserAggregate?)null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    // ---------- Transacción: rollback ante excepción ----------

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldRollbackAndRethrow()
    {
        // Arrange
        var command = new CreateScoreCommand("ext-123", 100, "John", DateTime.UtcNow);

        _userRepository.GetByExternalIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userRepository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");

        await _unitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAggregateUpdateThrows_ShouldRollbackAndRethrow()
    {
        // Arrange
        var existingUser = new User { Id = Guid.NewGuid(), ExternalId = "ext-123", Name = "John", CreatedAt = DateTime.UtcNow };
        var existingAggregate = new UserAggregate { UserId = existingUser.Id, TotalScore = 100, LastUpdated = DateTime.UtcNow };
        var command = new CreateScoreCommand(existingUser.ExternalId, 50, "John", DateTime.UtcNow);

        _userRepository.GetByExternalIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _userAggregateRepository.GetAsync(existingUser.Id, Arg.Any<CancellationToken>())
            .Returns(existingAggregate);
        _userAggregateRepository.UpdateAsync(Arg.Any<UserAggregate>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Concurrency conflict"));

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Concurrency conflict");
        await _unitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
    }

    // ---------- LastUpdated se refresca en cada score ----------

    [Fact]
    public async Task Handle_WhenAggregateExists_ShouldUpdateLastUpdatedTimestamp()
    {
        // Arrange
        var existingUser = new User { Id = Guid.NewGuid(), ExternalId = "ext-123", Name = "John", CreatedAt = DateTime.UtcNow };
        var oldTimestamp = DateTime.UtcNow.AddDays(-5);
        var existingAggregate = new UserAggregate { UserId = existingUser.Id, TotalScore = 100, LastUpdated = oldTimestamp };
        var command = new CreateScoreCommand(existingUser.ExternalId, 50, "John", DateTime.UtcNow);

        _userRepository.GetByExternalIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _userAggregateRepository.GetAsync(existingUser.Id, Arg.Any<CancellationToken>())
            .Returns(existingAggregate);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        existingAggregate.LastUpdated.Should().BeAfter(oldTimestamp);
    }
}