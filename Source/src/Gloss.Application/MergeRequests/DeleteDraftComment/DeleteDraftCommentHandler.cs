using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests.DeleteDraftComment;

public sealed class DeleteDraftCommentHandler(
    IDraftCommentRepository draftCommentRepository,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(
        Guid mergeRequestId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var comment = await draftCommentRepository.GetByIdAsync(commentId, cancellationToken).ConfigureAwait(false);
        if (comment is null || comment.MergeRequestId != mergeRequestId)
            return MergeRequestErrors.CommentNotFound;

        domainContext.Remove<DraftComment, Guid>(comment);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
