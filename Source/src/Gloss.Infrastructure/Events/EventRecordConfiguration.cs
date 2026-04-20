using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloss.Infrastructure.Events;

internal sealed class EventRecordConfiguration : IEntityTypeConfiguration<EventRecord>
{
    public void Configure(EntityTypeBuilder<EventRecord> builder)
    {
        builder.ToTable("event_store");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.StreamId).HasMaxLength(500).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Position).IsRequired();
        builder.Property(e => e.GlobalPosition).UseIdentityAlwaysColumn().ValueGeneratedOnAdd();
        builder.Property(e => e.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.HasIndex(e => new { e.StreamId, e.Position }).IsUnique();
        builder.HasIndex(e => e.Payload).HasMethod("gin");
        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => e.EventType);
    }
}
