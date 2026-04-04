namespace BuildingBlocks.Domain.Abstractions;

public interface ICacheable
{
    string CacheKeyId { get; }
}