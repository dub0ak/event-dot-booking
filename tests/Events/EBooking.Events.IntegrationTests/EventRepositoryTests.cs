namespace EBooking.Events.IntegrationTests;

using EBooking.Events.Domain;
using EBooking.Events.Application;
using EBooking.Events.Infrastructure;
using Microsoft.EntityFrameworkCore;

[Collection(IntegrationTestCollection.Name)]
public sealed class EventRepositoryTests
{
    private static readonly DateTime DefaultStartAt =
        new(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime DefaultEndAt =
        new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly PostgresFixture _fixture;

    public EventRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Constructor_Should_Throw_When_Context_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EventRepository(null!));
    }

    [Fact]
    public async Task AddAsync_Should_Save_Event()
    {
        await _fixture.ResetDatabaseAsync();

        var eventItem = CreateEvent(
            title: "Test Event",
            description: "Test Description",
            totalSeats: 100);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EventRepository(context);

            await repository.AddAsync(eventItem);
            await repository.SaveChangesAsync();
        }

        // Используем новый DbContext, чтобы убедиться,
        // что данные действительно записаны в PostgreSQL,
        // а не остались только в ChangeTracker.
        await using var verificationContext =
            _fixture.CreateContext();

        var savedEvent = await verificationContext.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == eventItem.Id);

        Assert.NotNull(savedEvent);
        Assert.Equal(eventItem.Id, savedEvent!.Id);
        Assert.Equal("Test Event", savedEvent.Title);
        Assert.Equal("Test Description", savedEvent.Description);
        Assert.Equal(DefaultStartAt, savedEvent.StartAt);
        Assert.Equal(DefaultEndAt, savedEvent.EndAt);
        Assert.Equal(100, savedEvent.TotalSeats);
        Assert.Equal(100, savedEvent.AvailableSeats);
    }

    [Fact]
    public async Task AddAsync_Should_Throw_When_Event_Is_Null()
    {
        await _fixture.ResetDatabaseAsync();

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.AddAsync(null!));
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Event_When_It_Exists()
    {
        await _fixture.ResetDatabaseAsync();

        var eventItem = CreateEvent();

        await SeedAsync(eventItem);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetByIdAsync(eventItem.Id);

        Assert.NotNull(result);
        Assert.Equal(eventItem.Id, result!.Id);
        Assert.Equal(eventItem.Title, result.Title);
        Assert.Equal(eventItem.Description, result.Description);
        Assert.Equal(eventItem.StartAt, result.StartAt);
        Assert.Equal(eventItem.EndAt, result.EndAt);
        Assert.Equal(eventItem.TotalSeats, result.TotalSeats);
        Assert.Equal(eventItem.AvailableSeats, result.AvailableSeats);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Event_Does_Not_Exist()
    {
        await _fixture.ResetDatabaseAsync();

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetByIdAsync(
            Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Not_Track_Event_By_Default()
    {
        await _fixture.ResetDatabaseAsync();

        var eventItem = CreateEvent();

        await SeedAsync(eventItem);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetByIdAsync(eventItem.Id);

        Assert.NotNull(result);

        var entry = context.Entry(result!);

        Assert.Equal(EntityState.Detached, entry.State);
        Assert.Empty(context.ChangeTracker.Entries<Event>());
    }

    [Fact]
    public async Task GetByIdAsync_Should_Track_Event_When_AsNoTracking_Is_False()
    {
        await _fixture.ResetDatabaseAsync();

        var eventItem = CreateEvent();

        await SeedAsync(eventItem);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetByIdAsync(
            eventItem.Id,
            asNoTracking: false);

        Assert.NotNull(result);

        var entry = context.Entry(result!);

        Assert.Equal(EntityState.Unchanged, entry.State);
        Assert.Single(context.ChangeTracker.Entries<Event>());
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_True_When_Event_Exists()
    {
        await _fixture.ResetDatabaseAsync();

        var eventItem = CreateEvent();

        await SeedAsync(eventItem);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.ExistsAsync(eventItem.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_False_When_Event_Does_Not_Exist()
    {
        await _fixture.ResetDatabaseAsync();

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.ExistsAsync(
            Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Event()
    {
        await _fixture.ResetDatabaseAsync();

        var eventItem = CreateEvent();

        await SeedAsync(eventItem);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new EventRepository(context);

            var trackedEvent = await repository.GetByIdAsync(
                eventItem.Id,
                asNoTracking: false);

            Assert.NotNull(trackedEvent);

            await repository.DeleteAsync(trackedEvent!);
            await repository.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.CreateContext();

        var exists = await verificationContext.Events
            .AnyAsync(candidate => candidate.Id == eventItem.Id);

        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_Should_Throw_When_Event_Is_Null()
    {
        await _fixture.ResetDatabaseAsync();

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.DeleteAsync(null!));
    }

    private async Task SeedAsync(params Event[] events)
    {
        await using var context = _fixture.CreateContext();

        await context.Events.AddRangeAsync(events);
        await context.SaveChangesAsync();
    }

    private static Event CreateEvent(
        string title = "Test Event",
        string? description = "Test Description",
        int totalSeats = 10,
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        return Event.Create(
            title,
            description,
            startAt ?? DefaultStartAt,
            endAt ?? DefaultEndAt,
            totalSeats);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Return_All_Events()
    {
        await _fixture.ResetDatabaseAsync();

        var firstEvent = CreateEvent(
            title: "First Event",
            startAt: DefaultStartAt,
            endAt: DefaultEndAt);

        var secondEvent = CreateEvent(
            title: "Second Event",
            startAt: DefaultStartAt.AddDays(1),
            endAt: DefaultEndAt.AddDays(1));

        await SeedAsync(firstEvent, secondEvent);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                Page = 1,
                PageSize = 10
            });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Filter_Title_Case_Insensitively()
    {
        await _fixture.ResetDatabaseAsync();

        var conference = CreateEvent(
            title: "Technology Conference",
            startAt: DefaultStartAt,
            endAt: DefaultEndAt);

        var workshop = CreateEvent(
            title: "Programming Workshop",
            startAt: DefaultStartAt.AddDays(1),
            endAt: DefaultEndAt.AddDays(1));

        await SeedAsync(conference, workshop);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                Title = "CONFERENCE",
                Page = 1,
                PageSize = 10
            });

        var eventItem = Assert.Single(result.Items);

        Assert.Equal(conference.Id, eventItem.Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Filter_Title_By_Substring()
    {
        await _fixture.ResetDatabaseAsync();

        var firstEvent = CreateEvent(
            title: "International Technology Conference",
            startAt: DefaultStartAt,
            endAt: DefaultEndAt);

        var secondEvent = CreateEvent(
            title: "Music Festival",
            startAt: DefaultStartAt.AddDays(1),
            endAt: DefaultEndAt.AddDays(1));

        await SeedAsync(firstEvent, secondEvent);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                Title = "technology",
                Page = 1,
                PageSize = 10
            });

        var eventItem = Assert.Single(result.Items);

        Assert.Equal(firstEvent.Id, eventItem.Id);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Return_Empty_Result_When_Title_Does_Not_Match()
    {
        await _fixture.ResetDatabaseAsync();

        await SeedAsync(
            CreateEvent(
                title: "Technology Conference",
                startAt: DefaultStartAt,
                endAt: DefaultEndAt));

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                Title = "concert",
                Page = 1,
                PageSize = 10
            });

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Filter_By_From_Inclusively()
    {
        await _fixture.ResetDatabaseAsync();

        var beforeBoundary = CreateEvent(
            title: "Before",
            startAt: DefaultStartAt.AddMinutes(-1),
            endAt: DefaultEndAt);

        var atBoundary = CreateEvent(
            title: "At Boundary",
            startAt: DefaultStartAt,
            endAt: DefaultEndAt);

        var afterBoundary = CreateEvent(
            title: "After",
            startAt: DefaultStartAt.AddMinutes(1),
            endAt: DefaultEndAt.AddMinutes(1));

        await SeedAsync(
            beforeBoundary,
            atBoundary,
            afterBoundary);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                From = DefaultStartAt,
                Page = 1,
                PageSize = 10
            });

        Assert.Equal(2, result.TotalCount);

        Assert.DoesNotContain(
            result.Items,
            eventItem => eventItem.Id == beforeBoundary.Id);

        Assert.Contains(
            result.Items,
            eventItem => eventItem.Id == atBoundary.Id);

        Assert.Contains(
            result.Items,
            eventItem => eventItem.Id == afterBoundary.Id);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Filter_By_To_Inclusively()
    {
        await _fixture.ResetDatabaseAsync();

        var beforeBoundary = CreateEvent(
            title: "Before",
            startAt: DefaultStartAt.AddHours(-2),
            endAt: DefaultEndAt.AddMinutes(-1));

        var atBoundary = CreateEvent(
            title: "At Boundary",
            startAt: DefaultStartAt,
            endAt: DefaultEndAt);

        var afterBoundary = CreateEvent(
            title: "After",
            startAt: DefaultStartAt.AddMinutes(1),
            endAt: DefaultEndAt.AddMinutes(1));

        await SeedAsync(
            beforeBoundary,
            atBoundary,
            afterBoundary);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                To = DefaultEndAt,
                Page = 1,
                PageSize = 10
            });

        Assert.Equal(2, result.TotalCount);

        Assert.Contains(
            result.Items,
            eventItem => eventItem.Id == beforeBoundary.Id);

        Assert.Contains(
            result.Items,
            eventItem => eventItem.Id == atBoundary.Id);

        Assert.DoesNotContain(
            result.Items,
            eventItem => eventItem.Id == afterBoundary.Id);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Filter_By_Date_Range()
    {
        await _fixture.ResetDatabaseAsync();

        var firstEvent = CreateEvent(
            title: "First",
            startAt: DefaultStartAt,
            endAt: DefaultEndAt);

        var secondEvent = CreateEvent(
            title: "Second",
            startAt: DefaultStartAt.AddDays(1),
            endAt: DefaultEndAt.AddDays(1));

        var thirdEvent = CreateEvent(
            title: "Third",
            startAt: DefaultStartAt.AddDays(2),
            endAt: DefaultEndAt.AddDays(2));

        await SeedAsync(
            firstEvent,
            secondEvent,
            thirdEvent);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                From = secondEvent.StartAt,
                To = secondEvent.EndAt,
                Page = 1,
                PageSize = 10
            });

        var eventItem = Assert.Single(result.Items);

        Assert.Equal(secondEvent.Id, eventItem.Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Combine_Title_And_Date_Filters()
    {
        await _fixture.ResetDatabaseAsync();

        var matchingEvent = CreateEvent(
            title: "Technology Conference",
            startAt: DefaultStartAt.AddDays(1),
            endAt: DefaultEndAt.AddDays(1));

        var wrongTitle = CreateEvent(
            title: "Music Festival",
            startAt: DefaultStartAt.AddDays(1),
            endAt: DefaultEndAt.AddDays(1));

        var wrongDate = CreateEvent(
            title: "Technology Workshop",
            startAt: DefaultStartAt.AddDays(5),
            endAt: DefaultEndAt.AddDays(5));

        await SeedAsync(
            matchingEvent,
            wrongTitle,
            wrongDate);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                Title = "technology",
                From = DefaultStartAt,
                To = DefaultStartAt.AddDays(2),
                Page = 1,
                PageSize = 10
            });

        var eventItem = Assert.Single(result.Items);

        Assert.Equal(matchingEvent.Id, eventItem.Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Order_By_StartAt_Then_By_Id()
    {
        await _fixture.ResetDatabaseAsync();

        var laterEvent = CreateEvent(
            title: "Later Event",
            startAt: DefaultStartAt.AddDays(1),
            endAt: DefaultEndAt.AddDays(1));

        var firstSameDateEvent = CreateEvent(
            title: "Same Date A",
            startAt: DefaultStartAt,
            endAt: DefaultEndAt);

        var secondSameDateEvent = CreateEvent(
            title: "Same Date B",
            startAt: DefaultStartAt,
            endAt: DefaultEndAt);

        await SeedAsync(
            laterEvent,
            secondSameDateEvent,
            firstSameDateEvent);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                Page = 1,
                PageSize = 10
            });

        var expectedSameDateOrder =
            new[]
            {
                firstSameDateEvent,
                secondSameDateEvent
            }
            .OrderBy(eventItem => eventItem.Id)
            .Select(eventItem => eventItem.Id)
            .ToArray();

        var items = result.Items.ToArray();

        Assert.Equal(
            expectedSameDateOrder[0],
            items[0].Id
        );

        Assert.Equal(
            expectedSameDateOrder[1],
            items[1].Id
        );

        Assert.Equal(
            laterEvent.Id,
            items[2].Id
        );
    }

    [Fact]
    public async Task GetEventsAsync_Should_Apply_Pagination()
    {
        await _fixture.ResetDatabaseAsync();

        var events = Enumerable
            .Range(1, 5)
            .Select(index =>
                CreateEvent(
                    title: $"Event {index}",
                    startAt: DefaultStartAt.AddDays(index),
                    endAt: DefaultEndAt.AddDays(index)))
            .ToArray();

        await SeedAsync(events);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                Page = 2,
                PageSize = 2
            });

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count);

        var items = result.Items.ToArray();
        Assert.Equal(events[2].Id, items[0].Id);
        Assert.Equal(events[3].Id, items[1].Id);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Return_Empty_Page_When_Page_Is_Beyond_Result()
    {
        await _fixture.ResetDatabaseAsync();

        await SeedAsync(
            CreateEvent(
                title: "First",
                startAt: DefaultStartAt,
                endAt: DefaultEndAt),
            CreateEvent(
                title: "Second",
                startAt: DefaultStartAt.AddDays(1),
                endAt: DefaultEndAt.AddDays(1)));

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                Page = 10,
                PageSize = 10
            });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(10, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetEventsAsync_Should_Not_Track_Returned_Events()
    {
        await _fixture.ResetDatabaseAsync();

        await SeedAsync(CreateEvent());

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);

        var result = await repository.GetEventsAsync(
            new GetEventsQuery
            {
                Page = 1,
                PageSize = 10
            });

        Assert.Single(result.Items);
        Assert.Empty(context.ChangeTracker.Entries<Event>());
    }

    [Fact]
    public async Task GetTopEventsAsync_Should_Return_Ten_Events_Ordered_By_Sold_Percentage()
    {
        await _fixture.ResetDatabaseAsync();

        var events = Enumerable.Range(1, 12)
            .Select(index =>
            {
                var eventItem = CreateEvent(
                    title: $"Event {index}",
                    totalSeats: 100,
                    startAt: DefaultStartAt.AddDays(index),
                    endAt: DefaultEndAt.AddDays(index));

                eventItem.TryReserveSeats(index * 5);

                return eventItem;
            })
            .ToArray();

        await SeedAsync(events);

        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);
        var result = await repository.GetTopEventsAsync();
        Assert.Equal(10, result.Count);
        var resultArray = result.ToArray();
        Assert.Equal(events[11].Id, resultArray[0].Id);
        Assert.Equal(events[10].Id, resultArray[1].Id);
        Assert.Equal(events[9].Id, resultArray[2].Id);
        Assert.DoesNotContain(result, eventItem => eventItem.Id == events[0].Id);
        Assert.DoesNotContain(result, eventItem => eventItem.Id == events[1].Id);
    }

    [Fact]
    public async Task GetTopEventsAsync_Should_Order_By_Sold_Percentage_Not_Sold_Count()
    {
        await _fixture.ResetDatabaseAsync();

        var manySeats = CreateEvent(title: "Many Seats", totalSeats: 1000);
        manySeats.TryReserveSeats(500);
        var fewSeats = CreateEvent(
            title: "Few Seats",
            totalSeats: 10,
            startAt: DefaultStartAt.AddDays(1),
            endAt: DefaultEndAt.AddDays(1)
        );
        fewSeats.TryReserveSeats(9);
        await SeedAsync(manySeats, fewSeats);
        await using var context = _fixture.CreateContext();
        var repository = new EventRepository(context);
        var result = await repository.GetTopEventsAsync();
        var resultArray = result.ToArray();
        Assert.Equal(2, resultArray.Length);
        Assert.Equal(fewSeats.Id, resultArray[0].Id);
        Assert.Equal(manySeats.Id, resultArray[1].Id);
    }
}