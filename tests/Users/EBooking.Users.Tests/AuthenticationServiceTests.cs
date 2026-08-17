namespace EBooking.Users.Tests;

using EBooking.Users.Application;
using EBooking.Users.Domain;

public sealed class AuthenticationServiceTests
{
    private const string GeneratedToken = "test-jwt-token";
    private const string ValidPassword = "StrongPassword123!";

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public IReadOnlyCollection<User> Users => _users;

        public int SaveChangesCalls { get; private set; }

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default,
            bool asNoTracking = true)
        {
            var user = _users.SingleOrDefault(user => user.Id == id);

            return Task.FromResult(user);
        }

        public Task<User?> GetByLoginAsync(
            string login,
            CancellationToken cancellationToken = default,
            bool asNoTracking = true)
        {
            var user = _users.SingleOrDefault(
                user => user.Login == login);

            return Task.FromResult(user);
        }

        public Task<bool> ExistsByLoginAsync(
            string login,
            CancellationToken cancellationToken = default)
        {
            var exists = _users.Any(
                user => user.Login == login);

            return Task.FromResult(exists);
        }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            _users.Add(user);

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;

            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        private const string HashPrefix = "hashed::";

        public string? LastHashedPassword { get; private set; }

        public string? LastVerifiedPassword { get; private set; }

        public string? LastVerifiedHash { get; private set; }

        public string Hash(string password)
        {
            LastHashedPassword = password;

            return HashPrefix + password;
        }

        public bool Verify(
            string password,
            string passwordHash)
        {
            LastVerifiedPassword = password;
            LastVerifiedHash = passwordHash;

            return passwordHash == HashPrefix + password;
        }
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public User? LastUser { get; private set; }

        public string GenerateToken(User user)
        {
            LastUser = user;

            return GeneratedToken;
        }
    }

    private sealed class TestEnvironment
    {
        public FakeUserRepository UserRepository { get; } = new();

        public FakePasswordHasher PasswordHasher { get; } = new();

        public FakeJwtTokenGenerator JwtTokenGenerator { get; } = new();

        public AuthenticationService AuthenticationService { get; }

        public TestEnvironment()
        {
            AuthenticationService = new AuthenticationService(
                UserRepository,
                PasswordHasher,
                JwtTokenGenerator);
        }
    }

    [Fact]
    public async Task RegisterAsync_Should_Create_User()
    {
        var environment = new TestEnvironment();

        var result = await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                ValidPassword));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("test-user", result.Login);
        Assert.Equal(UserRole.User, result.Role);
    }

    [Fact]
    public async Task RegisterAsync_Should_Normalize_Login()
    {
        var environment = new TestEnvironment();

        var result = await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "  Alice  ",
                ValidPassword));

        Assert.Equal("alice", result.Login);
    }

    [Fact]
    public async Task RegisterAsync_Should_Add_User_To_Repository()
    {
        var environment = new TestEnvironment();

        var result = await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                ValidPassword));

        var savedUser = Assert.Single(
            environment.UserRepository.Users);

        Assert.Equal(result.Id, savedUser.Id);
        Assert.Equal("test-user", savedUser.Login);
        Assert.Equal(UserRole.User, savedUser.Role);
    }

    [Fact]
    public async Task RegisterAsync_Should_Hash_Password()
    {
        var environment = new TestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                ValidPassword));

        Assert.Equal(
            ValidPassword,
            environment.PasswordHasher.LastHashedPassword);

        var savedUser = Assert.Single(
            environment.UserRepository.Users);

        Assert.Equal(
            $"hashed::{ValidPassword}",
            savedUser.PasswordHash);

        Assert.NotEqual(
            ValidPassword,
            savedUser.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_Should_Save_Changes()
    {
        var environment = new TestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                ValidPassword));

        Assert.Equal(
            1,
            environment.UserRepository.SaveChangesCalls);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Login_Already_Exists()
    {
        var environment = new TestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                ValidPassword));

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "test-user",
                    "AnotherPassword123!")));

        Assert.Equal(
            "User with login 'test-user' already exists.",
            exception.Message);

        Assert.Single(environment.UserRepository.Users);
        Assert.Equal(
            1,
            environment.UserRepository.SaveChangesCalls);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Login_Differs_Only_By_Case()
    {
        var environment = new TestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "Alice",
                ValidPassword));

        await Assert.ThrowsAsync<ValidationException>(
            () => environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "  ALICE  ",
                    "AnotherPassword123!")));

        Assert.Single(environment.UserRepository.Users);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Login_Is_Empty()
    {
        var environment = new TestEnvironment();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "   ",
                    ValidPassword)));

        Assert.Equal(
            "Login is required.",
            exception.Message);

        Assert.Empty(environment.UserRepository.Users);
        Assert.Equal(
            0,
            environment.UserRepository.SaveChangesCalls);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Password_Is_Empty()
    {
        var environment = new TestEnvironment();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "test-user",
                    "   ")));

        Assert.Equal(
            "Password is required.",
            exception.Message);

        Assert.Empty(environment.UserRepository.Users);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Password_Is_Too_Short()
    {
        var environment = new TestEnvironment();

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "test-user",
                    "1234567")));

        Assert.Equal(
            "Password must contain at least 8 characters.",
            exception.Message);

        Assert.Empty(environment.UserRepository.Users);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_When_Request_Is_Null()
    {
        var environment = new TestEnvironment();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => environment.AuthenticationService.RegisterAsync(
                null!));

        Assert.Empty(environment.UserRepository.Users);
    }

    [Fact]
    public async Task RegisterAsync_Should_Assign_User_Role_By_Default()
    {
        var environment = new TestEnvironment();

        var result = await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                ValidPassword));

        Assert.Equal(UserRole.User, result.Role);

        var savedUser = Assert.Single(
            environment.UserRepository.Users);

        Assert.Equal(UserRole.User, savedUser.Role);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_User_And_Token_When_Credentials_Are_Valid()
    {
        var environment = new TestEnvironment();

        var registeredUser =
            await environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "test-user",
                    ValidPassword));

        var result = await environment.AuthenticationService.LoginAsync(
            new LoginRequest(
                "test-user",
                ValidPassword));

        Assert.Equal(registeredUser.Id, result.User.Id);
        Assert.Equal("test-user", result.User.Login);
        Assert.Equal(UserRole.User, result.User.Role);
        Assert.Equal(GeneratedToken, result.Token);
    }

    [Fact]
    public async Task LoginAsync_Should_Normalize_Login()
    {
        var environment = new TestEnvironment();

        var registeredUser =
            await environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "alice",
                    ValidPassword));

        var result = await environment.AuthenticationService.LoginAsync(
            new LoginRequest(
                "  ALICE  ",
                ValidPassword));

        Assert.Equal(registeredUser.Id, result.User.Id);
        Assert.Equal("alice", result.User.Login);
    }

    [Fact]
    public async Task LoginAsync_Should_Verify_Password()
    {
        var environment = new TestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                ValidPassword));

        await environment.AuthenticationService.LoginAsync(
            new LoginRequest(
                "test-user",
                ValidPassword));

        Assert.Equal(
            ValidPassword,
            environment.PasswordHasher.LastVerifiedPassword);

        Assert.Equal(
            $"hashed::{ValidPassword}",
            environment.PasswordHasher.LastVerifiedHash);
    }

    [Fact]
    public async Task LoginAsync_Should_Generate_Token_For_Found_User()
    {
        var environment = new TestEnvironment();

        var registeredUser =
            await environment.AuthenticationService.RegisterAsync(
                new RegisterUserRequest(
                    "test-user",
                    ValidPassword));

        await environment.AuthenticationService.LoginAsync(
            new LoginRequest(
                "test-user",
                ValidPassword));

        Assert.NotNull(
            environment.JwtTokenGenerator.LastUser);

        Assert.Equal(
            registeredUser.Id,
            environment.JwtTokenGenerator.LastUser!.Id);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_When_User_Does_Not_Exist()
    {
        var environment = new TestEnvironment();

        var exception =
            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => environment.AuthenticationService.LoginAsync(
                    new LoginRequest(
                        "missing-user",
                        ValidPassword)));

        Assert.Equal(
            "Invalid login or password",
            exception.Message);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_When_Password_Is_Incorrect()
    {
        var environment = new TestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                ValidPassword));

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
        var environment = new TestEnvironment();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => environment.AuthenticationService.LoginAsync(
                new LoginRequest(
                    "   ",
                    ValidPassword)));
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_When_Password_Is_Empty()
    {
        var environment = new TestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                ValidPassword));

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => environment.AuthenticationService.LoginAsync(
                new LoginRequest(
                    "test-user",
                    "   ")));
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_When_Request_Is_Null()
    {
        var environment = new TestEnvironment();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => environment.AuthenticationService.LoginAsync(
                null!));
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Same_Error_For_Missing_User_And_Wrong_Password()
    {
        var environment = new TestEnvironment();

        await environment.AuthenticationService.RegisterAsync(
            new RegisterUserRequest(
                "test-user",
                ValidPassword));

        var missingUserException =
            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => environment.AuthenticationService.LoginAsync(
                    new LoginRequest(
                        "missing-user",
                        ValidPassword)));

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
}