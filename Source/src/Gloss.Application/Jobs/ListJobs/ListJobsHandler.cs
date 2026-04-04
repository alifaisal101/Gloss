using Gloss.Domain.Configs;
using Gloss.Domain.Repositories;

namespace Gloss.Application.Jobs.ListJobs;

public sealed class ListJobsHandler(
    IRepositoryRepository repositoryRepository,
    IConfigRepository configRepository)
{
    public async Task<IReadOnlyList<ScheduledJob>> HandleAsync(CancellationToken cancellationToken)
    {
        var config = await configRepository.FindAsync(cancellationToken).ConfigureAwait(false);
        if (config is null) return [];

        var repos = await repositoryRepository.ListAsync(cancellationToken).ConfigureAwait(false);
        return repos.Select(r => new ScheduledJob(r.ProjectPath, config.DefaultPollCron)).ToList();
    }
}
