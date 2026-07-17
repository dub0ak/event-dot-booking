namespace EBooking.Middleware;

using System.Text.Json;
using EBooking.Domain.Exceptions;
using EBooking.Handlers;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error");
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (InvalidCredentialsException ex)
        {
            _logger.LogWarning(ex, "Invalid Credentials");
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (NoAvailableSeatsException ex)
        {
            _logger.LogWarning(ex, "No available seats");
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (EventAlreadyStartedException ex)
        {
            _logger.LogWarning(ex, "Event already started");
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (ActiveBookingLimitExceededException ex)
        {
            _logger.LogWarning(ex, "Active booking limit exceeded");
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (ForbiddenOperationException ex)
        {
            _logger.LogWarning(ex, "Forbidden operation");
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Internal server error");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = message
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}