using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests.GetMergeRequest;

public sealed class GetMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IDraftCommentRepository draftCommentRepository)
{
    public async Task<MergeRequestDetailReadModel?> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return null;

        var comments = await draftCommentRepository.ListByMergeRequestAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        return MergeRequestDetailReadModel.From(mr, comments);
    }
}
