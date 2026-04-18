using Gloss.Domain.Repositories;

namespace Gloss.Application.Repositories;

public interface IRepoManager
{
    Task<string> EnsureReadyAsync(Repository repository, string headSha, CancellationToken cancellationToken);
}
