namespace EBooking.Events.Api;

using EBooking.Events.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Управление мероприятиями.
/// </summary>
[ApiController]
[Route("events")]
public sealed class EventsController : ControllerBase
{
    private readonly IEventsService _eventsService;

    public EventsController(IEventsService eventsService)
    {
        ArgumentNullException.ThrowIfNull(eventsService);

        _eventsService = eventsService;
    }

    /// <summary>
    /// Возвращает страницу мероприятий.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResult<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResult<EventDto>>> GetEvents(
        [FromQuery] GetEventsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _eventsService.GetEventsAsync(
            query,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Возвращает мероприятие по идентификатору.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetEventById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var eventItem = await _eventsService.GetEventByIdAsync(
            id,
            cancellationToken);

        return Ok(eventItem);
    }

    /// <summary>
    /// Создаёт мероприятие.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var eventItem = await _eventsService.CreateEventAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetEventById),
            new { id = eventItem.Id },
            eventItem);
    }

    /// <summary>
    /// Обновляет мероприятие.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> UpdateEvent(
        Guid id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var eventItem = await _eventsService.UpdateEventAsync(
            id,
            request,
            cancellationToken);

        return Ok(eventItem);
    }

    /// <summary>
    /// Удаляет мероприятие.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _eventsService.DeleteEventAsync(
            id,
            cancellationToken);

        return NoContent();
    }
}