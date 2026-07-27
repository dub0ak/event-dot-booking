namespace EBooking.Users.Domain;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Пользователь системы бронирования.
/// </summary>
public class User
{
    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Уникальный логин пользователя.
    /// </summary>
    public string Login { get; private set; } = null!;

    /// <summary>
    /// Хеш пароля пользователя.
    /// </summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>
    /// Роль пользователя.
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Создаёт нового пользователя.
    /// </summary>
    public static User Create(
        string login,
        string passwordHash,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new ValidationException(
                "Login cannot be empty."
            );
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ValidationException(
                "Password hash cannot be empty."
            );
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Login = login.Trim(),
            PasswordHash = passwordHash,
            Role = role
        };
    }

    private User() { }
}