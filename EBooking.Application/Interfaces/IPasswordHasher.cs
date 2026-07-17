namespace EBooking.Application.Interfaces;

public interface IPasswordHasher
{
    // dotnet add EBooking.Infrastructure package BCrypt.Net-Next
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}