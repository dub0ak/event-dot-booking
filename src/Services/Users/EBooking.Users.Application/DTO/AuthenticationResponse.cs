namespace EBooking.Users.Application;

public sealed record AuthenticationResponse(
    UserDto User,
    string Token
);