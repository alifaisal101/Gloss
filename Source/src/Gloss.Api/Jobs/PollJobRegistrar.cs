using BuildingBlocks.Infrastructure.Api.Jobs;
using Gloss.Domain.Configs;
using Hangfire;
using Gloss.Infrastructure.Repositories;

namespace Gloss.Api.Jobs;

internal sealed class PollJobRegistrar(IServiceProvider serviceProvider) : IRecurringJobRegistrar
{
    public void Register()
    {
        using var scope = serviceProvider.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
        var config = configRepo.FindAsync(CancellationToken.None).GetAwaiter().GetResult();
        if (config is null || string.IsNullOrWhiteSpace(config.DefaultPollCron)) return;

        RecurringJob.AddOrUpdate<RepositoryPollJob>("poll-all-repos", job => job.ExecuteAsync(CancellationToken.None), config.DefaultPollCron);
    }
}
