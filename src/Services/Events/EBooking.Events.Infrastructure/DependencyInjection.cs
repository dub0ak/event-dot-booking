namespace EBooking.Events.Infrastructure;

using Confluent.Kafka;
using EBooking.Events.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Регистрация компонентов инфраструктурного слоя.
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
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<EventDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IEventRepository, EventRepository>();
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.AddSingleton<IConsumer<string, string>>(
            serviceProvider =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<KafkaOptions>>()
                    .Value;

                var consumerConfiguration = new ConsumerConfig
                {
                    BootstrapServers = options.BootstrapServers,
                    GroupId = options.ConsumerGroup,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false
                };

                return new ConsumerBuilder<string, string>(
                        consumerConfiguration)
                    .Build();
            });
        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingConfirmedConsumer>();
        return services;
    }
}