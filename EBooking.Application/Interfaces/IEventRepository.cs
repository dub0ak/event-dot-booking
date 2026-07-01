namespace EBooking.Application.Interfaces;

using EBooking.Application.DTO;
using EBooking.Domain.Entities;

public interface IEventRepository
{
    Task<PaginatedResult<Event>> GetEventsAsync(GetEventsQueryDto query);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<Event?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true
    );
    Task AddAsync(Event eventItem);
    Task DeleteAsync(Event eventItem);
    Task SaveChangesAsync();
}