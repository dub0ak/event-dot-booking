namespace EBooking.Events.Application;

/// <summary>
/// Сценарии работы с мероприятиями.
/// </summary>
public interface IEventsService
{
    Task<PaginatedResult<EventDto>> GetEventsAsync(
        GetEventsQuery query,
        CancellationToken cancellationToken = default
    );

    Task<EventDto> GetEventByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<EventDto> CreateEventAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken = default
    );

    Task<EventDto> UpdateEventAsync(
        Guid id,
        UpdateEventRequest request,
        CancellationToken cancellationToken = default
    );

    Task DeleteEventAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyCollection<EventDto>> GetTopEventsAsync(
        CancellationToken cancellationToken = default
    );
}