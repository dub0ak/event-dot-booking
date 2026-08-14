namespace EBooking.Events.Infrastructure;

/// <summary>
/// Настройки подключения к Redis.
/// </summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Строка подключения к Redis.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
}