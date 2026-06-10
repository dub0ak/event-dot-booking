namespace EBooking.Repositories;

using EBooking.DataStore;
using EBooking.DTO;
using EBooking.Interfaces;
using EBooking.Models;
using Microsoft.EntityFrameworkCore;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<Event>> GetEventsAsync(GetEventsQueryDto query)
    {
        IQueryable<Event> filteredEvents = _context.Events.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            var title = query.Title.ToLower();

            filteredEvents = filteredEvents.Where(e =>
                e.Title.ToLower().Contains(title));
        }

        if (query.From.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.EndAt <= query.To.Value);
        }

        var totalCount = await filteredEvents.CountAsync();

        var items = await filteredEvents
            .OrderBy(e => e.StartAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            Items = items
        };
    }

    public async Task<Event?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        var query = _context.Events.AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            e => e.Id == id,
            cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(Event eventItem)
    {
        await _context.Events.AddAsync(eventItem);
    }

    public Task DeleteAsync(Event eventItem)
    {
        _context.Events.Remove(eventItem);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}