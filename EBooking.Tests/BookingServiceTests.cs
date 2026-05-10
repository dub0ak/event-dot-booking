namespace EBooking.Tests;

using EBooking.DataStore;
using EBooking.DTO;
using EBooking.Exceptions;
using EBooking.Interfaces;
using EBooking.Models;
using EBooking.Services;


public class BookingServiceTests
{
    private static IEventsService CreateEventsService()
    {
        return new EventsService();
    }

    private static BookingStore CreateBookingStore()
    {
        return new BookingStore();
    }

    private static BookingService CreateBookingService(
        BookingStore bookingStore,
        IEventsService eventsService)
    {
        return new BookingService(bookingStore, eventsService);
    }

    private static CreateEventDto CreateValidCreateDto(
        string title = "Test Event",
        string? description = "Test Description",
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        return new CreateEventDto
        {
            Title = title,
            Description = description,
            StartAt = startAt ?? new DateTime(2026, 4, 10, 10, 0, 0),
            EndAt = endAt ?? new DateTime(2026, 4, 10, 12, 0, 0)
        };
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreatePendingBooking_WhenEventExists()
    {
        var eventsService = CreateEventsService();
        var bookingStore = CreateBookingStore();
        var bookingService = CreateBookingService(bookingStore, eventsService);

        var createdEvent = eventsService.CreateEvent(CreateValidCreateDto());

        var result = await bookingService.CreateBookingAsync(createdEvent.Id);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(createdEvent.Id, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.Null(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateUniqueBookings_ForSameEvent()
    {
        var eventsService = CreateEventsService();
        var bookingStore = CreateBookingStore();
        var bookingService = CreateBookingService(bookingStore, eventsService);

        var createdEvent = eventsService.CreateEvent(CreateValidCreateDto());

        var booking1 = await bookingService.CreateBookingAsync(createdEvent.Id);
        var booking2 = await bookingService.CreateBookingAsync(createdEvent.Id);

        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.Equal(createdEvent.Id, booking1.EventId);
        Assert.Equal(createdEvent.Id, booking2.EventId);
        Assert.Equal(BookingStatus.Pending, booking1.Status);
        Assert.Equal(BookingStatus.Pending, booking2.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenBookingExists()
    {
        var eventsService = CreateEventsService();
        var bookingStore = CreateBookingStore();
        var bookingService = CreateBookingService(bookingStore, eventsService);

        var createdEvent = eventsService.CreateEvent(CreateValidCreateDto());
        var createdBooking = await bookingService.CreateBookingAsync(createdEvent.Id);

        var result = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        Assert.Equal(createdBooking.Id, result.Id);
        Assert.Equal(createdBooking.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReflectConfirmedStatus_WhenBookingWasConfirmed()
    {
        var eventsService = CreateEventsService();
        var bookingStore = CreateBookingStore();
        var bookingService = CreateBookingService(bookingStore, eventsService);

        var createdEvent = eventsService.CreateEvent(CreateValidCreateDto());
        var createdBooking = await bookingService.CreateBookingAsync(createdEvent.Id);

        var bookingEntity = bookingStore.GetById(createdBooking.Id);
        Assert.NotNull(bookingEntity);

        bookingEntity!.Confirm();

        var result = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReflectRejectedStatus_WhenBookingWasRejected()
    {
        var eventsService = CreateEventsService();
        var bookingStore = CreateBookingStore();
        var bookingService = CreateBookingService(bookingStore, eventsService);

        var createdEvent = eventsService.CreateEvent(CreateValidCreateDto());
        var createdBooking = await bookingService.CreateBookingAsync(createdEvent.Id);

        var bookingEntity = bookingStore.GetById(createdBooking.Id);
        Assert.NotNull(bookingEntity);

        bookingEntity!.Reject();

        var result = await bookingService.GetBookingByIdAsync(createdBooking.Id);

        Assert.Equal(BookingStatus.Rejected, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        var eventsService = CreateEventsService();
        var bookingStore = CreateBookingStore();
        var bookingService = CreateBookingService(bookingStore, eventsService);
        var missingEventId = Guid.NewGuid();

        var action = async () => await bookingService.CreateBookingAsync(missingEventId);

        var exception = await Assert.ThrowsAsync<NotFoundException>(action);
        Assert.Contains(missingEventId.ToString(), exception.Message);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowNotFoundException_WhenEventWasDeleted()
    {
        var eventsService = CreateEventsService();
        var bookingStore = CreateBookingStore();
        var bookingService = CreateBookingService(bookingStore, eventsService);

        var createdEvent = eventsService.CreateEvent(CreateValidCreateDto());
        eventsService.DeleteEvent(createdEvent.Id);

        var action = async () => await bookingService.CreateBookingAsync(createdEvent.Id);

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        var eventsService = CreateEventsService();
        var bookingStore = CreateBookingStore();
        var bookingService = CreateBookingService(bookingStore, eventsService);
        var missingBookingId = Guid.NewGuid();

        var action = async () => await bookingService.GetBookingByIdAsync(missingBookingId);

        var exception = await Assert.ThrowsAsync<NotFoundException>(action);
        Assert.Contains(missingBookingId.ToString(), exception.Message);
    }
}