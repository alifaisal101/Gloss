using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests.ListMergeRequests;

public sealed class ListMergeRequestsHandler(
    IMergeRequestRepository mergeRequestRepository,
    IMrReviewRepository mrReviewRepository,
    IRepositoryRepository repositoryRepository)
{
    public async Task<IReadOnlyList<MergeRequestReadModel>> HandleAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        var mrs = await mergeRequestRepository.ListByRepositoryAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        var repo = await repositoryRepository.GetByIdAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        if (repo is null) return [];

        var reviews = await mrReviewRepository.ListByUserAsync(Guid.Empty, cancellationToken).ConfigureAwait(false);
        var reviewByMrId = reviews.ToDictionary(r => r.MergeRequestId);

        return mrs.Select(mr => MergeRequestReadModel.From(mr, repo, reviewByMrId.GetValueOrDefault(mr.Id))).ToList();
    }
}
