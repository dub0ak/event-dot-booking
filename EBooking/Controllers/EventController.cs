namespace EBooking.Controllers;

using EBooking.Application.DTO;
using EBooking.Handlers;
using EBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

/// <summary>
/// Контроллер для работы с мероприятиями
/// </summary>
[ApiController]
[Route("[controller]")]
public class EventsController(IEventsService eventsService, IBookingService bookingService) : ControllerBase
{
    private readonly IEventsService _eventsService = eventsService;
    private readonly IBookingService _bookingService = bookingService;

    /// <summary>
    /// Получить список мероприятий с фильтрацией и пагинацией
    /// </summary>
    /// <param name="query">Параметры фильтрации и пагинации</param>
    /// <returns>Список мероприятий</returns>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResult<PaginatedResult<EventDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResult<PaginatedResult<EventDto>>>> GetEvents([FromQuery] GetEventsQueryDto query)
    {
        var result = await _eventsService.GetEventsAsync(query);

        return Ok(new ApiResult<PaginatedResult<EventDto>>
        {
            Status = true,
            Message = "Events returned successfully",
            Data = result
        });
    }

    /// <summary>
    /// Получить мероприятие по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <returns>Найденное мероприятие</returns>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResult<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResult<EventDto>>> GetEventById(Guid id)
    {
        var eventToReturn = await _eventsService.GetEventByIdAsync(id);

        return Ok(new ApiResult<EventDto>
        {
            Status = true,
            Message = $"Event with Id = {id} returned successfully",
            Data = eventToReturn
        });
    }

    /// <summary>
    /// Создать новое мероприятие
    /// </summary>
    /// <param name="eventData">Данные нового мероприятия</param>
    /// <returns>Созданное мероприятие</returns>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResult<BookingDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResult<EventDto>>> CreateEvent([FromBody] CreateEventDto eventData)
    {
        var createdEvent = await _eventsService.CreateEventAsync(eventData);

        return CreatedAtAction(
            nameof(GetEventById),
            new { id = createdEvent.Id },
            new ApiResult<EventDto>
            {
                Status = true,
                Message = "Event created successfully",
                Data = createdEvent
            });
    }

    /// <summary>
    /// Обновить существующее мероприятие
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <param name="eventData">Новые данные мероприятия</param>
    /// <returns>Обновлённое мероприятие</returns>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResult<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResult<EventDto>>> UpdateEvent(Guid id, [FromBody] UpdateEventDto eventData)
    {
        var updatedEvent = await _eventsService.UpdateEventAsync(id, eventData);

        return Ok(new ApiResult<EventDto>
        {
            Status = true,
            Message = $"Event with Id = {id} updated successfully",
            Data = updatedEvent
        });
    }

    /// <summary>
    /// Удалить мероприятие
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        await _eventsService.DeleteEventAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Создать бронь для мероприятия
    /// </summary>
    /// <param name="id">Идентификатор мероприятия</param>
    /// <returns>Созданная бронь</returns>
    [Authorize]
    [HttpPost("{id:guid}/book")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResult<BookingDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResult<BookingDto>>> CreateBooking(Guid id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var booking = await _bookingService.CreateBookingAsync(id, userId);
        return Accepted(
            Url.Action(
                nameof(BookingsController.GetBookingById),
                "Bookings",
                new { id = booking.Id },
                Request.Scheme
            ),
            new ApiResult<BookingDto>
            {
                Status = true,
                Message = $"Booking for event with Id = {id} created successfully",
                Data = booking
            }
        );
    }
}