using Gloss.Application.MergeRequests.PollAllRepositories;

namespace Gloss.Infrastructure.Repositories;

public sealed class RepositoryPollJob(PollAllRepositoriesHandler handler)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken) =>
        await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
}
