namespace EBooking.BackgroundServices;

using EBooking.DataStore;
using EBooking.Models;
using Microsoft.Extensions.Hosting;

public class BookingProcessingBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    private readonly BookingStore _bookingStore;
    private readonly EventStore _eventStore;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingProcessingBackgroundService(
        BookingStore bookingStore,
        EventStore eventStore,
        ILogger<BookingProcessingBackgroundService> logger)
    {
        _bookingStore = bookingStore;
        _eventStore = eventStore;
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

                var tasks = pendingBookings.Select(booking =>
                    ProcessBookingAsync(booking, stoppingToken));

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

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        if (booking.Status != BookingStatus.Pending)
        {
            return;
        }

        _logger.LogInformation(
            "Processing booking {BookingId} for event {EventId}",
            booking.Id,
            booking.EventId);

        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);

            try
            {
                var eventItem = _eventStore.GetById(booking.EventId);

                if (eventItem is null)
                {
                    booking.Reject();
                    _bookingStore.Update(booking);

                    _logger.LogWarning(
                        "Booking {BookingId} rejected because event {EventId} no longer exists",
                        booking.Id,
                        booking.EventId);

                    return;
                }

                booking.Confirm();
                _bookingStore.Update(booking);

                _logger.LogInformation(
                    "Booking {BookingId} confirmed at {ProcessedAt}",
                    booking.Id,
                    booking.ProcessedAt);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Booking processing for booking {BookingId} was cancelled",
                booking.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while processing booking {BookingId}",
                booking.Id);

            await RejectBookingAndReleaseSeatAsync(booking, stoppingToken);
        }
    }

    private async Task RejectBookingAndReleaseSeatAsync(Booking booking, CancellationToken stoppingToken)
    {
        await _processingSemaphore.WaitAsync(stoppingToken);

        try
        {
            var eventItem = _eventStore.GetById(booking.EventId);

            booking.Reject();
            _bookingStore.Update(booking);

            if (eventItem is not null)
            {
                eventItem.ReleaseSeats();
                _eventStore.Update(eventItem);
            }

            _logger.LogWarning(
                "Booking {BookingId} rejected and reserved seat was released",
                booking.Id);
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}