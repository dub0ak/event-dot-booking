namespace EBooking.Bookings.Api;

using EBooking.Bookings.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Управляет бронированиями текущего пользователя.
/// </summary>
[ApiController]
[Authorize]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(
        IBookingService bookingService)
    {
        ArgumentNullException.ThrowIfNull(bookingService);

        _bookingService = bookingService;
    }

    /// <summary>
    /// Создаёт новое бронирование.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(
        typeof(BookingDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDto>> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = User.GetUserId();

        var booking = await _bookingService.CreateBookingAsync(
            request.EventId,
            userId,
            request.SeatsCount,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetBooking),
            new { id = booking.Id },
            booking);
    }

    /// <summary>
    /// Возвращает бронирование по идентификатору.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(BookingDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetBooking(
        Guid id,
        CancellationToken cancellationToken)
    {
        var booking =
            await _bookingService.GetBookingByIdAsync(
                id,
                cancellationToken);

        return Ok(booking);
    }

    /// <summary>
    /// Отменяет бронирование.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var requesterIsAdmin = User.IsAdmin();

        await _bookingService.CancelBookingAsync(
            id,
            userId,
            requesterIsAdmin,
            cancellationToken);

        return NoContent();
    }
}