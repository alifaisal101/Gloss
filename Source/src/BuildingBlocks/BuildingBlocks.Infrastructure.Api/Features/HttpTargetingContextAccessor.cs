using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement.FeatureFilters;

namespace BuildingBlocks.Infrastructure.Api.Features;

/// <summary>
/// Builds the targeting context from HttpContext for Microsoft.FeatureManagement.
///
/// Extracts:
///   - UserId from ClaimTypes.NameIdentifier (JWT sub claim) or "anonymous"
///   - Groups from:
///       • X-Region header (e.g., "region:eu", "region:mena")
///       • ClaimTypes.Role claims (e.g., "role:admin", "role:beta-tester")
///       • X-Feature-Group header (explicit group override for testing)
///
/// This means you can target features by:
///   - Specific user IDs
///   - Region (via X-Region header or geo-IP middleware)
///   - Role / permission group
///   - Percentage rollout (consistent per-user hashing)
///   - Time window (holiday features, maintenance windows)
/// </summary>
internal sealed class HttpTargetingContextAccessor(IHttpContextAccessor httpContextAccessor)
    : ITargetingContextAccessor
{
    public ValueTask<TargetingContext> GetContextAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return new ValueTask<TargetingContext>(new TargetingContext { UserId = "system" });

        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value
            ?? "anonymous";

        var groups = new List<string>();

        // Region from header (set by API gateway, geo-IP middleware, or client)
        var region = httpContext.Request.Headers["X-Region"].FirstOrDefault();
        if (!string.IsNullOrEmpty(region))
            groups.Add($"region:{region}");

        // Roles from JWT
        var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(c => $"role:{c.Value}");
        groups.AddRange(roles);

        // Explicit feature group (for testing/QA overrides)
        var featureGroup = httpContext.Request.Headers["X-Feature-Group"].FirstOrDefault();
        if (!string.IsNullOrEmpty(featureGroup))
            groups.Add(featureGroup);

        return new ValueTask<TargetingContext>(new TargetingContext
        {
            UserId = userId,
            Groups = groups
        });
    }
}
