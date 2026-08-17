namespace EBooking.Events.Application;

using EBooking.Events.Domain;

/// <summary>
/// Порт доступа к хранилищу мероприятий.
/// </summary>
public interface IEventRepository
{
    Task<PaginatedResult<Event>> GetEventsAsync(
        GetEventsQuery query,
        CancellationToken cancellationToken = default
    );

    Task<Event?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true
    );

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(
        Event eventItem,
        CancellationToken cancellationToken = default
    );

    Task DeleteAsync(
        Event eventItem,
        CancellationToken cancellationToken = default
    );

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Event>> GetTopEventsAsync(CancellationToken cancellationToken = default);
}