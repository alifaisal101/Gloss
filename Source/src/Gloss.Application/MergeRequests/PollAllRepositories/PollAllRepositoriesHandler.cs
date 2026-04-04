using BuildingBlocks.Domain.Results;
using Gloss.Application.MergeRequests.PullMergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests.PollAllRepositories;

public sealed class PollAllRepositoriesHandler(
    IRepositoryRepository repositoryRepository,
    PullMergeRequestsHandler pullMergeRequestsHandler)
{
    public async Task<VoidResult> HandleAsync(CancellationToken cancellationToken)
    {
        var repos = await repositoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var repo in repos)
        {
            var result = await pullMergeRequestsHandler.HandleAsync(repo.Id, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess) return result;
        }
        return Result.Success();
    }
}
