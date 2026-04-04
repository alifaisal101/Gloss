using Gloss.Domain.MergeRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class DraftCommentEntityConfiguration : IEntityTypeConfiguration<DraftComment>
{
    public void Configure(EntityTypeBuilder<DraftComment> builder)
    {
        builder.ToTable("draft_comments");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.MergeRequestId);
    }
}
