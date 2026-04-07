using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Results;
using Gloss.Domain.Repositories;

namespace Gloss.Application.Repositories.UpdateRepository;

public sealed class UpdateRepositoryHandler(
    IRepositoryRepository repository,
    IDomainContext domainContext)
{
    public async Task<VoidResult> HandleAsync(UpdateRepositoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var repo = await repository.GetByIdAsync(command.RepositoryId, cancellationToken).ConfigureAwait(false);
        if (repo is null) return RepositoryErrors.NotFound;

        if (command.PollCron is not null)
            repo.SetPollCron(command.PollCron);

        if (command.AutoReviewEnabled is not null)
            repo.SetAutoReviewEnabled(command.AutoReviewEnabled.Value);

        domainContext.Save<Repository, Guid>(repo);
        await domainContext.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
