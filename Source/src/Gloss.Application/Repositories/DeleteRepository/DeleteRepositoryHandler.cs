using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.Repositories;

namespace Gloss.Application.Repositories.DeleteRepository;

public sealed class DeleteRepositoryHandler(
    IRepositoryRepository repositoryRepository,
    IDomainContext domainContext,
    IRepoManager repoManager)
{
    public async Task<VoidResult> HandleAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        var repo = await repositoryRepository.GetByIdAsync(repositoryId, cancellationToken).ConfigureAwait(false);
        if (repo is null) return RepositoryErrors.NotFound;

        domainContext.Remove<Repository, Guid>(repo);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);

        if (repo.LocalClonePath is not null)
            await repoManager.DeleteLocalCloneAsync(repo.LocalClonePath, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
