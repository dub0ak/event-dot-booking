namespace EBooking.Events.IntegrationTests;

/// <summary>
/// Общая коллекция интеграционных тестов,
/// использующих один экземпляр PostgreSQL-контейнера.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Events collection";
}