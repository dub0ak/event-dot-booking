namespace EBooking.Events.Infrastructure;

/// <summary>
/// Настройки подключения к Kafka.
/// </summary>
public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    /// <summary>
    /// Адрес брокера Kafka.
    /// </summary>
    public string BootstrapServers { get; init; } = string.Empty;

    /// <summary>
    /// Имя группы потребителей.
    /// </summary>
    public string ConsumerGroup { get; init; } = string.Empty;
}