namespace EBooking.Bookings.Application;

using EBooking.Bookings.Domain;

/// <summary>
/// Представление бронирования для внешних слоёв приложения.
/// </summary>
public sealed record BookingDto(
    Guid Id,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    BookingStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt
);