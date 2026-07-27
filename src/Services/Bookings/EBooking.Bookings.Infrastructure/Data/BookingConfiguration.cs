namespace EBooking.Bookings.Infrastructure;

using EBooking.Bookings.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Конфигурация таблицы бронирований.
/// </summary>
internal sealed class BookingConfiguration
    : IEntityTypeConfiguration<Booking>
{
    public void Configure(
        EntityTypeBuilder<Booking> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("bookings");
        builder.HasKey(booking => booking.Id);
        builder.Property(booking => booking.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(booking => booking.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(booking => booking.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(booking => booking.SeatsCount).HasColumnName("seats_count").IsRequired();
        builder.Property(booking => booking.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(booking => booking.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(booking => booking.ProcessedAt).HasColumnName("processed_at");
        builder.HasIndex(booking => booking.UserId).HasDatabaseName("ix_bookings_user_id");
        builder.HasIndex(booking => booking.Status).HasDatabaseName("ix_bookings_status");
        builder.HasIndex(
                booking => new
                {
                    booking.UserId,
                    booking.Status
                })
            .HasDatabaseName(
                "ix_bookings_user_id_status"
            );
    }
}