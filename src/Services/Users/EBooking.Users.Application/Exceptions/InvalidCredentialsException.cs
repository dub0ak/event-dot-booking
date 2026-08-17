namespace EBooking.Users.Application;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid login or password"){}
}