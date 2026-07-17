namespace EBooking.Tests;

using EBooking.Infrastructure.DataStore;
using EBooking.Application.DTO;
using EBooking.Domain.Exceptions;
using EBooking.Domain.Entities;
using EBooking.Application.Services;
using EBooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

public class BookingServiceTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();
    private sealed class TestEnvironment
    {
        public required DbContextOptions<AppDbContext> Options { get; init; }
        public required AppDbContext Context { get; init; }
        public required EventsService EventsService { get; init; }
        public required BookingService BookingService { get; init; }
        public required BookingProcessingService BookingProcessingService { get; init; }
    }

    private static TestEnvironment CreateTestEnvironment()
    {
        var dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var context = new AppDbContext(options);
        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);

        return new TestEnvironment
        {
            Options = options,
            Context = context,
            EventsService = new EventsService(eventRepository),
            BookingService = new BookingService(eventRepository, bookingRepository),
            BookingProcessingService = new BookingProcessingService(bookingRepository, eventRepository),
        };
    }

    private static BookingService CreateBookingService(DbContextOptions<AppDbContext> options)
    {
        var context = new AppDbContext(options);

        var eventRepository = new EventRepository(context);
        var bookingRepository = new BookingRepository(context);

        return new BookingService(eventRepository, bookingRepository);
    }

    private static CreateEventDto CreateValidCreateDto(
        string title = "Test Event",
        string? description = "Test Description",
        DateTime? startAt = null,
        DateTime? endAt = null,
        int totalSeats = 10)
    {
        return new CreateEventDto
        {
            Title = title,
            Description = description,
            StartAt = startAt ?? new DateTime(2030, 4, 10, 10, 0, 0, DateTimeKind.Utc),
            EndAt = endAt ?? new DateTime(2030, 4, 10, 12, 0, 0, DateTimeKind.Utc),
            TotalSeats = totalSeats
        };
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreatePendingBooking_WhenEventExists()
    {
        var environment = CreateTestEnvironment();
        var createdEvent = await environment.EventsService.CreateEventAsync(CreateValidCreateDto());

        var result = await environment.BookingService.CreateBookingAsync(createdEvent.Id, TestUserId);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(createdEvent.Id, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.Null(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateUniqueBookings_ForSameEvent()
    {
        var environment = CreateTestEnvironment();
        var createdEvent = await environment.EventsService.CreateEventAsync(CreateValidCreateDto());

        var booking1 = await environment.BookingService.CreateBookingAsync(createdEvent.Id, TestUserId);
        var booking2 = await environment.BookingService.CreateBookingAsync(createdEvent.Id, TestUserId);

        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.Equal(createdEvent.Id, booking1.EventId);
        Assert.Equal(createdEvent.Id, booking2.EventId);
        Assert.Equal(BookingStatus.Pending, booking1.Status);
        Assert.Equal(BookingStatus.Pending, booking2.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenBookingExists()
    {
        var environment = CreateTestEnvironment();
        var createdEvent = await environment.EventsService.CreateEventAsync(CreateValidCreateDto());
        var createdBooking = await environment.BookingService.CreateBookingAsync(createdEvent.Id, TestUserId);

        var result = await environment.BookingService.GetBookingByIdAsync(createdBooking.Id);

        Assert.Equal(createdBooking.Id, result.Id);
        Assert.Equal(createdBooking.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReflectConfirmedStatus_WhenBookingWasConfirmed()
    {
        var environment = CreateTestEnvironment();
        var createdEvent = await environment.EventsService.CreateEventAsync(CreateValidCreateDto());
        var createdBooking = await environment.BookingService.CreateBookingAsync(createdEvent.Id, TestUserId);

        var bookingEntity = await environment.Context.Bookings
            .FirstOrDefaultAsync(b => b.Id == createdBooking.Id);

        Assert.NotNull(bookingEntity);

        bookingEntity!.Confirm();
        await environment.Context.SaveChangesAsync();

        var result = await environment.BookingService.GetBookingByIdAsync(createdBooking.Id);

        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReflectRejectedStatus_WhenBookingWasRejected()
    {
        var environment = CreateTestEnvironment();
        var createdEvent = await environment.EventsService.CreateEventAsync(CreateValidCreateDto());
        var createdBooking = await environment.BookingService.CreateBookingAsync(createdEvent.Id, TestUserId);

        var bookingEntity = await environment.Context.Bookings
            .FirstOrDefaultAsync(b => b.Id == createdBooking.Id);

        Assert.NotNull(bookingEntity);

        bookingEntity!.Reject();
        await environment.Context.SaveChangesAsync();

        var result = await environment.BookingService.GetBookingByIdAsync(createdBooking.Id);

        Assert.Equal(BookingStatus.Rejected, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        var environment = CreateTestEnvironment();
        var missingEventId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => environment.BookingService.CreateBookingAsync(missingEventId, TestUserId));

        Assert.Contains(missingEventId.ToString(), exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNotFoundException_WhenEventWasDeleted()
    {
        var environment = CreateTestEnvironment();
        var createdEvent = await environment.EventsService.CreateEventAsync(CreateValidCreateDto());

        await environment.EventsService.DeleteEventAsync(createdEvent.Id);

        await Assert.ThrowsAsync<NotFoundException>(
            () => environment.BookingService.CreateBookingAsync(createdEvent.Id, TestUserId));
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        var environment = CreateTestEnvironment();
        var missingBookingId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => environment.BookingService.GetBookingByIdAsync(missingBookingId));

        Assert.Contains(missingBookingId.ToString(), exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldDecreaseAvailableSeats_WhenBookingCreated()
    {
        var environment = CreateTestEnvironment();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 3));

        await environment.BookingService.CreateBookingAsync(createdEvent.Id, TestUserId);

        var eventAfterBooking = await environment.EventsService.GetEventByIdAsync(createdEvent.Id);

        Assert.Equal(3, eventAfterBooking.TotalSeats);
        Assert.Equal(2, eventAfterBooking.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNoAvailableSeatsException_WhenNoSeatsAvailable()
    {
        var environment = CreateTestEnvironment();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 1));

        await environment.BookingService.CreateBookingAsync(createdEvent.Id, TestUserId);

        var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => environment.BookingService.CreateBookingAsync(createdEvent.Id, TestUserId));

        Assert.Equal("No available seats for this event", exception.Message);

        var eventAfterFailedBooking = await environment.EventsService.GetEventByIdAsync(createdEvent.Id);

        Assert.Equal(0, eventAfterFailedBooking.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldPreventOverbooking_WhenRequestsAreConcurrent()
    {
        var environment = CreateTestEnvironment();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 5));

        var tasks = Enumerable
            .Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                var bookingService = CreateBookingService(environment.Options);

                try
                {
                    var booking = await bookingService.CreateBookingAsync(createdEvent.Id, TestUserId);
                    return (Success: true, Booking: booking, Exception: (Exception?)null);
                }
                catch (Exception ex)
                {
                    return (Success: false, Booking: (BookingDto?)null, Exception: ex);
                }
            }))
            .ToList();

        var results = await Task.WhenAll(tasks);

        var successfulResults = results.Where(r => r.Success).ToList();
        var failedResults = results.Where(r => !r.Success).ToList();

        Assert.Equal(5, successfulResults.Count);
        Assert.Equal(15, failedResults.Count);

        Assert.All(failedResults, result =>
            Assert.IsType<NoAvailableSeatsException>(result.Exception));

        var eventAfterBookings = await environment.EventsService.GetEventByIdAsync(createdEvent.Id);

        Assert.Equal(0, eventAfterBookings.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateUniqueIds_WhenRequestsAreConcurrent()
    {
        var environment = CreateTestEnvironment();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 10));

        var tasks = Enumerable
            .Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                var bookingService = CreateBookingService(environment.Options);
                return await bookingService.CreateBookingAsync(createdEvent.Id, TestUserId);
            }))
            .ToList();

        var bookings = await Task.WhenAll(tasks);

        Assert.Equal(10, bookings.Length);

        var uniqueIds = bookings
            .Select(b => b.Id)
            .Distinct()
            .Count();

        Assert.Equal(10, uniqueIds);

        var eventAfterBookings = await environment.EventsService.GetEventByIdAsync(createdEvent.Id);

        Assert.Equal(0, eventAfterBookings.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrow_WhenEventAlreadyStarted()
    {
        var environment = CreateTestEnvironment();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(
                startAt: DateTime.UtcNow.AddHours(-2),
                endAt: DateTime.UtcNow.AddHours(1)));

        var exception =
            await Assert.ThrowsAsync<EventAlreadyStartedException>(
                () => environment.BookingService.CreateBookingAsync(
                    createdEvent.Id,
                    TestUserId));

        Assert.Contains(createdEvent.Id.ToString(), exception.Message);

        var eventAfterAttempt =
            await environment.EventsService.GetEventByIdAsync(createdEvent.Id);

        Assert.Equal(
            eventAfterAttempt.TotalSeats,
            eventAfterAttempt.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrow_WhenUserReachedActiveBookingLimit()
    {
        var environment = CreateTestEnvironment();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 20));

        for (var i = 0; i < 10; i++)
        {
            await environment.BookingService.CreateBookingAsync(
                createdEvent.Id,
                TestUserId);
        }

        var exception =
            await Assert.ThrowsAsync<ActiveBookingLimitExceededException>(
                () => environment.BookingService.CreateBookingAsync(
                    createdEvent.Id,
                    TestUserId));

        Assert.Equal(10, exception.Limit);
        Assert.Contains("10", exception.Message);

        var eventAfterAttempt =
            await environment.EventsService.GetEventByIdAsync(createdEvent.Id);

        Assert.Equal(10, eventAfterAttempt.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldUseIndependentLimits_ForDifferentUsers()
    {
        var environment = CreateTestEnvironment();

        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 20));

        for (var i = 0; i < 10; i++)
        {
            await environment.BookingService.CreateBookingAsync(
                createdEvent.Id,
                firstUserId);
        }

        var secondUserBooking =
            await environment.BookingService.CreateBookingAsync(
                createdEvent.Id,
                secondUserId);

        Assert.Equal(secondUserId, secondUserBooking.UserId);
        Assert.Equal(BookingStatus.Pending, secondUserBooking.Status);

        var eventAfterBookings =
            await environment.EventsService.GetEventByIdAsync(createdEvent.Id);

        Assert.Equal(9, eventAfterBookings.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldNotCountRejectedBooking_AsActive()
    {
        var environment = CreateTestEnvironment();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 20));

        var firstBooking =
            await environment.BookingService.CreateBookingAsync(
                createdEvent.Id,
                TestUserId);

        var bookingEntity = await environment.Context.Bookings
            .FirstAsync(booking => booking.Id == firstBooking.Id);

        bookingEntity.Reject();
        await environment.Context.SaveChangesAsync();

        for (var i = 0; i < 10; i++)
        {
            await environment.BookingService.CreateBookingAsync(
                createdEvent.Id,
                TestUserId);
        }

        var activeCount = await environment.Context.Bookings.CountAsync(
            booking =>
                booking.UserId == TestUserId &&
                (booking.Status == BookingStatus.Pending ||
                booking.Status == BookingStatus.Confirmed));

        Assert.Equal(10, activeCount);
    }

    [Fact]
    public async Task ProcessPendingBookingsAsync_ShouldConfirmBooking_WithoutReservingSecondSeat()
    {
        var environment = CreateTestEnvironment();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 5));

        var booking = await environment.BookingService.CreateBookingAsync(
            createdEvent.Id,
            TestUserId);

        var eventAfterCreation =
            await environment.EventsService.GetEventByIdAsync(
                createdEvent.Id);

        Assert.Equal(4, eventAfterCreation.AvailableSeats);

        await environment.BookingProcessingService
            .ProcessPendingBookingsAsync();

        var processedBooking =
            await environment.BookingService.GetBookingByIdAsync(
                booking.Id);

        var eventAfterProcessing =
            await environment.EventsService.GetEventByIdAsync(
                createdEvent.Id);

        Assert.Equal(BookingStatus.Confirmed, processedBooking.Status);
        Assert.Equal(4, eventAfterProcessing.AvailableSeats);
    }

    [Fact]
    public async Task CancelBookingAsync_ShouldCancelBooking_WhenRequestedByOwner()
    {
        var environment = CreateTestEnvironment();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 5));

        var booking = await environment.BookingService.CreateBookingAsync(
            createdEvent.Id,
            TestUserId);

        await environment.BookingService.CancelBookingAsync(
            booking.Id,
            TestUserId,
            UserRole.User);

        var cancelledBooking =
            await environment.BookingService.GetBookingByIdAsync(
                booking.Id);

        var eventAfterCancellation =
            await environment.EventsService.GetEventByIdAsync(
                createdEvent.Id);

        Assert.Equal(BookingStatus.Cancelled, cancelledBooking.Status);
        Assert.Equal(5, eventAfterCancellation.AvailableSeats);
    }

    [Fact]
    public async Task CancelBookingAsync_ShouldCancelBooking_WhenRequestedByAdmin()
    {
        var environment = CreateTestEnvironment();

        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 5));

        var booking = await environment.BookingService.CreateBookingAsync(
            createdEvent.Id,
            ownerId);

        await environment.BookingService.CancelBookingAsync(
            booking.Id,
            adminId,
            UserRole.Admin);

        var cancelledBooking =
            await environment.BookingService.GetBookingByIdAsync(
                booking.Id);

        var eventAfterCancellation =
            await environment.EventsService.GetEventByIdAsync(
                createdEvent.Id);

        Assert.Equal(BookingStatus.Cancelled, cancelledBooking.Status);
        Assert.Equal(5, eventAfterCancellation.AvailableSeats);
    }

    [Fact]
    public async Task CancelBookingAsync_ShouldThrowForbiddenOperationException_WhenRequestedByAnotherUser()
    {
        var environment = CreateTestEnvironment();

        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 5));

        var booking = await environment.BookingService.CreateBookingAsync(
            createdEvent.Id,
            ownerId);

        await Assert.ThrowsAsync<ForbiddenOperationException>(
            () => environment.BookingService.CancelBookingAsync(
                booking.Id,
                anotherUserId,
                UserRole.User));

        var bookingAfterAttempt =
            await environment.BookingService.GetBookingByIdAsync(
                booking.Id);

        var eventAfterAttempt =
            await environment.EventsService.GetEventByIdAsync(
                createdEvent.Id);

        Assert.Equal(BookingStatus.Pending, bookingAfterAttempt.Status);
        Assert.Equal(4, eventAfterAttempt.AvailableSeats);
    }

    [Fact]
    public async Task CancelBookingAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        var environment = CreateTestEnvironment();
        var missingBookingId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => environment.BookingService.CancelBookingAsync(
                missingBookingId,
                TestUserId,
                UserRole.User));

        Assert.Contains(missingBookingId.ToString(), exception.Message);
    }

    [Fact]
    public async Task CancelBookingAsync_ShouldNotReleaseSeatTwice_WhenBookingAlreadyCancelled()
    {
        var environment = CreateTestEnvironment();

        var createdEvent = await environment.EventsService.CreateEventAsync(
            CreateValidCreateDto(totalSeats: 5));

        var booking = await environment.BookingService.CreateBookingAsync(
            createdEvent.Id,
            TestUserId);

        await environment.BookingService.CancelBookingAsync(
            booking.Id,
            TestUserId,
            UserRole.User);

        await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => environment.BookingService.CancelBookingAsync(
                booking.Id,
                TestUserId,
                UserRole.User));

        var eventAfterSecondAttempt =
            await environment.EventsService.GetEventByIdAsync(
                createdEvent.Id);

        Assert.Equal(5, eventAfterSecondAttempt.AvailableSeats);
    }
}