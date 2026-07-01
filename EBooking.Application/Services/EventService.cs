namespace EBooking.Application.Services;

using EBooking.Application.DTO;
using EBooking.Domain.Exceptions;
using EBooking.Application.Interfaces;
using EBooking.Domain.Entities;

/// <summary>
/// Сервис для работы с мероприятиями
/// </summary>
public class EventsService : IEventsService
{
    private readonly IEventRepository _eventRepository;

    public EventsService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    /// <summary>
    /// Получить список мероприятий с фильтрацией и пагинацией
    /// </summary>
    public async Task<PaginatedResult<EventDto>> GetEventsAsync(GetEventsQueryDto query)
    {
        if (query.Page <= 0)
        {
            throw new ValidationException("Page must be greater than 0");
        }

        if (query.PageSize <= 0)
        {
            throw new ValidationException("PageSize must be greater than 0");
        }

        var result = await _eventRepository.GetEventsAsync(query);

        return new PaginatedResult<EventDto>
        {
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            Items = result.Items
                .Select(ToDto)
                .ToList()
        };
    }

    /// <summary>
    /// Получить мероприятие по идентификатору
    /// </summary>
    public async Task<EventDto> GetEventByIdAsync(Guid id)
    {
        var eventItem = await _eventRepository.GetByIdAsync(id);

        if (eventItem is null)
        {
            throw new NotFoundException($"Event with Id = {id} was not found");
        }

        return ToDto(eventItem);
    }

    /// <summary>
    /// Создать новое мероприятие
    /// </summary>
    public async Task<EventDto> CreateEventAsync(CreateEventDto eventData)
    {
        ValidateEventDates(eventData.StartAt, eventData.EndAt);

        if (!eventData.TotalSeats.HasValue)
        {
            throw new ValidationException("TotalSeats required");
        }

        var newEvent = Event.Create(
            eventData.Title,
            eventData.Description,
            eventData.StartAt,
            eventData.EndAt,
            eventData.TotalSeats.Value
        );

        await _eventRepository.AddAsync(newEvent);
        await _eventRepository.SaveChangesAsync();

        return ToDto(newEvent);
    }

    /// <summary>
    /// Обновить существующее мероприятие
    /// </summary>
    public async Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto eventData)
    {
        ValidateEventDates(eventData.StartAt, eventData.EndAt);

        var eventToUpdate = await _eventRepository.GetByIdAsync(
            id,
            asNoTracking: false);

        if (eventToUpdate is null)
        {
            throw new NotFoundException($"Event with Id = {id} was not found");
        }

        eventToUpdate.Title = eventData.Title;
        eventToUpdate.Description = eventData.Description;
        eventToUpdate.StartAt = eventData.StartAt;
        eventToUpdate.EndAt = eventData.EndAt;

        await _eventRepository.SaveChangesAsync();

        return ToDto(eventToUpdate);
    }

    /// <summary>
    /// Удалить мероприятие по идентификатору
    /// </summary>
    public async Task DeleteEventAsync(Guid id)
    {
        var eventToRemove = await _eventRepository.GetByIdAsync(
            id,
            asNoTracking: false);

        if (eventToRemove is null)
        {
            throw new NotFoundException($"Event with Id = {id} was not found");
        }

        await _eventRepository.DeleteAsync(eventToRemove);
        await _eventRepository.SaveChangesAsync();
    }

    private static EventDto ToDto(Event eventItem)
    {
        return new EventDto
        {
            Id = eventItem.Id,
            Title = eventItem.Title,
            Description = eventItem.Description,
            StartAt = eventItem.StartAt,
            EndAt = eventItem.EndAt,
            TotalSeats = eventItem.TotalSeats,
            AvailableSeats = eventItem.AvailableSeats
        };
    }

    private static void ValidateEventDates(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
        {
            throw new ValidationException("EndAt must be later than StartAt");
        }
    }
}