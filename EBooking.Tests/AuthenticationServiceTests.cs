namespace EBooking.Tests;

using EBooking.Application.DTO;
using EBooking.Application.Services;
using EBooking.Domain.Entities;
using EBooking.Domain.Exceptions;
using EBooking.Infrastructure.DataStore;
using EBooking.Infrastructure.Repositories;
using EBooking.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using EBooking.Application.Interfaces;

public class AuthenticationServiceTests
{
    private sealed class TestEnvironment
    {
        public required AppDbContext Context { get; init; }

        public required UserRepository UserRepository { get; init; }

        public required AuthenticationService AuthenticationService
        {
            get;
            init;
        }
    }

    private sealed class TestJwtTokenGenerator : IJwtTokenGenerator
    {
        public string GenerateToken(User user)
        {
            return "test-jwt-token";
        }
    }

    private static TestEnvironment CreateTestEnvironment()
    {
        var databaseName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new AppDbContext(options);
        var userRepository = new UserRepository(context);
        var passwordHasher = new PasswordHasher();
        var jwtTokenGenerator = new TestJwtTokenGenerator();

        var authenticationService = new AuthenticationService(
            userRepository,
            passwordHasher,
            jwtTokenGenerator
        );

        return new TestEnvironment
        {
            Context = context,
            UserRepository = userRepository,
            AuthenticationService = authenticationService
        };
    }

    [Fact]
    public async Task RegisterAsync_Should_Create_User()
    {
        var environment = CreateTestEnvironment();

        var request = new RegisterUserRequest(
            "test-user",
            "StrongPassword123!");

        var result =
            await environment.AuthenticationService.RegisterAsync(
                request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("test-user", result.Login);
        Assert.Equal(UserRole.User, result.Role);
    }

    [Fact]
    public async Task RegisterAsync_Should_Normalize_Login()
    {
        var environment = CreateTestEnvironment();

        var request = new RegisterUserRequest(
            "  Alice  ",
            "StrongPassword123!");

        var result =
            await environment.AuthenticationService.RegisterAsync(
                request);

        Assert.Equal("alice", result.Login);
    }

    [Fact]
    public async Task RegisterAsync_Should_Save_User()
    {
        var environment = CreateTestEnvironment();

        var request = new RegisterUserRequest(
            "test-user",
            "StrongPassword123!");

        var result =
            await environment.AuthenticationService.RegisterAsync(
                request);

        var savedUser =
            await environment.UserRepository.GetByIdAsync(result.Id);

        Assert.NotNull(savedUser);
        Assert.Equal(result.Id, savedUser!.Id);
        Assert.Equal("test-user", savedUser.Login);
        Assert.Equal(UserRole.User, savedUser.Role);
    }

    [Fact]
    public async Task RegisterAsync_Should_Save_Hashed_Password()
    {
        var environment = CreateTestEnvironment();

        const string password = "StrongPassword123!";

        var request = new RegisterUserRequest(
            "test-user",
            password);

        var result =
            await environment.AuthenticationService.RegisterAsync(
                request);

        var savedUser =
            await environment.UserRepository.GetByIdAsync(result.Id);

        Assert.NotNull(savedUser);
        Assert.NotEqual(password, savedUser!.PasswordHash);

        var passwordHasher = new PasswordHasher();

        Assert.True(
            passwordHasher.Verify(
                password,
                savedUser.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Login_Already_Exists()
    {
        var environment = CreateTestEnvironment();

        var firstRequest = new RegisterUserRequest(
            "test-user",
            "StrongPassword123!");

        var secondRequest = new RegisterUserRequest(
            "test-user",
            "AnotherPassword123!");

        await environment.AuthenticationService.RegisterAsync(
            firstRequest);

        await Assert.ThrowsAsync<ValidationException>(
            () => environment.AuthenticationService.RegisterAsync(
                secondRequest));

        var usersCount =
            await environment.Context.Users.CountAsync();

        Assert.Equal(1, usersCount);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Login_Differs_Only_By_Case()
    {
        var environment = CreateTestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "Alice",
                "StrongPassword123!"));

        await Assert.ThrowsAsync<ValidationException>(
            () => environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "ALICE",
                    "AnotherPassword123!")));

        var usersCount =
            await environment.Context.Users.CountAsync();

        Assert.Equal(1, usersCount);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Login_Is_Empty()
    {
        var environment = CreateTestEnvironment();

        var request = new RegisterUserRequest(
            "   ",
            "StrongPassword123!");

        await Assert.ThrowsAsync<ValidationException>(
            () => environment.AuthenticationService.RegisterAsync(
                request));

        Assert.Empty(environment.Context.Users);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Password_Is_Empty()
    {
        var environment = CreateTestEnvironment();

        var request = new RegisterUserRequest(
            "test-user",
            "   ");

        await Assert.ThrowsAsync<ValidationException>(
            () => environment.AuthenticationService.RegisterAsync(
                request));

        Assert.Empty(environment.Context.Users);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Password_Is_Too_Short()
    {
        var environment = CreateTestEnvironment();

        var request = new RegisterUserRequest(
            "test-user",
            "1234567");

        await Assert.ThrowsAsync<ValidationException>(
            () => environment.AuthenticationService.RegisterAsync(
                request));

        Assert.Empty(environment.Context.Users);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_User_When_Credentials_Are_Valid()
    {
        var environment = CreateTestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                "StrongPassword123!"));

        var result =
            await environment.AuthenticationService.LoginAsync(
                new LoginRequest(
                    "test-user",
                    "StrongPassword123!"));

        Assert.NotEqual(Guid.Empty, result.User.Id);
        Assert.Equal("test-user", result.User.Login);
        Assert.Equal(UserRole.User, result.User.Role);
        Assert.Equal("test-jwt-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_Should_Normalize_Login()
    {
        var environment = CreateTestEnvironment();

        var registeredUser =
            await environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "alice",
                    "StrongPassword123!"));

        var result =
            await environment.AuthenticationService.LoginAsync(
                new LoginRequest(
                    "  ALICE  ",
                    "StrongPassword123!"));

        Assert.Equal(registeredUser.Id, result.User.Id);
        Assert.Equal("alice", result.User.Login);
        Assert.Equal("test-jwt-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_When_User_Does_Not_Exist()
    {
        var environment = CreateTestEnvironment();

        var exception =
            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => environment.AuthenticationService.LoginAsync(
                    new LoginRequest(
                        "missing-user",
                        "StrongPassword123!")));

        Assert.Equal(
            "Invalid login or password",
            exception.Message);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_When_Password_Is_Incorrect()
    {
        var environment = CreateTestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                "StrongPassword123!"));

        var exception =
            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => environment.AuthenticationService.LoginAsync(
                    new LoginRequest(
                        "test-user",
                        "IncorrectPassword123!")));

        Assert.Equal(
            "Invalid login or password",
            exception.Message);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_When_Login_Is_Empty()
    {
        var environment = CreateTestEnvironment();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => environment.AuthenticationService.LoginAsync(
                new LoginRequest(
                    "   ",
                    "StrongPassword123!")));
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_When_Password_Is_Empty()
    {
        var environment = CreateTestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                "StrongPassword123!"));

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => environment.AuthenticationService.LoginAsync(
                new LoginRequest(
                    "test-user",
                    "   ")));
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Same_Error_For_Missing_User_And_Wrong_Password()
    {
        var environment = CreateTestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                "StrongPassword123!"));

