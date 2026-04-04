using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Batching;

internal static partial class BatchProcessorLogs
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Starting batch processing. Total Items: {Total}. Batch Size: {BatchSize}")]
    public static partial void LogStartingBatch(this ILogger logger, int total, int batchSize);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Batch chunk failed.")]
    public static partial void LogBatchChunkFailed(this ILogger logger, Exception ex);
}