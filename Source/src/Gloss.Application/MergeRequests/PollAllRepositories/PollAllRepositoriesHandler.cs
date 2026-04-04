using Gloss.Application.MergeRequests.PullMergeRequests;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests.PollAllRepositories;

public sealed class PollAllRepositoriesHandler(
    IRepositoryRepository repositoryRepository,
    PullMergeRequestsHandler pullMergeRequestsHandler)
{
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        var repos = await repositoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var repo in repos)
            await pullMergeRequestsHandler.HandleAsync(repo.Id, cancellationToken).ConfigureAwait(false);
    }
}
