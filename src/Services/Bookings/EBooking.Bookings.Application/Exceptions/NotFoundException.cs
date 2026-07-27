namespace EBooking.Bookings.Application;

/// <summary>
/// Возникает, когда запрошенный объект не найден.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) {}
}