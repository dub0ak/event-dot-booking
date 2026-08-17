namespace EBooking.Bookings.Infrastructure;

using Confluent.Kafka;
using EBooking.Bookings.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Регистрация инфраструктуры сервиса бронирований.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        var bootstrapServers =
            configuration[
                $"{KafkaOptions.SectionName}:"
                + nameof(KafkaOptions.BootstrapServers)]
            ?? throw new InvalidOperationException(
                "Configuration value "
                + "'Kafka:BootstrapServers' was not found.");

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            throw new InvalidOperationException(
                "Configuration value "
                + "'Kafka:BootstrapServers' cannot be empty.");
        }

        services.AddDbContext<BookingsDbContext>(
            options =>
                options.UseNpgsql(connectionString));

        services.AddScoped<
            IBookingRepository,
            BookingRepository>();

        services.AddSingleton<IProducer<string, string>>(
            _ =>
            {
                var producerConfig = new ProducerConfig
                {
                    BootstrapServers = bootstrapServers,
                    Acks = Acks.All,
                    EnableIdempotence = true
                };

                return new ProducerBuilder<string, string>(
                        producerConfig)
                    .Build();
            });

        services.AddSingleton<
            IBookingConfirmedPublisher,
            KafkaBookingConfirmedPublisher>();

        return services;
    }
}