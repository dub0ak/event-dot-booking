namespace EBooking.IntegrationTests;

using EBooking.Models;
using EBooking.Repositories;
using Xunit;

[Collection("Postgres collection")]
public class BookingRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public BookingRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_Should_Save_Booking()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);

        var eventItem = CreateEvent();
        await eventRepository.AddAsync(eventItem);
        await eventRepository.SaveChangesAsync();

        var booking = Booking.CreatePending(eventItem.Id);

        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();

        var loadedBooking = await bookingRepository.GetByIdAsync(booking.Id);

        Assert.NotNull(loadedBooking);
        Assert.Equal(booking.Id, loadedBooking!.Id);
        Assert.Equal(eventItem.Id, loadedBooking.EventId);
        Assert.Equal(BookingStatus.Pending, loadedBooking.Status);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Booking_Does_Not_Exist()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var bookingRepository = new BookingRepository(context);

        var result = await bookingRepository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingBookingIdsAsync_Should_Return_Only_Pending_Bookings()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);

        var eventItem = CreateEvent();
        await eventRepository.AddAsync(eventItem);
        await eventRepository.SaveChangesAsync();

        var pendingBooking = Booking.CreatePending(eventItem.Id);

        var confirmedBooking = Booking.CreatePending(eventItem.Id);
        confirmedBooking.Confirm();

        var rejectedBooking = Booking.CreatePending(eventItem.Id);
        rejectedBooking.Reject();

        await bookingRepository.AddAsync(pendingBooking);
        await bookingRepository.AddAsync(confirmedBooking);
        await bookingRepository.AddAsync(rejectedBooking);
        await bookingRepository.SaveChangesAsync();

        var pendingIds = await bookingRepository.GetPendingBookingIdsAsync(CancellationToken.None);

        Assert.Single(pendingIds);
        Assert.Contains(pendingBooking.Id, pendingIds);
        Assert.DoesNotContain(confirmedBooking.Id, pendingIds);
        Assert.DoesNotContain(rejectedBooking.Id, pendingIds);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Persist_Confirmed_Status()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);

        var eventItem = CreateEvent();
        await eventRepository.AddAsync(eventItem);
        await eventRepository.SaveChangesAsync();

        var booking = Booking.CreatePending(eventItem.Id);

        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();

        var bookingToUpdate = await bookingRepository.GetByIdAsync(
            booking.Id,
            asNoTracking: false);

        bookingToUpdate!.Confirm();

        await bookingRepository.SaveChangesAsync();

        var updatedBooking = await bookingRepository.GetByIdAsync(booking.Id);

        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Confirmed, updatedBooking!.Status);
        Assert.NotNull(updatedBooking.ProcessedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Persist_Rejected_Status()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);

        var eventItem = CreateEvent();
        await eventRepository.AddAsync(eventItem);
        await eventRepository.SaveChangesAsync();

        var booking = Booking.CreatePending(eventItem.Id);

        await bookingRepository.AddAsync(booking);
        await bookingRepository.SaveChangesAsync();

        var bookingToUpdate = await bookingRepository.GetByIdAsync(
            booking.Id,
            asNoTracking: false);

        bookingToUpdate!.Reject();

        await bookingRepository.SaveChangesAsync();

        var updatedBooking = await bookingRepository.GetByIdAsync(booking.Id);

        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Rejected, updatedBooking!.Status);
        Assert.NotNull(updatedBooking.ProcessedAt);
    }

    private static Event CreateEvent()
    {
        return Event.Create(
            "Repository Test Event",
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10);
    }
}