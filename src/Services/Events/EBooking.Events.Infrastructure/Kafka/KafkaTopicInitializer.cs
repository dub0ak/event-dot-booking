namespace EBooking.Events.Infrastructure;

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EBooking.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Гарантирует наличие Kafka-топика для подтверждённых бронирований.
/// </summary>
public sealed class KafkaTopicInitializer : IHostedService
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaTopicInitializer> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = _options.BootstrapServers
        };

        using var adminClient =
            new AdminClientBuilder(adminConfig).Build();

        var topicSpecification = new TopicSpecification
        {
            Name = KafkaTopics.BookingConfirmed,
            NumPartitions = 1,
            ReplicationFactor = 1
        };

        try
        {
            await adminClient.CreateTopicsAsync(
                [topicSpecification]);

            _logger.LogInformation(
                "Kafka topic {TopicName} created.",
                KafkaTopics.BookingConfirmed);
        }
        catch (CreateTopicsException exception)
            when (exception.Results.All(
                result =>
                    result.Error.Code ==
                    ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation(
                "Kafka topic {TopicName} already exists.",
                KafkaTopics.BookingConfirmed);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to create Kafka topic {TopicName}. " +
                "The Events service will continue starting.",
                KafkaTopics.BookingConfirmed);
        }
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}