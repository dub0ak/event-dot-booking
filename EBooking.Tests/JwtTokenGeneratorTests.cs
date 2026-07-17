using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EBooking.Domain.Entities;
using EBooking.Infrastructure.Security;

namespace EBooking.Tests;

public sealed class JwtTokenGeneratorTests
{
    private const string Secret = "super-secret-test-key-with-at-least-32-chars-kekw";
    private readonly JwtTokenGenerator _generator = CreateGenerator();

    private static readonly JwtOptions Options = new()
    {
        Secret = Secret,
        Issuer = "EBooking.Tests",
        Audience = "EBooking.Tests.Client",
        LifetimeMinutes = 60
    };

    [Fact]
    public void GenerateToken_Should_Return_NonEmpty_String()
    {
        var user = CreateUser();
        var token = _generator.GenerateToken(user);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateToken_Should_Contain_User_Id_Claim()
    {
        var user = CreateUser();
        var token = ReadToken(_generator.GenerateToken(user));
        Assert.Contains(
            token.Claims,
            claim =>
                claim.Type == ClaimTypes.NameIdentifier &&
                claim.Value == user.Id.ToString()
        );
    }

    [Fact]
    public void GenerateToken_Should_Contain_Login_Claim()
    {
        var user = CreateUser();
        var token = ReadToken(_generator.GenerateToken(user));
        Assert.Contains(
            token.Claims,
            claim =>
                claim.Type == ClaimTypes.Name &&
                claim.Value == user.Login);
    }

    [Fact]
    public void GenerateToken_Should_Contain_Role_Claim()
    {
        var user = CreateUser(UserRole.Admin);
        var token = ReadToken(_generator.GenerateToken(user));
        Assert.Contains(
            token.Claims,
            claim =>
                claim.Type == ClaimTypes.Role &&
                claim.Value == UserRole.Admin.ToString()
        );
    }

    [Fact]
    public void GenerateToken_Should_Set_Expiration()
    {
        var user = CreateUser();
        var beforeGeneration = DateTime.UtcNow;
        var token = ReadToken(_generator.GenerateToken(user));
        var afterGeneration = DateTime.UtcNow;
        var minimumExpectedExpiration = beforeGeneration.AddMinutes(Options.LifetimeMinutes);
        var maximumExpectedExpiration = afterGeneration.AddMinutes(Options.LifetimeMinutes);
        Assert.InRange(
            token.ValidTo,
            minimumExpectedExpiration.AddSeconds(-1),
            maximumExpectedExpiration.AddSeconds(1)
        );
    }

    [Fact]
    public void GenerateToken_Should_Set_Issuer_And_Audience()
    {
        var user = CreateUser();
        var token = ReadToken(_generator.GenerateToken(user));
        Assert.Equal(Options.Issuer, token.Issuer);
        Assert.Contains(Options.Audience, token.Audiences);
    }

    private static JwtTokenGenerator CreateGenerator()
    {
        return new JwtTokenGenerator(
            Microsoft.Extensions.Options.Options.Create(Options)
        );
    }

    private static JwtSecurityToken ReadToken(string token)
    {
        return new JwtSecurityTokenHandler().ReadJwtToken(token);
    }

    private static User CreateUser(
        UserRole role = UserRole.User)
    {
        return User.Create(
            "test-user",
            "password-hash",
            role
        );
    }

    [Fact]
    public void GenerateToken_Should_Throw_When_User_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(
            () => _generator.GenerateToken(null!));
    }
}