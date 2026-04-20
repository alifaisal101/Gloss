using BuildingBlocks.Application.EventSourcing;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.MergeRequests.Events;

namespace Gloss.Application.MergeRequests.DeleteDraftComment;

public sealed class DeleteDraftCommentHandler(
    IDraftCommentRepository draftCommentRepository,
    IDomainContext domainContext,
    IEventStore eventStore)
{
    public async Task<VoidResult> HandleAsync(
        Guid mergeRequestId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var comment = await draftCommentRepository.GetByIdAsync(commentId, cancellationToken).ConfigureAwait(false);
        if (comment is null || comment.MergeRequestId != mergeRequestId)
            return MergeRequestErrors.CommentNotFound;

        var body = comment.Body;

        domainContext.Remove<DraftComment, Guid>(comment);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        await eventStore.AppendAsync(
            $"mr-{mergeRequestId}",
            new CommentDeleted(mergeRequestId, commentId, body),
            cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
