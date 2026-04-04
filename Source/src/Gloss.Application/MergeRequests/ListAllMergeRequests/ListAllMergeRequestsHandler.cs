using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests.ListAllMergeRequests;

public sealed class ListAllMergeRequestsHandler(IMergeRequestRepository repository)
{
    public async Task<IReadOnlyList<MergeRequestReadModel>> HandleAsync(CancellationToken cancellationToken)
    {
        var mrs = await repository.ListAllAsync(cancellationToken).ConfigureAwait(false);
        return mrs.Select(MergeRequestReadModel.From).ToList();
    }
}
