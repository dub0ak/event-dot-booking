namespace EBooking.Events.Application;

using EBooking.Contracts;

/// <summary>
/// Обрабатывает событие подтверждения бронирования.
/// </summary>
public sealed class BookingConfirmedHandler : IBookingConfirmedHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cacheService;

    public BookingConfirmedHandler(
        IEventRepository eventRepository,
        ICacheService cacheService)
    {
        ArgumentNullException.ThrowIfNull(eventRepository);
        ArgumentNullException.ThrowIfNull(cacheService);

        _eventRepository = eventRepository;
        _cacheService = cacheService;
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

        await _eventRepository.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(
            CacheKeys.Event(message.EventId),
            cancellationToken
        );

        return BookingConfirmedHandlingResult.Processed;
    }
}