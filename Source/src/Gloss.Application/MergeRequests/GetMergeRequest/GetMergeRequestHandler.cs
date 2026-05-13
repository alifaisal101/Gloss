using BuildingBlocks.Application.Persistence;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests.GetMergeRequest;

public sealed class GetMergeRequestHandler(
    IMergeRequestRepository mergeRequestRepository,
    IDraftCommentRepository draftCommentRepository,
    IRepositoryRepository repositoryRepository,
    IMrCommitRepository commitRepository,
    IGitClient gitClient,
    IDomainContext domainContext)
{
    public async Task<MergeRequestDetailReadModel?> HandleAsync(Guid mergeRequestId, CancellationToken cancellationToken)
    {
        var mr = await mergeRequestRepository.GetByIdAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        if (mr is null) return null;

        var repo = await repositoryRepository.GetByIdAsync(mr.RepositoryId, cancellationToken).ConfigureAwait(false);
        if (repo is null) return null;

        mr.MarkSeen();
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        var comments = await draftCommentRepository.ListByMergeRequestAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        var commits = await commitRepository.ListByMergeRequestAsync(mergeRequestId, cancellationToken).ConfigureAwait(false);
        var rawDiscussions = await gitClient.GetMrDiscussionsAsync(repo.ProjectPath, mr.ProviderIid, cancellationToken).ConfigureAwait(false);
        var platformComments = rawDiscussions.Select(PlatformCommentReadModel.From).ToList();

        return MergeRequestDetailReadModel.From(mr, repo, comments, commits, platformComments);
    }
}
