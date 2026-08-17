namespace EBooking.Bookings.IntegrationTests;

using EBooking.Bookings.Domain;
using EBooking.Bookings.Infrastructure;

[Collection(IntegrationTestCollection.Name)]
public sealed class BookingRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public BookingRepositoryTests(
        PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_Should_Save_Booking()
    {
        await _fixture.ResetDatabaseAsync();

        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var createdAt =
            new DateTimeOffset(
                2026,
                7,
                26,
                12,
                0,
                0,
                TimeSpan.Zero);

        var booking = Booking.CreatePending(
            eventId,
            userId,
            seatsCount: 3,
            createdAt);

        await using (var context = _fixture.CreateContext())
        {
            var repository =
                new BookingRepository(context);

            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.CreateContext();

        var verificationRepository =
            new BookingRepository(verificationContext);

        var loadedBooking =
            await verificationRepository.GetByIdAsync(
                booking.Id);

        Assert.NotNull(loadedBooking);

        Assert.Equal(
            booking.Id,
            loadedBooking!.Id);

        Assert.Equal(
            eventId,
            loadedBooking.EventId);

        Assert.Equal(
            userId,
            loadedBooking.UserId);

        Assert.Equal(
            3,
            loadedBooking.SeatsCount);

        Assert.Equal(
            BookingStatus.Pending,
            loadedBooking.Status);

        Assert.Equal(
            createdAt,
            loadedBooking.CreatedAt);

        Assert.Null(
            loadedBooking.ProcessedAt);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Booking_Does_Not_Exist()
    {
        await _fixture.ResetDatabaseAsync();

        await using var context =
            _fixture.CreateContext();

        var repository =
            new BookingRepository(context);

        var result = await repository.GetByIdAsync(
            Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingBookingIdsAsync_Should_Return_Only_Pending_Bookings_In_Creation_Order()
    {
        await _fixture.ResetDatabaseAsync();

        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var firstPending = Booking.CreatePending(
            eventId,
            userId,
            seatsCount: 1,
            createdAt: UtcDate(2026, 7, 26, 10));

        var confirmed = Booking.CreatePending(
            eventId,
            userId,
            seatsCount: 1,
            createdAt: UtcDate(2026, 7, 26, 11));

        confirmed.Confirm(
            UtcDate(2026, 7, 26, 11, 5));

        var secondPending = Booking.CreatePending(
            eventId,
            userId,
            seatsCount: 2,
            createdAt: UtcDate(2026, 7, 26, 12));

        var cancelled = Booking.CreatePending(
            eventId,
            userId,
            seatsCount: 1,
            createdAt: UtcDate(2026, 7, 26, 13));

        cancelled.Cancel(
            UtcDate(2026, 7, 26, 13, 5));

        await using (var context = _fixture.CreateContext())
        {
            var repository =
                new BookingRepository(context);

            await repository.AddAsync(firstPending);
            await repository.AddAsync(confirmed);
            await repository.AddAsync(secondPending);
            await repository.AddAsync(cancelled);

            await repository.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.CreateContext();

        var verificationRepository =
            new BookingRepository(verificationContext);

        var pendingIds =
            await verificationRepository
                .GetPendingBookingIdsAsync();

        Assert.Equal(
            new[]
            {
                firstPending.Id,
                secondPending.Id
            },
            pendingIds);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Persist_Confirmed_Status()
    {
        await _fixture.ResetDatabaseAsync();

        var booking = Booking.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seatsCount: 2,
            createdAt: UtcDate(2026, 7, 26, 10));

        await using (var context = _fixture.CreateContext())
        {
            var repository =
                new BookingRepository(context);

            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();
        }

        var processedAt =
            UtcDate(2026, 7, 26, 11);

        await using (var context = _fixture.CreateContext())
        {
            var repository =
                new BookingRepository(context);

            var bookingToUpdate =
                await repository.GetByIdAsync(
                    booking.Id,
                    asNoTracking: false);

            Assert.NotNull(bookingToUpdate);

            bookingToUpdate!.Confirm(processedAt);

            await repository.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.CreateContext();

        var verificationRepository =
            new BookingRepository(verificationContext);

        var updatedBooking =
            await verificationRepository.GetByIdAsync(
                booking.Id);

        Assert.NotNull(updatedBooking);

        Assert.Equal(
            BookingStatus.Confirmed,
            updatedBooking!.Status);

        Assert.Equal(
            processedAt,
            updatedBooking.ProcessedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Persist_Cancelled_Status()
    {
        await _fixture.ResetDatabaseAsync();

        var booking = Booking.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seatsCount: 1);

        await using (var context = _fixture.CreateContext())
        {
            var repository =
                new BookingRepository(context);

            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();
        }

        var processedAt =
            UtcDate(2026, 7, 26, 12);

        await using (var context = _fixture.CreateContext())
        {
            var repository =
                new BookingRepository(context);

            var bookingToUpdate =
                await repository.GetByIdAsync(
                    booking.Id,
                    asNoTracking: false);

            Assert.NotNull(bookingToUpdate);

            bookingToUpdate!.Cancel(processedAt);

            await repository.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.CreateContext();

        var verificationRepository =
            new BookingRepository(verificationContext);

        var updatedBooking =
            await verificationRepository.GetByIdAsync(
                booking.Id);

        Assert.NotNull(updatedBooking);

        Assert.Equal(
            BookingStatus.Cancelled,
            updatedBooking!.Status);

        Assert.Equal(
            processedAt,
            updatedBooking.ProcessedAt);
    }

    [Fact]
    public async Task CountActiveByUserIdAsync_Should_Count_Only_Pending_And_Confirmed_Bookings_Of_Specified_User()
    {
        await _fixture.ResetDatabaseAsync();

        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var pendingBooking = Booking.CreatePending(
            eventId,
            firstUserId,
            seatsCount: 1);

        var confirmedBooking = Booking.CreatePending(
            eventId,
            firstUserId,
            seatsCount: 2);

        confirmedBooking.Confirm();

        var rejectedBooking = Booking.CreatePending(
            eventId,
            firstUserId,
            seatsCount: 1);

        rejectedBooking.Reject();

        var cancelledBooking = Booking.CreatePending(
            eventId,
            firstUserId,
            seatsCount: 1);

        cancelledBooking.Cancel();

        var anotherUserBooking = Booking.CreatePending(
            eventId,
            secondUserId,
            seatsCount: 1);

        await using (var context = _fixture.CreateContext())
        {
            var repository =
                new BookingRepository(context);

            await repository.AddAsync(pendingBooking);
            await repository.AddAsync(confirmedBooking);
            await repository.AddAsync(rejectedBooking);
            await repository.AddAsync(cancelledBooking);
            await repository.AddAsync(anotherUserBooking);

            await repository.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.CreateContext();

        var verificationRepository =
            new BookingRepository(verificationContext);

        var count =
            await verificationRepository
                .CountActiveByUserIdAsync(firstUserId);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetByIdAsync_WithTracking_Should_Persist_Entity_Changes()
    {
        await _fixture.ResetDatabaseAsync();

        var booking = Booking.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seatsCount: 4);

        await using (var context = _fixture.CreateContext())
        {
            var repository =
                new BookingRepository(context);

            await repository.AddAsync(booking);
            await repository.SaveChangesAsync();
        }

        await using (var context = _fixture.CreateContext())
        {
            var repository =
                new BookingRepository(context);

            var trackedBooking =
                await repository.GetByIdAsync(
                    booking.Id,
                    asNoTracking: false);

            Assert.NotNull(trackedBooking);

            trackedBooking!.Confirm();

            await repository.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.CreateContext();

        var verificationRepository =
            new BookingRepository(verificationContext);

        var result =
            await verificationRepository.GetByIdAsync(
                booking.Id);

        Assert.NotNull(result);

        Assert.Equal(
            BookingStatus.Confirmed,
            result!.Status);
    }

    private static DateTimeOffset UtcDate(
        int year,
        int month,
        int day,
        int hour,
        int minute = 0)
    {
        return new DateTimeOffset(
            year,
            month,
            day,
            hour,
            minute,
            0,
            TimeSpan.Zero);
    }
}