using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class IgnoredMergeRequestEntityConfiguration : IEntityTypeConfiguration<IgnoredMergeRequest>
{
    public void Configure(EntityTypeBuilder<IgnoredMergeRequest> builder)
    {
        builder.ToTable("ignored_merge_requests");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RepositoryId, x.ProviderIid }).IsUnique();

        builder.HasOne<Repository>().WithMany().HasForeignKey(x => x.RepositoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
