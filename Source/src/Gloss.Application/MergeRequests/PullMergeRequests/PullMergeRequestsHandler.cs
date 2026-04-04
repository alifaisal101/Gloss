using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.MergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests.PullMergeRequests;

public sealed class PullMergeRequestsHandler(
    IRepositoryRepository repositoryRepository,
    IMergeRequestRepository mergeRequestRepository,
    IGitClient gitClient,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        var repository = await repositoryRepository.GetByIdAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        if (repository is null) return MergeRequestErrors.RepositoryNotFound;

        var remoteMrs = await gitClient.GetOpenMergeRequestsAsync(repository.ProjectPath, cancellationToken).ConfigureAwait(false);
        var existingMrs = await mergeRequestRepository.ListByRepositoryAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        var existingByIid = existingMrs.ToDictionary(mr => mr.ProviderIid);

        foreach (var remote in remoteMrs)
        {
            if (existingByIid.TryGetValue(remote.Iid, out var existing))
                existing.Update(remote.Title, remote.Description, remote.SourceBranch, remote.TargetBranch, remote.AuthorUsername, remote.Diff);
            else
                domainContext.Save<MergeRequest, Guid>(MergeRequest.Create(
                    repositoryId, remote.Iid, remote.Title, remote.Description,
                    remote.SourceBranch, remote.TargetBranch, remote.AuthorUsername, remote.Diff));
        }

        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
