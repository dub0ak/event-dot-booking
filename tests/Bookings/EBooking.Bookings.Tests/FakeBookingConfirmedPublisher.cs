namespace EBooking.Bookings.Tests;

using EBooking.Bookings.Application;
using EBooking.Contracts;

internal sealed class FakeBookingConfirmedPublisher
    : IBookingConfirmedPublisher
{
    private readonly List<string>? _operations;

    public FakeBookingConfirmedPublisher(
        List<string>? operations = null)
    {
        _operations = operations;
    }

    public int PublishCallCount { get; private set; }

    public BookingConfirmed? PublishedMessage { get; private set; }

    public Task PublishAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(message);

        PublishCallCount++;
        PublishedMessage = message;
        _operations?.Add("Publish");

        return Task.CompletedTask;
    }
}