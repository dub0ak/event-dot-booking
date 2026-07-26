namespace EBooking.Events.Api;

using EBooking.Events.Domain;
using EBooking.Events.Application;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Преобразует исключения приложения в HTTP-ответы.
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
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request was cancelled by the client.");
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var statusCode = exception switch
        {
            ValidationException =>
                StatusCodes.Status400BadRequest,

            NoAvailableSeatsException =>
                StatusCodes.Status409Conflict,

            NotFoundException =>
                StatusCodes.Status404NotFound,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred.");
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request failed with status code {StatusCode}.",
                statusCode);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest =>
                "Validation error",

            StatusCodes.Status404NotFound =>
                "Resource not found",

            StatusCodes.Status409Conflict =>
                "Conflict",

            _ =>
                "Internal server error"
        };
    }
}