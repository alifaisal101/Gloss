using BuildingBlocks.Application.EventSourcing;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.MergeRequests.Events;

namespace Gloss.Application.MergeRequests.EditDraftComment;

public sealed class EditDraftCommentHandler(
    IDraftCommentRepository draftCommentRepository,
    IMrReviewRepository mrReviewRepository,
    IDomainContext domainContext,
    IEventStore eventStore)
{
    public async Task<VoidResult> HandleAsync(
        Guid mergeRequestId,
        Guid commentId,
        string filePath,
        int line,
        string body,
        string? reasoning,
        CancellationToken cancellationToken)
    {
        var comment = await draftCommentRepository.GetByIdAsync(commentId, cancellationToken).ConfigureAwait(false);
        if (comment is null) return MergeRequestErrors.CommentNotFound;

        var review = await mrReviewRepository.FindAsync(mergeRequestId, Guid.Empty, cancellationToken).ConfigureAwait(false);
        if (review is null || comment.MrReviewId != review.Id) return MergeRequestErrors.CommentNotFound;

        var bodyBefore = comment.Body;

        var result = comment.Update(filePath, line, body, reasoning);
        if (result.IsFailure) return result.Error;

        review.MarkStaged();
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        await eventStore.AppendAsync(
            $"mr-{mergeRequestId}",
            new CommentEdited(mergeRequestId, commentId, bodyBefore, body),
            cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
