using System.Diagnostics.CodeAnalysis;
using BuildingBlocks.Application.Batching;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Errors;
using BuildingBlocks.Domain.Models.Batching;
using BuildingBlocks.Domain.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Batching;

internal sealed class BatchProcessor(
    IServiceProvider serviceProvider,
    ILogger<BatchProcessor> logger) : IBatchProcessor
{
    public async Task<Result<BatchReport>> ProcessAsync<TId>(
        IEnumerable<TId> allIds,
        Func<IEnumerable<TId>, CancellationToken, Task<VoidResult>> batchLogic,
        BatchSize batchSize,
        CancellationToken cancellationToken = default)
    {
        var ids = allIds.ToList();
        var totalReport = BatchReport.Empty();

        logger.LogStartingBatch(ids.Count, batchSize.Value);

        foreach (var batch in ids.Chunk(batchSize))
        {
            var chunkReport = await ProcessChunkAsync(batch, batchLogic, cancellationToken).ConfigureAwait(false);
            totalReport = totalReport.Merge(chunkReport);
        }

        return Result.Success(totalReport);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", 
        Justification = "Batch processor must survive individual chunk failures to report them.")]
    private async Task<BatchReport> ProcessChunkAsync<TId>(
        TId[] batch,
        Func<IEnumerable<TId>, CancellationToken, Task<VoidResult>> batchLogic,
        CancellationToken cancellationToken)
    {
        var scope = serviceProvider.CreateAsyncScope();
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<IDomainContext>();

            try
            {
                var logicResult = await batchLogic(batch, cancellationToken).ConfigureAwait(false);

                if (logicResult.IsFailure)
                    return BatchReport.Create(0, batch.Length, [logicResult.Error]);

                await context.CommitAsync(cancellationToken).ConfigureAwait(false);

                return BatchReport.Create(batch.Length, 0, []);
            }
            catch (Exception ex)
            {
                logger.LogBatchChunkFailed(ex);
                var error = new DomainError("Batching.Exception", ex.Message);
                return BatchReport.Create(0, batch.Length, [error]);
            }
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }
}