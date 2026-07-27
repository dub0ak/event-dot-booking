namespace EBooking.Users.Api;

using EBooking.Users.Application;
using EBooking.Users.Domain;
using System.Net;
using System.Text.Json;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
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
        var (statusCode, message) = exception switch
        {
            ValidationException validationException =>
                (
                    HttpStatusCode.BadRequest,
                    validationException.Message
                ),

            InvalidCredentialsException invalidCredentialsException =>
                (
                    HttpStatusCode.Unauthorized,
                    invalidCredentialsException.Message
                ),

            _ =>
                (
                    HttpStatusCode.InternalServerError,
                    "An unexpected error occurred."
                )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception occurred while processing request.");
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request failed with status code {StatusCode}.",
                (int)statusCode);
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            new ErrorResponse(
                (int)statusCode,
                message
            )
        );
    }
}