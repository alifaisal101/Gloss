using Gloss.Domain.MergeRequests;
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
    }
}
