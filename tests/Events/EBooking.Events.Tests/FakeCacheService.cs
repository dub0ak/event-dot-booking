namespace EBooking.Events.Tests;

using EBooking.Events.Application;

public sealed class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, object?> _values = new();

    public int GetCallCount { get; private set; }

    public int SetCallCount { get; private set; }

    public int RemoveCallCount { get; private set; }

    public string? LastKey { get; private set; }

    public TimeSpan? LastExpiration { get; private set; }

    public Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        GetCallCount++;
        LastKey = key;

        if (_values.TryGetValue(key, out var value) &&
            value is T typedValue)
        {
            return Task.FromResult<T?>(typedValue);
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        SetCallCount++;
        LastKey = key;
        LastExpiration = expiration;
        _values[key] = value;

        return Task.CompletedTask;
    }

    public Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        RemoveCallCount++;
        LastKey = key;
        _values.Remove(key);

        return Task.CompletedTask;
    }

    public void Seed<T>(
        string key,
        T value)
    {
        _values[key] = value;
    }
}