namespace EBooking.Bookings.Domain;

/// <summary>
/// Бронирование пользователя на мероприятие.
/// </summary>
public sealed class Booking
{
    private Booking()
    {
    }

    private Booking(
        Guid id,
        Guid eventId,
        Guid userId,
        int seatsCount,
        DateTimeOffset createdAt)
    {
        Id = id;
        EventId = eventId;
        UserId = userId;
        SeatsCount = seatsCount;
        Status = BookingStatus.Pending;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Идентификатор бронирования.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Идентификатор мероприятия в сервисе Events.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Идентификатор пользователя в сервисе Users.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Количество забронированных мест.
    /// </summary>
    public int SeatsCount { get; private set; }

    /// <summary>
    /// Текущее состояние бронирования.
    /// </summary>
    public BookingStatus Status { get; private set; }

    /// <summary>
    /// Момент создания бронирования.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Момент подтверждения, отклонения или отмены бронирования.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>
    /// Создаёт новое бронирование в состоянии Pending.
    /// </summary>
    public static Booking CreatePending(
        Guid eventId,
        Guid userId,
        int seatsCount,
        DateTimeOffset? createdAt = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException(
                "Event identifier cannot be empty.",
                nameof(eventId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User identifier cannot be empty.",
                nameof(userId));
        }

        if (seatsCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(seatsCount),
                seatsCount,
                "Seats count must be greater than zero.");
        }

        return new Booking(
            Guid.NewGuid(),
            eventId,
            userId,
            seatsCount,
            createdAt ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Подтверждает ожидающее бронирование.
    /// </summary>
    public void Confirm(DateTimeOffset? processedAt = null)
    {
        EnsureStatus(
            BookingStatus.Pending,
            "confirm");

        Status = BookingStatus.Confirmed;
        ProcessedAt = processedAt ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Отклоняет ожидающее бронирование.
    /// </summary>
    public void Reject(DateTimeOffset? processedAt = null)
    {
        EnsureStatus(
            BookingStatus.Pending,
            "reject");

        Status = BookingStatus.Rejected;
        ProcessedAt = processedAt ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Отменяет активное бронирование.
    /// </summary>
    public void Cancel(DateTimeOffset? processedAt = null)
    {
        if (Status is not BookingStatus.Pending
            and not BookingStatus.Confirmed)
        {
            throw new InvalidBookingStatusException(
                Status,
                "cancel");
        }

        Status = BookingStatus.Cancelled;
        ProcessedAt = processedAt ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Пытается подтвердить ожидающее бронирование.
    /// </summary>
    public bool TryConfirm(DateTimeOffset? processedAt = null)
    {
        if (Status != BookingStatus.Pending)
        {
            return false;
        }

        Status = BookingStatus.Confirmed;
        ProcessedAt = processedAt ?? DateTimeOffset.UtcNow;

        return true;
    }

    private void EnsureStatus(
        BookingStatus requiredStatus,
        string operation)
    {
        if (Status != requiredStatus)
        {
            throw new InvalidBookingStatusException(
                Status,
                operation);
        }
    }
}