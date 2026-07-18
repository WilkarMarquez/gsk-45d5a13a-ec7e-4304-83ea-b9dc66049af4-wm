using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Leaderboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leaderboard.IntegrationTests.Common;

public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("leaderboard_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner _respawner = null!;
    private NpgsqlConnection _connection = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<LeaderboardDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using (var context = new LeaderboardDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        _connection = new NpgsqlConnection(ConnectionString);
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" }
        });
    }

    public async Task ResetDatabaseAsync() => await _respawner.ResetAsync(_connection);

    public async Task DisposeAsync()
    {
        await _connection.CloseAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresContainerFixture> { }

[Collection("Postgres")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgresContainerFixture Fixture;

    protected IntegrationTestBase(PostgresContainerFixture fixture) => Fixture = fixture;

    protected LeaderboardDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LeaderboardDbContext>()
            .UseNpgsql(Fixture.ConnectionString)
            .Options;
        return new LeaderboardDbContext(options);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await Fixture.ResetDatabaseAsync();
}