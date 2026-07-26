namespace EBooking.Events.Application;

/// <summary>
/// Данные для создания мероприятия.
/// </summary>
public sealed record CreateEventRequest(
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats
);