namespace Gloss.Application.MergeRequests;

public sealed record PlatformMrStatusData(string Kind, DateTimeOffset? OccurredAt, string? ByUsername);
