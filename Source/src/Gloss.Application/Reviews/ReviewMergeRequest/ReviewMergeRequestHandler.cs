using System.Net;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Application.Repositories;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.Reviews.ReviewMergeRequest;

public sealed class ReviewMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IRepositoryRepository repositoryRepository,
    IDraftCommentRepository draftCommentRepository,
    IRepoManager repoManager,
    IReviewProvider reviewProvider,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return MergeRequestErrors.NotFound;
        if (mr.Diff.Length > 50_000) return MergeRequestErrors.DiffTooLarge;
        if (mr.State == MergeRequestState.Reviewing) return MergeRequestErrors.AlreadyReviewing;
        if (mr.HeadSha is null) return MergeRequestErrors.MissingShas;

        var repository = await repositoryRepository.GetByIdAsync(mr.RepositoryId, cancellationToken).ConfigureAwait(false);
        if (repository is null) return MergeRequestErrors.RepositoryNotFound;

        mr.MarkReviewing();
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        string localPath;
        try
        {
            localPath = await repoManager.EnsureReadyAsync(repository, mr.HeadSha, cancellationToken).ConfigureAwait(false);
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
            var context = new ReviewContext(mr.Diff, localPath);
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
            domainContext.Save<DraftComment, Guid>(
                DraftComment.Create(mergeRequestId, comment.FilePath, comment.Line, comment.Body, comment.Reasoning));

        mr.MarkReady();
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
