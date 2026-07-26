namespace EBooking.Application.Interfaces;

using EBooking.Application.DTO;
using EBooking.Domain.Entities;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid eventId, Guid userId);
    Task<BookingDto> GetBookingByIdAsync(Guid bookingId);
    Task CancelBookingAsync(Guid bookingId, Guid requesterUserId, UserRole requesterRole);
}