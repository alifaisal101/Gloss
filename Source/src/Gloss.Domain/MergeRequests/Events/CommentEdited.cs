namespace Gloss.Domain.MergeRequests.Events;

public sealed record CommentEdited(Guid MergeRequestId, Guid CommentId, string BodyBefore, string BodyAfter);
