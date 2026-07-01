using EBooking.Application.Interfaces;

namespace EBooking.Application.Services;

public class BookingProcessingService : IBookingProcessingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;

    public BookingProcessingService(
        IBookingRepository bookingRepository,
        IEventRepository eventRepository)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
    }

    public async Task ProcessPendingBookingsAsync(CancellationToken cancellationToken = default)
    {
        var bookingIds = await _bookingRepository.GetPendingBookingIdsAsync(cancellationToken);

        foreach (var bookingId in bookingIds)
        {
            var booking = await _bookingRepository.GetByIdAsync(
                bookingId,
                cancellationToken,
                asNoTracking: false);

            if (booking is null || booking.Status != Domain.Entities.BookingStatus.Pending)
            {
                continue;
            }

            var eventEntity = await _eventRepository.GetByIdAsync(
                booking.EventId,
                cancellationToken,
                asNoTracking: false);

            if (eventEntity is null)
            {
                booking.Reject();
                await _bookingRepository.SaveChangesAsync(cancellationToken);
                continue;
            }

            if (eventEntity.TryReserveSeats(1))
            {
                booking.Confirm();
            }
            else
            {
                booking.Reject();
            }

            await _bookingRepository.SaveChangesAsync(cancellationToken);
        }
    }
}