namespace EBooking.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EBooking.Application.DTO;
using EBooking.Domain.Entities;
using EBooking.Handlers;
using Xunit;

[Collection("Postgres collection")]
public class AuthorizationTests : IAsyncLifetime
{
    private const string _password = "TestPassword123!";

    private readonly PostgresFixture _fixture;

    private EBookingWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public AuthorizationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        _factory = new EBookingWebApplicationFactory(
            _fixture.ConnectionString);

        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Book_Event_Without_Token_Should_Return_Unauthorized()
    {
        var response = await _client.PostAsync(
            $"/events/{Guid.NewGuid()}/book",
            null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Get_Booking_Without_Token_Should_Return_Unauthorized()
    {
        var response = await _client.GetAsync(
            $"/bookings/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Delete_Booking_Without_Token_Should_Return_Unauthorized()
    {
        var response = await _client.DeleteAsync(
            $"/bookings/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_Event_As_User_Should_Return_Forbidden()
    {
        await AuthorizeAsync(
            "ordinary-user",
            UserRole.User);

        var response = await _client.PostAsJsonAsync(
            "/events",
            CreateEventRequest());

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Update_Event_As_User_Should_Return_Forbidden()
    {
        await AuthorizeAsync(
            "ordinary-user",
            UserRole.User);

        var response = await _client.PutAsJsonAsync(
            $"/events/{Guid.NewGuid()}",
            CreateUpdateEventRequest());

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Delete_Event_As_User_Should_Return_Forbidden()
    {
        await AuthorizeAsync(
            "ordinary-user",
            UserRole.User);

        var response = await _client.DeleteAsync(
            $"/events/{Guid.NewGuid()}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_Event_As_Admin_Should_Return_Created()
    {
        await AuthorizeAsync(
            "admin-user",
            UserRole.Admin);

        var response = await _client.PostAsJsonAsync(
            "/events",
            CreateEventRequest());

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ApiResult<EventDto>>();

        Assert.NotNull(result);
        Assert.True(result!.Status);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data!.Id);
    }

    [Fact]
    public async Task Owner_Should_Be_Able_To_Cancel_Own_Booking()
    {
        var eventId = await CreateEventAsAdminAsync();

        await AuthorizeAsync(
            "booking-owner",
            UserRole.User);

        var bookingId = await CreateBookingAsync(eventId);

        var response = await _client.DeleteAsync(
            $"/bookings/{bookingId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task Another_User_Should_Not_Be_Able_To_Cancel_Foreign_Booking()
    {
        var eventId = await CreateEventAsAdminAsync();

        await AuthorizeAsync(
            "booking-owner",
            UserRole.User);

        var bookingId = await CreateBookingAsync(eventId);

        await AuthorizeAsync(
            "another-user",
            UserRole.User);

        var response = await _client.DeleteAsync(
            $"/bookings/{bookingId}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Admin_Should_Be_Able_To_Cancel_Foreign_Booking()
    {
        var eventId = await CreateEventAsAdminAsync();

        await AuthorizeAsync(
            "booking-owner",
            UserRole.User);

        var bookingId = await CreateBookingAsync(eventId);

        await AuthorizeAsync(
            "cancellation-admin",
            UserRole.Admin);

        var response = await _client.DeleteAsync(
            $"/bookings/{bookingId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    private async Task<Guid> CreateEventAsAdminAsync()
    {
        await AuthorizeAsync(
            $"event-admin-{Guid.NewGuid():N}",
            UserRole.Admin);

        var response = await _client.PostAsJsonAsync(
            "/events",
            CreateEventRequest());

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ApiResult<EventDto>>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Data);

        return result.Data!.Id;
    }

    private async Task<Guid> CreateBookingAsync(Guid eventId)
    {
        var response = await _client.PostAsync(
            $"/events/{eventId}/book",
            null);

        Assert.Equal(
            HttpStatusCode.Accepted,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ApiResult<BookingDto>>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Data);

        return result.Data!.Id;
    }

    private async Task AuthorizeAsync(
        string login,
        UserRole role)
    {
        var token = await RegisterAndLoginAsync(
            login,
            role);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }

    private async Task<string> RegisterAndLoginAsync(string login, UserRole role)
    {
        var registerRequest = new RegisterUserRequest(login, _password, role);

        var registerResponse = await _client.PostAsJsonAsync(
            "/auth/register",
            registerRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            registerResponse.StatusCode);

        var loginRequest = new LoginRequest(login, _password);

        var loginResponse = await _client.PostAsJsonAsync(
            "/auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var result = await loginResponse.Content
            .ReadFromJsonAsync<
                ApiResult<AuthenticationResponse>>();

        Assert.NotNull(result);
        Assert.True(result!.Status);
        Assert.NotNull(result.Data);
        Assert.False(
            string.IsNullOrWhiteSpace(result.Data!.Token));

        return result.Data.Token;
    }

    private static CreateEventDto CreateEventRequest()
    {
        return new CreateEventDto
        {
            Title = "Authorization Test Event",
            Description = "Created by integration test",
            StartAt = DateTime.UtcNow.AddDays(5),
            EndAt = DateTime.UtcNow.AddDays(6),
            TotalSeats = 10
        };
    }

    private static UpdateEventDto CreateUpdateEventRequest()
    {
        return new UpdateEventDto
        {
            Title = "Updated Authorization Test Event",
            Description = "Updated by integration test",
            StartAt = DateTime.UtcNow.AddDays(7),
            EndAt = DateTime.UtcNow.AddDays(8)
        };
    }
}