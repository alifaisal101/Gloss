using BuildingBlocks.Application.EventSourcing;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.Projection;

namespace Gloss.Application.Projection.UpdateProjection;

public sealed class UpdateProjectionHandler(
    IReviewerProjectionRepository projectionRepository,
    IEventStore eventStore,
    IProjectionEngine projectionEngine,
    IDomainContext domainContext)
{
    public Task<VoidResult> HandleAsync(CancellationToken cancellationToken)
    {
        _ = (projectionRepository, eventStore, projectionEngine, domainContext);
        throw new NotSupportedException();
    }
}
