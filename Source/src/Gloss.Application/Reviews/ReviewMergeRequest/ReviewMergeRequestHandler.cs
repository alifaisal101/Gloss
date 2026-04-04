using System.Net;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;

namespace Gloss.Application.Reviews.ReviewMergeRequest;

public sealed class ReviewMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IReviewProvider reviewProvider,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return MergeRequestErrors.NotFound;

        IReadOnlyList<ReviewComment> comments;
        try
        {
            comments = await reviewProvider.ReviewAsync(mr.Diff, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return MergeRequestErrors.LlmProviderUnauthorized;
        }

        foreach (var comment in comments)
            domainContext.Save<DraftComment, Guid>(
                DraftComment.Create(mergeRequestId, comment.FilePath, comment.Line, comment.Body, comment.Reasoning));

        mr.MarkReady();

        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
