using BuildingBlocks.Domain.Models;

namespace BuildingBlocks.Infrastructure.Persistence.Context;

internal interface IDomainContextTracker
{
    void Attach<T, TId>(T aggregate) where T : AggregateRoot<TId> where TId : notnull;
    bool IsAttached(object aggregate);
}