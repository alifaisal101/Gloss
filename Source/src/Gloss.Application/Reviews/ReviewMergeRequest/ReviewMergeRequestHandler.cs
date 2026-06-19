using System.Net;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Application.Repositories;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.Projection;
using Gloss.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Gloss.Application.Reviews.ReviewMergeRequest;

public sealed partial class ReviewMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IMrReviewRepository mrReviewRepository,
    IRepositoryRepository repositoryRepository,
    IDraftCommentRepository draftCommentRepository,
    IRepoManager repoManager,
    IReviewProvider reviewProvider,
    IReviewerProjectionRepository projectionRepository,
    IDomainContext domainContext,
    ILogger<ReviewMergeRequestHandler> logger)
{
    public async Task<VoidResult> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return MergeRequestErrors.NotFound;

        var review = await mrReviewRepository.FindAsync(mergeRequestId, Guid.Empty, cancellationToken).ConfigureAwait(false);
        if (review is null) return MergeRequestErrors.NotFound;

        var repository = await repositoryRepository.GetByIdAsync(mr.RepositoryId, cancellationToken).ConfigureAwait(false);
        if (repository is null) return MergeRequestErrors.RepositoryNotFound;

        var beginResult = review.BeginReview(mr.HeadSha, mr.Diff);
        if (beginResult.IsFailure) return beginResult.Error;
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        string localPath;
        try
        {
            localPath = await repoManager.EnsureReadyAsync(repository, mr.HeadSha!, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogRepoCloneFailed(logger, mergeRequestId, ex);
            review.ResetToPending();
            await domainContext.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            return MergeRequestErrors.RepoCloneFailed;
        }

        repository.SetCloned(localPath);

        IReadOnlyList<ReviewComment> comments;
        try
        {
            var projection = await projectionRepository.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
            var context = new ReviewContext(mr.Diff, localPath, projection?.Content);
            comments = await reviewProvider.ReviewAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Any review failure (auth, a bad/unknown model → 404, transport, parse) must release the
            // aggregate from "Reviewing" so the user can retry — never leave it wedged. Mirrors the
            // repo-clone failure path above.
            LogReviewFailed(logger, mergeRequestId, ex);
            review.ResetToPending();
            await domainContext.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            return ex is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden }
                ? MergeRequestErrors.LlmProviderUnauthorized
                : MergeRequestErrors.ReviewFailed;
        }

        await ReplaceDraftCommentsAsync(review.Id, comments, cancellationToken).ConfigureAwait(false);

        review.CompleteReview();
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task ReplaceDraftCommentsAsync(Guid reviewId, IReadOnlyList<ReviewComment> comments, CancellationToken cancellationToken)
    {
        var existing = await draftCommentRepository.ListByMrReviewAsync(reviewId, cancellationToken).ConfigureAwait(false);
        foreach (var c in existing)
            domainContext.Remove<DraftComment, Guid>(c);

        foreach (var comment in comments)
        {
            var dc = DraftComment.Create(reviewId, comment.FilePath, comment.Line, comment.Body, comment.Reasoning);
            if (dc.IsSuccess)
                domainContext.Save<DraftComment, Guid>(dc.Value);
            else
                LogCommentDropped(logger, comment.FilePath, comment.Line, dc.Error.Code);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Repository clone/fetch failed for merge request {MergeRequestId}; resetting to Pending")]
    private static partial void LogRepoCloneFailed(ILogger logger, Guid mergeRequestId, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Review failed for merge request {MergeRequestId}; resetting to Pending")]
    private static partial void LogReviewFailed(ILogger logger, Guid mergeRequestId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dropped review comment for {FilePath}:{Line} — {ErrorCode}")]
    private static partial void LogCommentDropped(ILogger logger, string filePath, int line, string errorCode);
}
