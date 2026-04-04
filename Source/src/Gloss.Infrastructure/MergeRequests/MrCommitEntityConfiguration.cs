using Gloss.Domain.MergeRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class MrCommitEntityConfiguration : IEntityTypeConfiguration<MrCommit>
{
    public void Configure(EntityTypeBuilder<MrCommit> builder)
    {
        builder.ToTable("mr_commits");
        builder.HasKey(x => x.Id);
        builder.HasOne<MergeRequest>().WithMany().HasForeignKey(x => x.MergeRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}
