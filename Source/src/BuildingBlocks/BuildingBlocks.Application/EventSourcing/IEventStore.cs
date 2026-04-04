using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Application.EventSourcing;

public interface IEventStore
{
    Task AppendAsync<TPayload>(string streamId, TPayload payload, CancellationToken cancellationToken = default)
        where TPayload : notnull;

    Task AppendEventsAsync(
        string streamId,
        IReadOnlyList<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default);

    Task<long> GetVersionAsync(string streamId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredEvent>> ReadStreamAsync(
        string streamId,
        long fromPosition = 0,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredEvent>> QueryAsync(EventQuery query, CancellationToken cancellationToken = default);
}