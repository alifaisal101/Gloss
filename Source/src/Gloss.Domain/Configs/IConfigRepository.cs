using BuildingBlocks.Domain.Abstractions;

namespace Gloss.Domain.Configs;

public interface IConfigRepository : IRepository<Config, Guid>
{
    Task<Config?> FindAsync(CancellationToken cancellationToken);
}
