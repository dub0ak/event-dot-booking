namespace EBooking.Services;

using EBooking.DataStore;
using EBooking.DTO;
using EBooking.Exceptions;
using EBooking.Interfaces;
using EBooking.Models;

public class BookingService : IBookingService
{
    private readonly BookingStore _bookingStore;
    private readonly EventStore _eventStore;
    private readonly object _bookingLock = new();

    public BookingService(BookingStore bookingStore, EventStore eventStore)
    {
        _bookingStore = bookingStore;
        _eventStore = eventStore;
    }

    public Task<BookingDto> CreateBookingAsync(Guid eventId)
    {
        lock (_bookingLock)
        {
            var eventItem = _eventStore.GetById(eventId);

            if (eventItem is null)
            {
                throw new NotFoundException($"Event with Id = {eventId} was not found");
            }

            if (!eventItem.TryReserveSeats())
            {
                throw new NoAvailableSeatsException("No available seats for this event");
            }

            _eventStore.Update(eventItem);

            var booking = Booking.CreatePending(eventId);
            _bookingStore.Add(booking);

            return Task.FromResult(ToDto(booking));
        }
    }

    public Task<BookingDto> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = _bookingStore.GetById(bookingId);

        if (booking is null)
        {
            throw new NotFoundException($"Booking with Id = {bookingId} was not found");
        }

        return Task.FromResult(ToDto(booking));
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