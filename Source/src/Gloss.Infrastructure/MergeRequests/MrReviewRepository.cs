using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.EfCore;
using Gloss.Domain.MergeRequests;
using Microsoft.EntityFrameworkCore;

namespace Gloss.Infrastructure.MergeRequests;

internal sealed class MrReviewRepository(GlossDbContext db, IDomainContext domainContext)
    : EfCoreRepository<MrReview, Guid>(db, domainContext), IMrReviewRepository
{
    public async Task<MrReview?> FindAsync(Guid mergeRequestId, Guid userId, CancellationToken cancellationToken) =>
        await DbContext.Set<MrReview>()
            .FirstOrDefaultAsync(r => r.MergeRequestId == mergeRequestId && r.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<MrReview>> ListByMergeRequestAsync(Guid mergeRequestId, CancellationToken cancellationToken) =>
        await DbContext.Set<MrReview>()
            .Where(r => r.MergeRequestId == mergeRequestId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<MrReview>> ListByUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await DbContext.Set<MrReview>()
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
