namespace BuildingBlocks.Application.Features;

/// <summary>
/// <para>
/// Feature flag abstraction. Lives in Application layer so handlers can
/// check features without coupling to any specific provider.
/// </para>
/// <para>
/// Default provider: Microsoft.FeatureManagement (appsettings.json driven).
/// Optional: Unleash (self-hosted OSS). See Features/UnleashFeatureService.cs.example.
/// </para>
/// <para>
/// Usage in handlers:
///   if (await features.IsEnabledAsync("NewPricingEngine", ct))
///       return await newEngine.CalculateAsync(order, ct);
/// </para>
/// <para>
/// Usage with targeting (user/region):
///   if (await features.IsEnabledForAsync("BetaDashboard", userId, ct))
///       return betaResponse;
/// </para>
/// </summary>
public interface IFeatureService
{
    /// <summary>Check if a feature is globally enabled.</summary>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>Check if a feature is enabled for a specific user (targeting).</summary>
    Task<bool> IsEnabledForAsync(string featureName, string userId, CancellationToken cancellationToken = default);

    /// <summary>Check if a feature is enabled for a user in specific groups (region, role).</summary>
    Task<bool> IsEnabledForAsync(string featureName, string userId, IEnumerable<string> groups, CancellationToken cancellationToken = default);
}