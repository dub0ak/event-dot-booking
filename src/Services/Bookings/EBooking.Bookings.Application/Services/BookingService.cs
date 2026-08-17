namespace EBooking.Bookings.Application;

using EBooking.Bookings.Domain;

/// <summary>
/// Сервис управления бронированиями.
/// </summary>
public sealed class BookingService : IBookingService
{
    private const int MaxActiveBookingsPerUser = 10;

    private readonly IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        ArgumentNullException.ThrowIfNull(bookingRepository);

        _bookingRepository = bookingRepository;
    }

    public async Task<BookingDto> CreateBookingAsync(
        Guid eventId,
        Guid userId,
        int seatsCount,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException(
                "Event identifier cannot be empty.",
                nameof(eventId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User identifier cannot be empty.",
                nameof(userId));
        }

        if (seatsCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(seatsCount),
                seatsCount,
                "Seats count must be greater than zero.");
        }

        var activeBookingCount =
            await _bookingRepository.CountActiveByUserIdAsync(
                userId,
                cancellationToken);

        if (activeBookingCount >= MaxActiveBookingsPerUser)
        {
            throw new ActiveBookingLimitExceededException(
                MaxActiveBookingsPerUser);
        }

        var booking = Booking.CreatePending(
            eventId,
            userId,
            seatsCount);

        await _bookingRepository.AddAsync(
            booking,
            cancellationToken);

        await _bookingRepository.SaveChangesAsync(
            cancellationToken);

        return ToDto(booking);
    }

    public async Task<BookingDto> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Booking identifier cannot be empty.",
                nameof(bookingId));
        }

        var booking = await _bookingRepository.GetByIdAsync(
            bookingId,
            cancellationToken);

        if (booking is null)
        {
            throw new NotFoundException(
                $"Booking with Id = {bookingId} was not found.");
        }

        return ToDto(booking);
    }

    public async Task CancelBookingAsync(
        Guid bookingId,
        Guid requesterUserId,
        bool requesterIsAdmin,
        CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Booking identifier cannot be empty.",
                nameof(bookingId));
        }

        if (requesterUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Requester user identifier cannot be empty.",
                nameof(requesterUserId));
        }

        var booking = await _bookingRepository.GetByIdAsync(
            bookingId,
            cancellationToken,
            asNoTracking: false);

        if (booking is null)
        {
            throw new NotFoundException(
                $"Booking with Id = {bookingId} was not found.");
        }

        var requesterIsOwner = booking.UserId == requesterUserId;

        if (!requesterIsOwner && !requesterIsAdmin)
        {
            throw new ForbiddenOperationException(
                "You cannot cancel another user's booking.");
        }

        booking.Cancel();

        await _bookingRepository.SaveChangesAsync(
            cancellationToken);
    }

    private static BookingDto ToDto(Booking booking)
    {
        return new BookingDto(
            booking.Id,
            booking.EventId,
            booking.UserId,
            booking.SeatsCount,
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt);
    }
}