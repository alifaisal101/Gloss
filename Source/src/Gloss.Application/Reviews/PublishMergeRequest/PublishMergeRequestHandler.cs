using System.Net;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Application.MergeRequests;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.Reviews.PublishMergeRequest;

public sealed class PublishMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IDraftCommentRepository draftCommentRepository,
    IRepositoryRepository repositoryRepository,
    IGitClient gitClient,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return MergeRequestErrors.NotFound;
        if (mr.State != MergeRequestState.Ready) return MergeRequestErrors.NotReady;
        if (mr.BaseSha is null || mr.HeadSha is null || mr.StartSha is null) return MergeRequestErrors.MissingShas;

        var repo = await repositoryRepository.GetByIdAsync(mr.RepositoryId, cancellationToken).ConfigureAwait(false);
        if (repo is null) return MergeRequestErrors.RepositoryNotFound;

        var comments = await draftCommentRepository.ListByMergeRequestAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);

        try
        {
            foreach (var comment in comments)
                await gitClient.PublishCommentAsync(
                    repo.ProjectPath, mr.ProviderIid,
                    mr.BaseSha, mr.HeadSha, mr.StartSha,
                    comment.FilePath, comment.Line, comment.Body,
                    cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return MergeRequestErrors.GitProviderUnauthorized;
        }

        mr.MarkPublished();
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
