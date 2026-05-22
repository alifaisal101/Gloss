using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests.ListAllMergeRequests;

public sealed class ListAllMergeRequestsHandler(
    IMergeRequestRepository mergeRequestRepository,
    IMrReviewRepository mrReviewRepository,
    IRepositoryRepository repositoryRepository)
{
    public async Task<IReadOnlyList<MergeRequestReadModel>> HandleAsync(CancellationToken cancellationToken)
    {
        var mrs = await mergeRequestRepository.ListAllAsync(cancellationToken).ConfigureAwait(false);
        var repos = await repositoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var repoById = repos.ToDictionary(r => r.Id);

        var reviews = await mrReviewRepository.ListByUserAsync(Guid.Empty, cancellationToken).ConfigureAwait(false);
        var reviewByMrId = reviews.ToDictionary(r => r.MergeRequestId);

        return mrs
            .Where(mr => repoById.ContainsKey(mr.RepositoryId))
            .Select(mr => MergeRequestReadModel.From(mr, repoById[mr.RepositoryId], reviewByMrId.GetValueOrDefault(mr.Id)))
            .ToList();
    }
}
