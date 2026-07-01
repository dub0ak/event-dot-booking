namespace EBooking.Domain.Entities;

using EBooking.Domain.Exceptions;

/// <summary>
/// Доменная модель мероприятия
/// </summary>
public class Event
{
    /// <summary>
    /// Уникальный идентификатор мероприятия
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Название мероприятия
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Описание мероприятия
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата и время начала мероприятия
    /// </summary>
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания мероприятия
    /// </summary>
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Общее количество мест на мероприятии
    /// </summary>
    public int TotalSeats { get; private set; }

    /// <summary>
    /// Текущее количество свободных мест
    /// </summary>
    public int AvailableSeats { get; private set; }

    public static Event Create(
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats)
    {
        if (totalSeats <= 0)
        {
            throw new ValidationException("TotalSeats must be greater than 0");
        }

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }

    public bool TryReserveSeats(int count = 1)
    {
        if (count <= 0)
        {
            throw new ValidationException("Seat count must be greater than 0");
        }

        if (AvailableSeats < count)
        {
            return false;
        }

        AvailableSeats -= count;
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        if (count <= 0)
        {
            throw new ValidationException("Seat count must be greater than 0");
        }

        AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
    }

    private Event()
    {
        Title = null!;
    }

    public ICollection<Booking> Bookings { get; private set; } = [];
}