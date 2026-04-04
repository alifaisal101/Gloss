using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.EfCore;
using Gloss.Domain.Configs;

namespace Gloss.Infrastructure.Configs;

internal sealed class ConfigRepository(GlossDbContext db, IDomainContext domainContext)
    : EfCoreRepository<Config, Guid>(db, domainContext), IConfigRepository
{
    public async Task<Config?> FindAsync(CancellationToken cancellationToken) =>
        await GetByIdAsync(Config.SingletonId, cancellationToken).ConfigureAwait(false);
}
