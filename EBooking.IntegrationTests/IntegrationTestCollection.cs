namespace EBooking.IntegrationTests;

[CollectionDefinition("Postgres collection")]
public sealed class IntegrationTestCollection : ICollectionFixture<PostgresFixture>
{
}