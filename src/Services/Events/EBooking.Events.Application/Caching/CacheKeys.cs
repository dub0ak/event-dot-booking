namespace EBooking.Events.Application;

public static class CacheKeys
{
    private const string EventPrefix = "event:";

    public const string TopEvents = "events:top10";

    public static string Event(Guid eventId)
    {
        return $"{EventPrefix}{eventId}";
    }
}