namespace EBooking.Events.Application;

/// <summary>
/// Результат обработки сообщения о подтверждённой брони.
/// </summary>
public enum BookingConfirmedHandlingResult
{
    /// <summary>
    /// Количество доступных мест успешно уменьшено.
    /// </summary>
    Processed,

    /// <summary>
    /// Мероприятие с указанным идентификатором не найдено.
    /// </summary>
    EventNotFound,

    /// <summary>
    /// У мероприятия недостаточно свободных мест.
    /// </summary>
    InsufficientSeats
}