namespace EBooking.Events.Application;

using EBooking.Contracts;

/// <summary>
/// Обрабатывает событие подтверждения бронирования.
/// </summary>
public sealed class BookingConfirmedHandler : IBookingConfirmedHandler
{
    private readonly IEventRepository _eventRepository;

    public BookingConfirmedHandler(
        IEventRepository eventRepository)
    {
        ArgumentNullException.ThrowIfNull(eventRepository);

        _eventRepository = eventRepository;
    }

    /// <inheritdoc />
    public async Task<BookingConfirmedHandlingResult> HandleAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var eventItem = await _eventRepository.GetByIdAsync(
            message.EventId,
            cancellationToken,
            asNoTracking: false);

        if (eventItem is null)
        {
            return BookingConfirmedHandlingResult.EventNotFound;
        }

        if (!eventItem.TryReserveSeats(message.SeatsCount))
        {
            return BookingConfirmedHandlingResult.InsufficientSeats;
        }

        await _eventRepository.SaveChangesAsync(
            cancellationToken);

        return BookingConfirmedHandlingResult.Processed;
    }
}