namespace EBooking.Events.Domain;

/// <summary>
/// Ошибка нарушения правил валидации доменной модели.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) {}
}