namespace EBooking.Contracts;

/// <summary>
/// Событие, публикуемое после подтверждения бронирования.
/// </summary>
/// <param name="BookingId">Идентификатор бронирования.</param>
/// <param name="EventId">Идентификатор мероприятия.</param>
/// <param name="UserId">Идентификатор пользователя.</param>
/// <param name="SeatsCount">Количество подтверждённых мест.</param>
/// <param name="ConfirmedAt">Момент подтверждения в UTC.</param>
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTimeOffset ConfirmedAt
);