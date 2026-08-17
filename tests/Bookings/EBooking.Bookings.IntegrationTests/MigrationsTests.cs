namespace EBooking.Bookings.IntegrationTests;

using Microsoft.EntityFrameworkCore;

[Collection(IntegrationTestCollection.Name)]
public sealed class MigrationsTests
{
    private readonly PostgresFixture _fixture;

    public MigrationsTests(
        PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MigrateAsync_Should_Create_Bookings_Table()
    {
        await using var context =
            _fixture.CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        var canConnect =
            await context.Database.CanConnectAsync();

        var appliedMigrations =
            await context.Database
                .GetAppliedMigrationsAsync();

        var tableExists =
            await context.Database
                .SqlQueryRaw<bool>(
                    """
                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = 'public'
                          AND table_name = 'bookings'
                    ) AS "Value"
                    """)
                .SingleAsync();

        Assert.True(canConnect);
        Assert.Contains(
            appliedMigrations,
            migration =>
                migration.EndsWith(
                    "_InitialCreate",
                    StringComparison.Ordinal));

        Assert.True(tableExists);
    }
}