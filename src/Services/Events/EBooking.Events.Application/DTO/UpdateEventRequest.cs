namespace EBooking.Events.Application;

/// <summary>
/// Данные для обновления мероприятия.
/// </summary>
public sealed record UpdateEventRequest(
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt
);