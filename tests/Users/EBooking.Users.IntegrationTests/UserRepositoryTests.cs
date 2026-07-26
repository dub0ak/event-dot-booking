namespace EBooking.Users.IntegrationTests;

using EBooking.Users.Domain;
using EBooking.Users.Infrastructure;
using Microsoft.EntityFrameworkCore;

[Collection(IntegrationTestCollection.Name)]
public class UserRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public UserRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_Should_Save_User()
    {
        await _fixture.ResetDatabaseAsync();

        var user = CreateUser(
            login: "test-user",
            passwordHash: "test-password-hash");

        await using (var context = _fixture.CreateContext())
        {
            var userRepository = new UserRepository(context);

            await userRepository.AddAsync(user);
            await userRepository.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.CreateContext();

        var verificationRepository =
            new UserRepository(verificationContext);

        var loadedUser =
            await verificationRepository.GetByIdAsync(user.Id);

        Assert.NotNull(loadedUser);
        Assert.Equal(user.Id, loadedUser!.Id);
        Assert.Equal("test-user", loadedUser.Login);
        Assert.Equal("test-password-hash", loadedUser.PasswordHash);
        Assert.Equal(UserRole.User, loadedUser.Role);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_User_Does_Not_Exist()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var userRepository = new UserRepository(context);

        var result = await userRepository.GetByIdAsync(
            Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByLoginAsync_Should_Return_User_When_Login_Exists()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var userRepository = new UserRepository(context);

        var user = CreateUser(login: "alice");

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        var result = await userRepository.GetByLoginAsync("alice");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
        Assert.Equal("alice", result.Login);
        Assert.Equal(UserRole.User, result.Role);
    }

    [Fact]
    public async Task GetByLoginAsync_Should_Return_Null_When_Login_Does_Not_Exist()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var userRepository = new UserRepository(context);

        var result = await userRepository.GetByLoginAsync(
            "missing-user");

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsByLoginAsync_Should_Return_True_When_Login_Exists()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var userRepository = new UserRepository(context);

        var user = CreateUser(login: "existing-user");

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        var exists = await userRepository.ExistsByLoginAsync(
            "existing-user");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByLoginAsync_Should_Return_False_When_Login_Does_Not_Exist()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var userRepository = new UserRepository(context);

        var exists = await userRepository.ExistsByLoginAsync(
            "missing-user");

        Assert.False(exists);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Throw_When_Login_Is_Duplicated()
    {
        await _fixture.ResetDatabaseAsync();
        await using var context = _fixture.CreateContext();

        var userRepository = new UserRepository(context);

        var firstUser = CreateUser(
            login: "duplicate-login",
            passwordHash: "first-password-hash");

        var secondUser = CreateUser(
            login: "duplicate-login",
            passwordHash: "second-password-hash");

        await userRepository.AddAsync(firstUser);
        await userRepository.AddAsync(secondUser);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => userRepository.SaveChangesAsync());
    }

    private static User CreateUser(
        string login = "test-user",
        string passwordHash = "test-password-hash",
        UserRole role = UserRole.User)
    {
        return User.Create(
            login,
            passwordHash,
            role
        );
    }
}