using Gloss.Domain.Projection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloss.Infrastructure.Projection;

internal sealed class ReviewerProjectionEntityConfiguration : IEntityTypeConfiguration<ReviewerProjection>
{
    public void Configure(EntityTypeBuilder<ReviewerProjection> builder)
    {
        builder.ToTable("reviewer_projections");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Content).IsRequired();
        builder.Property(p => p.Version).IsRequired();
        builder.Property(p => p.LastProcessedGlobalPosition).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
    }
}
