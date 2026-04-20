using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests.DeleteMergeRequest;

public sealed class DeleteMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return MergeRequestErrors.NotFound;

        domainContext.Remove<MergeRequest, Guid>(mr);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
