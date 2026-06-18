using BuildingBlocks.Domain.Abstractions;

namespace Gloss.Domain.MergeRequests;

public interface IIgnoredMergeRequestRepository : IRepository<IgnoredMergeRequest, Guid>
{
    Task<IReadOnlyList<IgnoredMergeRequest>> ListByRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken);
    Task<IgnoredMergeRequest?> FindAsync(Guid repositoryId, int providerIid, CancellationToken cancellationToken);
}
