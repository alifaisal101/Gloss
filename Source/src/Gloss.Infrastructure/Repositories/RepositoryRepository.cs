using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.EfCore;
using Gloss.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gloss.Infrastructure.Repositories;

internal sealed class RepositoryRepository(GlossDbContext db, IDomainContext domainContext)
    : EfCoreRepository<Repository, Guid>(db, domainContext), IRepositoryRepository
{
    public async Task<IReadOnlyList<Repository>> ListAsync(CancellationToken cancellationToken) =>
        await DbContext.Set<Repository>().ToListAsync(cancellationToken).ConfigureAwait(false);
}
