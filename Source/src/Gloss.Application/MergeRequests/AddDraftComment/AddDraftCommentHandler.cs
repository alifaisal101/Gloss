using BuildingBlocks.Application.EventSourcing;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.MergeRequests.Events;

namespace Gloss.Application.MergeRequests.AddDraftComment;

public sealed class AddDraftCommentHandler(
    IMergeRequestRepository mergeRequestRepository,
    IDomainContext domainContext,
    IEventStore eventStore)
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

        mr.MarkStaged();
        domainContext.Save<DraftComment, Guid>(commentResult.Value);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        await eventStore.AppendAsync(
            $"mr-{mergeRequestId}",
            new CommentAdded(mergeRequestId, commentResult.Value.Id, body),
            cancellationToken).ConfigureAwait(false);

        return DraftCommentReadModel.From(commentResult.Value);
    }
}
