namespace Gloss.Application.MergeRequests;

public sealed record PlatformCommentReadModel(
    string AuthorUsername,
    string Body,
    string? FilePath,
    int? Line,
    DateTimeOffset CreatedAt)
{
    public static PlatformCommentReadModel From(PlatformCommentData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new(data.AuthorUsername, data.Body, data.FilePath, data.Line, data.CreatedAt);
    }
}
