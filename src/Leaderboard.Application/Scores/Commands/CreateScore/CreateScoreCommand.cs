using MediatR;

namespace Leaderboard.Application.Scores.Commands.CreateScore;

public record CreateScoreCommand
(
    string UserId,
    int Score,
    string Name,
    DateTime Timestamp
) : IRequest;