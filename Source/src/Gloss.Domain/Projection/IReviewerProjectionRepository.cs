using BuildingBlocks.Domain.Abstractions;

namespace Gloss.Domain.Projection;

public interface IReviewerProjectionRepository : IRepository<ReviewerProjection, Guid>
{
    Task<ReviewerProjection?> GetCurrentAsync(CancellationToken cancellationToken = default);
}
