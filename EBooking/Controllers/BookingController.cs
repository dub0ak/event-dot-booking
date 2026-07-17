namespace EBooking.Controllers;

using EBooking.Application.DTO;
using EBooking.Handlers;
using EBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using EBooking.Domain.Entities;

/// <summary>
/// Контроллер для работы с бронированиями
/// </summary>
[ApiController]
[Route("[controller]")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    private readonly IBookingService _bookingService = bookingService;

    /// <summary>
    /// Получить бронь по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор брони</param>
    /// <returns>Найденная бронь</returns>
    [Authorize]
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiResult<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResult<BookingDto>>> GetBookingById(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        return Ok(new ApiResult<BookingDto>
        {
            Status = true,
            Message = $"Booking with Id = {id} returned successfully",
            Data = booking
        });
    }


    /// <summary>
    /// Отменить бронь по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор брони</param>
    /// <returns>Ответ без содержимого при успешной отмене</returns>
    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var role = User.IsInRole(nameof(UserRole.Admin))
            ? UserRole.Admin
            : UserRole.User;

        await _bookingService.CancelBookingAsync(id, userId, role);

        return NoContent();
    }
}
