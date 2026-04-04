using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.EfCore;
using Gloss.Domain.MergeRequests;
using Microsoft.EntityFrameworkCore;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class DraftCommentRepository(GlossDbContext db, IDomainContext domainContext)
    : EfCoreRepository<DraftComment, Guid>(db, domainContext), IDraftCommentRepository
{
    public async Task<IReadOnlyList<DraftComment>> ListByMergeRequestAsync(Guid mergeRequestId, CancellationToken cancellationToken) =>
        await DbContext.Set<DraftComment>()
            .Where(dc => dc.MergeRequestId == mergeRequestId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
