using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.Repositories;

namespace Gloss.Application.Repositories.UpdatePollCron;

public sealed class UpdatePollCronHandler(
    IRepositoryRepository repository,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(UpdatePollCronCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var repo = await repository.GetByIdAsync(command.RepositoryId, cancellationToken).ConfigureAwait(false);
        if (repo is null) return RepositoryErrors.NotFound;

        repo.SetPollCron(command.PollCron);
        domainContext.Save<Repository, Guid>(repo);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
