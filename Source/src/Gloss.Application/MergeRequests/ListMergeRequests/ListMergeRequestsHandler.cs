using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests.ListMergeRequests;

public sealed class ListMergeRequestsHandler(IMergeRequestRepository repository)
{
    public async Task<IReadOnlyList<MergeRequestReadModel>> HandleAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        var mrs = await repository.ListByRepositoryAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        return mrs.Select(MergeRequestReadModel.From).ToList();
    }
}
