using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests;

public sealed record MergeRequestDetailReadModel(Guid Id, string State, IReadOnlyList<DraftCommentReadModel> DraftComments)
{
    public static MergeRequestDetailReadModel From(MergeRequest mr, IReadOnlyList<DraftComment> comments)
    {
        ArgumentNullException.ThrowIfNull(mr);
        ArgumentNullException.ThrowIfNull(comments);
        return new(mr.Id, mr.State.ToString(), comments.Select(DraftCommentReadModel.From).ToList());
    }
}
