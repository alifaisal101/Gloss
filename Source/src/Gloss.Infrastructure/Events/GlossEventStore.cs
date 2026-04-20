using System.Text.Json;
using BuildingBlocks.Application.EventSourcing;
using BuildingBlocks.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gloss.Infrastructure.Events;

internal sealed class GlossEventStore(GlossDbContext context) : IEventStore
{
    public async Task AppendAsync<TPayload>(
        string streamId,
        TPayload payload,
        CancellationToken cancellationToken = default)
        where TPayload : notnull
    {
        await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                await AcquireStreamLockAsync(streamId, cancellationToken).ConfigureAwait(false);
                var position = await GetNextPositionAsync(streamId, cancellationToken).ConfigureAwait(false);
                context.Events.Add(EventRecord.Create(streamId, typeof(TPayload).Name, position, JsonSerializer.SerializeToDocument(payload)));
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    public Task AppendEventsAsync(
        string streamId,
        IReadOnlyList<IDomainEvent> events,
        long expectedVersion,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Event-sourced aggregate writes are not used in Gloss.");

    public async Task<long> GetVersionAsync(string streamId, CancellationToken cancellationToken = default)
        => await context.Events
            .Where(e => e.StreamId == streamId)
            .Select(e => (long?)e.Position)
            .MaxAsync(cancellationToken).ConfigureAwait(false) ?? -1L;

    public async Task<IReadOnlyList<StoredEvent>> ReadStreamAsync(
        string streamId,
        long fromPosition = 0,
        CancellationToken cancellationToken = default)
        => await context.Events
            .Where(e => e.StreamId == streamId && e.Position >= fromPosition)
            .OrderBy(e => e.Position)
            .Select(e => ToStoredEvent(e))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<StoredEvent>> QueryAsync(
        EventQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = context.Events.AsQueryable();
        if (query.StreamId is not null) q = q.Where(e => e.StreamId == query.StreamId);
        if (query.EventType is not null) q = q.Where(e => e.EventType == query.EventType);
        if (query.Since is not null) q = q.Where(e => e.OccurredAt >= query.Since);
        if (query.Until is not null) q = q.Where(e => e.OccurredAt <= query.Until);
        if (query.FromGlobalPosition is not null) q = q.Where(e => e.GlobalPosition >= query.FromGlobalPosition);
        q = q.OrderBy(e => e.GlobalPosition);
        if (query.Limit is not null) q = q.Take(query.Limit.Value);
        return await q.Select(e => ToStoredEvent(e)).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> AcquireStreamLockAsync(string streamId, CancellationToken cancellationToken)
        => await context.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({streamId}))", cancellationToken).ConfigureAwait(false);

    private async Task<long> GetNextPositionAsync(string streamId, CancellationToken cancellationToken)
        => await GetVersionAsync(streamId, cancellationToken).ConfigureAwait(false) + 1;

    private static StoredEvent ToStoredEvent(EventRecord e) => new()
    {
        Id = e.Id,
        StreamId = e.StreamId,
        EventType = e.EventType,
        Position = e.Position,
        GlobalPosition = e.GlobalPosition,
        Payload = e.Payload,
        OccurredAt = e.OccurredAt,
    };
}
