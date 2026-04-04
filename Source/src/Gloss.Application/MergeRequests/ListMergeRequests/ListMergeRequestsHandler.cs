using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests.ListMergeRequests;

public sealed class ListMergeRequestsHandler(
    IMergeRequestRepository mergeRequestRepository,
    IRepositoryRepository repositoryRepository)
{
    public async Task<IReadOnlyList<MergeRequestReadModel>> HandleAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        var mrs = await mergeRequestRepository.ListByRepositoryAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        var repo = await repositoryRepository.GetByIdAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        if (repo is null) return [];
        return mrs.Select(mr => MergeRequestReadModel.From(mr, repo)).ToList();
    }
}
