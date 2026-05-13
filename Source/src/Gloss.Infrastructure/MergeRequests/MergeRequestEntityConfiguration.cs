using System.Text.Json;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class MergeRequestEntityConfiguration : IEntityTypeConfiguration<MergeRequest>
{
    public void Configure(EntityTypeBuilder<MergeRequest> builder)
    {
        builder.ToTable("merge_requests");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RepositoryId, x.ProviderIid }).IsUnique();

        builder.Property(x => x.Status)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<MergeRequestStatus>(v, (JsonSerializerOptions?)null)!);

        builder.Property(x => x.PlatformStatus)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<PlatformMrStatus>(v, (JsonSerializerOptions?)null)!);

        builder.Property(x => x.ReviewJobId);
        builder.Property(x => x.IsApproved);
        builder.HasOne<Repository>().WithMany().HasForeignKey(x => x.RepositoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
