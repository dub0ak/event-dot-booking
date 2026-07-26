namespace EBooking.Bookings.Application;

using EBooking.Contracts;

/// <summary>
/// Подтверждает ожидающие бронирования и публикует доменные события.
/// </summary>
public sealed class BookingProcessingService
    : IBookingProcessingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingConfirmedPublisher _publisher;

    public BookingProcessingService(
        IBookingRepository bookingRepository,
        IBookingConfirmedPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(bookingRepository);
        ArgumentNullException.ThrowIfNull(publisher);

        _bookingRepository = bookingRepository;
        _publisher = publisher;
    }

    public async Task ProcessPendingBookingsAsync(
        CancellationToken cancellationToken = default)
    {
        var bookingIds =
            await _bookingRepository.GetPendingBookingIdsAsync(
                cancellationToken);

        foreach (var bookingId in bookingIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var booking = await _bookingRepository.GetByIdAsync(
                bookingId,
                cancellationToken,
                asNoTracking: false);

            if (booking is null)
            {
                continue;
            }

            var confirmedAt = DateTimeOffset.UtcNow;

            if (!booking.TryConfirm(confirmedAt))
            {
                continue;
            }

            await _bookingRepository.SaveChangesAsync(cancellationToken);

            var message = new BookingConfirmed(
                booking.Id,
                booking.EventId,
                booking.UserId,
                booking.SeatsCount,
                confirmedAt);

            await _publisher.PublishAsync(message, cancellationToken);
        }
    }
}