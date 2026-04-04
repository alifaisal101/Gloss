using BuildingBlocks.Domain.Abstractions;

namespace Gloss.Domain.MergeRequests;

public interface IMergeRequestRepository : IRepository<MergeRequest, Guid>
{
    Task<IReadOnlyList<MergeRequest>> ListByRepositoryAsync(Guid repositoryId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MergeRequest>> ListAllAsync(CancellationToken cancellationToken);
}
