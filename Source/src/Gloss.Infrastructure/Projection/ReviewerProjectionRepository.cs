using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.EfCore;
using Gloss.Domain.Projection;
using Microsoft.EntityFrameworkCore;

namespace Gloss.Infrastructure.Projection;

internal sealed class ReviewerProjectionRepository(GlossDbContext db, IDomainContext domainContext)
    : EfCoreRepository<ReviewerProjection, Guid>(db, domainContext), IReviewerProjectionRepository
{
    public async Task<ReviewerProjection?> GetCurrentAsync(CancellationToken cancellationToken = default)
        => await DbContext.Set<ReviewerProjection>()
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
}
