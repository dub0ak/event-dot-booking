namespace EBooking.Bookings.Tests;

using EBooking.Bookings.Application;
using EBooking.Bookings.Domain;

internal sealed class FakeBookingRepository : IBookingRepository
{
    private readonly Dictionary<Guid, Booking> _bookings = new();

    public int ActiveBookingCount { get; set; }

    public int SaveChangesCallCount { get; private set; }

    public List<string> Operations { get; } = new();

    public Booking? BookingAdded { get; private set; }

    public IReadOnlyCollection<Booking> Bookings =>
        _bookings.Values.ToArray();

    public void Seed(Booking booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        _bookings[booking.Id] = booking;
    }

    public Task<Booking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _bookings.TryGetValue(id, out var booking);

        return Task.FromResult(booking);
    }

    public Task<IReadOnlyCollection<Guid>> GetPendingBookingIdsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<Guid> ids = _bookings.Values
            .Where(booking => booking.Status == BookingStatus.Pending)
            .Select(booking => booking.Id)
            .ToArray();

        return Task.FromResult(ids);
    }

    public Task<int> CountActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(ActiveBookingCount);
    }

    public Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(booking);

        _bookings[booking.Id] = booking;
        BookingAdded = booking;
        Operations.Add("Add");

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SaveChangesCallCount++;
        Operations.Add("Save");

        return Task.CompletedTask;
    }
}