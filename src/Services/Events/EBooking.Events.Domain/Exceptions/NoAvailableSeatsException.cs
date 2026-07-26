namespace EBooking.Events.Domain;

/// <summary>
/// Ошибка отсутствия достаточного количества свободных мест.
/// </summary>
public class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException(string message) : base(message) {}
}