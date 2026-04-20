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

        var comment = DraftComment.Create(mergeRequestId, filePath, line, body, reasoning, DraftCommentState.UserAdded);
        domainContext.Save<DraftComment, Guid>(comment);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return DraftCommentReadModel.From(comment);
    }
}
