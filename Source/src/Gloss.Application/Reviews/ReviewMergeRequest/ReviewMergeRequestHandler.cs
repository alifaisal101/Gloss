using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;

namespace Gloss.Application.Reviews.ReviewMergeRequest;

public sealed class ReviewMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IReviewProvider reviewProvider,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return MergeRequestErrors.NotFound;

        var comments = await reviewProvider.ReviewAsync(mr.Diff, cancellationToken).ConfigureAwait(false);

        foreach (var comment in comments)
            domainContext.Save<DraftComment, Guid>(
                DraftComment.Create(mergeRequestId, comment.FilePath, comment.Line, comment.Body, comment.Reasoning));

        mr.MarkReady();

        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
