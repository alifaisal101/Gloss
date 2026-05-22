using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Application.Jobs;
using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests.DeleteMergeRequest;

public sealed class DeleteMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IMrReviewRepository mrReviewRepository,
    IJobScheduler jobScheduler,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return MergeRequestErrors.NotFound;

        var reviews = await mrReviewRepository.ListByMergeRequestAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        var reviewJobIds = reviews.Select(r => r.ReviewJobId).Where(id => id is not null).ToList();

        domainContext.Remove<MergeRequest, Guid>(mr);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        foreach (var jobId in reviewJobIds)
            jobScheduler.CancelReview(jobId!);

        return Result.Success();
    }
}
