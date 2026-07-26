namespace EBooking.Bookings.Application;

/// <summary>
/// Возникает при превышении лимита активных бронирований пользователя.
/// </summary>
public sealed class ActiveBookingLimitExceededException : Exception
{
    public ActiveBookingLimitExceededException(int limit) : base($"The active booking limit of {limit} has been reached.")
    {
        Limit = limit;
    }

    public int Limit { get; }
}