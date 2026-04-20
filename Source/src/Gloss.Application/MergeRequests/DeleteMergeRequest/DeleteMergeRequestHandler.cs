using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Application.Jobs;
using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests.DeleteMergeRequest;

public sealed class DeleteMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IJobScheduler jobScheduler,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return MergeRequestErrors.NotFound;

        var reviewJobId = mr.ReviewJobId;
        domainContext.Remove<MergeRequest, Guid>(mr);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        if (reviewJobId is not null)
            jobScheduler.CancelReview(reviewJobId);

        return Result.Success();
    }
}
