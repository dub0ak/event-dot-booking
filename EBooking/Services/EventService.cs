namespace EBooking.Services;

using EBooking.DataStore;
using EBooking.DTO;
using EBooking.Exceptions;
using EBooking.Interfaces;
using EBooking.Models;

/// <summary>
/// Сервис для работы с мероприятиями
/// </summary>
public class EventsService : IEventsService
{
    private readonly EventStore _eventStore;

    public EventsService(EventStore eventStore)
    {
        _eventStore = eventStore;
    }

    /// <summary>
    /// Получить список мероприятий с фильтрацией и пагинацией
    /// </summary>
    /// <param name="query">Параметры фильтрации и пагинации</param>
    /// <returns>Пагинированный список мероприятий</returns>
    /// <exception cref="ValidationException">
    /// Выбрасывается, если номер страницы или размер страницы некорректны
    /// </exception>
    public PaginatedResult<EventDto> GetEvents(GetEventsQueryDto query)
    {
        if (query.Page <= 0)
        {
            throw new ValidationException("Page must be greater than 0");
        }

        if (query.PageSize <= 0)
        {
            throw new ValidationException("PageSize must be greater than 0");
        }

        IEnumerable<Event> filteredEvents = _eventStore.GetAll();

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            filteredEvents = filteredEvents.Where(e =>
                e.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));
        }

        if (query.From.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.StartAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.EndAt <= query.To.Value);
        }

        var totalCount = filteredEvents.Count();

        var items = filteredEvents
            .OrderBy(e => e.StartAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ToDto)
            .ToList();

        return new PaginatedResult<EventDto>
        {
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            Items = items
        };
    }

    /// <summary>
    /// Получить мероприятие по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <returns>Найденное мероприятие</returns>
    /// <exception cref="NotFoundException">
    /// Выбрасывается, если мероприятие не найдено
    /// </exception>
    public EventDto GetEventById(Guid id)
    {
        var eventItem = _eventStore.GetById(id);

        if (eventItem is null)
        {
            throw new NotFoundException($"Event with Id = {id} was not found");
        }

        return ToDto(eventItem);
    }

    /// <summary>
    /// Создать новое мероприятие
    /// </summary>
    /// <param name="eventData">Данные нового мероприятия</param>
    /// <returns>Созданное мероприятие</returns>
    /// <exception cref="ValidationException">
    /// Выбрасывается, если дата окончания раньше или равна дате начала
    /// </exception>
    public EventDto CreateEvent(CreateEventDto eventData)
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

        _eventStore.Add(newEvent);

        return ToDto(newEvent);
    }

    /// <summary>
    /// Обновить существующее мероприятие
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <param name="eventData">Новые данные мероприятия</param>
    /// <returns>Обновлённое мероприятие</returns>
    /// <exception cref="NotFoundException">
    /// Выбрасывается, если мероприятие не найдено
    /// </exception>
    /// <exception cref="ValidationException">
    /// Выбрасывается, если дата окончания раньше или равна дате начала
    /// </exception>
    public EventDto UpdateEvent(Guid id, UpdateEventDto eventData)
    {
        ValidateEventDates(eventData.StartAt, eventData.EndAt);

        var eventToUpdate = _eventStore.GetById(id);

        if (eventToUpdate is null)
        {
            throw new NotFoundException($"Event with Id = {id} was not found");
        }

        eventToUpdate.Title = eventData.Title;
        eventToUpdate.Description = eventData.Description;
        eventToUpdate.StartAt = eventData.StartAt;
        eventToUpdate.EndAt = eventData.EndAt;

        _eventStore.Update(eventToUpdate);

        return ToDto(eventToUpdate);
    }

    /// <summary>
    /// Удалить мероприятие по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <exception cref="NotFoundException">
    /// Выбрасывается, если мероприятие не найдено
    /// </exception>
    public void DeleteEvent(Guid id)
    {
        var eventToRemove = _eventStore.GetById(id);

        if (eventToRemove is null)
        {
            throw new NotFoundException($"Event with Id = {id} was not found");
        }

        _eventStore.Delete(id);
    }

    /// <summary>
    /// Преобразовать доменную модель в DTO ответа
    /// </summary>
    /// <param name="eventItem">Модель мероприятия</param>
    /// <returns>DTO мероприятия</returns>
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

    /// <summary>
    /// Проверить корректность диапазона дат
    /// </summary>
    /// <param name="startAt">Дата начала</param>
    /// <param name="endAt">Дата окончания</param>
    /// <exception cref="ValidationException">
    /// Выбрасывается, если дата окончания раньше или равна дате начала
    /// </exception>
    private static void ValidateEventDates(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
        {
            throw new ValidationException("EndAt must be later than StartAt");
        }
    }
}