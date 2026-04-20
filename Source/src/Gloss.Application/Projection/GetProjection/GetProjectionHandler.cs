using Gloss.Domain.Projection;

namespace Gloss.Application.Projection.GetProjection;

public sealed class GetProjectionHandler(IReviewerProjectionRepository projectionRepository)
{
    public Task<ProjectionResponse?> HandleAsync(CancellationToken cancellationToken)
    {
        _ = projectionRepository;
        throw new NotSupportedException();
    }
}
