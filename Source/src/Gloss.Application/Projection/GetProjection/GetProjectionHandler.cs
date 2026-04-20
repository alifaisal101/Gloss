using Gloss.Domain.Projection;

namespace Gloss.Application.Projection.GetProjection;

public sealed class GetProjectionHandler(IReviewerProjectionRepository projectionRepository)
{
    public async Task<ProjectionResponse?> HandleAsync(CancellationToken cancellationToken)
    {
        var projection = await projectionRepository.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (projection is null) return null;
        return new ProjectionResponse(projection.Content, projection.Version, projection.UpdatedAt);
    }
}
