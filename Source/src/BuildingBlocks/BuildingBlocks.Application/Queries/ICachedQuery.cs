namespace BuildingBlocks.Application.Queries;

/// <summary>
/// <para>Marker for queries whose results are auto-cached.</para>
/// <para>
/// CacheTags determine invalidation scope: when ANY aggregate matching
/// a tag is modified, the query's cached result is evicted.
/// </para>
/// </summary>
public interface ICachedQuery
{
    IReadOnlyList<string> CacheTags { get; }
    TimeSpan? CacheDuration => null;
}