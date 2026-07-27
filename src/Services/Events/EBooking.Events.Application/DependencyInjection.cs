namespace EBooking.Events.Application;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Регистрация сервисов слоя Application.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IEventsService, EventsService>();
        services.AddScoped<IBookingConfirmedHandler, BookingConfirmedHandler>();
        return services;
    }
}