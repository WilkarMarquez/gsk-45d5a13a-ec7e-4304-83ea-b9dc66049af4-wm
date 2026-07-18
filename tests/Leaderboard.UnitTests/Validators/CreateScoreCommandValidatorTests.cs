using FluentValidation.TestHelper;
using Leaderboard.Application.Scores.Commands.CreateScore;

namespace Leaderboard.UnitTests.Validators;

public class CreateScoreCommandValidatorTests
{
    private readonly CreateScoreCommandValidator _sut = new();

    // ---------- UserId ----------

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Should_HaveError_WhenUserIdIsEmpty(string? userId)
    {
        var command = new CreateScoreCommand(userId!, 100, "John", DateTime.UtcNow);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_HaveError_WhenUserIdExceedsMaxLength()
    {
        var longUserId = new string('a', 101);
        var command = new CreateScoreCommand(longUserId, 100, "John", DateTime.UtcNow);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_NotHaveError_WhenUserIdIsExactlyMaxLength()
    {
        var maxLengthUserId = new string('a', 100);
        var command = new CreateScoreCommand(maxLengthUserId, 100, "John", DateTime.UtcNow);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    // ---------- Score ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_HaveError_WhenScoreIsNotPositive(int score)
    {
        var command = new CreateScoreCommand("ext-123", score, "John", DateTime.UtcNow);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Score);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void Should_NotHaveError_WhenScoreIsPositive(int score)
    {
        var command = new CreateScoreCommand("ext-123", score, "John", DateTime.UtcNow);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Score);
    }

    // ---------- Timestamp ----------

    [Fact]
    public void Should_HaveError_WhenTimestampIsTooFarInTheFuture()
    {
        var futureTimestamp = DateTime.UtcNow.AddMinutes(10);
        var command = new CreateScoreCommand("ext-123", 100, "John", futureTimestamp);

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
    }

    [Fact]
    public void Should_NotHaveError_WhenTimestampIsNow()
    {
        var command = new CreateScoreCommand("ext-123", 100, "John", DateTime.UtcNow);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Timestamp);
    }

    [Fact]
    public void Should_NotHaveError_WhenTimestampIsInThePast()
    {
        var pastTimestamp = DateTime.UtcNow.AddDays(-30);
        var command = new CreateScoreCommand("ext-123", 100, "John", pastTimestamp);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Timestamp);
    }

    // ---------- Comando completo válido ----------

    [Fact]
    public void Should_NotHaveAnyErrors_WhenCommandIsValid()
    {
        var command = new CreateScoreCommand("ext-123", 100, "John Doe", DateTime.UtcNow);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}