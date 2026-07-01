namespace EBooking.IntegrationTests;

using EBooking.Domain.Entities;
using EBooking.Infrastructure.Repositories;
using EBooking.Application.DTO;
using Xunit;

[Collection("Postgres collection")]
public class EventRepositoryTests
{
    private readonly PostgresFixture _fixture;
    public EventRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_Should_Save_Event()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);
        var createdEvent = Event.Create(
            "Test Event",
            "Description",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            100
        );

        await repository.AddAsync(createdEvent);
        await repository.SaveChangesAsync();
        var loadedEvent = await repository.GetByIdAsync(createdEvent.Id);
        Assert.NotNull(loadedEvent);
        Assert.Equal(createdEvent.Id, loadedEvent!.Id);
        Assert.Equal("Test Event", loadedEvent.Title);
        Assert.Equal(100, loadedEvent.TotalSeats);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Event_Does_Not_Exist()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);
        var result = await repository.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_True_For_Existing_Event()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);
        var createdEvent = Event.Create(
            "Exists Test",
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            50);

        await repository.AddAsync(createdEvent);
        await repository.SaveChangesAsync();
        var exists = await repository.ExistsAsync(createdEvent.Id);
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_False_For_Missing_Event()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);
        var exists = await repository.ExistsAsync(Guid.NewGuid());
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Event()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);
        var createdEvent = Event.Create(
            "Delete Test",
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10);
        await repository.AddAsync(createdEvent);
        await repository.SaveChangesAsync();
        await repository.DeleteAsync(createdEvent);
        await repository.SaveChangesAsync();
        var loadedEvent = await repository.GetByIdAsync(createdEvent.Id);
        Assert.Null(loadedEvent);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Return_All_Events_Ordered_By_StartAt()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);
        var laterEvent = Event.Create(
            "Later Event",
            null,
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow.AddDays(4),
            10);

        var earlierEvent = Event.Create(
            "Earlier Event",
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10);
        await repository.AddAsync(laterEvent);
        await repository.AddAsync(earlierEvent);
        await repository.SaveChangesAsync();
        var query = new GetEventsQueryDto{Page = 1, PageSize = 10};
        var result = await repository.GetEventsAsync(query);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Earlier Event", result.Items[0].Title);
        Assert.Equal("Later Event", result.Items[1].Title);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Filter_By_Title()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);
        var targetEvent = Event.Create(
            "CSharp Conference",
            null,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            10);
        var otherEvent = Event.Create(
            "Rust Workshop",
            null,
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow.AddDays(4),
            10);

        await repository.AddAsync(targetEvent);
        await repository.AddAsync(otherEvent);
        await repository.SaveChangesAsync();

        var query = new GetEventsQueryDto
        {
            Title = "csharp",
            Page = 1,
            PageSize = 10
        };

        var result = await repository.GetEventsAsync(query);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("CSharp Conference", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Filter_By_From_Date()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var oldEvent = Event.Create(
            "Old Event",
            null,
            UtcDate(2026, 1, 1),
            UtcDate(2026, 1, 2),
            10);

        var newEvent = Event.Create(
            "New Event",
            null,
            UtcDate(2026, 6, 1),
            UtcDate(2026, 6, 2),
            10);

        await repository.AddAsync(oldEvent);
        await repository.AddAsync(newEvent);
        await repository.SaveChangesAsync();

        var query = new GetEventsQueryDto
        {
            From = UtcDate(2026, 5, 1),
            Page = 1,
            PageSize = 10
        };

        var result = await repository.GetEventsAsync(query);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("New Event", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Filter_By_To_Date()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var earlyEvent = Event.Create(
            "Early Event",
            null,
            UtcDate(2026, 1, 1),
            UtcDate(2026, 1, 2),
            10);

        var lateEvent = Event.Create(
            "Late Event",
            null,
            UtcDate(2026, 6, 1),
            UtcDate(2026, 6, 2),
            10);

        await repository.AddAsync(earlyEvent);
        await repository.AddAsync(lateEvent);
        await repository.SaveChangesAsync();

        var query = new GetEventsQueryDto
        {
            To = UtcDate(2026, 2, 1),
            Page = 1,
            PageSize = 10
        };

        var result = await repository.GetEventsAsync(query);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Early Event", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Filter_By_Date_Range()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var tooEarlyEvent = Event.Create(
            "Too Early",
            null,
            UtcDate(2026, 1, 1),
            UtcDate(2026, 1, 2),
            10);

        var insideRangeEvent = Event.Create(
            "Inside Range",
            null,
            UtcDate(2026, 3, 1),
            UtcDate(2026, 3, 2),
            10);

        var tooLateEvent = Event.Create(
            "Too Late",
            null,
            UtcDate(2026, 7, 1),
            UtcDate(2026, 7, 2),
            10);

        await repository.AddAsync(tooEarlyEvent);
        await repository.AddAsync(insideRangeEvent);
        await repository.AddAsync(tooLateEvent);
        await repository.SaveChangesAsync();

        var query = new GetEventsQueryDto
        {
            From = UtcDate(2026, 2, 1),
            To = UtcDate(2026, 4, 1),
            Page = 1,
            PageSize = 10
        };

        var result = await repository.GetEventsAsync(query);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Inside Range", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Apply_Pagination()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var firstEvent = Event.Create(
            "Event 1",
            null,
            UtcDate(2026, 1, 1),
            UtcDate(2026, 1, 2),
            10);

        var secondEvent = Event.Create(
            "Event 2",
            null,
            UtcDate(2026, 1, 3),
            UtcDate(2026, 1, 4),
            10);

        var thirdEvent = Event.Create(
            "Event 3",
            null,
            UtcDate(2026, 1, 5),
            UtcDate(2026, 1, 6),
            10);

        await repository.AddAsync(firstEvent);
        await repository.AddAsync(secondEvent);
        await repository.AddAsync(thirdEvent);
        await repository.SaveChangesAsync();

        var query = new GetEventsQueryDto
        {
            Page = 2,
            PageSize = 1
        };

        var result = await repository.GetEventsAsync(query);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal("Event 2", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Apply_Combined_Filters_And_Pagination()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var firstMatchingEvent = Event.Create(
            "CSharp Basics",
            null,
            UtcDate(2026, 1, 1),
            UtcDate(2026, 1, 2),
            10
        );

        var secondMatchingEvent = Event.Create(
            "CSharp Advanced",
            null,
            UtcDate(2026, 2, 1),
            UtcDate(2026, 2, 2),
            10
        );

        var thirdMatchingEvent = Event.Create(
            "CSharp Internals",
            null,
            UtcDate(2026, 3, 1),
            UtcDate(2026, 3, 2),
            10);

        var nonMatchingEvent = Event.Create(
            "Rust Basics",
            null,
            UtcDate(2026, 2, 1),
            UtcDate(2026, 2, 2),
            10);

        await repository.AddAsync(firstMatchingEvent);
        await repository.AddAsync(secondMatchingEvent);
        await repository.AddAsync(thirdMatchingEvent);
        await repository.AddAsync(nonMatchingEvent);
        await repository.SaveChangesAsync();

        var query = new GetEventsQueryDto
        {
            Title = "csharp",
            From = UtcDate(2026, 1, 15),
            To = UtcDate(2026, 4, 1),
            Page = 2,
            PageSize = 1
        };

        var result = await repository.GetEventsAsync(query);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal("CSharp Internals", result.Items[0].Title);
    }

    private static DateTime UtcDate(int year, int month, int day)
    {
        return new DateTime(
            year,
            month,
            day,
            0,
            0,
            0,
            DateTimeKind.Utc);
    }
}