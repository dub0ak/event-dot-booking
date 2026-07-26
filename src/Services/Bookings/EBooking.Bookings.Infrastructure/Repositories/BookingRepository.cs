namespace EBooking.Bookings.Infrastructure;

using EBooking.Bookings.Application;
using EBooking.Bookings.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Репозиторий бронирований на EF Core.
/// </summary>
public sealed class BookingRepository : IBookingRepository
{
    private readonly BookingsDbContext _dbContext;

    public BookingRepository(
        BookingsDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<Booking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        IQueryable<Booking> query =
            _dbContext.Bookings;

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(
            booking => booking.Id == id,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>>
        GetPendingBookingIdsAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Where(
                booking =>
                    booking.Status == BookingStatus.Pending)
            .OrderBy(booking => booking.CreatedAt)
            .Select(booking => booking.Id)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Bookings.CountAsync(
            booking =>
                booking.UserId == userId
                && (
                    booking.Status == BookingStatus.Pending
                    || booking.Status == BookingStatus.Confirmed
                ),
            cancellationToken);
    }

    public async Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(booking);

        await _dbContext.Bookings.AddAsync(
            booking,
            cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}