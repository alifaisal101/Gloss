using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests.AddDraftComment;

public sealed class AddDraftCommentHandler(
    IMergeRequestRepository mergeRequestRepository,
    IDomainContext domainContext)
{
    public async Task<Result<DraftCommentReadModel>> HandleAsync(
        Guid mergeRequestId,
        string filePath,
        int line,
        string body,
        string? reasoning,
        CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return MergeRequestErrors.NotFound;

        var commentResult = DraftComment.Create(mergeRequestId, filePath, line, body, reasoning, DraftCommentState.UserAdded);
        if (commentResult.IsFailure) return commentResult.Error;

        domainContext.Save<DraftComment, Guid>(commentResult.Value);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return DraftCommentReadModel.From(commentResult.Value);
    }
}
