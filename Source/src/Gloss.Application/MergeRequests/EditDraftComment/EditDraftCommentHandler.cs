using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests.EditDraftComment;

public sealed class EditDraftCommentHandler(
    IDraftCommentRepository draftCommentRepository,
    IDomainContext domainContext)
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

        comment.Update(filePath, line, body, reasoning);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
