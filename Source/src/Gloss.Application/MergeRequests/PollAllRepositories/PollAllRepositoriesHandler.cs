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
        if (config is null) return Result.Success();

        config.SetPolling(true);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var repos = await EnsureRepositoriesAsync(config, cancellationToken).ConfigureAwait(false);
            foreach (var repo in repos)
            {
                var result = await pullMergeRequestsHandler.HandleAsync(repo.Id, cancellationToken).ConfigureAwait(false);
                if (!result.IsSuccess) return result;
            }
            return Result.Success();
        }
        finally
        {
            config.SetPolling(false);
            await domainContext.CommitAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyList<Repository>> EnsureRepositoriesAsync(Config config, CancellationToken cancellationToken)
    {
        var existing = await repositoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        var existingByPath = existing.ToDictionary(r => r.ProjectPath, StringComparer.OrdinalIgnoreCase);

        var created = config.GitProjects
            .Where(path => !existingByPath.ContainsKey(path))
            .Select(path => Repository.Create(path, config.GitProvider.Value))
            .ToList();

        foreach (var repo in created)
            domainContext.Save<Repository, Guid>(repo);

        if (created.Count > 0)
            await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        return [.. existing, .. created];
    }
}
