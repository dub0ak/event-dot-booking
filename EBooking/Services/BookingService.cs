namespace EBooking.Services;

using EBooking.DataStore;
using EBooking.DTO;
using EBooking.Exceptions;
using EBooking.Interfaces;
using EBooking.Models;
using Microsoft.EntityFrameworkCore;

public class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);

    private readonly AppDbContext _context;

    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BookingDto> CreateBookingAsync(Guid eventId)
    {
        await BookingSemaphore.WaitAsync();

        try
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventItem is null)
            {
                throw new NotFoundException($"Event with Id = {eventId} was not found");
            }

            if (!eventItem.TryReserveSeats())
            {
                throw new NoAvailableSeatsException("No available seats for this event");
            }

            var booking = Booking.CreatePending(eventId);

            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            return ToDto(booking);
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    public async Task<BookingDto> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId);

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