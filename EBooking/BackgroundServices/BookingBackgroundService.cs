namespace EBooking.BackgroundServices;

using EBooking.Interfaces;
using Microsoft.Extensions.DependencyInjection;

public class BookingProcessingBackgroundService : BackgroundService
{
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
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var bookingRepository = scope.ServiceProvider
                .GetRequiredService<IBookingRepository>();

            var eventRepository = scope.ServiceProvider
                .GetRequiredService<IEventRepository>();

            var pendingBookingIds = await bookingRepository
                .GetPendingBookingIdsAsync(stoppingToken);

            foreach (var bookingId in pendingBookingIds)
            {
                var booking = await bookingRepository.GetByIdAsync(
                    bookingId,
                    stoppingToken,
                    asNoTracking: false);

                if (booking is null)
                {
                    continue;
                }

                var eventExists = await eventRepository.ExistsAsync(
                    booking.EventId,
                    stoppingToken);

                if (!eventExists)
                {
                    booking.Reject();

                    await bookingRepository.SaveChangesAsync(stoppingToken);

                    _logger.LogWarning(
                        "Booking {BookingId} was rejected because event {EventId} was not found",
                        booking.Id,
                        booking.EventId);

                    continue;
                }

                booking.Confirm();

                await bookingRepository.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "Booking {BookingId} was confirmed",
                    booking.Id);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}