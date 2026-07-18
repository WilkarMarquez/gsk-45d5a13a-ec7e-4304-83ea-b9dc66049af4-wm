using FluentAssertions;
using Leaderboard.Application.Scores.Commands.CreateScore;
using Leaderboard.Application.IRepositories;
using Leaderboard.Infrastructure.Repositories;
using Leaderboard.Infrastructure.Persistence;
using Leaderboard.IntegrationTests.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Leaderboard.IntegrationTests.Scores;

public class CreateScoreCommandIntegrationTests : IntegrationTestBase
{
    public CreateScoreCommandIntegrationTests(PostgresContainerFixture fixture) : base(fixture) { }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddDbContext<LeaderboardDbContext>(o => o.UseNpgsql(Fixture.ConnectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IScoreRepository, ScoreRepository>();
        services.AddScoped<IUserAggregateRepository, UserAggregateRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateScoreCommand).Assembly));

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Handle_NewUser_ShouldCreateUserScoreEventAndAggregate_EndToEnd()
    {
        // Arrange
        await using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new CreateScoreCommand("ext-e2e-1", 100, "Alice", DateTime.UtcNow);

        // Act
        await mediator.Send(command);

        // Assert
        await using var verifyContext = CreateContext();

        var user = await verifyContext.Users.SingleAsync(u => u.ExternalId == "ext-e2e-1");
        user.Name.Should().Be("Alice");

        var scoreEvent = await verifyContext.ScoreEvents.SingleAsync(s => s.UserId == user.Id);
        scoreEvent.Score.Should().Be(100);

        var aggregate = await verifyContext.UserAggregates.SingleAsync(a => a.UserId == user.Id);
        aggregate.TotalScore.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ExistingUser_ShouldAccumulateAggregate_AcrossMultipleRequests()
    {
        await using var provider = BuildServiceProvider();

        using (var scope1 = provider.CreateScope())
        {
            await scope1.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new CreateScoreCommand("ext-e2e-2", 100, "Bob", DateTime.UtcNow));
        }

        using (var scope2 = provider.CreateScope())
        {
            await scope2.ServiceProvider.GetRequiredService<IMediator>()
                .Send(new CreateScoreCommand("ext-e2e-2", 50, "Bob", DateTime.UtcNow));
        }

        await using var verifyContext = CreateContext();
        var user = await verifyContext.Users.SingleAsync(u => u.ExternalId == "ext-e2e-2");

        (await verifyContext.ScoreEvents.CountAsync(s => s.UserId == user.Id)).Should().Be(2);

        var aggregate = await verifyContext.UserAggregates.SingleAsync(a => a.UserId == user.Id);
        aggregate.TotalScore.Should().Be(150);
    }

    [Fact]
    public async Task Handle_ShouldReflectInLeaderboardQuery_AfterMultipleScores()
    {
        // Este test valida la integración COMPLETA: CreateScore -> GetLeaderboard
        await using var provider = BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new CreateScoreCommand("ext-lb-1", 100, "Alice", DateTime.UtcNow));
            await mediator.Send(new CreateScoreCommand("ext-lb-2", 300, "Bob", DateTime.UtcNow));
        }

        await using var context = CreateContext();
        var scoreRepository = new ScoreRepository(context);

        var leaderboard = await scoreRepository.GetLeaderboardAsync(DateTime.UtcNow.AddDays(-1), 10, CancellationToken.None);

        leaderboard.Should().HaveCount(2);
        leaderboard.First().ExternalUserId.Should().Be("ext-lb-2"); // Bob va primero (300 > 100)
    }
}