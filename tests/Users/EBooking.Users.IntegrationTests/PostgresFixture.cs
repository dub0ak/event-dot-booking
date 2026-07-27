namespace EBooking.Users.IntegrationTests;

using EBooking.Users.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("ebooking_users_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public string ConnectionString =>
        _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public UserDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<UserDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

        return new UserDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}