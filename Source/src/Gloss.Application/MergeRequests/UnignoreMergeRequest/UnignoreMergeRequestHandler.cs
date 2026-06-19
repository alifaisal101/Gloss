using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests.UnignoreMergeRequest;

public sealed class UnignoreMergeRequestHandler(
    IIgnoredMergeRequestRepository ignoredMergeRequestRepository,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid ignoredMergeRequestId, CancellationToken cancellationToken)
    {
        var ignored = await ignoredMergeRequestRepository.GetByIdAsync(ignoredMergeRequestId, cancellationToken).ConfigureAwait(false);
        if (ignored is not null)
        {
            domainContext.Remove<IgnoredMergeRequest, Guid>(ignored);
            await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
