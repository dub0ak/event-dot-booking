namespace EBooking.Application.DTO;

public sealed record AuthenticationResponse(
    UserDto User,
    string Token
);