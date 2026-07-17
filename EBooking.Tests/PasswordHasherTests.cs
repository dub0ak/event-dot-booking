namespace EBooking.Tests;

using EBooking.Infrastructure.Security;
using Xunit;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_Should_Return_Hash_Different_From_Password()
    {
        var passwordHasher = new PasswordHasher();
        const string password = "StrongPassword123!";

        var passwordHash = passwordHasher.Hash(password);

        Assert.NotEmpty(passwordHash);
        Assert.NotEqual(password, passwordHash);
    }

    [Fact]
    public void Hash_Should_Return_Different_Hashes_For_Same_Password()
    {
        var passwordHasher = new PasswordHasher();
        const string password = "StrongPassword123!";

        var firstHash = passwordHasher.Hash(password);
        var secondHash = passwordHasher.Hash(password);

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void Verify_Should_Return_True_When_Password_Is_Correct()
    {
        var passwordHasher = new PasswordHasher();
        const string password = "StrongPassword123!";

        var passwordHash = passwordHasher.Hash(password);

        var result = passwordHasher.Verify(
            password,
            passwordHash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_Should_Return_False_When_Password_Is_Incorrect()
    {
        var passwordHasher = new PasswordHasher();
        const string password = "StrongPassword123!";

        var passwordHash = passwordHasher.Hash(password);

        var result = passwordHasher.Verify(
            "IncorrectPassword123!",
            passwordHash);

        Assert.False(result);
    }

    [Fact]
    public void Hash_Should_Throw_When_Password_Is_Empty()
    {
        var passwordHasher = new PasswordHasher();

        Assert.Throws<ArgumentException>(
            () => passwordHasher.Hash(string.Empty));
    }

    [Fact]
    public void Verify_Should_Throw_When_Hash_Is_Empty()
    {
        var passwordHasher = new PasswordHasher();

        Assert.Throws<ArgumentException>(
            () => passwordHasher.Verify(
                "StrongPassword123!",
                string.Empty));
    }
}