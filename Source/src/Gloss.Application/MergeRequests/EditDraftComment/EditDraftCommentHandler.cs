using BuildingBlocks.Application.EventSourcing;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.MergeRequests.Events;

namespace Gloss.Application.MergeRequests.EditDraftComment;

public sealed class EditDraftCommentHandler(
    IDraftCommentRepository draftCommentRepository,
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
        if (comment is null || comment.MergeRequestId != mergeRequestId)
            return MergeRequestErrors.CommentNotFound;

        var bodyBefore = comment.Body;

        var result = comment.Update(filePath, line, body, reasoning);
        if (result.IsFailure) return result.Error;

        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        await eventStore.AppendAsync(
            $"mr-{mergeRequestId}",
            new CommentEdited(mergeRequestId, commentId, bodyBefore, body),
            cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
