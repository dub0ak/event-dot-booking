namespace EBooking.Application.DTO;

using EBooking.Domain.Entities;

public sealed record UserDto(
    Guid Id,
    string Login,
    UserRole Role
);