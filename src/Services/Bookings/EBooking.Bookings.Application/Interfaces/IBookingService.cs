namespace EBooking.Bookings.Application;

/// <summary>
/// Сервис управления бронированиями.
/// </summary>
public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(
        Guid eventId,
        Guid userId,
        int seatsCount,
        CancellationToken cancellationToken = default
    );

    Task<BookingDto> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default
    );

    Task CancelBookingAsync(
        Guid bookingId,
        Guid requesterUserId,
        bool requesterIsAdmin,
        CancellationToken cancellationToken = default
    );
}