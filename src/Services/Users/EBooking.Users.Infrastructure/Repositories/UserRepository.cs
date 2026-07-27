namespace EBooking.Users.Infrastructure;

using EBooking.Users.Application;
using EBooking.Users.Domain;
using Microsoft.EntityFrameworkCore;


public sealed class UserRepository : IUserRepository
{
    private readonly UserDbContext _context;

    public UserRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        IQueryable<User> query = _context.Users;

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<User?> GetByLoginAsync(
        string login,
        CancellationToken cancellationToken = default,
        bool asNoTracking = true)
    {
        IQueryable<User> query = _context.Users;

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(user => user.Login == login, cancellationToken);
    }

    public Task<bool> ExistsByLoginAsync(
        string login,
        CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(user => user.Login == login, cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}