namespace EBooking.Users.Application;

using EBooking.Users.Domain;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}