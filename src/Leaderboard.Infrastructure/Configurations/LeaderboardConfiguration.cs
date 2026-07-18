using Leaderboard.Application.IConfigurations;

namespace Leaderboard.Infrastructure.Configurations;

public class LeaderboardConfiguration
    : ILeaderboardConfiguration
{
    public int WindowDays { get; set; }

}