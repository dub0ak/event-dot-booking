namespace EBooking.Events.Infrastructure;

using EBooking.Events.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Конфигурация хранения мероприятия.
/// </summary>
internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(eventItem => eventItem.Id);
        builder.Property(eventItem => eventItem.Id).ValueGeneratedNever();
        builder.Property(eventItem => eventItem.Title).IsRequired().HasMaxLength(200);
        builder.Property(eventItem => eventItem.Description).HasMaxLength(1000);
        builder.Property(eventItem => eventItem.StartAt).IsRequired();
        builder.Property(eventItem => eventItem.EndAt).IsRequired();
        builder.Property(eventItem => eventItem.TotalSeats).IsRequired();
        builder.Property(eventItem => eventItem.AvailableSeats).IsRequired();
    }
}