using Leaderboard.Application.IConfigurations;
using Leaderboard.Application.IRepositories;
using Leaderboard.Infrastructure.Configurations;
using Leaderboard.Infrastructure.Persistence;
using Leaderboard.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leaderboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        services.AddOptions<LeaderboardConfiguration>()
        .Bind(configuration.GetSection("LeaderboardConfiguration"));

        services.AddSingleton<ILeaderboardConfiguration>(sp =>
            sp.GetRequiredService<IOptions<LeaderboardConfiguration>>().Value);

        services.AddDbContext<LeaderboardDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Leaderboard"));
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserAggregateRepository, UserAggregateRepository>();
        services.AddScoped<IScoreRepository, ScoreRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}