namespace EBooking.Application.Interfaces;

public interface IBookingProcessingService
{
    Task ProcessPendingBookingsAsync(CancellationToken cancellationToken = default);
}