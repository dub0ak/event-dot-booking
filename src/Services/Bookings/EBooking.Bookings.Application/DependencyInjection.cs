namespace EBooking.Bookings.Application;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Регистрация зависимостей слоя Application.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingProcessingService, BookingProcessingService>();
        return services;
    }
}