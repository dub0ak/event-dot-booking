namespace EBooking.Application.DTO;

public sealed record LoginRequest(
    string Login,
    string Password
);