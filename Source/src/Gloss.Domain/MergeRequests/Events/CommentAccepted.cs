namespace Gloss.Domain.MergeRequests.Events;

public sealed record CommentAccepted(Guid MergeRequestId, Guid CommentId, string Body);
