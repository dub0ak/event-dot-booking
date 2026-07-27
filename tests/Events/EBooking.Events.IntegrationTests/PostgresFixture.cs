namespace EBooking.Events.IntegrationTests;

using EBooking.Events.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

/// <summary>
/// Поднимает отдельный PostgreSQL-контейнер для интеграционных тестов Events.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("ebooking_events_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    /// <summary>
    /// Строка подключения к тестовому контейнеру.
    /// </summary>
    public string ConnectionString =>
        _container.GetConnectionString();

    /// <summary>
    /// Запускает PostgreSQL перед выполнением тестовой коллекции.
    /// </summary>
    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    /// <summary>
    /// Останавливает и удаляет контейнер после выполнения тестов.
    /// </summary>
    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Создаёт новый экземпляр контекста для отдельного теста.
    /// </summary>
    public EventDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<EventDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

        return new EventDbContext(options);
    }

    /// <summary>
    /// Полностью пересоздаёт тестовую базу и применяет миграции Events.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
}