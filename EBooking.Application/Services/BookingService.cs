namespace EBooking.Application.Services;

using EBooking.Application.DTO;
using EBooking.Domain.Exceptions;
using EBooking.Application.Interfaces;
using EBooking.Domain.Entities;

public class BookingService : IBookingService
{
    private const int MaxActiveBookingsPerUser = 10;
    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);
    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;

    public BookingService(
        IEventRepository eventRepository,
        IBookingRepository bookingRepository)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<BookingDto> CreateBookingAsync(Guid eventId, Guid userId)
    {
        await BookingSemaphore.WaitAsync();

        try
        {
            var eventItem = await _eventRepository.GetByIdAsync(
                eventId,
                asNoTracking: false);

            if (eventItem is null)
            {
                throw new NotFoundException(
                    $"Event with Id = {eventId} was not found");
            }

            if (eventItem.StartAt <= DateTime.UtcNow)
            {
                throw new EventAlreadyStartedException(eventId);
            }

            var activeBookingCount =
                await _bookingRepository.CountActiveByUserIdAsync(userId);

            if (activeBookingCount >= MaxActiveBookingsPerUser)
            {
                throw new ActiveBookingLimitExceededException(
                    MaxActiveBookingsPerUser);
            }

            if (!eventItem.TryReserveSeats())
            {
                throw new NoAvailableSeatsException(
                    "No available seats for this event");
            }

            var booking = Booking.CreatePending(eventId, userId);

            await _bookingRepository.AddAsync(booking);
            await _bookingRepository.SaveChangesAsync();

            return ToDto(booking);
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    public async Task<BookingDto> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);

        if (booking is null)
        {
            throw new NotFoundException($"Booking with Id = {bookingId} was not found");
        }

        return ToDto(booking);
    }

    private static BookingDto ToDto(Booking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            UserId = booking.UserId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
    }

    public async Task CancelBookingAsync(
        Guid bookingId,
        Guid requesterUserId,
        UserRole requesterRole)
    {
        var booking = await _bookingRepository.GetByIdAsync(
            bookingId,
            asNoTracking: false);

        if (booking is null)
        {
            throw new NotFoundException(
                $"Booking with Id = {bookingId} was not found");
        }

        var isOwner = booking.UserId == requesterUserId;
        var isAdmin = requesterRole == UserRole.Admin;

        if (!isOwner && !isAdmin)
        {
            throw new ForbiddenOperationException(
                "You cannot cancel another user's booking");
        }

        var eventEntity = await _eventRepository.GetByIdAsync(
            booking.EventId,
            asNoTracking: false);

        if (eventEntity is null)
        {
            throw new NotFoundException(
                $"Event with Id = {booking.EventId} was not found");
        }

        booking.Cancel();
        eventEntity.ReleaseSeats();

        await _bookingRepository.SaveChangesAsync();
    }
}