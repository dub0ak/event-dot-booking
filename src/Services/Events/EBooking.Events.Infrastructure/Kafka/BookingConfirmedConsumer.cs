namespace EBooking.Events.Infrastructure;

using System.Text.Json;
using Confluent.Kafka;
using EBooking.Contracts;
using EBooking.Events.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Получает сообщения о подтверждённых бронированиях
/// и передаёт их в прикладной обработчик.
/// </summary>
public sealed class BookingConfirmedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingConfirmedConsumer> _logger;

    public BookingConfirmedConsumer(
        IConsumer<string, string> consumer,
        IServiceScopeFactory scopeFactory,
        ILogger<BookingConfirmedConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _consumer = consumer;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _consumer.Subscribe(KafkaTopics.BookingConfirmed);

        _logger.LogInformation(
            "Kafka consumer subscribed to topic {TopicName}.",
            KafkaTopics.BookingConfirmed);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> consumeResult;

                try
                {
                    consumeResult = _consumer.Consume(stoppingToken);
                }
                catch (ConsumeException exception)
                {
                    _logger.LogError(
                        exception,
                        "Kafka consume error: {Reason}",
                        exception.Error.Reason);

                    continue;
                }

                await ProcessMessageAsync(
                    consumeResult,
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Штатная остановка приложения.
        }
        finally
        {
            _consumer.Close();

            _logger.LogInformation(
                "Kafka consumer for topic {TopicName} stopped.",
                KafkaTopics.BookingConfirmed
            );
        }
    }

    private async Task ProcessMessageAsync(
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken)
    {
        BookingConfirmed? message;

        try
        {
            message = JsonSerializer.Deserialize<BookingConfirmed>(
                consumeResult.Message.Value);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Invalid BookingConfirmed message at {TopicPartitionOffset}.",
                consumeResult.TopicPartitionOffset);

            _consumer.Commit(consumeResult);
            return;
        }

        if (message is null)
        {
            _logger.LogWarning(
                "Empty BookingConfirmed message at {TopicPartitionOffset}.",
                consumeResult.TopicPartitionOffset);

            _consumer.Commit(consumeResult);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var handler = scope.ServiceProvider
                .GetRequiredService<IBookingConfirmedHandler>();

            var result = await handler.HandleAsync(
                message,
                cancellationToken);

            switch (result)
            {
                case BookingConfirmedHandlingResult.Processed:
                    _logger.LogInformation(
                        "Booking {BookingId} processed for event {EventId}. " +
                        "Reserved seats: {SeatsCount}.",
                        message.BookingId,
                        message.EventId,
                        message.SeatsCount);
                    break;

                case BookingConfirmedHandlingResult.EventNotFound:
                    _logger.LogWarning(
                        "Event {EventId} was not found for booking {BookingId}. " +
                        "Message skipped.",
                        message.EventId,
                        message.BookingId);
                    break;

                case BookingConfirmedHandlingResult.InsufficientSeats:
                    _logger.LogWarning(
                        "Event {EventId} does not have enough available seats " +
                        "for booking {BookingId}. Requested seats: {SeatsCount}. " +
                        "Message skipped.",
                        message.EventId,
                        message.BookingId,
                        message.SeatsCount);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown handling result: {result}.");
            }

            _consumer.Commit(consumeResult);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to process booking {BookingId} for event {EventId}. " +
                "Kafka offset will not be committed.",
                message.BookingId,
                message.EventId);
        }
    }
}