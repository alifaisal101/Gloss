using Gloss.Domain.MergeRequests;

namespace Gloss.Application.MergeRequests;

public sealed record DraftCommentReadModel(Guid Id, string FilePath, int LineNumber, string Body, string? Reasoning)
{
    public static DraftCommentReadModel From(DraftComment dc)
    {
        ArgumentNullException.ThrowIfNull(dc);
        return new(dc.Id, dc.FilePath, dc.Line, dc.Body, dc.Reasoning);
    }
}
