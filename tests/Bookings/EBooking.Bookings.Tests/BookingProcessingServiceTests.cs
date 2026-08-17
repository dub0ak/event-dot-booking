namespace EBooking.Bookings.Tests;

using EBooking.Bookings.Application;
using EBooking.Bookings.Domain;

public sealed class BookingProcessingServiceTests
{
    [Fact]
    public async Task ProcessPendingBookingsAsync_Should_Confirm_And_Publish()
    {
        var booking = Booking.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seatsCount: 2);

        var repository = new FakeBookingRepository();
        repository.Seed(booking);

        var publisher = new FakeBookingConfirmedPublisher(
            repository.Operations);

        var service = new BookingProcessingService(
            repository,
            publisher);

        await service.ProcessPendingBookingsAsync();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);

        Assert.Equal(1, repository.SaveChangesCallCount);
        Assert.Equal(1, publisher.PublishCallCount);

        var message = Assert.IsType<EBooking.Contracts.BookingConfirmed>(publisher.PublishedMessage);

        Assert.Equal(booking.Id, message.BookingId);
        Assert.Equal(booking.EventId, message.EventId);
        Assert.Equal(booking.UserId, message.UserId);
        Assert.Equal(booking.SeatsCount, message.SeatsCount);
        Assert.Equal(booking.ProcessedAt, message.ConfirmedAt);
    }

    [Fact]
    public async Task ProcessPendingBookingsAsync_Should_Save_Before_Publish()
    {
        var booking = Booking.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seatsCount: 1);

        var repository = new FakeBookingRepository();
        repository.Seed(booking);

        var publisher = new FakeBookingConfirmedPublisher(
            repository.Operations);

        var service = new BookingProcessingService(
            repository,
            publisher);

        await service.ProcessPendingBookingsAsync();

        Assert.Equal(
            new[] { "Save", "Publish" },
            repository.Operations);
    }

    [Fact]
    public async Task ProcessPendingBookingsAsync_Should_Process_All_Pending()
    {
        var firstBooking = Booking.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seatsCount: 1);

        var secondBooking = Booking.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seatsCount: 3);

        var repository = new FakeBookingRepository();
        repository.Seed(firstBooking);
        repository.Seed(secondBooking);

        var publisher = new FakeBookingConfirmedPublisher();

        var service = new BookingProcessingService(
            repository,
            publisher);

        await service.ProcessPendingBookingsAsync();

        Assert.Equal(
            BookingStatus.Confirmed,
            firstBooking.Status);

        Assert.Equal(
            BookingStatus.Confirmed,
            secondBooking.Status);

        Assert.Equal(2, repository.SaveChangesCallCount);
        Assert.Equal(2, publisher.PublishCallCount);
    }

    [Fact]
    public async Task ProcessPendingBookingsAsync_Should_Not_Process_Cancelled()
    {
        var booking = Booking.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seatsCount: 1);

        booking.Cancel();

        var repository = new FakeBookingRepository();
        repository.Seed(booking);

        var publisher = new FakeBookingConfirmedPublisher();

        var service = new BookingProcessingService(
            repository,
            publisher);

        await service.ProcessPendingBookingsAsync();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(0, repository.SaveChangesCallCount);
        Assert.Equal(0, publisher.PublishCallCount);
    }

    [Fact]
    public async Task ProcessPendingBookingsAsync_Should_Not_Process_Confirmed()
    {
        var booking = Booking.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            seatsCount: 1);

        booking.Confirm();

        var repository = new FakeBookingRepository();
        repository.Seed(booking);

        var publisher = new FakeBookingConfirmedPublisher();

        var service = new BookingProcessingService(
            repository,
            publisher);

        await service.ProcessPendingBookingsAsync();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(0, repository.SaveChangesCallCount);
        Assert.Equal(0, publisher.PublishCallCount);
    }

    [Fact]
    public async Task ProcessPendingBookingsAsync_Should_Do_Nothing_When_Empty()
    {
        var repository = new FakeBookingRepository();
        var publisher = new FakeBookingConfirmedPublisher();

        var service = new BookingProcessingService(
            repository,
            publisher);

        await service.ProcessPendingBookingsAsync();

        Assert.Equal(0, repository.SaveChangesCallCount);
        Assert.Equal(0, publisher.PublishCallCount);
    }
}