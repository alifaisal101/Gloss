using Gloss.Domain.Repositories;

namespace Gloss.Application.Repositories;

public sealed record RepositoryReadModel(
    Guid Id,
    string ProjectPath,
    string Provider,
    string? PollCron)
{
    public static RepositoryReadModel From(Repository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        return new(repository.Id, repository.ProjectPath, repository.Provider, repository.PollCron);
    }
}
