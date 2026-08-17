namespace EBooking.Users.Application;

public sealed record RegisterUserRequest(
    string Login,
    string Password
);