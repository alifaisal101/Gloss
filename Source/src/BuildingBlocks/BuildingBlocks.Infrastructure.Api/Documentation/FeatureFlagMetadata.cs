namespace BuildingBlocks.Infrastructure.Api.Documentation;

/// <summary>
/// Metadata attached to feature-gated endpoints for OpenAPI documentation.
/// </summary>
public sealed record FeatureFlagMetadata(string FeatureName);
