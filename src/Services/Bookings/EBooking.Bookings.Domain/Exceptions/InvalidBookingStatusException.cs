namespace EBooking.Bookings.Domain;

/// <summary>
/// Возникает при попытке выполнить недопустимый переход состояния бронирования.
/// </summary>
public sealed class InvalidBookingStatusException : Exception
{
    public InvalidBookingStatusException(
        BookingStatus currentStatus,
        string operation)
        : base(
            $"Cannot {operation} booking in '{currentStatus}' status.")
    {
        CurrentStatus = currentStatus;
        Operation = operation;
    }

    public BookingStatus CurrentStatus { get; }

    public string Operation { get; }
}