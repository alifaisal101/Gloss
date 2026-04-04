using Gloss.Domain.Repositories;

namespace Gloss.Application.Repositories.ListRepositories;

public sealed class ListRepositoriesHandler(IRepositoryRepository repository)
{
    public async Task<IReadOnlyList<RepositoryReadModel>> HandleAsync(CancellationToken cancellationToken)
    {
        var repos = await repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return repos.Select(RepositoryReadModel.From).ToList();
    }
}
