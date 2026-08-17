namespace EBooking.Events.Infrastructure;

using System.Text.Json;
using EBooking.Events.Application;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

/// <summary>
/// Реализация кеша на основе Redis.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var value = await database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to get value from Redis cache for key {CacheKey}.",
                key
            );

            return default;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to deserialize Redis cache value for key {CacheKey}.",
                key
            );

            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiration),
                "Cache expiration must be > 0"
            );
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var serializedValue = JsonSerializer.Serialize(value);

            await database.StringSetAsync(
                key,
                serializedValue,
                expiration);
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to set Redis cache value for key {CacheKey}.",
                key
            );
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to serialize Redis cache value for key {CacheKey}.",
                key
            );
        }
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var database = _connectionMultiplexer.GetDatabase();

            await database.KeyDeleteAsync(key);
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to remove Redis cache value for key {CacheKey}.",
                key);
        }
    }
}