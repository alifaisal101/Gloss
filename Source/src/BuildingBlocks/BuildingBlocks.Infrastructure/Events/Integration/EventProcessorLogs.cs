using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Events.Integration;

internal static partial class EventProcessorLogs
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Error processing integration event {EventId}")]
    public static partial void LogProcessingError(this ILogger logger, Exception ex, Guid eventId);
}