using BuildingBlocks.Application.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;

namespace BuildingBlocks.Infrastructure.Api.Features;

public static class FeatureFlagConfiguration
{
    /// <summary>
    /// <para>Registers feature flag infrastructure.</para>
    /// <para>
    /// Default provider (local): Microsoft.FeatureManagement reads from
    /// "FeatureManagement" section in appsettings.json. Zero infrastructure.
    /// Supports: Percentage, TimeWindow, Targeting (user/group rollout).
    /// </para>
    /// <para>
    /// Optional provider (unleash): Connects to a self-hosted Unleash server.
    /// Install NuGet: Unleash.Client and register UnleashFeatureService instead.
    /// </para>
    /// <para>appsettings.json structure for local provider:</para>
    /// <para>
    ///   "FeatureManagement": {
    ///     "NewCheckout": true,                          // simple on/off
    ///     "BetaDashboard": {
    ///       "EnabledFor": [{
    ///         "Name": "Targeting",
    ///         "Parameters": {
    ///           "Audience": {
    ///             "Users": ["user-123", "user-456"],    // specific users
    ///             "Groups": [{ "Name": "BetaTesters", "RolloutPercentage": 100 }],
    ///             "DefaultRolloutPercentage": 10         // 10% of everyone else
    ///           }
    ///         }
    ///       }]
    ///     },
    ///     "HolidayBanner": {
    ///       "EnabledFor": [{
    ///         "Name": "TimeWindow",
    ///         "Parameters": { "Start": "2026-12-20", "End": "2027-01-05" }
    ///       }]
    ///     },
    ///     "GradualRollout": {
    ///       "EnabledFor": [{
    ///         "Name": "Percentage",
    ///         "Parameters": { "Value": 25 }             // 25% of requests
    ///       }]
    ///     }
    ///   }
    /// </para>
    /// </summary>
    public static IServiceCollection AddBuildingBlocksFeatureFlags(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var config = configuration.GetSection(FeatureFlagConfig.SectionName).Get<FeatureFlagConfig>()
            ?? new FeatureFlagConfig();

        // Microsoft.FeatureManagement: reads "FeatureManagement" section
        services.AddFeatureManagement()
            .AddFeatureFilter<PercentageFilter>()
            .AddFeatureFilter<TimeWindowFilter>()
            .AddFeatureFilter<TargetingFilter>();

        // Targeting context: extracts user/groups from HttpContext
        services.AddSingleton<ITargetingContextAccessor, HttpTargetingContextAccessor>();

        // Register the appropriate IFeatureService implementation
        if (config.Provider.Equals("unleash", StringComparison.OrdinalIgnoreCase))
        {
            // To use Unleash:
            //   1. Install: dotnet add package Unleash.Client
            //   2. Replace this line with: services.AddSingleton<IFeatureService, UnleashFeatureService>()
            //   3. Configure UnleashApiUrl, UnleashApiToken, UnleashAppName in appsettings
            //
            // See BuildingBlocks.Infrastructure.Api/Features/UnleashFeatureService.cs.example
            throw new InvalidOperationException(
                "Unleash provider selected but not configured. " +
                "Install Unleash.Client NuGet and register UnleashFeatureService. " +
                "See Features/UnleashFeatureService.cs.example for implementation.");
        }

        services.AddScoped<IFeatureService, MicrosoftFeatureService>();

        return services;
    }
}
