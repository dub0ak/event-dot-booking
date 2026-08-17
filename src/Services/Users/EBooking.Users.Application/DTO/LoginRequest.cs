namespace EBooking.Users.Application;

public sealed record LoginRequest(
    string Login,
    string Password
);