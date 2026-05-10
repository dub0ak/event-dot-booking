namespace EBooking.Interfaces;

using EBooking.DTO;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid eventId);
    Task<BookingDto> GetBookingByIdAsync(Guid bookingId);
}