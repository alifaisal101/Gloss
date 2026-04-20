namespace Gloss.Domain.MergeRequests.Events;

public sealed record CommentDeleted(Guid MergeRequestId, Guid CommentId, string Body);
