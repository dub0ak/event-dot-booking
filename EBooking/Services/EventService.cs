namespace EBooking.Services;
using EBooking.Models;
using EBooking.DTO;
using EBooking.Interfaces;

/// <summary>
/// Сервис, реализующий работу с интерфейсом Event-модели
/// </summary>
public class EventsService : IEventsService
{
    /// <summary>
    /// Коллекция зарегистрированных мероприятий
    /// </summary>
    private static readonly List<Event> Events = [];
    private static int _nextId = 1;

    private static EventDto ToDto(Event eventItem)
    {
        return new EventDto
        {
            Id = eventItem.Id,
            Title = eventItem.Title,
            Description = eventItem.Description,
            StartAt = eventItem.StartAt,
            EndAt = eventItem.EndAt
        };
    }

    /// <summary>
    /// Получить все зарегистрированные мероприятия
    /// </summary>
    public List<EventDto> GetAllEvents()
    {
        return Events.Select(ToDto).ToList();
    }

    /// <summary>
    /// Получить мероприятие по Id
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    public EventDto? GetEventById(int id)
    {
        var eventItem = Events.FirstOrDefault(e => e.Id == id);
        return eventItem is null ? null : ToDto(eventItem);
    }

    /// <summary>
    /// Зарегистрировать новое мероприятие
    /// </summary>
    /// <param name="eventData">Данные для регистрации</param>
    public EventDto CreateEvent(CreateEventDto eventData)
    {
        var newEvent = new Event
        {
            Id = _nextId++,
            Title = eventData.Title,
            Description = eventData.Description,
            StartAt = eventData.StartAt,
            EndAt = eventData.EndAt
        };
        Events.Add(newEvent);
        return ToDto(newEvent);
    }

    /// <summary>
    /// Обновить информацию о мероприятии
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <param name="eventData">Данные обновления</param>
    public EventDto? UpdateEvent(int id, UpdateEventDto eventData)
    {
        var eventToUpdate = Events.FirstOrDefault(e => e.Id == id);
        if (eventToUpdate is null)
        {
            return null;
        }
        eventToUpdate.Title = eventData.Title;
        eventToUpdate.Description = eventData.Description;
        eventToUpdate.StartAt = eventData.StartAt;
        eventToUpdate.EndAt = eventData.EndAt;
        return ToDto(eventToUpdate);
    }

    /// <summary>
    /// Удалить мероприятие по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    public bool DeleteEvent(int id)
    {
        var eventToRemove = Events.FirstOrDefault(e => e.Id == id);
        if (eventToRemove is null)
        {
            return false;
        }
        Events.Remove(eventToRemove);
        return true;
    }
}