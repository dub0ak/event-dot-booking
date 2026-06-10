namespace EBooking.Tests;

using EBooking.DataStore;
using EBooking.DTO;
using EBooking.Exceptions;
using EBooking.Services;
using Microsoft.EntityFrameworkCore;

public class EventsServiceTests
{
    private static EventsService CreateService()
    {
        var dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var context = new AppDbContext(options);

        return new EventsService(context);
    }

    private static CreateEventDto CreateValidCreateDto(
        string title = "Test Event",
        string? description = "Test Description",
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        return new CreateEventDto
        {
            Title = title,
            Description = description,
            StartAt = startAt ?? new DateTime(2026, 4, 10, 10, 0, 0),
            EndAt = endAt ?? new DateTime(2026, 4, 10, 12, 0, 0),
            TotalSeats = 10
        };
    }

    private static UpdateEventDto CreateValidUpdateDto(
        string title = "Updated Event",
        string? description = "Updated Description",
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        return new UpdateEventDto
        {
            Title = title,
            Description = description,
            StartAt = startAt ?? new DateTime(2026, 4, 11, 9, 0, 0),
            EndAt = endAt ?? new DateTime(2026, 4, 11, 11, 0, 0)
        };
    }

    [Fact]
    public async Task CreateEvent_ShouldCreateEvent()
    {
        var service = CreateService();
        var dto = CreateValidCreateDto();

        var result = await service.CreateEventAsync(dto);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(dto.Title, result.Title);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.StartAt, result.StartAt);
        Assert.Equal(dto.EndAt, result.EndAt);
        Assert.Equal(10, result.TotalSeats);
        Assert.Equal(10, result.AvailableSeats);
    }

    [Fact]
    public async Task GetEvents_ShouldReturnCreatedEvents()
    {
        var service = CreateService();

        await service.CreateEventAsync(CreateValidCreateDto(title: "Event 1"));
        await service.CreateEventAsync(CreateValidCreateDto(
            title: "Event 2",
            startAt: new DateTime(2026, 4, 11, 10, 0, 0),
            endAt: new DateTime(2026, 4, 11, 12, 0, 0)));

        var result = await service.GetEventsAsync(new GetEventsQueryDto());

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetEventById_ShouldReturnEvent_WhenEventExists()
    {
        var service = CreateService();
        var created = await service.CreateEventAsync(CreateValidCreateDto(title: "My Event"));

        var result = await service.GetEventByIdAsync(created.Id);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("My Event", result.Title);
    }

    [Fact]
    public async Task GetEventById_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        var service = CreateService();
        var missingId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetEventByIdAsync(missingId));

        Assert.Contains(missingId.ToString(), exception.Message);
    }

    [Fact]
    public async Task UpdateEvent_ShouldUpdateEvent_WhenEventExists()
    {
        var service = CreateService();
        var created = await service.CreateEventAsync(CreateValidCreateDto(title: "Old Title"));

        var updateDto = CreateValidUpdateDto(
            title: "New Title",
            description: "New Description",
            startAt: new DateTime(2026, 5, 1, 14, 0, 0),
            endAt: new DateTime(2026, 5, 1, 16, 0, 0));

        var result = await service.UpdateEventAsync(created.Id, updateDto);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("New Title", result.Title);
        Assert.Equal("New Description", result.Description);
        Assert.Equal(updateDto.StartAt, result.StartAt);
        Assert.Equal(updateDto.EndAt, result.EndAt);
    }

    [Fact]
    public async Task UpdateEvent_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        var service = CreateService();
        var updateDto = CreateValidUpdateDto();
        var missingId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateEventAsync(missingId, updateDto));

        Assert.Contains(missingId.ToString(), exception.Message);
    }

    [Fact]
    public async Task DeleteEvent_ShouldDeleteEvent_WhenEventExists()
    {
        var service = CreateService();
        var created = await service.CreateEventAsync(CreateValidCreateDto());

        await service.DeleteEventAsync(created.Id);
        var result = await service.GetEventsAsync(new GetEventsQueryDto());

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task DeleteEvent_ShouldThrowNotFoundException_WhenEventDoesNotExist()
    {
        var service = CreateService();
        var missingId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteEventAsync(missingId));

        Assert.Contains(missingId.ToString(), exception.Message);
    }

    [Fact]
    public async Task GetEvents_ShouldFilterByTitle_CaseInsensitive()
    {
        var service = CreateService();

        await service.CreateEventAsync(CreateValidCreateDto(title: "ASP.NET Meetup"));
        await service.CreateEventAsync(CreateValidCreateDto(
            title: "Python Workshop",
            startAt: new DateTime(2026, 4, 11, 10, 0, 0),
            endAt: new DateTime(2026, 4, 11, 12, 0, 0)));
        await service.CreateEventAsync(CreateValidCreateDto(
            title: "Advanced asp.net Core",
            startAt: new DateTime(2026, 4, 12, 10, 0, 0),
            endAt: new DateTime(2026, 4, 12, 12, 0, 0)));

        var result = await service.GetEventsAsync(new GetEventsQueryDto
        {
            Title = "asp.net"
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item =>
            Assert.Contains("asp.net", item.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetEvents_ShouldFilterByFrom()
    {
        var service = CreateService();

        await service.CreateEventAsync(CreateValidCreateDto(
            title: "Before",
            startAt: new DateTime(2026, 4, 10, 10, 0, 0),
            endAt: new DateTime(2026, 4, 10, 12, 0, 0)));
        await service.CreateEventAsync(CreateValidCreateDto(
            title: "After",
            startAt: new DateTime(2026, 4, 15, 10, 0, 0),
            endAt: new DateTime(2026, 4, 15, 12, 0, 0)));

        var result = await service.GetEventsAsync(new GetEventsQueryDto
        {
            From = new DateTime(2026, 4, 12, 0, 0, 0)
        });

        Assert.Single(result.Items);
        Assert.Equal("After", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEvents_ShouldFilterByTo()
    {
        var service = CreateService();

        await service.CreateEventAsync(CreateValidCreateDto(
            title: "Inside",
            startAt: new DateTime(2026, 4, 10, 10, 0, 0),
            endAt: new DateTime(2026, 4, 10, 12, 0, 0)));
        await service.CreateEventAsync(CreateValidCreateDto(
            title: "Outside",
            startAt: new DateTime(2026, 4, 20, 10, 0, 0),
            endAt: new DateTime(2026, 4, 20, 12, 0, 0)));

        var result = await service.GetEventsAsync(new GetEventsQueryDto
        {
            To = new DateTime(2026, 4, 15, 23, 59, 59)
        });

        Assert.Single(result.Items);
        Assert.Equal("Inside", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEvents_ShouldApplyPagination()
    {
        var service = CreateService();

        for (int i = 1; i <= 5; i++)
        {
            await service.CreateEventAsync(CreateValidCreateDto(
                title: $"Event {i}",
                startAt: new DateTime(2026, 4, i, 10, 0, 0),
                endAt: new DateTime(2026, 4, i, 12, 0, 0)));
        }

        var result = await service.GetEventsAsync(new GetEventsQueryDto
        {
            Page = 2,
            PageSize = 2
        });

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Event 3", result.Items[0].Title);
        Assert.Equal("Event 4", result.Items[1].Title);
    }

    [Fact]
    public async Task GetEvents_ShouldApplyCombinedFilters()
    {
        var service = CreateService();

        await service.CreateEventAsync(CreateValidCreateDto(
            title: "ASP.NET Basic",
            startAt: new DateTime(2026, 4, 10, 10, 0, 0),
            endAt: new DateTime(2026, 4, 10, 12, 0, 0)));

        await service.CreateEventAsync(CreateValidCreateDto(
            title: "ASP.NET Advanced",
            startAt: new DateTime(2026, 4, 20, 10, 0, 0),
            endAt: new DateTime(2026, 4, 20, 12, 0, 0)));

        await service.CreateEventAsync(CreateValidCreateDto(
            title: "Python Advanced",
            startAt: new DateTime(2026, 4, 20, 10, 0, 0),
            endAt: new DateTime(2026, 4, 20, 12, 0, 0)));

        var result = await service.GetEventsAsync(new GetEventsQueryDto
        {
            Title = "asp.net",
            From = new DateTime(2026, 4, 15, 0, 0, 0),
            To = new DateTime(2026, 4, 30, 23, 59, 59),
            Page = 1,
            PageSize = 10
        });

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("ASP.NET Advanced", result.Items[0].Title);
    }

    [Fact]
    public async Task GetEvents_ShouldThrowValidationException_WhenPageIsInvalid()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.GetEventsAsync(new GetEventsQueryDto
            {
                Page = 0,
                PageSize = 10
            }));

        Assert.Equal("Page must be greater than 0", exception.Message);
    }

    [Fact]
    public async Task GetEvents_ShouldThrowValidationException_WhenPageSizeIsInvalid()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.GetEventsAsync(new GetEventsQueryDto
            {
                Page = 1,
                PageSize = 0
            }));

        Assert.Equal("PageSize must be greater than 0", exception.Message);
    }

    [Fact]
    public async Task CreateEvent_ShouldThrowValidationException_WhenEndAtIsEarlierThanStartAt()
    {
        var service = CreateService();

        var dto = CreateValidCreateDto(
            startAt: new DateTime(2026, 4, 10, 12, 0, 0),
            endAt: new DateTime(2026, 4, 10, 10, 0, 0));

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateEventAsync(dto));

        Assert.Equal("EndAt must be later than StartAt", exception.Message);
    }

    [Fact]
    public async Task UpdateEvent_ShouldThrowValidationException_WhenEndAtIsEarlierThanStartAt()
    {
        var service = CreateService();
        var created = await service.CreateEventAsync(CreateValidCreateDto());

        var dto = CreateValidUpdateDto(
            startAt: new DateTime(2026, 4, 11, 12, 0, 0),
            endAt: new DateTime(2026, 4, 11, 11, 0, 0));

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateEventAsync(created.Id, dto));

        Assert.Equal("EndAt must be later than StartAt", exception.Message);
    }
}