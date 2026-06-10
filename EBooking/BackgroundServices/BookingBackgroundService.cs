namespace EBooking.BackgroundServices;

using EBooking.DataStore;
using EBooking.Models;
using Microsoft.EntityFrameworkCore;

public class BookingProcessingBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;

    public BookingProcessingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingProcessingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Booking processing background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingBookingIds = await GetPendingBookingIdsAsync(stoppingToken);

                var tasks = pendingBookingIds.Select(bookingId =>
                    ProcessBookingAsync(bookingId, stoppingToken));

                await Task.WhenAll(tasks);

                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing bookings");
                await Task.Delay(PollingInterval, stoppingToken);
            }
        }

        _logger.LogInformation("Booking processing background service stopped");
    }

    private async Task<List<Guid>> GetPendingBookingIdsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Pending)
            .Select(b => b.Id)
            .ToListAsync(stoppingToken);
    }

    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var booking = await context.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);

            if (booking is null || booking.Status != BookingStatus.Pending)
            {
                return;
            }

            _logger.LogInformation(
                "Processing booking {BookingId} for event {EventId}",
                booking.Id,
                booking.EventId);

            var eventExists = await context.Events
                .AnyAsync(e => e.Id == booking.EventId, stoppingToken);

            if (!eventExists)
            {
                booking.Reject();

                await context.SaveChangesAsync(stoppingToken);

                _logger.LogWarning(
                    "Booking {BookingId} rejected because event {EventId} no longer exists",
                    booking.Id,
                    booking.EventId);

                return;
            }

            booking.Confirm();

            await context.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Booking {BookingId} confirmed at {ProcessedAt}",
                booking.Id,
                booking.ProcessedAt);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Booking processing for booking {BookingId} was cancelled",
                bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while processing booking {BookingId}",
                bookingId);

            await RejectBookingAndReleaseSeatAsync(bookingId, stoppingToken);
        }
    }

    private async Task RejectBookingAndReleaseSeatAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);

        if (booking is null || booking.Status != BookingStatus.Pending)
        {
            return;
        }

        var eventItem = await context.Events
            .FirstOrDefaultAsync(e => e.Id == booking.EventId, stoppingToken);

        booking.Reject();

        if (eventItem is not null)
        {
            eventItem.ReleaseSeats();
        }

        await context.SaveChangesAsync(stoppingToken);

        _logger.LogWarning(
            "Booking {BookingId} rejected and reserved seat was released",
            booking.Id);
    }
}