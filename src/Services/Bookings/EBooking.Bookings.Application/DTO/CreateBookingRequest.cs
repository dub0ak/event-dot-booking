namespace EBooking.Bookings.Application;

/// <summary>
/// Запрос на создание бронирования.
/// </summary>
public sealed record CreateBookingRequest(
    Guid EventId,
    int SeatsCount = 1
);