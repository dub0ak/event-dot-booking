namespace EBooking.BackgroundServices;

using EBooking.DataStore;
using EBooking.Models;
using Microsoft.Extensions.Hosting;

public class BookingProcessingBackgroundService : BackgroundService
{
    private readonly BookingStore _bookingStore;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;

    public BookingProcessingBackgroundService(
        BookingStore bookingStore,
        ILogger<BookingProcessingBackgroundService> logger)
    {
        _bookingStore = bookingStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Booking processing background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingBookings = _bookingStore.GetPending();

                foreach (var booking in pendingBookings)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Дополнительная защита от повторной обработки,
                    // если статус уже успел измениться
                    if (booking.Status != BookingStatus.Pending)
                    {
                        continue;
                    }

                    _logger.LogInformation(
                        "Processing booking {BookingId} for event {EventId}",
                        booking.Id,
                        booking.EventId);

                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                    if (booking.Status == BookingStatus.Pending)
                    {
                        booking.Confirm();

                        _logger.LogInformation(
                            "Booking {BookingId} confirmed at {ProcessedAt}",
                            booking.Id,
                            booking.ProcessedAt);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing bookings");

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        _logger.LogInformation("Booking processing background service stopped");
    }
}