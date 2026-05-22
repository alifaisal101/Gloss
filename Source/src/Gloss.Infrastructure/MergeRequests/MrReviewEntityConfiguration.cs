using System.Text.Json;
using Gloss.Domain.MergeRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class MrReviewEntityConfiguration : IEntityTypeConfiguration<MrReview>
{
    public void Configure(EntityTypeBuilder<MrReview> builder)
    {
        builder.ToTable("mr_reviews");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.MergeRequestId, x.UserId }).IsUnique();

        builder.Property(x => x.Status)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<MergeRequestStatus>(v, (JsonSerializerOptions?)null)!);

        builder.Property(x => x.ReviewJobId);
        builder.HasOne<MergeRequest>().WithMany().HasForeignKey(x => x.MergeRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}
