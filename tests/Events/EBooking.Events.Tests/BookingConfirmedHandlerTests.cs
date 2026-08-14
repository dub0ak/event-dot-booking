namespace EBooking.Events.Tests.Application;

using EBooking.Contracts;
using EBooking.Events.Application;
using EBooking.Events.Domain;

public sealed class BookingConfirmedHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Save_Changes_And_Invalidate_Cache_When_Processed()
    {
        var repository = new FakeEventRepository();
        var cacheService = new FakeCacheService();

        var eventItem = Event.Create(
            "Test Event",
            null,
            new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc),
            10
        );

        repository.GetByIdResult = eventItem;

        var handler = new BookingConfirmedHandler(repository, cacheService);

        var message = new BookingConfirmed(
            Guid.NewGuid(),
            eventItem.Id,
            Guid.NewGuid(),
            2,
            DateTimeOffset.UtcNow
        );

        var result = await handler.HandleAsync(message);
        Assert.Equal(BookingConfirmedHandlingResult.Processed, result);
        Assert.Equal(8, eventItem.AvailableSeats);
        Assert.Equal(1, repository.SaveChangesCallCount);
        Assert.Equal(1, cacheService.RemoveCallCount);
        Assert.Equal(CacheKeys.Event(eventItem.Id), cacheService.LastKey);
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Save_Or_Invalidate_Cache_When_Event_Not_Found()
    {
        var repository = new FakeEventRepository();
        var cacheService = new FakeCacheService();
        var handler = new BookingConfirmedHandler(repository, cacheService);
        var message = new BookingConfirmed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2,
            DateTimeOffset.UtcNow
        );
        var result = await handler.HandleAsync(message);
        Assert.Equal(BookingConfirmedHandlingResult.EventNotFound, result);
        Assert.Equal(0, repository.SaveChangesCallCount);
        Assert.Equal(0, cacheService.RemoveCallCount);
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Save_Or_Invalidate_Cache_When_Seats_Are_Insufficient()
    {
        var repository = new FakeEventRepository();
        var cacheService = new FakeCacheService();
        var eventItem = Event.Create(
            "Test Event",
            null,
            new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc),
            1
        );
        repository.GetByIdResult = eventItem;
        var handler = new BookingConfirmedHandler(repository, cacheService);
        var message = new BookingConfirmed(
            Guid.NewGuid(),
            eventItem.Id,
            Guid.NewGuid(),
            2,
            DateTimeOffset.UtcNow
        );
        var result = await handler.HandleAsync(message);
        Assert.Equal(BookingConfirmedHandlingResult.InsufficientSeats, result);
        Assert.Equal(0, repository.SaveChangesCallCount);
        Assert.Equal(0, cacheService.RemoveCallCount);
    }
}
