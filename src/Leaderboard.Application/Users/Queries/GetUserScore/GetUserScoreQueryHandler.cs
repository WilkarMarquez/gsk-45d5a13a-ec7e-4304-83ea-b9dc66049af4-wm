using Leaderboard.Application.IRepositories;
using MediatR;

namespace Leaderboard.Application.Users.Queries.GetUserScore;

public class GetUserScoreQueryHandler
    : IRequestHandler<GetUserScoreQuery, UserScoreResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserAggregateRepository _userAggregateRepository;

    public GetUserScoreQueryHandler(
        IUserRepository userRepository,
        IUserAggregateRepository userAggregateRepository)
    {
        _userRepository = userRepository;
        _userAggregateRepository = userAggregateRepository;
    }

    public async Task<UserScoreResponse> Handle(
        GetUserScoreQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByExternalIdAsync(
            request.UserId,
            cancellationToken) ?? throw new KeyNotFoundException("User not found.");

        var aggregate = await _userAggregateRepository.GetAsync(
            user.Id,
            cancellationToken);

        return new UserScoreResponse(
            request.UserId,
            user.Name,
            aggregate?.TotalScore ?? 0);
    }
}