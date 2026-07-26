namespace EBooking.Events.Infrastructure;

using EBooking.Events.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Контекст базы данных сервиса мероприятий.
/// </summary>
public sealed class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) {}

    /// <summary>
    /// Мероприятия.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}