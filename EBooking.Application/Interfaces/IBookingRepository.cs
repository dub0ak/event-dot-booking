namespace EBooking.Application.Interfaces;

using EBooking.Domain.Entities;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true
    );
    Task<List<Guid>> GetPendingBookingIdsAsync(CancellationToken cancellationToken);
    Task AddAsync(Booking booking);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

}