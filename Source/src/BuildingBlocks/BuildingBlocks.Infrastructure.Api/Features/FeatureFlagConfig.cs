namespace BuildingBlocks.Infrastructure.Api.Features;

public sealed class FeatureFlagConfig
{
    public const string SectionName = "FeatureFlags";

    /// <summary>
    /// Provider: "local" (Microsoft.FeatureManagement from appsettings),
    ///           "unleash" (Unleash OSS server).
    /// Default: "local".
    /// </summary>
    public string Provider { get; init; } = "local";

    /// <summary>Unleash API URL (only when Provider = "unleash").</summary>
    public Uri? UnleashApiUrl { get; init; }

    /// <summary>Unleash API token (only when Provider = "unleash").</summary>
    public string? UnleashApiToken { get; init; }

    /// <summary>Unleash app name for this service.</summary>
    public string? UnleashAppName { get; init; }

    /// <summary>Unleash environment (development, production).</summary>
    public string? UnleashEnvironment { get; init; }
}