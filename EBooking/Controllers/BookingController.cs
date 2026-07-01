namespace EBooking.Controllers;

using EBooking.Application.DTO;
using EBooking.Handlers;
using EBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
}