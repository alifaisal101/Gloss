using BuildingBlocks.Domain.Models.Batching;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Batching;

public interface IBatchProcessor
{
    Task<Result<BatchReport>> ProcessAsync<TId>(
        IEnumerable<TId> allIds,
        Func<IEnumerable<TId>, CancellationToken, Task<VoidResult>> batchLogic,
        BatchSize batchSize,
        CancellationToken cancellationToken = default);
}