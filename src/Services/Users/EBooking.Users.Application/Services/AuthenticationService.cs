namespace EBooking.Users.Application;

using EBooking.Users.Domain;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<UserDto> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var login = NormalizeRegistrationLogin(request.Login);

        ValidatePassword(request.Password);

        var loginExists = await _userRepository.ExistsByLoginAsync(
            login,
            cancellationToken);

        if (loginExists)
        {
            throw new ValidationException($"User with login '{login}' already exists.");
        }

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(login, passwordHash, UserRole.User);
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        return new UserDto(user.Id, user.Login, user.Role);
    }

    public async Task<AuthenticationResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var login = NormalizeLogin(request.Login);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidCredentialsException();
        }

        var user = await _userRepository.GetByLoginAsync(login, cancellationToken);

        if (user is null)
        {
            throw new InvalidCredentialsException();
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var userDto = new UserDto(user.Id, user.Login, user.Role);
        var token = _jwtTokenGenerator.GenerateToken(user);

        return new AuthenticationResponse(userDto, token);
    }

    private static string NormalizeLogin(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new InvalidCredentialsException();
        }

        return login.Trim().ToLowerInvariant();
    }

    private static string NormalizeRegistrationLogin(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new ValidationException("Login is required.");
        }

        return login.Trim().ToLowerInvariant();
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required.");
        }

        if (password.Length < 8)
        {
            throw new ValidationException("Password must contain at least 8 characters.");
        }
    }
}