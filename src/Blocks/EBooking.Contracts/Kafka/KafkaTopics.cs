namespace EBooking.Contracts;

/// <summary>
/// Имена Kafka-топиков, используемых сервисами EBooking.
/// </summary>
public static class KafkaTopics
{
    /// <summary>
    /// Топик подтверждённых бронирований.
    /// </summary>
    public const string BookingConfirmed = "booking-confirmed";
}