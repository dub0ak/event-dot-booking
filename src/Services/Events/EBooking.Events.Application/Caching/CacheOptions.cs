namespace EBooking.Events.Application;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int EventTtlMinutes { get; init; } = 10;

    public int TopEventsTtlMinutes { get; init; } = 5;
}