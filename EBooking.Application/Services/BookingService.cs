namespace EBooking.Application.Services;

using EBooking.Application.DTO;
using EBooking.Domain.Exceptions;
using EBooking.Application.Interfaces;
using EBooking.Domain.Entities;

public class BookingService : IBookingService
{
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

    public async Task<BookingDto> CreateBookingAsync(Guid eventId)
    {
        await BookingSemaphore.WaitAsync();

        try
        {
            var eventItem = await _eventRepository.GetByIdAsync(
                eventId,
                asNoTracking: false);

            if (eventItem is null)
            {
                throw new NotFoundException($"Event with Id = {eventId} was not found");
            }

            if (!eventItem.TryReserveSeats())
            {
                throw new NoAvailableSeatsException("No available seats for this event");
            }

            var booking = Booking.CreatePending(eventId);

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
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
    }
}