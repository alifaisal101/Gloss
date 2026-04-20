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
    public async Task<VoidResult> HandleAsync(CancellationToken cancellationToken)
    {
        var current = await projectionRepository.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var fromPosition = current is not null ? current.LastProcessedGlobalPosition + 1 : 0;

        var newEvents = await eventStore.QueryAsync(
            new EventQuery { FromGlobalPosition = fromPosition },
            cancellationToken).ConfigureAwait(false);

        if (newEvents.Count == 0) return Result.Success();

        var updated = await projectionEngine.BuildUpdatedProjectionAsync(
            current?.Content ?? string.Empty,
            newEvents,
            cancellationToken).ConfigureAwait(false);

        var lastPosition = newEvents.Max(e => e.GlobalPosition);
        var next = current is null
            ? ReviewerProjection.Seed(updated, lastPosition)
            : current.NextVersion(updated, lastPosition);

        domainContext.Save<ReviewerProjection, Guid>(next);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
