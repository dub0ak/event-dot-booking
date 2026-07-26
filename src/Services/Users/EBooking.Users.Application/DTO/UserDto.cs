namespace EBooking.Users.Application;

using EBooking.Users.Domain;

public sealed record UserDto(
    Guid Id,
    string Login,
    UserRole Role
);