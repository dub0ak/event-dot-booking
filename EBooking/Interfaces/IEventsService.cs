namespace EBooking.Interfaces;

using EBooking.DTO;

/// <summary>
/// Интерфейс сервиса для работы с мероприятиями
/// </summary>
public interface IEventsService
{
    /// <summary>
    /// Получить мероприятия с фильтрацией и пагинацией
    /// </summary>
    Task<PaginatedResult<EventDto>> GetEventsAsync(GetEventsQueryDto query);

    /// <summary>
    /// Получить мероприятие по Id
    /// </summary>
    Task<EventDto> GetEventByIdAsync(Guid id);

    /// <summary>
    /// Зарегистрировать новое мероприятие
    /// </summary>
    Task<EventDto> CreateEventAsync(CreateEventDto eventData);

    /// <summary>
    /// Обновить информацию о мероприятии
    /// </summary>
    Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto eventData);

    /// <summary>
    /// Удалить мероприятие по идентификатору
    /// </summary>
    Task DeleteEventAsync(Guid id);
}