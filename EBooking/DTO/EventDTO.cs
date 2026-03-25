namespace EBooking.DTO;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO для возврата данных мероприятия клиенту
/// </summary>
public class EventDto
{
    /// <summary>
    /// Идентификатор мероприятия
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Название мероприятия
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание мероприятия
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата и время начала
    /// </summary>
    public DateTime? StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания
    /// </summary>
    public DateTime? EndAt { get; set; }
}

/// <summary>
/// DTO для создания мероприятия
/// </summary>
public class CreateEventDto : IValidatableObject
{
    /// <summary>
    /// Название мероприятия
    /// </summary>
    [Required(ErrorMessage = "Title required")]
    [MinLength(1, ErrorMessage = "Title cannot be empty")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание мероприятия
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата и время начала
    /// </summary>
    [Required(ErrorMessage = "StartAt required")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания
    /// </summary>
    [Required(ErrorMessage = "EndAt required")]
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Валидатор даты
    /// </summary>
    /// <param name="validationContext"></param>
    /// <returns></returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var now = DateTime.Now;
        if (EndAt <= now || StartAt <= now) {
            yield return new ValidationResult(
                "EndAt or StartAt must be in future",
                [nameof(EndAt), nameof(StartAt)]
            );
        }
        if (EndAt <= StartAt)
        {
            yield return new ValidationResult(
                "EndAt must be later than StartAt",
                [nameof(EndAt), nameof(StartAt)]
            );
        }
    }
}

/// <summary>
/// DTO для обновления мероприятия
/// </summary>
public class UpdateEventDto : IValidatableObject
{
    /// <summary>
    /// Название мероприятия
    /// </summary>
    [Required(ErrorMessage = "Title required")]
    [MinLength(1, ErrorMessage = "Title cannot be empty")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание мероприятия
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата и время начала
    /// </summary>
    [Required(ErrorMessage = "StartAt required")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата и время окончания
    /// </summary>
    [Required(ErrorMessage = "EndAt required")]
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Валидатор даты
    /// </summary>
    /// <param name="validationContext"></param>
    /// <returns></returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var now = DateTime.Now;
        if (EndAt <= now || StartAt <= now) {
            yield return new ValidationResult(
                "EndAt or StartAt must be in future",
                [nameof(EndAt), nameof(StartAt)]
            );
        }
        if (EndAt <= StartAt) {
            yield return new ValidationResult(
                "EndAt must be later than StartAt",
                [nameof(EndAt), nameof(StartAt)]
            );
        }
    }
}