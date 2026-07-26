using EBooking.Application.Interfaces;
using EBooking.Domain.Entities;
using EBooking.Infrastructure.DataStore;
using Microsoft.EntityFrameworkCore;

namespace EBooking.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
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

        return await query.FirstOrDefaultAsync(
            user => user.Id == id,
            cancellationToken);
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

        return await query.FirstOrDefaultAsync(
            user => user.Login == login,
            cancellationToken);
    }

    public Task<bool> ExistsByLoginAsync(
        string login,
        CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(
            user => user.Login == login,
            cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(
            user,
            cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}