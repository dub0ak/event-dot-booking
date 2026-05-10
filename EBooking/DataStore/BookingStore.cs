namespace EBooking.DataStore;

using EBooking.Models;

public class BookingStore
{
    private readonly List<Booking> _bookings = new();
    private readonly object _sync = new();

    public Booking Add(Booking booking)
    {
        lock (_sync)
        {
            _bookings.Add(booking);
            return booking;
        }
    }

    public void Update(Booking booking)
    {
        lock (_sync)
        {
            var index = _bookings.FindIndex(b => b.Id == booking.Id);

            if (index == -1)
            {
                return;
            }

            _bookings[index] = booking;
        }
    }

    public Booking? GetById(Guid id)
    {
        lock (_sync)
        {
            return _bookings.FirstOrDefault(b => b.Id == id);
        }
    }

    public List<Booking> GetPending()
    {
        lock (_sync)
        {
            return _bookings
                .Where(b => b.Status == BookingStatus.Pending)
                .ToList();
        }
    }
}