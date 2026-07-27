namespace EBooking.Bookings.Infrastructure;

/// <summary>
/// Настройки подключения к Kafka.
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>
    /// Название секции в конфигурации.
    /// </summary>
    public const string SectionName = "Kafka";

    /// <summary>
    /// Адреса Kafka-брокеров.
    /// </summary>
    public string BootstrapServers { get; init; } = string.Empty;
}