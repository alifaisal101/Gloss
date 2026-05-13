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
        var remoteIids = remoteMrs.Select(r => r.Iid).ToHashSet();
        var newMrs = new List<MergeRequest>();

        foreach (var remote in remoteMrs)
            newMrs.AddRange(await ProcessOpenMrAsync(repository, remote, existingByIid, cancellationToken).ConfigureAwait(false));

        await UpdateDisappearedMrStatusesAsync(repository, existingMrs, remoteIids, cancellationToken).ConfigureAwait(false);

        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        if (repository.AutoReviewEnabled && newMrs.Count > 0)
        {
            foreach (var newMr in newMrs)
                newMr.SetReviewJobId(jobScheduler.EnqueueReview(newMr.Id));
            await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    private async Task<IEnumerable<MergeRequest>> ProcessOpenMrAsync(
        Repository repository,
        MergeRequestData remote,
        Dictionary<int, MergeRequest> existingByIid,
        CancellationToken cancellationToken)
    {
        MergeRequest mr;
        var isNew = false;

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
            mr = MergeRequest.Create(repository.Id, remote.Iid, remote.Title, remote.Description,
                remote.SourceBranch, remote.TargetBranch, remote.AuthorUsername, remote.Diff,
                remote.BaseSha, remote.HeadSha, remote.StartSha);
            domainContext.Save<MergeRequest, Guid>(mr);
            isNew = true;
        }

        var isApproved = await gitClient.IsMergeRequestApprovedAsync(repository.ProjectPath, remote.Iid, cancellationToken).ConfigureAwait(false);
        mr.SetApproved(isApproved);

        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        var remoteCommits = await gitClient.GetCommitsAsync(repository.ProjectPath, remote.Iid, cancellationToken).ConfigureAwait(false);
        foreach (var commit in remoteCommits)
            domainContext.Save<MrCommit, Guid>(MrCommit.Create(mr.Id, commit.Sha, commit.Title, commit.AuthorName, commit.Diff));

        return isNew ? [mr] : [];
    }

    private async Task UpdateDisappearedMrStatusesAsync(
        Repository repository,
        IReadOnlyList<MergeRequest> existingMrs,
        HashSet<int> remoteIids,
        CancellationToken cancellationToken)
    {
        foreach (var mr in existingMrs.Where(mr => !remoteIids.Contains(mr.ProviderIid) && mr.PlatformStatus is PlatformMrStatus.Open))
        {
            var statusData = await gitClient.GetMergeRequestStatusAsync(repository.ProjectPath, mr.ProviderIid, cancellationToken).ConfigureAwait(false);
            var platformStatus = statusData.Kind switch
            {
                "Closed" => (PlatformMrStatus)new PlatformMrStatus.Closed(statusData.OccurredAt!.Value, statusData.ByUsername ?? string.Empty),
                "Merged" => new PlatformMrStatus.Merged(statusData.OccurredAt!.Value, statusData.ByUsername ?? string.Empty),
                _ => new PlatformMrStatus.Open()
            };
            mr.UpdatePlatformStatus(platformStatus);
        }
    }
}
