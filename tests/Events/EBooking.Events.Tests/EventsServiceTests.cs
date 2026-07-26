namespace EBooking.Events.Tests.Application;

using EBooking.Events.Application;
using EBooking.Events.Domain;

public sealed class EventsServiceTests
{
    private static readonly DateTime DefaultStartAt =
        new(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime DefaultEndAt =
        new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_Should_Throw_When_Repository_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EventsService(null!));
    }

    [Fact]
    public async Task GetEventsAsync_Should_Throw_When_Query_Is_Null()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.GetEventsAsync(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetEventsAsync_Should_Throw_When_Page_Is_Not_Positive(
        int page)
    {
        var service = CreateService();

        var query = new GetEventsQuery
        {
            Page = page,
            PageSize = 10
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.GetEventsAsync(query));

        Assert.Equal(
            "Page must be greater than 0.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetEventsAsync_Should_Throw_When_PageSize_Is_Not_Positive(
        int pageSize)
    {
        var service = CreateService();

        var query = new GetEventsQuery
        {
            Page = 1,
            PageSize = pageSize
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.GetEventsAsync(query));

        Assert.Equal(
            "PageSize must be greater than 0.",
            exception.Message);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(1000)]
    public async Task GetEventsAsync_Should_Throw_When_PageSize_Exceeds_Maximum(
        int pageSize)
    {
        var service = CreateService();

        var query = new GetEventsQuery
        {
            Page = 1,
            PageSize = pageSize
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.GetEventsAsync(query));

        Assert.Equal(
            "PageSize must not exceed 100.",
            exception.Message);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Throw_When_To_Is_Earlier_Than_From()
    {
        var service = CreateService();

        var query = new GetEventsQuery
        {
            From = DefaultStartAt,
            To = DefaultStartAt.AddMinutes(-1)
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.GetEventsAsync(query));

        Assert.Equal(
            "To must be greater than or equal to From.",
            exception.Message);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Allow_To_Equal_From()
    {
        var repository = new FakeEventRepository();
        var service = new EventsService(repository);

        var query = new GetEventsQuery
        {
            From = DefaultStartAt,
            To = DefaultStartAt
        };

        await service.GetEventsAsync(query);

        Assert.Equal(1, repository.GetEventsCallCount);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Pass_Query_And_Token_To_Repository()
    {
        var repository = new FakeEventRepository();
        var service = new EventsService(repository);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var query = new GetEventsQuery
        {
            Title = "conference",
            From = DefaultStartAt,
            To = DefaultEndAt,
            Page = 3,
            PageSize = 25
        };

        await service.GetEventsAsync(
            query,
            cancellationTokenSource.Token);

        Assert.Same(query, repository.LastQuery);
        Assert.Equal(
            cancellationTokenSource.Token,
            repository.LastCancellationToken);
        Assert.Equal(1, repository.GetEventsCallCount);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Map_Repository_Result_To_Dto()
    {
        var repository = new FakeEventRepository();

        var firstEvent = CreateEvent(
            "First Event",
            "First Description",
            10);

        var secondEvent = CreateEvent(
            "Second Event",
            null,
            20);

        secondEvent.TryReserveSeats(3);

        repository.GetEventsResult =
            new PaginatedResult<Event>(
                TotalCount: 42,
                Page: 2,
                PageSize: 2,
                Items: [firstEvent, secondEvent]);

        var service = new EventsService(repository);

        var result = await service.GetEventsAsync(
            new GetEventsQuery
            {
                Page = 2,
                PageSize = 2
            });

        Assert.Equal(42, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count);

        Assert.Collection(
            result.Items,
            first =>
            {
                Assert.Equal(firstEvent.Id, first.Id);
                Assert.Equal("First Event", first.Title);
                Assert.Equal("First Description", first.Description);
                Assert.Equal(DefaultStartAt, first.StartAt);
                Assert.Equal(DefaultEndAt, first.EndAt);
                Assert.Equal(10, first.TotalSeats);
                Assert.Equal(10, first.AvailableSeats);
            },
            second =>
            {
                Assert.Equal(secondEvent.Id, second.Id);
                Assert.Equal("Second Event", second.Title);
                Assert.Null(second.Description);
                Assert.Equal(20, second.TotalSeats);
                Assert.Equal(17, second.AvailableSeats);
            });
    }

    [Theory]
    [InlineData("Get")]
    [InlineData("Update")]
    [InlineData("Delete")]
    public async Task Service_Should_Throw_When_Event_Id_Is_Empty(
        string operation)
    {
        var service = CreateService();

        var exception = operation switch
        {
            "Get" => await Assert.ThrowsAsync<ValidationException>(
                () => service.GetEventByIdAsync(Guid.Empty)),

            "Update" => await Assert.ThrowsAsync<ValidationException>(
                () => service.UpdateEventAsync(
                    Guid.Empty,
                    CreateUpdateRequest())),

            "Delete" => await Assert.ThrowsAsync<ValidationException>(
                () => service.DeleteEventAsync(Guid.Empty)),

            _ => throw new InvalidOperationException()
        };

        Assert.Equal(
            "Event Id cannot be empty.",
            exception.Message);
    }

    [Fact]
    public async Task GetEventByIdAsync_Should_Return_Mapped_Event()
    {
        var repository = new FakeEventRepository();
        var eventItem = CreateEvent();

        repository.GetByIdResult = eventItem;

        var service = new EventsService(repository);

        var result = await service.GetEventByIdAsync(eventItem.Id);

        Assert.Equal(eventItem.Id, result.Id);
        Assert.Equal(eventItem.Title, result.Title);
        Assert.Equal(eventItem.Description, result.Description);
        Assert.Equal(eventItem.StartAt, result.StartAt);
        Assert.Equal(eventItem.EndAt, result.EndAt);
        Assert.Equal(eventItem.TotalSeats, result.TotalSeats);
        Assert.Equal(eventItem.AvailableSeats, result.AvailableSeats);

        Assert.Equal(eventItem.Id, repository.LastRequestedId);
        Assert.True(repository.LastAsNoTracking);
    }

    [Fact]
    public async Task GetEventByIdAsync_Should_Throw_When_Event_Does_Not_Exist()
    {
        var repository = new FakeEventRepository();
        var service = new EventsService(repository);
        var missingId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetEventByIdAsync(missingId));

        Assert.Equal(
            $"Event with Id = {missingId} was not found.",
            exception.Message);
    }

    [Fact]
    public async Task CreateEventAsync_Should_Throw_When_Request_Is_Null()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateEventAsync(null!));
    }

    [Fact]
    public async Task CreateEventAsync_Should_Create_Add_And_Save_Event()
    {
        var repository = new FakeEventRepository();
        var service = new EventsService(repository);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var request = new CreateEventRequest(
            "  Test Event  ",
            "  Test Description  ",
            DefaultStartAt,
            DefaultEndAt,
            100);

        var result = await service.CreateEventAsync(
            request,
            cancellationTokenSource.Token);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Event", result.Title);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(DefaultStartAt, result.StartAt);
        Assert.Equal(DefaultEndAt, result.EndAt);
        Assert.Equal(100, result.TotalSeats);
        Assert.Equal(100, result.AvailableSeats);

        Assert.Equal(1, repository.AddCallCount);
        Assert.Equal(1, repository.SaveChangesCallCount);
        Assert.NotNull(repository.LastAddedEvent);
        Assert.Equal(result.Id, repository.LastAddedEvent!.Id);
        Assert.Equal(
            cancellationTokenSource.Token,
            repository.LastCancellationToken);
    }

    [Fact]
    public async Task CreateEventAsync_Should_Not_Save_When_Domain_Validation_Fails()
    {
        var repository = new FakeEventRepository();
        var service = new EventsService(repository);

        var request = new CreateEventRequest(
            " ",
            null,
            DefaultStartAt,
            DefaultEndAt,
            10);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateEventAsync(request));

        Assert.Equal(0, repository.AddCallCount);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task UpdateEventAsync_Should_Throw_When_Request_Is_Null()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdateEventAsync(
                Guid.NewGuid(),
                null!));
    }

    [Fact]
    public async Task UpdateEventAsync_Should_Load_Tracked_Event_Update_And_Save()
    {
        var repository = new FakeEventRepository();
        var eventItem = CreateEvent("Old Title", "Old Description", 50);

        repository.GetByIdResult = eventItem;

        var service = new EventsService(repository);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var request = new UpdateEventRequest(
            "  New Title  ",
            "  New Description  ",
            DefaultStartAt.AddDays(1),
            DefaultEndAt.AddDays(1));

        var result = await service.UpdateEventAsync(
            eventItem.Id,
            request,
            cancellationTokenSource.Token);

        Assert.Equal(eventItem.Id, result.Id);
        Assert.Equal("New Title", result.Title);
        Assert.Equal("New Description", result.Description);
        Assert.Equal(DefaultStartAt.AddDays(1), result.StartAt);
        Assert.Equal(DefaultEndAt.AddDays(1), result.EndAt);

        Assert.Equal(50, result.TotalSeats);
        Assert.Equal(50, result.AvailableSeats);

        Assert.Equal(eventItem.Id, repository.LastRequestedId);
        Assert.False(repository.LastAsNoTracking);
        Assert.Equal(1, repository.GetByIdCallCount);
        Assert.Equal(1, repository.SaveChangesCallCount);
        Assert.Equal(
            cancellationTokenSource.Token,
            repository.LastCancellationToken);
    }

    [Fact]
    public async Task UpdateEventAsync_Should_Throw_When_Event_Does_Not_Exist()
    {
        var repository = new FakeEventRepository();
        var service = new EventsService(repository);
        var missingId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateEventAsync(
                missingId,
                CreateUpdateRequest()));

        Assert.Equal(
            $"Event with Id = {missingId} was not found.",
            exception.Message);

        Assert.False(repository.LastAsNoTracking);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task UpdateEventAsync_Should_Not_Save_When_Domain_Validation_Fails()
    {
        var repository = new FakeEventRepository();
        var eventItem = CreateEvent();

        repository.GetByIdResult = eventItem;

        var service = new EventsService(repository);

        var request = new UpdateEventRequest(
            " ",
            null,
            DefaultStartAt.AddDays(1),
            DefaultEndAt.AddDays(1));

        await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateEventAsync(
                eventItem.Id,
                request));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task DeleteEventAsync_Should_Load_Tracked_Event_Delete_And_Save()
    {
        var repository = new FakeEventRepository();
        var eventItem = CreateEvent();

        repository.Seed(eventItem);

        var service = new EventsService(repository);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        await service.DeleteEventAsync(
            eventItem.Id,
            cancellationTokenSource.Token);

        Assert.Equal(eventItem.Id, repository.LastRequestedId);
        Assert.False(repository.LastAsNoTracking);

        Assert.Equal(1, repository.GetByIdCallCount);
        Assert.Equal(1, repository.DeleteCallCount);
        Assert.Equal(1, repository.SaveChangesCallCount);

        Assert.Same(eventItem, repository.LastDeletedEvent);
        Assert.Empty(repository.Events);

        Assert.Equal(
            cancellationTokenSource.Token,
            repository.LastCancellationToken);
    }

    [Fact]
    public async Task DeleteEventAsync_Should_Throw_When_Event_Does_Not_Exist()
    {
        var repository = new FakeEventRepository();
        var service = new EventsService(repository);
        var missingId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteEventAsync(missingId));

        Assert.Equal(
            $"Event with Id = {missingId} was not found.",
            exception.Message);

        Assert.False(repository.LastAsNoTracking);
        Assert.Equal(0, repository.DeleteCallCount);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    private static EventsService CreateService()
    {
        return new EventsService(
            new FakeEventRepository());
    }

    private static Event CreateEvent(
        string title = "Test Event",
        string? description = "Test Description",
        int totalSeats = 10)
    {
        return Event.Create(
            title,
            description,
            DefaultStartAt,
            DefaultEndAt,
            totalSeats);
    }

    private static UpdateEventRequest CreateUpdateRequest()
    {
        return new UpdateEventRequest(
            "Updated Event",
            "Updated Description",
            DefaultStartAt.AddDays(1),
            DefaultEndAt.AddDays(1)
        );
    }
}