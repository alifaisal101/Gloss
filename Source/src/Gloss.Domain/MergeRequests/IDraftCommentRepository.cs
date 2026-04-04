using BuildingBlocks.Domain.Abstractions;

namespace Gloss.Domain.MergeRequests;

public interface IDraftCommentRepository : IRepository<DraftComment, Guid>
{
    Task<IReadOnlyList<DraftComment>> ListByMergeRequestAsync(Guid mergeRequestId, CancellationToken cancellationToken);
}
