using BuildingBlocks.Application.Features;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;

namespace BuildingBlocks.Infrastructure.Api.Features;

/// <summary>
/// Default IFeatureService backed by Microsoft.FeatureManagement.
/// Reads features from "FeatureManagement" section in appsettings.json.
/// Supports targeting, percentage rollout, and time windows.
/// </summary>
internal sealed class MicrosoftFeatureService(IFeatureManager featureManager) : IFeatureService
{
    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
        await featureManager.IsEnabledAsync(featureName).ConfigureAwait(false);

    public async Task<bool> IsEnabledForAsync(string featureName, string userId, CancellationToken cancellationToken = default)
    {
        var context = new TargetingContext { UserId = userId };
        return await featureManager.IsEnabledAsync(featureName, context).ConfigureAwait(false);
    }

    public async Task<bool> IsEnabledForAsync(
        string featureName, string userId, IEnumerable<string> groups, CancellationToken cancellationToken = default)
    {
        var context = new TargetingContext { UserId = userId, Groups = groups.ToList() };
        return await featureManager.IsEnabledAsync(featureName, context).ConfigureAwait(false);
    }
}
