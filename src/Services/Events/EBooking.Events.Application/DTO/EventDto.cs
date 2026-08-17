namespace EBooking.Events.Application;

/// <summary>
/// Представление мероприятия для внешних потребителей.
/// </summary>
public sealed record EventDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats,
    int AvailableSeats
);