using BuildingBlocks.Infrastructure.Api.Documentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Infrastructure.Api.Features;

public static class FeatureGateExtensions
{
    /// <summary>
    /// Gates this endpoint behind a feature flag. Returns 404 when disabled.
    /// Also attaches metadata for Scalar documentation.
    /// </summary>
    public static TBuilder RequireFeature<TBuilder>(this TBuilder builder, string featureName)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(new FeatureGateFilter(featureName));
        builder.WithMetadata(new FeatureFlagMetadata(featureName));
        builder.WithSummary($"[{featureName}]");

        return builder;
    }
}