namespace EBooking.Controllers;

using EBooking.DTO;
using EBooking.Interfaces;
using EBooking.Handlers;
using Microsoft.AspNetCore.Mvc;
using System.Net;

/// <summary>
/// Контроллер CRUD-операций для Event-модели
/// </summary>
/// <param name="_eventsService"></param>
[ApiController]
[Route("[controller]")]
public class EventsController(IEventsService _eventsService) : ControllerBase
{
    /// <summary>
    /// Получить список всех зарегистрированных мероприятий
    /// </summary>
    /// <response code="200"></response> 
    [Produces("application/json")]
    [HttpGet]
    public ActionResult<ApiResult<List<EventDto>>> GetAllEvents()
    {
        var events = _eventsService.GetAllEvents();

        return Ok(new ApiResult<List<EventDto>> {
            Data = events,
            Status = true,
            Message = "All events returned successfully"
        });
    }

    /// <summary>
    /// Получить мероприятие по Id
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <response code="200"></response> 
    [Produces("application/json")]
    [HttpGet("{id:int}")]
    public ActionResult<ApiBaseResult> GetEventById(int id)
    {
        var event_to_return = _eventsService.GetEventById(id);
        if (event_to_return == null) {
            return NotFound(new ApiResult {
                Status = false,
                Message = $"Error: Unable to find event with Id = {id}"
            });
        }
        return Ok(new ApiResult<EventDto> {
            Data = event_to_return,
            Status = true,
            Message = $"Event with Id = {id}"
        });
    }

    /// <summary>
    /// Зарегистрировать новое мероприятие
    /// </summary>
    /// <param name="eventData">Данные для регистрации</param>
    /// <response code="201"></response> 
    [Produces("application/json")]
    [HttpPost]
    public ActionResult<ApiBaseResult> CreateEvent([FromBody] CreateEventDto eventData)
    {
        var event_to_return = _eventsService.CreateEvent(eventData);
        return CreatedAtAction(
            nameof(GetEventById),
            new {id = event_to_return.Id},
            new ApiResult<EventDto>
            {
                Data = event_to_return,
                Status = true,
                Message = "New event created successfully"
            }
        );
    }

    /// <summary>
    /// Обновить информацию о мероприятии
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <param name="eventData">Данные обновления</param>
    /// <response code="200"></response> 
    [Produces("application/json")]
    [HttpPut("{id:int}")]
    public ActionResult<ApiBaseResult> UpdateEvent(int id, [FromBody] UpdateEventDto eventData)
    {
        var event_to_return = _eventsService.UpdateEvent(id, eventData);
        if (event_to_return != null) {
            return Ok(new ApiResult<EventDto> {
                Data = event_to_return,
                Status = true,
                Message = $"Event with Id = {id} updated"
            });
        }
        return NotFound(new ApiResult {
            Status = false,
            Message = $"Error: Unable to update event with Id = {id}"
        });
    }

    /// <summary>
    /// Удалить мероприятие по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <response code="204"></response> 
    [Produces("application/json")]
    [HttpDelete("{id:int}")]
    public ActionResult<ApiBaseResult> DeleteEvent(int id)
    {
        if (_eventsService.DeleteEvent(id)) {
            return NoContent();
        }
        return NotFound(new ApiResult {
            Status = false,
            Message = $"Error: Unable to delete event with Id = {id}"
        });
    }
}
