namespace EBooking.Bookings.Tests;

using EBooking.Bookings.Application;
using EBooking.Bookings.Domain;

public sealed class BookingServiceTests
{
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task CreateBookingAsync_Should_Create_Pending_Booking()
    {
        var repository = new FakeBookingRepository();
        var service = new BookingService(repository);

        var result = await service.CreateBookingAsync(
            EventId,
            UserId,
            seatsCount: 2);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(EventId, result.EventId);
        Assert.Equal(UserId, result.UserId);
        Assert.Equal(2, result.SeatsCount);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Null(result.ProcessedAt);

        Assert.NotNull(repository.BookingAdded);
        Assert.Equal(1, repository.SaveChangesCallCount);
        Assert.Equal(
            new[] { "Add", "Save" },
            repository.Operations);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_When_Limit_Is_Reached()
    {
        var repository = new FakeBookingRepository
        {
            ActiveBookingCount = 10
        };

        var service = new BookingService(repository);

        var exception =
            await Assert.ThrowsAsync<ActiveBookingLimitExceededException>(
                () => service.CreateBookingAsync(
                    EventId,
                    UserId,
                    seatsCount: 1));

        Assert.Equal(10, exception.Limit);
        Assert.Null(repository.BookingAdded);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Allow_Booking_Below_Limit()
    {
        var repository = new FakeBookingRepository
        {
            ActiveBookingCount = 9
        };

        var service = new BookingService(repository);

        var result = await service.CreateBookingAsync(
            EventId,
            UserId,
            seatsCount: 1);

        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.NotNull(repository.BookingAdded);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Return_Booking()
    {
        var booking = Booking.CreatePending(
            EventId,
            UserId,
            seatsCount: 3);

        var repository = new FakeBookingRepository();
        repository.Seed(booking);

        var service = new BookingService(repository);

        var result = await service.GetBookingByIdAsync(
            booking.Id);

        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(booking.EventId, result.EventId);
        Assert.Equal(booking.UserId, result.UserId);
        Assert.Equal(booking.SeatsCount, result.SeatsCount);
        Assert.Equal(booking.Status, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Throw_When_Not_Found()
    {
        var repository = new FakeBookingRepository();
        var service = new BookingService(repository);

        var bookingId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetBookingByIdAsync(bookingId));

        Assert.Contains(
            bookingId.ToString(),
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Cancel_Own_Booking()
    {
        var booking = Booking.CreatePending(
            EventId,
            UserId,
            seatsCount: 1);

        var repository = new FakeBookingRepository();
        repository.Seed(booking);

        var service = new BookingService(repository);

        await service.CancelBookingAsync(
            booking.Id,
            UserId,
            requesterIsAdmin: false);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Allow_Admin_To_Cancel()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var booking = Booking.CreatePending(
            EventId,
            ownerId,
            seatsCount: 1);

        var repository = new FakeBookingRepository();
        repository.Seed(booking);

        var service = new BookingService(repository);

        await service.CancelBookingAsync(
            booking.Id,
            adminId,
            requesterIsAdmin: true);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Reject_Other_User()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();

        var booking = Booking.CreatePending(
            EventId,
            ownerId,
            seatsCount: 1);

        var repository = new FakeBookingRepository();
        repository.Seed(booking);

        var service = new BookingService(repository);

        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => service.CancelBookingAsync(
                booking.Id,
                requesterId,
                requesterIsAdmin: false));

        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Throw_When_Not_Found()
    {
        var repository = new FakeBookingRepository();
        var service = new BookingService(repository);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.CancelBookingAsync(
                Guid.NewGuid(),
                UserId,
                requesterIsAdmin: false));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_For_Empty_EventId()
    {
        var repository = new FakeBookingRepository();
        var service = new BookingService(repository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateBookingAsync(
                Guid.Empty,
                UserId,
                seatsCount: 1));

        Assert.Equal("eventId", exception.ParamName);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_For_Empty_UserId()
    {
        var repository = new FakeBookingRepository();
        var service = new BookingService(repository);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateBookingAsync(
                EventId,
                Guid.Empty,
                seatsCount: 1));

        Assert.Equal("userId", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateBookingAsync_Should_Throw_For_Invalid_SeatsCount(
        int seatsCount)
    {
        var repository = new FakeBookingRepository();
        var service = new BookingService(repository);

        var exception =
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => service.CreateBookingAsync(
                    EventId,
                    UserId,
                    seatsCount));

        Assert.Equal("seatsCount", exception.ParamName);
    }
}