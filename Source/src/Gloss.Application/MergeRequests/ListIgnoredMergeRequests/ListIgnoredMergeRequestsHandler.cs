using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests.ListIgnoredMergeRequests;

public sealed class ListIgnoredMergeRequestsHandler(
    IIgnoredMergeRequestRepository ignoredMergeRequestRepository,
    IRepositoryRepository repositoryRepository)
{
    public async Task<IReadOnlyList<IgnoredMergeRequestReadModel>> HandleAsync(CancellationToken cancellationToken)
    {
        var ignored = await ignoredMergeRequestRepository.ListAllAsync(cancellationToken).ConfigureAwait(false);
        if (ignored.Count == 0) return [];

        var repos = await repositoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var pathByRepositoryId = repos.ToDictionary(r => r.Id, r => r.ProjectPath);

        return ignored
            .Select(i => new IgnoredMergeRequestReadModel(
                i.Id,
                i.RepositoryId,
                i.ProviderIid,
                i.Title,
                pathByRepositoryId.GetValueOrDefault(i.RepositoryId, string.Empty),
                i.IgnoredAt))
            .ToList();
    }
}
