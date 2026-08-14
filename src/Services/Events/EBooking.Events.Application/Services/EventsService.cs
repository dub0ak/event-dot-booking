namespace EBooking.Events.Application;

using EBooking.Events.Domain;


/// <summary>
/// Реализует сценарии управления мероприятиями.
/// </summary>
public sealed class EventsService : IEventsService
{
    private const int MaximumPageSize = 100;

    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;

    public EventsService(
        IEventRepository eventRepository,
        ICacheService cacheService,
        CacheOptions cacheOptions) {
        ArgumentNullException.ThrowIfNull(eventRepository);
        ArgumentNullException.ThrowIfNull(cacheService);
        ArgumentNullException.ThrowIfNull(cacheOptions);

        _eventRepository = eventRepository;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions;
    }

    public async Task<PaginatedResult<EventDto>> GetEventsAsync(
        GetEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        ValidateQuery(query);

        var result = await _eventRepository.GetEventsAsync(
            query,
            cancellationToken);

        return new PaginatedResult<EventDto>(
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.Items
                .Select(ToDto)
                .ToArray()
        );
    }

    public async Task<EventDto> GetEventByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        var cacheKey = CacheKeys.Event(id);

        var cachedEvent = await _cacheService.GetAsync<EventDto>(
            cacheKey,
            cancellationToken);

        if (cachedEvent is not null)
        {
            return cachedEvent;
        }

        var eventItem = await _eventRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (eventItem is null)
        {
            throw CreateNotFoundException(id);
        }

        var eventDto = ToDto(eventItem);

        await _cacheService.SetAsync(
            cacheKey,
            eventDto,
            TimeSpan.FromMinutes(_cacheOptions.EventTtlMinutes),
            cancellationToken
        );

        return eventDto;
    }

    public async Task<EventDto> CreateEventAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var eventItem = Event.Create(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats);

        await _eventRepository.AddAsync(
            eventItem,
            cancellationToken);

        await _eventRepository.SaveChangesAsync(
            cancellationToken);

        return ToDto(eventItem);
    }

    public async Task<EventDto> UpdateEventAsync(
        Guid id,
        UpdateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        ArgumentNullException.ThrowIfNull(request);

        var eventItem = await _eventRepository.GetByIdAsync(
            id,
            cancellationToken,
            asNoTracking: false);

        if (eventItem is null)
        {
            throw CreateNotFoundException(id);
        }

        eventItem.Update(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt
        );

        await _eventRepository.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.Event(id), cancellationToken);

        return ToDto(eventItem);
    }

    public async Task DeleteEventAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        var eventItem = await _eventRepository.GetByIdAsync(
            id,
            cancellationToken,
            asNoTracking: false);

        if (eventItem is null)
        {
            throw CreateNotFoundException(id);
        }

        await _eventRepository.DeleteAsync(eventItem, cancellationToken);
        await _eventRepository.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.Event(id), cancellationToken);
    }

    private static EventDto ToDto(Event eventItem)
    {
        return new EventDto(
            eventItem.Id,
            eventItem.Title,
            eventItem.Description,
            eventItem.StartAt,
            eventItem.EndAt,
            eventItem.TotalSeats,
            eventItem.AvailableSeats
        );
    }

    private static void ValidateQuery(GetEventsQuery query)
    {
        if (query.Page <= 0)
        {
            throw new ValidationException(
                "Page must be greater than 0.");
        }

        if (query.PageSize <= 0)
        {
            throw new ValidationException(
                "PageSize must be greater than 0.");
        }

        if (query.PageSize > MaximumPageSize)
        {
            throw new ValidationException(
                $"PageSize must not exceed {MaximumPageSize}.");
        }

        if (query.From.HasValue &&
            query.To.HasValue &&
            query.To.Value < query.From.Value)
        {
            throw new ValidationException(
                "To must be greater than or equal to From.");
        }
    }

    private static void ValidateId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ValidationException(
                "Event Id cannot be empty.");
        }
    }

    private static NotFoundException CreateNotFoundException(Guid id)
    {
        return new NotFoundException(
            $"Event with Id = {id} was not found.");
    }
}