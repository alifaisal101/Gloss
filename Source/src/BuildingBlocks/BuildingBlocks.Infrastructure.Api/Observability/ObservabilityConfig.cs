namespace BuildingBlocks.Infrastructure.Api.Observability;

public sealed class ObservabilityConfig
{
    public const string SectionName = "Observability";

    public bool Enabled { get; init; } = true;
    public string ServiceName { get; init; } = "ModularMonolith";
    public string ServiceVersion { get; init; } = "1.0.0";

    /// <summary>OTLP endpoint (e.g. http://localhost:4317 for Jaeger/Grafana Agent).</summary>
    public string? OtlpEndpoint { get; init; }

    /// <summary>Fall back to console exporter when no OTLP endpoint configured.</summary>
    public bool ConsoleExporter { get; init; }
}