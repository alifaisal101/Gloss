using System.Net;
using BuildingBlocks.Application.EventSourcing;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Application.MergeRequests;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.MergeRequests.Events;
using Gloss.Domain.Repositories;

namespace Gloss.Application.Reviews.PublishMergeRequest;

public sealed class PublishMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IDraftCommentRepository draftCommentRepository,
    IRepositoryRepository repositoryRepository,
    IGitClient gitClient,
    IEventStore eventStore,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return MergeRequestErrors.NotFound;

        var markPublishedResult = mr.MarkPublished();
        if (markPublishedResult.IsFailure) return markPublishedResult.Error;

        var repo = await repositoryRepository.GetByIdAsync(mr.RepositoryId, cancellationToken).ConfigureAwait(false);
        if (repo is null) return MergeRequestErrors.RepositoryNotFound;

        var comments = await draftCommentRepository.ListByMergeRequestAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        var shas = await gitClient.GetMrShasAsync(repo.ProjectPath, mr.ProviderIid, cancellationToken).ConfigureAwait(false);

        try
        {
            foreach (var comment in comments)
                await gitClient.PublishCommentAsync(
                    repo.ProjectPath, mr.ProviderIid,
                    shas?.BaseSha, shas?.HeadSha, shas?.StartSha,
                    comment.FilePath, comment.Line, comment.Body,
                    cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return MergeRequestErrors.GitProviderUnauthorized;
        }

        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        foreach (var comment in comments.Where(c => c.State == DraftCommentState.Generated))
            await eventStore.AppendAsync(
                $"mr-{mergeRequestId}",
                new CommentAccepted(mergeRequestId, comment.Id, comment.Body),
                cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
