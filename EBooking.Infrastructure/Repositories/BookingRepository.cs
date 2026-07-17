namespace EBooking.Infrastructure.Repositories;

using EBooking.Infrastructure.DataStore;
using EBooking.Application.Interfaces;
using EBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        var query = _context.Bookings.AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            b => b.Id == id,
            cancellationToken);
    }

    public async Task<List<Guid>> GetPendingBookingIdsAsync(CancellationToken cancellationToken)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Bookings.CountAsync(
            booking =>
                booking.UserId == userId &&
                (booking.Status == BookingStatus.Pending ||
                booking.Status == BookingStatus.Confirmed),
            cancellationToken
        );
    }
}