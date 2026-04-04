namespace BuildingBlocks.Application.Queries;

/// <summary>
/// <para>
/// Cached result of a query index lookup: just IDs + total count.
/// The actual entities are resolved individually via the repository,
/// which goes through CachedRepository → individual FusionCache entries.
/// </para>
/// <para>
/// This is the key insight:
///   - Query index has a SHORT TTL (minutes) → cheap to rebuild (just SELECT id)
///   - Entity caches have a LONGER TTL (10+ min) → expensive to rebuild (full row)
///   - Entity update → evict ONE entity + evict query index (via tag)
///   - Next query → re-fetch ID list (cheap) → resolve entities (most still cached)
/// </para>
/// </summary>
public sealed record QueryIndex<TId>(IReadOnlyList<TId> Ids, int TotalCount);
