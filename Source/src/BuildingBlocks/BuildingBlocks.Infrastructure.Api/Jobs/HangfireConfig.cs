namespace BuildingBlocks.Infrastructure.Api.Jobs;

public sealed class HangfireConfig
{
    public const string SectionName = "Hangfire";

    public bool Enabled { get; init; } = true;

    /// <summary>Connection string name from ConnectionStrings section.</summary>
    public string ConnectionStringName { get; init; } = "Hangfire";

    /// <summary>Worker count. Default: Environment.ProcessorCount.</summary>
    public int? WorkerCount { get; init; }

    /// <summary>Queue names in priority order.</summary>
    public IReadOnlyList<string> Queues { get; init; } = ["critical", "default", "low"];
    
    /// <summary>Dashboard path. Set null to disable.</summary>
    public string? DashboardPath { get; init; } = "/hangfire";

    /// <summary>How long to keep successful jobs. Default 24h.</summary>
    public int SuccessfulJobExpirationHours { get; init; } = 24;

    /// <summary>Schema name for Hangfire tables. Keeps them isolated.</summary>
    public string SchemaName { get; init; } = "hangfire";
}