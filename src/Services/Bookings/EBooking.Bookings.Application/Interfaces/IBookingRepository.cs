namespace EBooking.Bookings.Application;

using EBooking.Bookings.Domain;

/// <summary>
/// Репозиторий бронирований.
/// </summary>
public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true
    );

    Task<IReadOnlyCollection<Guid>> GetPendingBookingIdsAsync(
        CancellationToken cancellationToken = default
    );

    Task<int> CountActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default
    );

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default
    );
}