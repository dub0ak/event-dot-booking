namespace EBooking.Bookings.Api;

using EBooking.Bookings.Application;

/// <summary>
/// Периодически запускает обработку ожидающих бронирований.
/// </summary>
public sealed class BookingProcessingBackgroundService
    : BackgroundService
{
    private static readonly TimeSpan ProcessingInterval =
        TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;

    public BookingProcessingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingProcessingBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Booking processing background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while processing pending bookings.");
            }

            try
            {
                await Task.Delay(
                    ProcessingInterval,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Booking processing background service stopped.");
    }

    private async Task ProcessBookingsAsync(
        CancellationToken cancellationToken)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var processingService =
            scope.ServiceProvider
                .GetRequiredService<IBookingProcessingService>();

        await processingService.ProcessPendingBookingsAsync(
            cancellationToken);
    }
}