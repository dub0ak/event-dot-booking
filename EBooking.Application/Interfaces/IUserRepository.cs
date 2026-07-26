using EBooking.Domain.Entities;

namespace EBooking.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true);

    Task<User?> GetByLoginAsync(
        string login,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true);

    Task<bool> ExistsByLoginAsync(
        string login,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}