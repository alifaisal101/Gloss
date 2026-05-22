using BuildingBlocks.Application.EventSourcing;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.MergeRequests.Events;

namespace Gloss.Application.MergeRequests.DeleteDraftComment;

public sealed class DeleteDraftCommentHandler(
    IDraftCommentRepository draftCommentRepository,
    IMrReviewRepository mrReviewRepository,
    IDomainContext domainContext,
    IEventStore eventStore)
{
    public async Task<VoidResult> HandleAsync(
        Guid mergeRequestId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var comment = await draftCommentRepository.GetByIdAsync(commentId, cancellationToken).ConfigureAwait(false);
        if (comment is null) return MergeRequestErrors.CommentNotFound;

        var review = await mrReviewRepository.FindAsync(mergeRequestId, Guid.Empty, cancellationToken).ConfigureAwait(false);
        if (review is null || comment.MrReviewId != review.Id) return MergeRequestErrors.CommentNotFound;

        var body = comment.Body;

        review.MarkStaged();
        domainContext.Remove<DraftComment, Guid>(comment);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        await eventStore.AppendAsync(
            $"mr-{mergeRequestId}",
            new CommentDeleted(mergeRequestId, commentId, body),
            cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
