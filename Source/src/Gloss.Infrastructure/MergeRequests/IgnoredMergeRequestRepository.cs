using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.EfCore;
using Gloss.Domain.MergeRequests;
using Microsoft.EntityFrameworkCore;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class IgnoredMergeRequestRepository(GlossDbContext db, IDomainContext domainContext)
    : EfCoreRepository<IgnoredMergeRequest, Guid>(db, domainContext), IIgnoredMergeRequestRepository
{
    public async Task<IReadOnlyList<IgnoredMergeRequest>> ListByRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken) =>
        await DbContext.Set<IgnoredMergeRequest>()
            .Where(i => i.RepositoryId == repositoryId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IgnoredMergeRequest?> FindAsync(Guid repositoryId, int providerIid, CancellationToken cancellationToken) =>
        await DbContext.Set<IgnoredMergeRequest>()
            .FirstOrDefaultAsync(i => i.RepositoryId == repositoryId && i.ProviderIid == providerIid, cancellationToken)
            .ConfigureAwait(false);
}
