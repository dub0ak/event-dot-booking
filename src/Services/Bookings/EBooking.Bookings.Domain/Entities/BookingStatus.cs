namespace EBooking.Bookings.Domain;

/// <summary>
/// Состояние бронирования.
/// </summary>
public enum BookingStatus
{
    Pending,
    Confirmed,
    Rejected,
    Cancelled
}