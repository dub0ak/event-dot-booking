namespace EBooking.Events.Infrastructure;

using EBooking.Events.Application;
using EBooking.Events.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Репозиторий мероприятий на основе Entity Framework Core.
/// </summary>
public sealed class EventRepository : IEventRepository
{
    private readonly EventDbContext _context;

    public EventRepository(EventDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<PaginatedResult<Event>> GetEventsAsync(
        GetEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Event> events = _context.Events.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            var normalizedTitle = query.Title.Trim();

            events = events.Where(eventItem =>
                EF.Functions.ILike(
                    eventItem.Title,
                    $"%{normalizedTitle}%"));
        }

        if (query.From.HasValue)
        {
            events = events.Where(eventItem => eventItem.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            events = events.Where(eventItem => eventItem.EndAt <= query.To.Value);
        }

        var totalCount = await events.CountAsync(cancellationToken);

        var items = await events
            .OrderBy(eventItem => eventItem.StartAt)
            .ThenBy(eventItem => eventItem.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PaginatedResult<Event>(
            totalCount,
            query.Page,
            query.PageSize,
            items
        );
    }

    public async Task<Event?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        IQueryable<Event> query = _context.Events;

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            eventItem => eventItem.Id == id,
            cancellationToken
        );
    }

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.Events.AnyAsync(
            eventItem => eventItem.Id == id,
            cancellationToken
        );
    }

    public async Task AddAsync(
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventItem);

        await _context.Events.AddAsync(
            eventItem,
            cancellationToken
        );
    }

    public Task DeleteAsync(
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventItem);
        cancellationToken.ThrowIfCancellationRequested();

        _context.Events.Remove(eventItem);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}