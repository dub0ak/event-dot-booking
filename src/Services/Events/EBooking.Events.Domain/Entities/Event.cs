namespace EBooking.Events.Domain;

/// <summary>
/// Мероприятие, доступное для бронирования.
/// </summary>
public class Event
{
    /// <summary>
    /// Уникальный идентификатор мероприятия.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Название мероприятия.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Описание мероприятия.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Дата и время начала мероприятия.
    /// </summary>
    public DateTime StartAt { get; private set; }

    /// <summary>
    /// Дата и время окончания мероприятия.
    /// </summary>
    public DateTime EndAt { get; private set; }

    /// <summary>
    /// Общее количество мест.
    /// </summary>
    public int TotalSeats { get; private set; }

    /// <summary>
    /// Текущее количество свободных мест.
    /// </summary>
    public int AvailableSeats { get; private set; }

    /// <summary>
    /// Создаёт новое мероприятие.
    /// </summary>
    public static Event Create(
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats)
    {
        ValidateTitle(title);
        ValidateDates(startAt, endAt);
        ValidateTotalSeats(totalSeats);

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = NormalizeDescription(description),
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }

    /// <summary>
    /// Изменяет основные сведения о мероприятии.
    /// </summary>
    public void Update(
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt)
    {
        ValidateTitle(title);
        ValidateDates(startAt, endAt);

        Title = title.Trim();
        Description = NormalizeDescription(description);
        StartAt = startAt;
        EndAt = endAt;
    }

    /// <summary>
    /// Пытается зарезервировать указанное количество мест.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если места были зарезервированы;
    /// иначе <see langword="false"/>.
    /// </returns>
    public bool TryReserveSeats(int count = 1)
    {
        ValidateSeatCount(count);
        if (AvailableSeats < count)
        {
            return false;
        }
        AvailableSeats -= count;
        return true;
    }

    /// <summary>
    /// Возвращает указанное количество мест в доступный остаток.
    /// </summary>
    public void ReleaseSeats(int count = 1)
    {
        ValidateSeatCount(count);
        AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Event title cannot be empty.");
        }
    }

    private static void ValidateDates(
        DateTime startAt,
        DateTime endAt)
    {
        if (endAt <= startAt)
        {
            throw new ValidationException("Event end date must be later than start date.");
        }
    }

    private static void ValidateTotalSeats(int totalSeats)
    {
        if (totalSeats <= 0)
        {
            throw new ValidationException("Total seats must be greater than 0.");
        }
    }

    private static void ValidateSeatCount(int count)
    {
        if (count <= 0)
        {
            throw new ValidationException("Seat count must be greater than 0.");
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private Event() {}
}