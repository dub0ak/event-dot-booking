namespace EBooking.Users.IntegrationTests;

using Microsoft.EntityFrameworkCore;

[Collection(IntegrationTestCollection.Name)]
public class MigrationsTests
{
    private readonly PostgresFixture _fixture;

    public MigrationsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migrate_Should_Create_Database()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect);
    }
}