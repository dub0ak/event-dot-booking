namespace EBooking.Users.Application;

public interface IAuthenticationService
{
    Task<UserDto> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default
    );

    Task<AuthenticationResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    );
}