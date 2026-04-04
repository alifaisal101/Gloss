using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Errors;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Domain.Models;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }
    protected AggregateRoot(TId id) : base(id) { }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected static VoidResult CheckRule(IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var error = rule.Check();
        return error != DomainError.None ? Result.Failure(error) : Result.Success();
    }
}