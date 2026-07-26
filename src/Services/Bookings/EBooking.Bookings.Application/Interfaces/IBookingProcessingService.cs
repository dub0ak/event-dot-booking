namespace EBooking.Bookings.Application;

/// <summary>
/// Обрабатывает ожидающие подтверждения бронирования.
/// </summary>
public interface IBookingProcessingService
{
    Task ProcessPendingBookingsAsync(CancellationToken cancellationToken = default);
}