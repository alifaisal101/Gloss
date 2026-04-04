namespace BuildingBlocks.Infrastructure.Api.Middleware;

public sealed class RateLimitConfig
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; init; } = true;
    public int PermitLimit { get; init; } = 100;
    public int WindowSeconds { get; init; } = 60;
    public int QueueLimit { get; init; } = 10;
}