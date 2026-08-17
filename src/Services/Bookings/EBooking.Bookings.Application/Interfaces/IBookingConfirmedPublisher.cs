namespace EBooking.Bookings.Application;

using EBooking.Contracts;

/// <summary>
/// Публикует событие подтверждения бронирования.
/// </summary>
public interface IBookingConfirmedPublisher
{
    Task PublishAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default
    );
}