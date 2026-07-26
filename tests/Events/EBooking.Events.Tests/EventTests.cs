namespace EBooking.Events.Tests.Domain;

using EBooking.Events.Domain;

public sealed class EventTests
{
    private static readonly DateTime DefaultStartAt =
        new(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime DefaultEndAt =
        new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_Should_Create_Event_With_Expected_Values()
    {
        var eventItem = Event.Create(
            "Test Event",
            "Test Description",
            DefaultStartAt,
            DefaultEndAt,
            100);

        Assert.NotEqual(Guid.Empty, eventItem.Id);
        Assert.Equal("Test Event", eventItem.Title);
        Assert.Equal("Test Description", eventItem.Description);
        Assert.Equal(DefaultStartAt, eventItem.StartAt);
        Assert.Equal(DefaultEndAt, eventItem.EndAt);
        Assert.Equal(100, eventItem.TotalSeats);
        Assert.Equal(100, eventItem.AvailableSeats);
    }

    [Fact]
    public void Create_Should_Trim_Title_And_Description()
    {
        var eventItem = Event.Create(
            "  Test Event  ",
            "  Test Description  ",
            DefaultStartAt,
            DefaultEndAt,
            10);

        Assert.Equal("Test Event", eventItem.Title);
        Assert.Equal("Test Description", eventItem.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_Title_Is_Empty(string? title)
    {
        var exception = Assert.Throws<ValidationException>(() =>
            Event.Create(
                title!,
                null,
                DefaultStartAt,
                DefaultEndAt,
                10));

        Assert.Equal(
            "Event title cannot be empty.",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Normalize_Empty_Description_To_Null(
        string? description)
    {
        var eventItem = Event.Create(
            "Test Event",
            description,
            DefaultStartAt,
            DefaultEndAt,
            10);

        Assert.Null(eventItem.Description);
    }

    [Fact]
    public void Create_Should_Throw_When_EndAt_Equals_StartAt()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            Event.Create(
                "Test Event",
                null,
                DefaultStartAt,
                DefaultStartAt,
                10));

        Assert.Equal(
            "Event end date must be later than start date.",
            exception.Message);
    }

    [Fact]
    public void Create_Should_Throw_When_EndAt_Is_Earlier_Than_StartAt()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            Event.Create(
                "Test Event",
                null,
                DefaultStartAt,
                DefaultStartAt.AddMinutes(-1),
                10));

        Assert.Equal(
            "Event end date must be later than start date.",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Should_Throw_When_TotalSeats_Is_Not_Positive(
        int totalSeats)
    {
        var exception = Assert.Throws<ValidationException>(() =>
            Event.Create(
                "Test Event",
                null,
                DefaultStartAt,
                DefaultEndAt,
                totalSeats));

        Assert.Equal(
            "Total seats must be greater than 0.",
            exception.Message);
    }

    [Fact]
    public void Update_Should_Update_Event_Properties()
    {
        var eventItem = CreateEvent();

        var newStartAt = DefaultStartAt.AddDays(1);
        var newEndAt = DefaultEndAt.AddDays(1);

        eventItem.Update(
            "Updated Event",
            "Updated Description",
            newStartAt,
            newEndAt);

        Assert.Equal("Updated Event", eventItem.Title);
        Assert.Equal("Updated Description", eventItem.Description);
        Assert.Equal(newStartAt, eventItem.StartAt);
        Assert.Equal(newEndAt, eventItem.EndAt);
        Assert.Equal(10, eventItem.TotalSeats);
        Assert.Equal(10, eventItem.AvailableSeats);
    }

    [Fact]
    public void Update_Should_Trim_Title_And_Description()
    {
        var eventItem = CreateEvent();

        eventItem.Update(
            "  Updated Event  ",
            "  Updated Description  ",
            DefaultStartAt.AddDays(1),
            DefaultEndAt.AddDays(1));

        Assert.Equal("Updated Event", eventItem.Title);
        Assert.Equal("Updated Description", eventItem.Description);
    }

    [Fact]
    public void Update_Should_Throw_When_Title_Is_Empty()
    {
        var eventItem = CreateEvent();

        var exception = Assert.Throws<ValidationException>(() =>
            eventItem.Update(
                " ",
                null,
                DefaultStartAt.AddDays(1),
                DefaultEndAt.AddDays(1)));

        Assert.Equal(
            "Event title cannot be empty.",
            exception.Message);
    }

    [Fact]
    public void Update_Should_Throw_When_Date_Range_Is_Invalid()
    {
        var eventItem = CreateEvent();

        var exception = Assert.Throws<ValidationException>(() =>
            eventItem.Update(
                "Updated Event",
                null,
                DefaultEndAt,
                DefaultStartAt));

        Assert.Equal(
            "Event end date must be later than start date.",
            exception.Message);
    }

    [Fact]
    public void TryReserveSeats_Should_Decrease_AvailableSeats()
    {
        var eventItem = CreateEvent(totalSeats: 10);

        var result = eventItem.TryReserveSeats(3);

        Assert.True(result);
        Assert.Equal(7, eventItem.AvailableSeats);
    }

    [Fact]
    public void TryReserveSeats_Should_Return_False_When_Seats_Are_Insufficient()
    {
        var eventItem = CreateEvent(totalSeats: 2);

        var result = eventItem.TryReserveSeats(3);

        Assert.False(result);
        Assert.Equal(2, eventItem.AvailableSeats);
    }

    [Fact]
    public void TryReserveSeats_Should_Allow_Reserving_All_Available_Seats()
    {
        var eventItem = CreateEvent(totalSeats: 5);

        var result = eventItem.TryReserveSeats(5);

        Assert.True(result);
        Assert.Equal(0, eventItem.AvailableSeats);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryReserveSeats_Should_Throw_When_Count_Is_Not_Positive(
        int count)
    {
        var eventItem = CreateEvent();

        var exception = Assert.Throws<ValidationException>(() =>
            eventItem.TryReserveSeats(count));

        Assert.Equal(
            "Seat count must be greater than 0.",
            exception.Message);
    }

    [Fact]
    public void ReleaseSeats_Should_Increase_AvailableSeats()
    {
        var eventItem = CreateEvent(totalSeats: 10);
        eventItem.TryReserveSeats(4);

        eventItem.ReleaseSeats(2);

        Assert.Equal(8, eventItem.AvailableSeats);
    }

    [Fact]
    public void ReleaseSeats_Should_Not_Exceed_TotalSeats()
    {
        var eventItem = CreateEvent(totalSeats: 10);
        eventItem.TryReserveSeats(2);

        eventItem.ReleaseSeats(100);

        Assert.Equal(10, eventItem.AvailableSeats);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReleaseSeats_Should_Throw_When_Count_Is_Not_Positive(
        int count)
    {
        var eventItem = CreateEvent();

        var exception = Assert.Throws<ValidationException>(() =>
            eventItem.ReleaseSeats(count));

        Assert.Equal(
            "Seat count must be greater than 0.",
            exception.Message);
    }

    private static Event CreateEvent(int totalSeats = 10)
    {
        return Event.Create(
            "Test Event",
            "Test Description",
            DefaultStartAt,
            DefaultEndAt,
            totalSeats);
    }
}