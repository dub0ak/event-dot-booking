namespace EBooking.Events.Tests.Application;

using EBooking.Events.Application;
using EBooking.Events.Domain;

/// <summary>
/// Тестовая реализация репозитория мероприятий.
///
/// Хранит данные в памяти и фиксирует обращения сервиса
/// к методам репозитория.
/// </summary>
internal sealed class FakeEventRepository : IEventRepository
{
    private readonly List<Event> _events = [];
    public IReadOnlyCollection<Event> Events => _events;
    public GetEventsQuery? LastQuery { get; private set; }
    public Guid? LastRequestedId { get; private set; }
    public bool? LastAsNoTracking { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }
    public Event? LastAddedEvent { get; private set; }
    public Event? LastDeletedEvent { get; private set; }
    public int GetEventsCallCount { get; private set; }
    public int GetByIdCallCount { get; private set; }
    public int AddCallCount { get; private set; }
    public int DeleteCallCount { get; private set; }
    public int SaveChangesCallCount { get; private set; }
    public PaginatedResult<Event>? GetEventsResult { get; set; }
    public Event? GetByIdResult { get; set; }
    public void Seed(params Event[] events)
    {
        _events.AddRange(events);
    }

    public Task<PaginatedResult<Event>> GetEventsAsync(
        GetEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        GetEventsCallCount++;
        LastQuery = query;
        LastCancellationToken = cancellationToken;

        if (GetEventsResult is not null)
        {
            return Task.FromResult(GetEventsResult);
        }

        return Task.FromResult(
            new PaginatedResult<Event>(
                _events.Count,
                query.Page,
                query.PageSize,
                _events.ToArray()));
    }

    public Task<Event?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        GetByIdCallCount++;
        LastRequestedId = id;
        LastAsNoTracking = asNoTracking;
        LastCancellationToken = cancellationToken;

        var result = GetByIdResult ??
            _events.SingleOrDefault(eventItem => eventItem.Id == id);

        return Task.FromResult(result);
    }

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LastRequestedId = id;
        LastCancellationToken = cancellationToken;

        return Task.FromResult(
            _events.Any(eventItem => eventItem.Id == id));
    }

    public Task AddAsync(
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        AddCallCount++;
        LastAddedEvent = eventItem;
        LastCancellationToken = cancellationToken;

        _events.Add(eventItem);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Event eventItem,
        CancellationToken cancellationToken = default)
    {
        DeleteCallCount++;
        LastDeletedEvent = eventItem;
        LastCancellationToken = cancellationToken;

        _events.Remove(eventItem);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        LastCancellationToken = cancellationToken;

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<Event> GetTopEventsResult { get; set; } = Array.Empty<Event>();

    public int GetTopEventsCallCount { get; private set; }

    public Task<IReadOnlyCollection<Event>> GetTopEventsAsync(
        CancellationToken cancellationToken = default)
    {
        GetTopEventsCallCount++;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(GetTopEventsResult);
    }
}