namespace EBooking.Bookings.Infrastructure;

using EBooking.Bookings.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Контекст базы данных сервиса бронирований.
/// </summary>
public sealed class BookingsDbContext : DbContext
{
    public BookingsDbContext(
        DbContextOptions<BookingsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}