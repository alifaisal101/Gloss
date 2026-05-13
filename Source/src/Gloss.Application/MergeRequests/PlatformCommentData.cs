namespace Gloss.Application.MergeRequests;

public sealed record PlatformCommentData(
    string AuthorUsername,
    string Body,
    string? FilePath,
    int? Line,
    DateTimeOffset CreatedAt);