        var missingUserException =
            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => environment.AuthenticationService.LoginAsync(
                    new LoginRequest(
                        "missing-user",
                        "StrongPassword123!")));

        var wrongPasswordException =
            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => environment.AuthenticationService.LoginAsync(
                    new LoginRequest(
                        "test-user",
                        "IncorrectPassword123!")));

        Assert.Equal(
            missingUserException.Message,
            wrongPasswordException.Message);
    }

    [Fact]
    public async Task RegisterAsync_Should_Assign_User_Role_By_Default()
    {
        var environment = CreateTestEnvironment();

        var result =
            await environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "test-user",
                    "StrongPassword123!"));

        Assert.Equal(UserRole.User, result.Role);

        var savedUser =
            await environment.UserRepository.GetByIdAsync(result.Id);

        Assert.NotNull(savedUser);
        Assert.Equal(UserRole.User, savedUser!.Role);
    }

    [Fact]
    public async Task RegisterAsync_Should_Create_Admin_When_Admin_Role_Is_Provided()
    {
        var environment = CreateTestEnvironment();

        var result =
            await environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "admin",
                    "StrongPassword123!",
                    UserRole.Admin));

        Assert.Equal(UserRole.Admin, result.Role);

        var savedUser =
            await environment.UserRepository.GetByIdAsync(result.Id);

        Assert.NotNull(savedUser);
        Assert.Equal(UserRole.Admin, savedUser!.Role);
    }
}