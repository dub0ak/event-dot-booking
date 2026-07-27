namespace EBooking.Bookings.IntegrationTests;

using EBooking.Bookings.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

/// <summary>
/// Поднимает отдельный контейнер PostgreSQL
/// для интеграционных тестов сервиса Bookings.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("ebooking_bookings_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public string ConnectionString =>
        _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public BookingsDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<BookingsDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

        return new BookingsDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}