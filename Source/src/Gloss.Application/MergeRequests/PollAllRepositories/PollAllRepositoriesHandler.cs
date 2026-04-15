using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Application.MergeRequests.PullMergeRequests;
using Gloss.Domain.Configs;
using Gloss.Domain.Repositories;

namespace Gloss.Application.MergeRequests.PollAllRepositories;

public sealed class PollAllRepositoriesHandler(
    IRepositoryRepository repositoryRepository,
    IConfigRepository configRepository,
    PullMergeRequestsHandler pullMergeRequestsHandler,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is not null)
        {
            config.SetPolling(true);
            await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var repos = await repositoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var repo in repos)
            {
                var result = await pullMergeRequestsHandler.HandleAsync(repo.Id, cancellationToken).ConfigureAwait(false);
                if (!result.IsSuccess) return result;
            }
            return Result.Success();
        }
        finally
        {
            if (config is not null)
            {
                config.SetPolling(false);
                await domainContext.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
