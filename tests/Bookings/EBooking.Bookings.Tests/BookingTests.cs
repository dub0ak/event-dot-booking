namespace EBooking.Bookings.Tests;

using EBooking.Bookings.Domain;

public sealed class BookingTests
{
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void CreatePending_Should_Create_Booking_With_Expected_Values()
    {
        var createdAt = new DateTimeOffset(
            2026,
            7,
            26,
            10,
            0,
            0,
            TimeSpan.Zero);

        var booking = Booking.CreatePending(
            EventId,
            UserId,
            seatsCount: 2,
            createdAt);

        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(EventId, booking.EventId);
        Assert.Equal(UserId, booking.UserId);
        Assert.Equal(2, booking.SeatsCount);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(createdAt, booking.CreatedAt);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    public void CreatePending_Should_Throw_When_EventId_Is_Empty()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Booking.CreatePending(
                Guid.Empty,
                UserId,
                seatsCount: 1));

        Assert.Equal("eventId", exception.ParamName);
    }

    [Fact]
    public void CreatePending_Should_Throw_When_UserId_Is_Empty()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            Booking.CreatePending(
                EventId,
                Guid.Empty,
                seatsCount: 1));

        Assert.Equal("userId", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void CreatePending_Should_Throw_When_SeatsCount_Is_Not_Positive(
        int seatsCount)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Booking.CreatePending(
                EventId,
                UserId,
                seatsCount));

        Assert.Equal("seatsCount", exception.ParamName);
    }

    [Fact]
    public void Confirm_Should_Change_Status_And_Set_ProcessedAt()
    {
        var booking = CreatePendingBooking();

        var processedAt = new DateTimeOffset(
            2026,
            7,
            26,
            11,
            0,
            0,
            TimeSpan.Zero);

        booking.Confirm(processedAt);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(processedAt, booking.ProcessedAt);
    }

    [Fact]
    public void Confirm_Should_Throw_When_Booking_Is_Not_Pending()
    {
        var booking = CreatePendingBooking();
        booking.Confirm();

        var exception = Assert.Throws<InvalidBookingStatusException>(
            () => booking.Confirm());

        Assert.Equal(BookingStatus.Confirmed, exception.CurrentStatus);
        Assert.Equal("confirm", exception.Operation);
    }

    [Fact]
    public void Reject_Should_Change_Status_And_Set_ProcessedAt()
    {
        var booking = CreatePendingBooking();

        var processedAt = new DateTimeOffset(
            2026,
            7,
            26,
            11,
            30,
            0,
            TimeSpan.Zero);

        booking.Reject(processedAt);

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.Equal(processedAt, booking.ProcessedAt);
    }

    [Fact]
    public void Reject_Should_Throw_When_Booking_Is_Not_Pending()
    {
        var booking = CreatePendingBooking();
        booking.Reject();

        var exception = Assert.Throws<InvalidBookingStatusException>(
            () => booking.Reject());

        Assert.Equal(BookingStatus.Rejected, exception.CurrentStatus);
        Assert.Equal("reject", exception.Operation);
    }

    [Fact]
    public void Cancel_Should_Cancel_Pending_Booking()
    {
        var booking = CreatePendingBooking();

        var processedAt = new DateTimeOffset(
            2026,
            7,
            26,
            12,
            0,
            0,
            TimeSpan.Zero);

        booking.Cancel(processedAt);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(processedAt, booking.ProcessedAt);
    }

    [Fact]
    public void Cancel_Should_Cancel_Confirmed_Booking()
    {
        var booking = CreatePendingBooking();
        booking.Confirm();

        var processedAt = new DateTimeOffset(
            2026,
            7,
            26,
            12,
            30,
            0,
            TimeSpan.Zero);

        booking.Cancel(processedAt);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(processedAt, booking.ProcessedAt);
    }

    [Theory]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.Cancelled)]
    public void Cancel_Should_Throw_For_Inactive_Status(
        BookingStatus status)
    {
        var booking = CreatePendingBooking();

        switch (status)
        {
            case BookingStatus.Rejected:
                booking.Reject();
                break;

            case BookingStatus.Cancelled:
                booking.Cancel();
                break;
        }

        var exception = Assert.Throws<InvalidBookingStatusException>(
            () => booking.Cancel());

        Assert.Equal(status, exception.CurrentStatus);
        Assert.Equal("cancel", exception.Operation);
    }

    [Fact]
    public void TryConfirm_Should_Return_True_For_Pending_Booking()
    {
        var booking = CreatePendingBooking();

        var processedAt = new DateTimeOffset(
            2026,
            7,
            26,
            13,
            0,
            0,
            TimeSpan.Zero);

        var result = booking.TryConfirm(processedAt);

        Assert.True(result);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(processedAt, booking.ProcessedAt);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.Cancelled)]
    public void TryConfirm_Should_Return_False_When_Booking_Is_Not_Pending(
        BookingStatus status)
    {
        var booking = CreatePendingBooking();

        switch (status)
        {
            case BookingStatus.Confirmed:
                booking.Confirm();
                break;

            case BookingStatus.Rejected:
                booking.Reject();
                break;

            case BookingStatus.Cancelled:
                booking.Cancel();
                break;
        }

        var originalProcessedAt = booking.ProcessedAt;

        var result = booking.TryConfirm(
            new DateTimeOffset(
                2026,
                7,
                27,
                10,
                0,
                0,
                TimeSpan.Zero));

        Assert.False(result);
        Assert.Equal(status, booking.Status);
        Assert.Equal(originalProcessedAt, booking.ProcessedAt);
    }

    private static Booking CreatePendingBooking()
    {
        return Booking.CreatePending(
            EventId,
            UserId,
            seatsCount: 1);
    }
}