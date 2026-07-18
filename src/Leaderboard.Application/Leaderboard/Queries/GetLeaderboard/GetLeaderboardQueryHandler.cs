using Leaderboard.Application.IRepositories;
using Leaderboard.Application.IConfigurations;
using MediatR;

namespace Leaderboard.Application.Leaderboard.Queries.GetLeaderboard;

public class GetLeaderboardQueryHandler
    : IRequestHandler<GetLeaderboardQuery,
        IReadOnlyCollection<LeaderboardItemResponse>>
{
    private readonly IScoreRepository _scoreRepository;
    private readonly ILeaderboardConfiguration _leaderboardConfiguration;

    public GetLeaderboardQueryHandler(
        IScoreRepository scoreRepository,
        ILeaderboardConfiguration leaderboardConfiguration)
    {
        _scoreRepository = scoreRepository;
        _leaderboardConfiguration = leaderboardConfiguration;
    }

    public async Task<IReadOnlyCollection<LeaderboardItemResponse>> Handle(
        GetLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.AddDays(
            _leaderboardConfiguration.WindowDays);

        var leaderboard =
        await _scoreRepository.GetLeaderboardAsync(from, request.Top, cancellationToken);

        return leaderboard
            .Select((x, index) =>
                new LeaderboardItemResponse(
                    index + 1,
                    x.ExternalUserId,
                    x.Name,
                    x.Score))
            .ToList();
    }
}