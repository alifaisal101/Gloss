using BuildingBlocks.Domain.Abstractions;

namespace Gloss.Domain.Repositories;

public interface IRepositoryRepository : IRepository<Repository, Guid>
{
    Task<IReadOnlyList<Repository>> ListAsync(CancellationToken cancellationToken);
}
