using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.EfCore;
using Gloss.Domain.MergeRequests;
using Microsoft.EntityFrameworkCore;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class MergeRequestRepository(GlossDbContext db, IDomainContext domainContext)
    : EfCoreRepository<MergeRequest, Guid>(db, domainContext), IMergeRequestRepository
{
    public async Task<IReadOnlyList<MergeRequest>> ListByRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken) =>
        await DbContext.Set<MergeRequest>()
            .Where(mr => mr.RepositoryId == repositoryId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<MergeRequest>> ListAllAsync(CancellationToken cancellationToken) =>
        await DbContext.Set<MergeRequest>()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
