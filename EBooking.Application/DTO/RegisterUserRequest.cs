namespace EBooking.Application.DTO;
using EBooking.Domain.Entities;

public sealed record RegisterUserRequest(
    string Login,
    string Password,
    UserRole Role = UserRole.User
);