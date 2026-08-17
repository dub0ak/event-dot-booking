namespace EBooking.Events.Application;

/// <summary>
/// Ошибка отсутствия запрашиваемой доменной сущности.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) {}
}