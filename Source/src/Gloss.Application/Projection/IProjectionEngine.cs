using BuildingBlocks.Application.EventSourcing;

namespace Gloss.Application.Projection;

public interface IProjectionEngine
{
    Task<string> BuildUpdatedProjectionAsync(
        string currentProjection,
        IReadOnlyList<StoredEvent> newEvents,
        CancellationToken cancellationToken = default);
}
