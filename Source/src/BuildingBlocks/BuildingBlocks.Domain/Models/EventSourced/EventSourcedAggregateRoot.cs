using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Domain.Models.EventSourced;

public abstract class EventSourcedAggregateRoot<TId> : AggregateRoot<TId>
    where TId : notnull
{
    public long Version { get; private set; } = -1;

    protected EventSourcedAggregateRoot() { }
    protected EventSourcedAggregateRoot(TId id) : base(id) { }

    protected void Record(IDomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ApplyMethodDispatcher.Dispatch(this, @event);
        AddDomainEvent(@event);
    }

    internal void RehydrateFrom(IEnumerable<IDomainEvent> events, long version)
    {
        foreach (var @event in events) ApplyMethodDispatcher.Dispatch(this, @event);
        Version = version;
    }

    internal void MarkCommitted(long newVersion)
    {
        Version = newVersion;
        ClearDomainEvents();
    }
}