using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.EfCore;
using Gloss.Domain.MergeRequests;
using Microsoft.EntityFrameworkCore;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class DraftCommentRepository(GlossDbContext db, IDomainContext domainContext)
    : EfCoreRepository<DraftComment, Guid>(db, domainContext), IDraftCommentRepository
{
    public async Task<IReadOnlyList<DraftComment>> ListByMrReviewAsync(Guid mrReviewId, CancellationToken cancellationToken) =>
        await DbContext.Set<DraftComment>()
            .Where(dc => dc.MrReviewId == mrReviewId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
