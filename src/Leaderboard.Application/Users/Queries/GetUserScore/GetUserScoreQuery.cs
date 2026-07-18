using MediatR;

namespace Leaderboard.Application.Users.Queries.GetUserScore;

public record GetUserScoreQuery(string UserId)
    : IRequest<UserScoreResponse>;