namespace EBooking.Services;

using EBooking.DataStore;
using EBooking.DTO;
using EBooking.Exceptions;
using EBooking.Interfaces;
using EBooking.Models;

public class BookingService : IBookingService
{
    private readonly BookingStore _bookingStore;
    private readonly IEventsService _eventsService;

    public BookingService(BookingStore bookingStore, IEventsService eventsService)
    {
        _bookingStore = bookingStore;
        _eventsService = eventsService;
    }

    public Task<BookingDto> CreateBookingAsync(Guid eventId)
    {
        _eventsService.GetEventById(eventId);

        var booking = Booking.CreatePending(eventId);
        _bookingStore.Add(booking);

        return Task.FromResult(ToDto(booking));
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