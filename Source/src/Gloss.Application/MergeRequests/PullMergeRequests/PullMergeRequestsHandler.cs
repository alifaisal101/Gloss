using System.Net;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Application.Jobs;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests.PullMergeRequests;

public sealed class PullMergeRequestsHandler(
    IRepositoryRepository repositoryRepository,
    IMergeRequestRepository mergeRequestRepository,
    IMrCommitRepository commitRepository,
    IGitClient gitClient,
    IJobScheduler jobScheduler,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        var repository = await repositoryRepository.GetByIdAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        if (repository is null) return MergeRequestErrors.RepositoryNotFound;

        IReadOnlyList<MergeRequestData> remoteMrs;
        try
        {
            remoteMrs = await gitClient.GetOpenMergeRequestsAsync(repository.ProjectPath, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return MergeRequestErrors.GitProviderUnauthorized;
        }

        var existingMrs = await mergeRequestRepository.ListByRepositoryAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        var existingByIid = existingMrs.ToDictionary(mr => mr.ProviderIid);
        var newMrs = new List<MergeRequest>();

        foreach (var remote in remoteMrs)
        {
            MergeRequest mr;
            if (existingByIid.TryGetValue(remote.Iid, out var existing))
            {
                existing.Update(remote.Title, remote.Description, remote.SourceBranch, remote.TargetBranch, remote.AuthorUsername, remote.Diff, remote.BaseSha, remote.HeadSha, remote.StartSha);
                mr = existing;

                var staleCommits = await commitRepository.ListByMergeRequestAsync(mr.Id, cancellationToken).ConfigureAwait(false);
                foreach (var stale in staleCommits)
                    domainContext.Remove<MrCommit, Guid>(stale);
            }
            else
            {
                mr = MergeRequest.Create(repositoryId, remote.Iid, remote.Title, remote.Description,
                    remote.SourceBranch, remote.TargetBranch, remote.AuthorUsername, remote.Diff,
                    remote.BaseSha, remote.HeadSha, remote.StartSha);
                domainContext.Save<MergeRequest, Guid>(mr);
                newMrs.Add(mr);
            }

            await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

            var remoteCommits = await gitClient.GetCommitsAsync(repository.ProjectPath, remote.Iid, cancellationToken).ConfigureAwait(false);
            foreach (var commit in remoteCommits)
                domainContext.Save<MrCommit, Guid>(MrCommit.Create(mr.Id, commit.Sha, commit.Title, commit.AuthorName, commit.Diff));
        }

        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        if (repository.AutoReviewEnabled && newMrs.Count > 0)
        {
            foreach (var newMr in newMrs)
                newMr.SetReviewJobId(jobScheduler.EnqueueReview(newMr.Id));
            await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
