namespace EBooking.Interfaces;

using EBooking.DTO;

/// <summary>
/// Интерфейс Event-модели
/// </summary>
public interface IEventsService
{
    /// <summary>
    /// Получить все зарегистрированные мероприятия
    /// </summary>
    PaginatedResult<EventDto> GetEvents(GetEventsQueryDto query);
    /// <summary>
    /// Получить мероприятие по Id
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    EventDto GetEventById(Guid id);
    /// <summary>
    /// Зарегистрировать новое мероприятие
    /// </summary>
    /// <param name="eventData">Данные для регистрации</param>
    EventDto CreateEvent(CreateEventDto eventData);
    /// <summary>
    /// Обновить информацию о мероприятии
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <param name="eventData">Данные обновления</param>
    EventDto UpdateEvent(Guid id, UpdateEventDto eventData);
    /// <summary>
    /// Удалить мероприятие по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    void DeleteEvent(Guid id);
}