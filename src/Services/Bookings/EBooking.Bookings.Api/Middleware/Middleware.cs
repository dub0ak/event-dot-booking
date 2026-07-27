namespace EBooking.Bookings.Api;

using EBooking.Bookings.Application;
using EBooking.Bookings.Domain;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Преобразует необработанные исключения приложения
/// в стандартные HTTP-ответы.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(
                context,
                exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var statusCode = exception switch
        {
            ArgumentException =>
                StatusCodes.Status400BadRequest,

            InvalidBookingStatusException =>
                StatusCodes.Status400BadRequest,

            UnauthorizedAccessException =>
                StatusCodes.Status401Unauthorized,

            ForbiddenOperationException =>
                StatusCodes.Status403Forbidden,

            NotFoundException =>
                StatusCodes.Status404NotFound,

            ActiveBookingLimitExceededException =>
                StatusCodes.Status409Conflict,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        LogException(
            exception,
            statusCode);

        if (context.Response.HasStarted)
        {
            throw exception;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = statusCode ==
                     StatusCodes.Status500InternalServerError
                ? "An unexpected server error occurred."
                : exception.Message,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(
            problemDetails,
            context.RequestAborted);
    }

    private void LogException(
        Exception exception,
        int statusCode)
    {
        if (statusCode >=
            StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing the request.");

            return;
        }

        _logger.LogWarning(
            exception,
            "Request failed with status code {StatusCode}.",
            statusCode);
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest =>
                "Bad Request",

            StatusCodes.Status401Unauthorized =>
                "Unauthorized",

            StatusCodes.Status403Forbidden =>
                "Forbidden",

            StatusCodes.Status404NotFound =>
                "Not Found",

            StatusCodes.Status409Conflict =>
                "Conflict",

            _ =>
                "Internal Server Error"
        };
    }
}