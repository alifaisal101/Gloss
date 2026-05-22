using BuildingBlocks.Domain.Abstractions;

namespace Gloss.Domain.MergeRequests;

public interface IMrReviewRepository : IRepository<MrReview, Guid>
{
    Task<MrReview?> FindAsync(Guid mergeRequestId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MrReview>> ListByMergeRequestAsync(Guid mergeRequestId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MrReview>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);
}
