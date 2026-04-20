namespace Gloss.Domain.MergeRequests.Events;

public sealed record CommentAdded(Guid MergeRequestId, Guid CommentId, string Body);
