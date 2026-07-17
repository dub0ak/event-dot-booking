namespace EBooking.Domain.Entities;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Rejected,
    Cancelled
}

public class Booking
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    private Booking(
        Guid id,
        Guid eventId,
        Guid userId)
    {
        Id = id;
        EventId = eventId;
        UserId = userId;

        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public static Booking CreatePending(Guid eventId, Guid userId)
    {
        return new Booking(
            Guid.NewGuid(),
            eventId,
            userId
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

    public void Cancel()
    {
        if (Status != BookingStatus.Pending && Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "Only active bookings can be cancelled."
            );
        }

        Status = BookingStatus.Cancelled;
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

    private Booking()
    {
    }

    public Event? Event { get; private set; }

    public User? User { get; private set; }
}