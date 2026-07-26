namespace EBooking.Events.Application;

using EBooking.Contracts;

/// <summary>
/// Обрабатывает сообщения о подтверждённых бронированиях.
/// </summary>
public interface IBookingConfirmedHandler
{
    /// <summary>
    /// Уменьшает количество доступных мест у соответствующего мероприятия.
    /// </summary>
    Task<BookingConfirmedHandlingResult> HandleAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default
    );
}