namespace EBooking.IntegrationTests;

using EBooking.Domain.Entities;
using EBooking.Infrastructure.DataStore;
using EBooking.Infrastructure.Repositories;
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
        var user = await CreateUserAsync(context);
        var booking = Booking.CreatePending(eventItem.Id, user.Id);

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

        var user = await CreateUserAsync(context);

        var pendingBooking = Booking.CreatePending(eventItem.Id, user.Id);

        var confirmedBooking = Booking.CreatePending(eventItem.Id, user.Id);
        confirmedBooking.Confirm();

        var rejectedBooking = Booking.CreatePending(eventItem.Id, user.Id);
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
        var user = await CreateUserAsync(context);
        var booking = Booking.CreatePending(eventItem.Id, user.Id);

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
        var user = await CreateUserAsync(context);
        var booking = Booking.CreatePending(eventItem.Id, user.Id);

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

    [Fact]
    public async Task CountActiveByUserIdAsync_Should_Count_Only_Active_User_Bookings()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);

        var firstUser = await CreateUserAsync(
            context,
            "first-user");

        var secondUser = await CreateUserAsync(
            context,
            "second-user");

        var eventItem = CreateEvent();

        await eventRepository.AddAsync(eventItem);
        await eventRepository.SaveChangesAsync();

        var pendingBooking = Booking.CreatePending(
            eventItem.Id,
            firstUser.Id);

        var confirmedBooking = Booking.CreatePending(
            eventItem.Id,
            firstUser.Id);
        confirmedBooking.Confirm();

        var rejectedBooking = Booking.CreatePending(
            eventItem.Id,
            firstUser.Id);
        rejectedBooking.Reject();

        var cancelledBooking = Booking.CreatePending(
            eventItem.Id,
            firstUser.Id);
        cancelledBooking.Cancel();

        var anotherUserBooking = Booking.CreatePending(
            eventItem.Id,
            secondUser.Id);

        await bookingRepository.AddAsync(pendingBooking);
        await bookingRepository.AddAsync(confirmedBooking);
        await bookingRepository.AddAsync(rejectedBooking);
        await bookingRepository.AddAsync(cancelledBooking);
        await bookingRepository.AddAsync(anotherUserBooking);
        await bookingRepository.SaveChangesAsync();

        var count = await bookingRepository.CountActiveByUserIdAsync(
            firstUser.Id);

        Assert.Equal(2, count);
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

    private static async Task<User> CreateUserAsync(AppDbContext context, string login = "test-user")
    {
        var user = User.Create(
            login,
            new string('0', 64),
            UserRole.User
        );

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}