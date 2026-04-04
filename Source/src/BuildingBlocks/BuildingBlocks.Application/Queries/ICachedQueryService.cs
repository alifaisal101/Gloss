using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Models;

namespace BuildingBlocks.Application.Queries;

/// <summary>
/// <para>Two-level query cache:</para>
/// <para>
///   Level 1: Query hash → QueryIndex (ID list + total count)
///            Short TTL, tagged for aggregate-type invalidation.
/// </para>
/// <para>
///   Level 2: Entity ID → full aggregate (individual FusionCache entries)
///            Longer TTL, managed by CachedRepository automatically.
/// </para>
/// <para>
/// When an entity changes:
///   1. CacheInvalidationHook evicts that ONE entity cache entry
///   2. CacheInvalidationHook evicts ALL query indices tagged with that type
///   3. Next query: re-fetch ID list (cheap) → resolve entities (most still cached)
/// </para>
/// <para>Returns aggregates directly — no DTOs needed.</para>
/// </summary>
public interface ICachedQueryService
{
    /// <summary>
    /// Two-level cached paged query. Returns aggregates directly.
    /// The aggregate IS the read model for standard CRUD.
    /// </summary>
    Task<PagedResult<T>> QueryAsync<TQuery, T, TId>(
        TQuery query,
        IRepository<T, TId> repo,
        Func<TQuery, CancellationToken, Task<(IReadOnlyList<TId> Ids, int Total)>> indexFactory,
        CancellationToken cancellationToken)
        where TQuery : IPagedQuery
        where T : AggregateRoot<TId>, ICacheable
        where TId : notnull;

    /// <summary>
    /// Two-level cached paged query with projection.
    /// Use ONLY when you genuinely need a different shape (rare).
    /// </summary>
    Task<PagedResult<TResult>> QueryAsync<TQuery, T, TId, TResult>(
        TQuery query,
        IRepository<T, TId> repo,
        Func<TQuery, CancellationToken, Task<(IReadOnlyList<TId> Ids, int Total)>> indexFactory,
        Func<T, TResult> mapper,
        CancellationToken cancellationToken)
        where TQuery : IPagedQuery
        where T : AggregateRoot<TId>, ICacheable
        where TId : notnull;

    /// <summary>
    /// Simple blob cache for complex projections/aggregations.
    /// </summary>
    Task<TResult> ExecuteAsync<TQuery, TResult>(
        TQuery query,
        Func<TQuery, Task<TResult>> factory,
        CancellationToken cancellationToken)
        where TQuery : ICachedQuery;
}