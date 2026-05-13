using System.Net;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Application.Repositories;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.Projection;
using Gloss.Domain.Repositories;

namespace Gloss.Application.Reviews.ReviewMergeRequest;

public sealed class ReviewMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IRepositoryRepository repositoryRepository,
    IDraftCommentRepository draftCommentRepository,
    IRepoManager repoManager,
    IReviewProvider reviewProvider,
    IReviewerProjectionRepository projectionRepository,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return MergeRequestErrors.NotFound;
        var repository = await repositoryRepository.GetByIdAsync(mr.RepositoryId, cancellationToken).ConfigureAwait(false);
        if (repository is null) return MergeRequestErrors.RepositoryNotFound;

        var markReviewingResult = mr.BeginReview();
        if (markReviewingResult.IsFailure) return markReviewingResult.Error;
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        string localPath;
        try
        {
            localPath = await repoManager.EnsureReadyAsync(repository, mr.HeadSha!, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            mr.ResetToPending();
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
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return MergeRequestErrors.LlmProviderUnauthorized;
        }

        var existing = await draftCommentRepository.ListByMergeRequestAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        foreach (var c in existing)
            domainContext.Remove<DraftComment, Guid>(c);

        foreach (var comment in comments)
        {
            var dc = DraftComment.Create(mergeRequestId, comment.FilePath, comment.Line, comment.Body, comment.Reasoning);
            if (dc.IsSuccess)
                domainContext.Save<DraftComment, Guid>(dc.Value);
        }

        mr.CompleteReview();
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
