namespace EBooking.Users.IntegrationTests;

using EBooking.Users.Application;
using EBooking.Users.Domain;
using EBooking.Users.Infrastructure;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthenticationEndpointsTests
    : IAsyncLifetime
{
    private const string ValidPassword = "StrongPassword123!";

    private readonly PostgresFixture _fixture;

    private EBookingWebApplicationFactory _factory = null!;

    private HttpClient _client = null!;

    public AuthenticationEndpointsTests(
        PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        _factory =
            new EBookingWebApplicationFactory(
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
    public async Task Register_Should_Return_NoContent_And_Save_User()
    {
        var response = await RegisterAsync("test-user", ValidPassword);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var context = _fixture.CreateContext();

        var user = await context.Users.AsNoTracking().SingleAsync();

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("test-user", user.Login);
        Assert.Equal(UserRole.User, user.Role);
        Assert.NotEqual(ValidPassword, user.PasswordHash);

        var passwordHasher = new PasswordHasher();

        Assert.True(
            passwordHasher.Verify(
                ValidPassword,
                user.PasswordHash
            )
        );
    }

    [Fact]
    public async Task Register_Should_Normalize_Login()
    {
        var response = await RegisterAsync(
            "  Alice  ",
            ValidPassword
        );

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var context =
            _fixture.CreateContext();

        var user = await context.Users
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal("alice", user.Login);
    }

    [Fact]
    public async Task Register_Should_Return_BadRequest_When_Login_Already_Exists()
    {
        var firstResponse = await RegisterAsync(
            "alice",
            ValidPassword
        );

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstResponse.StatusCode
        );

        var secondResponse = await RegisterAsync(
            "  ALICE  ",
            "AnotherPassword123!");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode
        );

        await AssertErrorResponseAsync(
            secondResponse,
            HttpStatusCode.BadRequest,
            "User with login 'alice' already exists.");

        await using var context =_fixture.CreateContext();

        Assert.Equal(
            1,
            await context.Users.CountAsync()
        );
    }

    [Fact]
    public async Task Register_Should_Return_BadRequest_When_Login_Is_Empty()
    {
        var response = await RegisterAsync(
            "   ",
            ValidPassword
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "Login is required."
        );

        await AssertDatabaseIsEmptyAsync();
    }

    [Fact]
    public async Task Register_Should_Return_BadRequest_When_Password_Is_Empty()
    {
        var response = await RegisterAsync(
            "test-user",
            "   "
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "Password is required."
        );

        await AssertDatabaseIsEmptyAsync();
    }

    [Fact]
    public async Task Register_Should_Return_BadRequest_When_Password_Is_Too_Short()
    {
        var response = await RegisterAsync(
            "test-user",
            "1234567"
        );

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode
        );

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "Password must contain at least 8 characters."
        );

        await AssertDatabaseIsEmptyAsync();
    }

    [Fact]
    public async Task Login_Should_Return_User_And_Token_When_Credentials_Are_Valid()
    {
        var registerResponse = await RegisterAsync(
            "test-user",
            ValidPassword
        );

        Assert.Equal(
            HttpStatusCode.NoContent,
            registerResponse.StatusCode
        );

        var loginResponse = await LoginAsync(
            "test-user",
            ValidPassword
        );

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode
        );

        using var document = await ReadJsonAsync(loginResponse);

        var root = document.RootElement;

        Assert.True(root.GetProperty("status").GetBoolean());

        Assert.Equal(
            "User authenticated successfully",
            root.GetProperty("message").GetString()
        );

        var data = root.GetProperty("data");

        var user = data.GetProperty("user");

        Assert.NotEqual(
            Guid.Empty,
            user.GetProperty("id").GetGuid()
        );

        Assert.Equal(
            "test-user",
            user.GetProperty("login").GetString()
        );

        Assert.Equal(
            "User",
            user.GetProperty("role").GetString()
        );

        var token = data.GetProperty("token").GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        Assert.Equal(
            3,
            token!.Split('.').Length
        );
    }

    [Fact]
    public async Task Login_Should_Normalize_Login()
    {
        await RegisterAsync(
            "alice",
            ValidPassword
        );

        var response = await LoginAsync(
            "  ALICE  ",
            ValidPassword
        );

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using var document =
            await ReadJsonAsync(response);

        var login = document.RootElement
            .GetProperty("data")
            .GetProperty("user")
            .GetProperty("login")
            .GetString();

        Assert.Equal("alice", login);
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_User_Does_Not_Exist()
    {
        var response = await LoginAsync(
            "missing-user",
            ValidPassword
        );

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Invalid login or password"
        );
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_Password_Is_Incorrect()
    {
        await RegisterAsync(
            "test-user",
            ValidPassword
        );

        var response = await LoginAsync(
            "test-user",
            "IncorrectPassword123!"
        );

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Invalid login or password"
        );
    }

    [Fact]
    public async Task Login_Should_Return_Same_Error_For_Missing_User_And_Wrong_Password()
    {
        await RegisterAsync(
            "test-user",
            ValidPassword
        );

        var missingUserResponse = await LoginAsync(
            "missing-user",
            ValidPassword
        );

        var wrongPasswordResponse = await LoginAsync(
            "test-user",
            "IncorrectPassword123!"
        );

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            missingUserResponse.StatusCode
        );

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            wrongPasswordResponse.StatusCode
        );

        var missingUserMessage =
            await ReadErrorMessageAsync(
                missingUserResponse
            );

        var wrongPasswordMessage =
            await ReadErrorMessageAsync(
                wrongPasswordResponse
            );

        Assert.Equal(
            missingUserMessage,
            wrongPasswordMessage
        );

        Assert.Equal(
            "Invalid login or password",
            missingUserMessage
        );
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_Login_Is_Empty()
    {
        var response = await LoginAsync(
            "   ",
            ValidPassword
        );

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Invalid login or password"
        );
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_Password_Is_Empty()
    {
        await RegisterAsync(
            "test-user",
            ValidPassword);

        var response = await LoginAsync(
            "test-user",
            "   ");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Invalid login or password");
    }

    private Task<HttpResponseMessage> RegisterAsync(
        string login,
        string password)
    {
        return _client.PostAsJsonAsync(
            "/auth/register",
            new RegisterUserRequest(
                login,
                password));
    }

    private Task<HttpResponseMessage> LoginAsync(
        string login,
        string password)
    {
        return _client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(
                login,
                password));
    }

    private async Task AssertDatabaseIsEmptyAsync()
    {
        await using var context =
            _fixture.CreateContext();

        Assert.Empty(
            await context.Users
                .AsNoTracking()
                .ToListAsync());
    }

    private static async Task AssertErrorResponseAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode,
        string expectedMessage)
    {
        using var document =
            await ReadJsonAsync(response);

        var root =
            document.RootElement;

        Assert.Equal(
            (int)expectedStatusCode,
            root.GetProperty("statusCode")
                .GetInt32());

        Assert.Equal(
            expectedMessage,
            root.GetProperty("message")
                .GetString());
    }

    private static async Task<string?>
        ReadErrorMessageAsync(
            HttpResponseMessage response)
    {
        using var document =
            await ReadJsonAsync(response);

        return document.RootElement
            .GetProperty("message")
            .GetString();
    }

    private static async Task<JsonDocument>
        ReadJsonAsync(
            HttpResponseMessage response)
    {
        var content =
            await response.Content
                .ReadAsStringAsync();

        Assert.False(
            string.IsNullOrWhiteSpace(content));

        return JsonDocument.Parse(content);
    }
}