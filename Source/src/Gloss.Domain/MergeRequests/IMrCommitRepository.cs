using BuildingBlocks.Domain.Abstractions;

namespace Gloss.Domain.MergeRequests;

public interface IMrCommitRepository : IRepository<MrCommit, Guid>
{
    Task<IReadOnlyList<MrCommit>> ListByMergeRequestAsync(Guid mergeRequestId, CancellationToken cancellationToken);
}
