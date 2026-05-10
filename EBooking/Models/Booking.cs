namespace EBooking.Models;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Rejected
}

public class Booking
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    private Booking(Guid id, Guid eventId)
    {
        Id = id;
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        ProcessedAt = null;
    }

    public static Booking CreatePending(Guid eventId)
    {
        return new Booking(
            Guid.NewGuid(),
            eventId
        );
    }

    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException("Only pending bookings can be confirmed.");
        }
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException("Only pending bookings can be rejected.");
        }
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    public bool TryConfirm()
    {
        if (Status != BookingStatus.Pending)
        {
            return false;
        }

        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;

        return true;
    }
}