namespace EBooking.Bookings.Infrastructure;

using System.Text.Json;
using Confluent.Kafka;
using EBooking.Bookings.Application;
using EBooking.Contracts;

/// <summary>
/// Публикует сообщения о подтверждённых бронированиях в Kafka.
/// </summary>
public sealed class KafkaBookingConfirmedPublisher
    : IBookingConfirmedPublisher
{
    private readonly IProducer<string, string> _producer;

    public KafkaBookingConfirmedPublisher(
        IProducer<string, string> producer)
    {
        ArgumentNullException.ThrowIfNull(producer);

        _producer = producer;
    }

    public async Task PublishAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var serializedMessage =
            JsonSerializer.Serialize(message);

        var kafkaMessage = new Message<string, string>
        {
            Key = message.EventId.ToString(),
            Value = serializedMessage
        };

        await _producer.ProduceAsync(
            KafkaTopics.BookingConfirmed,
            kafkaMessage,
            cancellationToken);
    }
}