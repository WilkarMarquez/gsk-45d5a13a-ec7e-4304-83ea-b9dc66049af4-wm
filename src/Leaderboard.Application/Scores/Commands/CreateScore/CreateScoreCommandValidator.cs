using FluentValidation;

namespace Leaderboard.Application.Scores.Commands.CreateScore;

public class CreateScoreCommandValidator : AbstractValidator<CreateScoreCommand>
{
    public CreateScoreCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Score)
            .GreaterThan(0);

        RuleFor(x => x.Timestamp)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5));
    }
}