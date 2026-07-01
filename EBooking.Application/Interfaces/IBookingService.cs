namespace EBooking.Application.Interfaces;

using EBooking.Application.DTO;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid eventId);
    Task<BookingDto> GetBookingByIdAsync(Guid bookingId);
}