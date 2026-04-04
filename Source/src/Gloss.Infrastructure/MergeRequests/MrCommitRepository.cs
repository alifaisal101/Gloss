using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.EfCore;
using Gloss.Domain.MergeRequests;
using Microsoft.EntityFrameworkCore;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class MrCommitRepository(GlossDbContext db, IDomainContext domainContext)
    : EfCoreRepository<MrCommit, Guid>(db, domainContext), IMrCommitRepository
{
    public async Task<IReadOnlyList<MrCommit>> ListByMergeRequestAsync(Guid mergeRequestId, CancellationToken cancellationToken) =>
        await db.Set<MrCommit>()
            .Where(c => c.MergeRequestId == mergeRequestId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
